using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace KahaGameCore.EditorTool
{
    public class UnusedResourceFinder : EditorWindow
    {
        private static bool m_isFinding = false;

        private static List<string> m_allScenePath = null;
        private static List<string> m_allPrefab = null;

        private static List<Sprite> m_allUsingSpriteAssets = null;
        private static List<Texture> m_allUsingTextureAssets = null;

        private static List<string> m_allUnusedResourcesPath = null;
        private static string m_defaultIconPath = "";

        private Vector2 m_scrollPosition_prefab = Vector2.zero;
        private Vector2 m_scrollPosition_unusedAssets = Vector2.zero;

        private bool m_showPrefab = false;

        [MenuItem("Window/Unused Resource Finder")]
        private static void StartFindUnusedResource()
        {
            if (m_isFinding)
            {
                EditorUtility.DisplayDialog("Error", "Unused Resource Finder is working.", "ok");
                return;
            }

            StartCompare();
        }

        private static void StartCompare()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    EditorUtility.DisplayDialog("Warning", "Save all scenes before using this tool.", "ok");
                    return;
                }
            }

            m_allScenePath = new List<string>();
            m_allPrefab = new List<string>();
            m_allUnusedResourcesPath = new List<string>();

            string[] _allAssets = AssetDatabase.GetAllAssetPaths();

            for (int i = 0; i < _allAssets.Length; i++)
            {
                if (_allAssets[i].Contains(".unity")
                    && !_allAssets[i].Contains(".unity.")
                    && !_allAssets[i].Contains("unitypackage"))
                {
                    m_allScenePath.Add(_allAssets[i]);
                }
                else if (_allAssets[i].Contains(".prefab"))
                {
                    m_allPrefab.Add(_allAssets[i]);
                }
                else if (!m_allUnusedResourcesPath.Contains(_allAssets[i]))
                {
                    if (_allAssets[i].Contains(".unity."))
                    {
                        continue;
                    }
                    else
                    {
                        if (AssetDatabase.LoadAssetAtPath<Texture>(_allAssets[i]) == null
                            && AssetDatabase.LoadAssetAtPath<Material>(_allAssets[i]) == null)
                        {
                            continue;
                        }
                    }

                    m_allUnusedResourcesPath.Add(_allAssets[i]);
                }
            }

            for (int _assetIndex = 0; _assetIndex < _allAssets.Length; _assetIndex++)
            {
                string[] _dependencies = AssetDatabase.GetDependencies(_allAssets[_assetIndex], true);
                for (int _dependencyIndex = 0; _dependencyIndex < _dependencies.Length; _dependencyIndex++)
                {
                    if (_dependencies[_dependencyIndex] == _allAssets[_assetIndex])
                    {
                        continue;
                    }
                    if (m_allUnusedResourcesPath.Contains(_dependencies[_dependencyIndex]))
                    {
                        m_allUnusedResourcesPath.Remove(_dependencies[_dependencyIndex]);
                    }
                }
            }

            for (int i = 0; i < m_allScenePath.Count; i++)
            {
                Scene _next = SceneManager.GetSceneByPath(m_allScenePath[i]);
                if (string.IsNullOrEmpty(_next.name))
                {
                    m_allUnusedResourcesPath.Add(m_allScenePath[i]);
                }
            }

            m_defaultIconPath = m_allUnusedResourcesPath.Find(x => x.Contains("UnitySceneIcon.png"));
            m_allUnusedResourcesPath.Remove(m_defaultIconPath);

            CreateWindow<UnusedResourceFinder>();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show Unused Assets"))
            {
                m_showPrefab = false;
            }
            if (GUILayout.Button("Show Prefab"))
            {
                m_showPrefab = true;
            }
            EditorGUILayout.EndHorizontal();
            DrawUILine(Color.white);
            if (m_showPrefab)
            {
                ShowPrefabs();
            }
            else
            {
                ShowUnsedAssets();
            }
        }

        private void ShowPrefabs()
        {
            DrawItems(ref m_allPrefab, ref m_scrollPosition_prefab);
        }

        private void ShowUnsedAssets()
        {
            DrawItems(ref m_allUnusedResourcesPath, ref m_scrollPosition_unusedAssets);
        }

        private void DrawItems(ref List<string> paths, ref Vector2 scrollPos)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true);

            for (int i = 0; i < paths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUIStyle _middleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter };

                if (!TryDrawOnGUITexture(AssetDatabase.LoadAssetAtPath<Texture>(paths[i])))
                {
                    TryDrawOnGUITexture(AssetDatabase.LoadAssetAtPath<Texture>(m_defaultIconPath));
                }

                string[] _pathParts = paths[i].Split('/');
                EditorGUILayout.LabelField(_pathParts[_pathParts.Length - 1], _middleStyle, GUILayout.Width(250f));

                if (GUILayout.Button("Select", GUILayout.Width(50f)))
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(paths[i]));
                }
                if (GUILayout.Button("Check", GUILayout.Width(50f)))
                {
                    paths.RemoveAt(i);
                }
                if (GUILayout.Button("Delete", GUILayout.Width(50f)))
                {
                    AssetDatabase.DeleteAsset(paths[i]);
                    AssetDatabase.Refresh();
                    paths.RemoveAt(i);
                }

                EditorGUILayout.EndHorizontal();
                DrawUILine(Color.black);
            }

            EditorGUILayout.EndScrollView();
        }

        // https://answers.unity.com/questions/953254/display-a-sprite-in-an-editorwindow.html        
        private bool TryDrawOnGUITexture(Texture aTexture)
        {
            if (aTexture == null)
                return false;

            Rect rect = GUILayoutUtility.GetRect(100, 100);
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(rect, aTexture, ScaleMode.ScaleToFit);
            }

            return true;
        }

        // https://forum.unity.com/threads/horizontal-line-in-editor-window.520812/
        private void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
    }
}
