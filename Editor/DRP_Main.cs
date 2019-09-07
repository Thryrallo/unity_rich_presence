using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Thry
{

    public class DRP_UpdatedFileGetter : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            string file_path = "";
            if (importedAssets.Length > 0)
                file_path = importedAssets[0];
            else if (movedAssets.Length > 0)
                file_path = movedAssets[0];
            else
                return;
            if (!File.Exists(DRP_Main.DRP_LAST_UPDATED_FILE_PATH)) File.Create(DRP_Main.DRP_LAST_UPDATED_FILE_PATH).Close();
            StreamWriter writer = new StreamWriter(DRP_Main.DRP_LAST_UPDATED_FILE_PATH, false);
            string file_name = Regex.Match(file_path, @"(?<=\/|^)[^\/]+$").Value;
            writer.Write(file_name);
            writer.Close();
        }
    }

    [InitializeOnLoad]
    public class DRP_Main
    {
        public const string DRP_LAST_UPDATED_FILE_PATH = ".thry_drp_last_updated_file";

        public static Discord.Discord discord;
        static string clientID;

        static string last_updated_file_name = "";

        static bool disposing = false;

        static float lastUpdate = 0;
        const float UPDATE_RATE = 3;

        static float last_callback = 0;
        const float CALLBACK_RATE = 1.0f;

        static DRP_Main()
        {
            last_updated_file_name = Helper.ReadFileIntoString(DRP_LAST_UPDATED_FILE_PATH);
            clientID = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
            if (clientID == null)
            {
                clientID = "619838876994764800";
            }
            discord = new Discord.Discord(Int64.Parse(clientID), (UInt64)Discord.CreateFlags.Default);
            discord.SetLogHook(Discord.LogLevel.Debug, (level, message) =>
            {
                Debug.Log("Log["+ level+"] "+message);
            });

            var applicationManager = discord.GetApplicationManager();
            applicationManager.GetOAuth2Token((Discord.Result result, ref Discord.OAuth2Token oauth2Token) =>
            {
                Debug.Log("Access Token "+ oauth2Token.AccessToken);
            });

            var userManager = discord.GetUserManager();

            userManager.OnCurrentUserUpdate += () =>
            {
                Debug.Log("----------------------------Discord Rich Presence---------------------------");
                var currentUser = userManager.GetCurrentUser();
                Debug.Log("Username: \"" + currentUser.Username+ "\", Id: \"" + currentUser.Id+"\"");
                Debug.Log("-------------------------------------------------------------------------------");
            };

            UpdateActivity();

            EditorApplication.update += Update;
        }

        static void Update()
        {
            if (EditorApplication.isCompiling)
            {
                if (discord != null && !disposing)
                {
                    disposing = true;
                    discord.Dispose();
                }
            }
            else
            {
                if(Time.realtimeSinceStartup - lastUpdate > UPDATE_RATE)
                {
                    UpdateActivity();
                    lastUpdate = Time.realtimeSinceStartup;
                }
                if (Time.realtimeSinceStartup - last_callback > CALLBACK_RATE)
                {
                    last_callback = Time.realtimeSinceStartup;
                    discord.RunCallbacks();
                }
            }
        }

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetCurrentUnixTimestampMillis()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
        }

        public static long GetUnityStartUpTimeStamp()
        {
            return GetCurrentUnixTimestampMillis() - (long)EditorApplication.timeSinceStartup*1000;
        }

        private static void UpdateActivity()
        {
            var activityManager = discord.GetActivityManager();
            
            string scene = "No Scene";
            if (Selection.gameObjects.Length>0 && Selection.gameObjects[0].scene.IsValid())
                scene = "Scene: " + Selection.gameObjects[0].scene.name;
            else if (SceneManager.GetActiveScene().IsValid() && SceneManager.GetActiveScene().path != "")
                scene = "Scene: " + SceneManager.GetActiveScene().name;
            string file = "";
            if (last_updated_file_name.Length > 0)
                file = "Last Edited: " + last_updated_file_name;

            var activity = new Discord.Activity
            {
                State = file,
                Details = scene,
                Timestamps =
            {
                Start = GetUnityStartUpTimeStamp(),
            },
                Assets =
            {
                LargeImage = "unity",
                LargeText = Application.productName,
                SmallImage = "foo smallImageKey",
                SmallText = "foo smallImageText",
            },
                Party = {
            },
                Secrets = {
            },
                Instance = true,
            };
            
            activityManager.UpdateActivity(activity, result =>
            {
            });
        }
    }
}