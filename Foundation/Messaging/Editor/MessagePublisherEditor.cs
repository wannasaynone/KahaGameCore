using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Foundation.Messaging.Editor
{
    public sealed class MessagePublisherEditor : EditorWindow
    {
        private readonly Dictionary<Type, Dictionary<string, object>> messageParameters =
            new Dictionary<Type, Dictionary<string, object>>();
        private Vector2 scrollPosition;

        [MenuItem("Tools/Message Publisher")]
        public static void ShowWindow()
        {
            GetWindow<MessagePublisherEditor>("Message Publisher");
        }

        private void OnGUI()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.Label("Publish Messages", EditorStyles.boldLabel);

            IEnumerable<Type> messageTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    type.IsSubclassOf(typeof(MessageBase)));

            foreach (Type type in messageTypes)
            {
                DrawMessage(type);
            }

            GUILayout.EndScrollView();
        }

        private void DrawMessage(Type type)
        {
            GUILayout.Label(type.Name, EditorStyles.boldLabel);
            if (!messageParameters.ContainsKey(type))
            {
                messageParameters[type] = new Dictionary<string, object>();
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                DrawField(type, field);
            }

            if (!GUILayout.Button("Publish " + type.Name))
            {
                return;
            }

            object instance = Activator.CreateInstance(type);
            foreach (KeyValuePair<string, object> parameter in messageParameters[type])
            {
                type.GetField(parameter.Key)?.SetValue(instance, parameter.Value);
            }

            typeof(MessageBus)
                .GetMethod(nameof(MessageBus.Publish))
                ?.MakeGenericMethod(type)
                .Invoke(null, new[] { instance });
        }

        private void DrawField(Type type, FieldInfo field)
        {
            Dictionary<string, object> values = messageParameters[type];
            if (field.FieldType == typeof(int))
            {
                values[field.Name] = EditorGUILayout.IntField(
                    field.Name,
                    values.ContainsKey(field.Name) ? (int)values[field.Name] : 0);
            }
            else if (field.FieldType == typeof(float))
            {
                values[field.Name] = EditorGUILayout.FloatField(
                    field.Name,
                    values.ContainsKey(field.Name) ? (float)values[field.Name] : 0f);
            }
            else if (field.FieldType == typeof(Vector3))
            {
                values[field.Name] = EditorGUILayout.Vector3Field(
                    field.Name,
                    values.ContainsKey(field.Name) ? (Vector3)values[field.Name] : Vector3.zero);
            }
            else if (field.FieldType == typeof(string))
            {
                values[field.Name] = EditorGUILayout.TextField(
                    field.Name,
                    values.ContainsKey(field.Name) ? (string)values[field.Name] : string.Empty);
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return Array.Empty<Type>();
            }
        }
    }
}
