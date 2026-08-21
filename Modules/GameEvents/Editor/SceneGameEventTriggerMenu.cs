using System;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    internal static class SceneGameEventTriggerMenu
    {
        internal const string SceneTriggerMenuPath =
            "GameObject/Add Game Event Parts/Add Scene Trigger";
        internal const string SceneTrigger2DMenuPath =
            "GameObject/Add Game Event Parts/Add Scene Trigger 2D";
        private const int MenuPriority = 20;

        [MenuItem(SceneTriggerMenuPath, false, MenuPriority)]
        private static void AddSceneTrigger(MenuCommand command)
        {
            AddSceneTrigger(GetParent(command));
        }

        [MenuItem(SceneTriggerMenuPath, true)]
        private static bool CanAddSceneTrigger(MenuCommand command)
        {
            return GetParent(command) != null;
        }

        [MenuItem(SceneTrigger2DMenuPath, false, MenuPriority + 1)]
        private static void AddSceneTrigger2D(MenuCommand command)
        {
            AddSceneTrigger2D(GetParent(command));
        }

        [MenuItem(SceneTrigger2DMenuPath, true)]
        private static bool CanAddSceneTrigger2D(MenuCommand command)
        {
            return GetParent(command) != null;
        }

        internal static SceneGameEventTrigger AddSceneTrigger(GameObject parent)
        {
            GameObject triggerObject = CreateTriggerObject(parent, "Scene Trigger");

            BoxCollider collider = Undo.AddComponent<BoxCollider>(triggerObject);
            collider.isTrigger = true;

            Rigidbody body = Undo.AddComponent<Rigidbody>(triggerObject);
            body.isKinematic = true;
            body.useGravity = false;

            SceneGameEventTrigger trigger =
                Undo.AddComponent<SceneGameEventTrigger>(triggerObject);
            Selection.activeGameObject = triggerObject;
            return trigger;
        }

        internal static SceneGameEventTrigger2D AddSceneTrigger2D(GameObject parent)
        {
            GameObject triggerObject = CreateTriggerObject(parent, "Scene Trigger 2D");

            BoxCollider2D collider = Undo.AddComponent<BoxCollider2D>(triggerObject);
            collider.isTrigger = true;

            Rigidbody2D body = Undo.AddComponent<Rigidbody2D>(triggerObject);
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            SceneGameEventTrigger2D trigger =
                Undo.AddComponent<SceneGameEventTrigger2D>(triggerObject);
            Selection.activeGameObject = triggerObject;
            return trigger;
        }

        private static GameObject GetParent(MenuCommand command)
        {
            return command?.context as GameObject ?? Selection.activeGameObject;
        }

        private static GameObject CreateTriggerObject(GameObject parent, string objectName)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            GameObject triggerObject = new GameObject(objectName)
            {
                layer = parent.layer
            };
            Undo.RegisterCreatedObjectUndo(triggerObject, $"Create {objectName}");
            GameObjectUtility.SetParentAndAlign(triggerObject, parent);
            return triggerObject;
        }
    }
}
