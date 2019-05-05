using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Net;
using System.ComponentModel;
using JsonFx.Json;

namespace KahaGameCore.Editor
{
    public static class KahaGameCoreUpdater
    {
        private class PackageData
        {
            public string name = "";
            public string displayName = "";
            public string version = "";
            public string unity = "";
            public string description = "";
            public string category = "";
        }


        private const string m_packageJsonPath = "https://raw.githubusercontent.com/wannasaynone/KahaGameCore/master/package.json";
        private const string m_packageUri = "https://github.com/wannasaynone/KahaGameCore/blob/master/Release/KahaGameCore-{0}.unitypackage?raw=true";

        private static bool m_isChecking = false;
        private static string m_packageFileName = "KahaGameCore-{0}.unitypackage";
        private static string m_versionText = "";
        private static string m_path = "";

        [MenuItem("Tools/Update KahaGameCore")]
        public static void CkeckUpdate()
        {
            if (m_isChecking)
            {
                Debug.LogError("Is Updating Package, wait for completed");
                return;
            }

            m_isChecking = true;

            using (WebClient _client = new WebClient())
            {
                Debug.Log("Checking Version...");
                _client.DownloadStringCompleted += OnGotPackageJsonString;
                _client.DownloadStringAsync(new Uri(m_packageJsonPath));
            }
        }

        private static void OnGotPackageJsonString(object sender, DownloadStringCompletedEventArgs e)
        {
            string _localVersionTextFilePath = Path.Combine(Application.persistentDataPath, "version.txt");
            m_versionText = JsonReader.Deserialize<PackageData>(e.Result).version;

            string _localVersionString = "";
            if(File.Exists(_localVersionTextFilePath))
            {
                _localVersionString = File.ReadAllText(_localVersionTextFilePath);
            }
            else
            {
                Debug.Log("Can't find version text file, will force update for this time");
            }

            if (_localVersionString == m_versionText)
            {
                Debug.LogFormat("Is Newest Version {0}", m_versionText);
                m_isChecking = false;
                return;
            }
            else
            {
                Debug.LogFormat("Start updating package, version={0}(current={1})", m_versionText, _localVersionString);
                if(File.Exists(Path.Combine(Application.persistentDataPath, string.Format(m_packageFileName, _localVersionString))))
                {
                    File.Delete(Path.Combine(Application.persistentDataPath, string.Format(m_packageFileName, _localVersionString)));
                    Debug.Log("Old package deleted:" + Path.Combine(Application.persistentDataPath, string.Format(m_packageFileName, _localVersionString)));
                }
            }

            m_path = Path.Combine(Application.persistentDataPath, string.Format(m_packageFileName, m_versionText));

            if (File.Exists(m_path))
            {
                OnDownloadComplete(null, null);
                return;
            }

            using (WebClient _client = new WebClient())
            {
                Debug.Log("Start Downloading new package...");
                m_isChecking = true;
                _client.DownloadProgressChanged += OnDownloadProgressChanged;
                _client.DownloadFileCompleted += OnDownloadComplete;
                _client.DownloadFileAsync(new Uri(string.Format(m_packageUri, m_versionText)), m_path);
            }
        }

        private static void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Debug.Log("Downloading:" + e.ProgressPercentage + "%");
        }

        private static void OnDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            m_isChecking = false;
            Debug.Log("Download Complete:" + m_path + ", version=" + m_versionText);
            using (StreamWriter _streamWriter = new StreamWriter(Path.Combine(Application.persistentDataPath, "version.txt")))
            {
                _streamWriter.Write(m_versionText);
            }
            AssetDatabase.ImportPackage(m_path, true);
        }
    }
}
