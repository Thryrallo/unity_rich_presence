using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class DRP_Settings : ModuleSettings
    {
        private static bool ValuesInit;
        private static DRP_Data data;

        public DRP_Settings()
        {
            if (!ValuesInit)
                InitValues();
        }

        public static DRP_Data GetData()
        {
            if (!ValuesInit)
                InitValues();
            return data;
        }

        public override void Draw()
        {
            GUILayout.Label("Discord Rich Presence",EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Update rate: ",GUILayout.ExpandWidth(false));
            data.update_rate = GUILayout.HorizontalSlider(data.update_rate, 0, 10);
            GUILayout.Label(((int)(data.update_rate*10))/10.0+" seconds", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
                FileHelper.SaveValueToFile("drp", data.ToString(), ModuleSettings.MODULES_CONFIG);
        }
        
        private static void InitValues()
        {
            string stringData = FileHelper.LoadValueFromFile("drp", ModuleSettings.MODULES_CONFIG);
            if (stringData != null)
                data = Parser.ParseToObject<DRP_Data>(stringData);
            else
                data = new DRP_Data();
            ValuesInit = true;
        }
    }

    public class DRP_Data
    {
        public float update_rate = 3;
        public override string ToString() { return "{update_rate:" + update_rate + "}"; }
    }
}
