using System.IO;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Editor
{
    public class AssetBundleBuilder
    {
        public static string GetBuildPath()
        {
            BuildTarget _buildTarget = EditorUserBuildSettings.activeBuildTarget;
            return Application.dataPath + "/../AssetBundle/" + _buildTarget + "/";
        }

        [MenuItem("Tools/Build AssetBundle")]
        private static void BuildAssetBundle()
        {
            string _path = GetBuildPath();

            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }
            else
            {
                Directory.Delete(_path, true);
                Directory.CreateDirectory(_path);
            }

            Debug.Log("Build Assetbundle to " + _path);

            AssetBundleManifest _result = BuildPipeline.BuildAssetBundles(_path, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

            for(int i = 0; i < _result.GetAllAssetBundles().Length; i++)
            {
                Debug.Log("Built AssetBundle: " + _result.GetAllAssetBundles()[i]);
            }

            System.Diagnostics.Process.Start(_path);

            Debug.Log("Build AssetBundle complete");
        }
    }
}
