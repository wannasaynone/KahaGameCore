using System;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.Presentation.Editor
{
    internal static class ParameterStateBinderMenu
    {
        private const string MenuPath =
            "GameObject/Kaha Game Core/Add Parameter State Binder";
        private const int MenuPriority = 22;

        [MenuItem(MenuPath, false, MenuPriority)]
        private static void CreateParameterStateBinder(MenuCommand command)
        {
            CreateParameterStateBinder(GetParent(command));
        }

        [MenuItem(MenuPath, true)]
        private static bool CanCreateParameterStateBinder(MenuCommand command)
        {
            return GetParent(command) != null;
        }

        internal static ParameterStateBinder CreateParameterStateBinder(GameObject parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            GameObject binderObject = new GameObject("Parameter State Binder")
            {
                layer = parent.layer
            };
            Undo.RegisterCreatedObjectUndo(
                binderObject,
                "Create Parameter State Binder");
            GameObjectUtility.SetParentAndAlign(binderObject, parent);

            ParameterStateBinder binder =
                Undo.AddComponent<ParameterStateBinder>(binderObject);
            Selection.activeGameObject = binderObject;
            return binder;
        }

        private static GameObject GetParent(MenuCommand command)
        {
            return command?.context as GameObject ?? Selection.activeGameObject;
        }
    }
}
