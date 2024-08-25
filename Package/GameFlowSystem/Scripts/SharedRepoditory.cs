using System.Collections.Generic;
using KahaGameCore.Actor;
using KahaGameCore.GameData.Implemented;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem
{
    public static class SharedRepoditory
    {
        private static List<MonoBehaviour> sharedComponents = new List<MonoBehaviour>();
        public static GameStaticDataManager gameStaticDataManager = null;
        public static GeneralActor playerInstance;

        public static void AddSharedComponent(MonoBehaviour component)
        {
            if (sharedComponents.Contains(component))
            {
                Debug.LogError("SharedRepoditory already contains the component.");
                return;
            }

            sharedComponents.Add(component);
        }

        public static void RemoveSharedComponent(MonoBehaviour component)
        {
            if (!sharedComponents.Contains(component))
            {
                Debug.LogError("SharedRepoditory does not contain the component.");
                return;
            }

            sharedComponents.Remove(component);
        }

        public static T Find<T>() where T : MonoBehaviour
        {
            return sharedComponents.Find(x => x is T) as T;
        }
    }
}