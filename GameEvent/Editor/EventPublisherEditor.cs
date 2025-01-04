using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace KahaGameCore.GameEvent.Editor
{
    public class EventPublisherEditor : EditorWindow
    {
        private Dictionary<Type, Dictionary<string, object>> eventParams = new Dictionary<Type, Dictionary<string, object>>();
        private Vector2 scrollPosition;

        [MenuItem("Tools/Game Event Publisher")]
        public static void ShowWindow()
        {
            GetWindow<EventPublisherEditor>("Game Event Publisher");
        }

        private void OnGUI()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.Label("Publish Game Events", EditorStyles.boldLabel);

            var gameEventTypes = AppDomain.CurrentDomain.GetAssemblies()
                                         .SelectMany(assembly =>
                                         {
                                             try
                                             {
                                                 return assembly.GetTypes();
                                             }
                                             catch (ReflectionTypeLoadException)
                                             {
                                                 return new Type[0];
                                             }
                                         })
                                         .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(GameEventBase)));

            foreach (Type type in gameEventTypes)
            {
                GUILayout.Label(type.Name, EditorStyles.boldLabel);

                if (!eventParams.ContainsKey(type))
                {
                    eventParams[type] = new Dictionary<string, object>();
                }

                // 動態生成 UI 來輸入參數
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.FieldType == typeof(int))
                    {
                        eventParams[type][field.Name] = EditorGUILayout.IntField(field.Name, eventParams[type].ContainsKey(field.Name) ? (int)eventParams[type][field.Name] : 0);
                    }
                    else if (field.FieldType == typeof(float))
                    {
                        eventParams[type][field.Name] = EditorGUILayout.FloatField(field.Name, eventParams[type].ContainsKey(field.Name) ? (float)eventParams[type][field.Name] : 0f);
                    }
                    else if (field.FieldType == typeof(Vector3))
                    {
                        eventParams[type][field.Name] = EditorGUILayout.Vector3Field(field.Name, eventParams[type].ContainsKey(field.Name) ? (Vector3)eventParams[type][field.Name] : Vector3.zero);
                    }
                    else if (field.FieldType == typeof(string))
                    {
                        eventParams[type][field.Name] = EditorGUILayout.TextField(field.Name, eventParams[type].ContainsKey(field.Name) ? (string)eventParams[type][field.Name] : "");
                    }
                    // 根據需要添加更多類型
                }

                if (GUILayout.Button("Publish " + type.Name))
                {
                    // 創建事件實例並設置參數
                    var instance = Activator.CreateInstance(type);
                    foreach (var param in eventParams[type])
                    {
                        type.GetField(param.Key).SetValue(instance, param.Value);
                    }

                    // 使用反射來發佈事件
                    typeof(EventBus)
                        .GetMethod("Publish")
                        .MakeGenericMethod(type)
                        .Invoke(null, new object[] { instance });
                }
            }
            GUILayout.EndScrollView();
        }

        private static void HorizontalLine()
        {
            GUIStyle horizontalLine;
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 1;

            GUILayout.Space(10);

            var c = GUI.color;
            GUI.color = Color.gray;
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUI.color = c;

            GUILayout.Space(10);
        }

    }
}

