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
        public const string DRP_LAST_UPDATED_FILE_PATH = "Thry/thry_drp_last_updated_file";

        public static Discord.Discord discord;
        static string clientID;

        static string last_updated_file_name = "";

        static bool disposing = false;

        static float lastUpdate = 0;

        static float last_callback = 0;

        static bool isRunning = false;

        static DRP_Main()
        {
            last_updated_file_name = FileHelper.ReadFileIntoString(DRP_LAST_UPDATED_FILE_PATH);
            clientID = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
            if (clientID == null)
            {
                clientID = "619838876994764800";
            }

            if(InitDiscord())
            {
                EditorApplication.update += Update;
            }
            else
            {
                EditorApplication.update += TryInitDiscord;
            }

            ModuleHandler.RegisterPreModuleRemoveFunction(delegate ()
            {   
                if(isRunning) EditorApplication.update -= Update;
                else EditorApplication.update -= TryInitDiscord;
                discord.Dispose();
            });
        }

        static float lastTry = 0;
        static void TryInitDiscord()
        {
            if(Time.realtimeSinceStartup - lastTry < 5)
            {
                return;
            }
            lastTry = Time.realtimeSinceStartup;
            if (InitDiscord())
            {
                EditorApplication.update += Update;
                EditorApplication.update -= TryInitDiscord;
            }
        }

        static bool InitDiscord()
        {
            try
            {
                discord = new Discord.Discord(Int64.Parse(clientID), (UInt64)Discord.CreateFlags.NoRequireDiscord);
            }catch(Exception e)
            {
                if(e.GetType() == typeof(Discord.ResultException))
                {
                    return false;
                }
                return false;
            }
            discord.SetLogHook(Discord.LogLevel.Debug, (level, message) =>
            {
                Debug.Log("Log[" + level + "] " + message);
            });

            var applicationManager = discord.GetApplicationManager();
            applicationManager.GetOAuth2Token((Discord.Result result, ref Discord.OAuth2Token oauth2Token) =>
            {
                Debug.Log("Access Token " + oauth2Token.AccessToken);
            });

            var userManager = discord.GetUserManager();

            userManager.OnCurrentUserUpdate += () =>
            {
                Debug.Log("----------------------------Discord Rich Presence---------------------------");
                var currentUser = userManager.GetCurrentUser();
                Debug.Log("Username: \"" + currentUser.Username + "\", Id: \"" + currentUser.Id + "\"");
                Debug.Log("-------------------------------------------------------------------------------");
            };

            UpdateActivity();
            isRunning = true;

            return true;
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
                if (isRunning == false) return;
                if(Time.realtimeSinceStartup - lastUpdate > DRP_Settings.GetData().update_rate)
                {
                    try
                    {
                        UpdateActivity();
                    }catch(Exception e)
                    {
                        //throws if discords is not running, too lazy to something else
                        isRunning = false;
                        EditorApplication.update -= Update;
                        EditorApplication.update += TryInitDiscord;
                    }
                    lastUpdate = Time.realtimeSinceStartup;
                }
                if (Time.realtimeSinceStartup - last_callback > DRP_Settings.GetData().update_rate / 3)
                {
                    last_callback = Time.realtimeSinceStartup;
                    try
                    {
                        discord.RunCallbacks();
                    }
                    catch (Exception e)
                    {
                        //throws if discords is not running, too lazy to something else
                        isRunning = false;
                        EditorApplication.update -= Update;
                        EditorApplication.update += TryInitDiscord;
                    }
                }
            }
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
                Start = Helper.GetUnityStartUpTimeStamp(),
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