﻿using UnityEngine;
using UnityEditor;

namespace ResourceManagement.Samples
{
    //simple helper GUI to configure ResourceManager build data
    public class ResourceManagerSettings : EditorWindow
    {
        [MenuItem("Window/ResourceManager Build Settings", priority = 2060)]
        static void ShowWindow()
        {
            var window = GetWindow<ResourceManagerSettings>();
            window.titleContent = new GUIContent("RM Settings");
            window.Show();
        }

        private void OnGUI()
        {
            var tpath = EditorPrefs.GetString("RMTargetFolder", "Assets/Prefabs");
            var tnewPath = EditorGUILayout.DelayedTextField("Target Asset Folder", tpath);
            if (tpath != tnewPath)
                EditorPrefs.SetString("RMTargetFolder", tnewPath);

            var ext = EditorPrefs.GetString("RMTargetExtension", "*.prefab");
            var newExt = EditorGUILayout.DelayedTextField("Target Asset Extension", ext);
            if (ext != newExt)
                EditorPrefs.SetString("RMTargetExtension", newExt);


            int mode = EditorPrefs.GetInt("RMProviderMode", 0);
            ResourceManagerRuntimeData.ProviderMode val = (ResourceManagerRuntimeData.ProviderMode)EditorGUILayout.EnumPopup("Provider Mode", (ResourceManagerRuntimeData.ProviderMode)mode);
            if (mode != (int)val)
                EditorPrefs.SetInt("RMProviderMode", (int)val);

            bool connectProfiler = EditorPrefs.GetBool("RMProfileEvents", false);
            bool setConnectProfiler = EditorGUILayout.ToggleLeft("Profile Events", connectProfiler);
            if (setConnectProfiler != connectProfiler)
                EditorPrefs.SetBool("RMProfileEvents", setConnectProfiler);

            var path = EditorPrefs.GetString("RMBundlePath", "Assets/StreamingAssets");
            var newPath = EditorGUILayout.DelayedTextField("AssetBundle Path", path);
            if (path != newPath)
                EditorPrefs.SetString("RMBundlePath", newPath);

            var lpath = EditorPrefs.GetString("RMBundleLoadPrefix", "{Application.streamingAssetsPath}/");
            var lnewPath = EditorGUILayout.DelayedTextField("AssetBundle Load Prefix", lpath);
            if (lpath != lnewPath)
                EditorPrefs.SetString("RMBundleLoadPrefix", lnewPath);
        }

    }
}