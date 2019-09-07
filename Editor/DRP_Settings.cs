using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class DRP_Settings : ModuleSettings
    {
        private static bool ValuesInit;

        public DRP_Settings()
        {
            if (!ValuesInit)
                InitValues();
        }

        public override void Draw()
        {
            GUILayout.Label("Discord Rich Presence",EditorStyles.boldLabel);
        }

        public override void InitValues()
        {
            ValuesInit = true;
        }
    }
}
