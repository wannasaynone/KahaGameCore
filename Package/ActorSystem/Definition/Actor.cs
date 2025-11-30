using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.ActorSystem.Definition
{
    public class Actor
    {
        public Instance Instance { get; private set; }
        private List<ControllerBase> controllers = new List<ControllerBase>();

        private Transform root;

        public void UpdateInstance(Instance instance)
        {
            if (Instance == instance)
            {
                return;
            }

            if (Instance != null)
            {
                Instance.transform.SetParent(null);
            }

            Instance = instance;
            foreach (var controller in controllers)
            {
                controller.SetControlTarget(instance);
            }

            SetUpRoot();
        }

        public void AddController(ControllerBase controller)
        {
            if (!controllers.Contains(controller))
            {
                controllers.Add(controller);
                controller.SetControlTarget(Instance);
                SetUpRoot();
                Debug.Log($"[Actor] Controller {controller.GetType().Name} added to Actor with Instance {Instance.gameObject.name}");
            }
        }

        public void RemoveController(ControllerBase controller)
        {
            if (controllers.Contains(controller))
            {
                controllers.Remove(controller);
                controller.RemoveControlTarget();
            }
        }

        private void SetUpRoot()
        {
            string instanceName = Instance != null ? Instance.gameObject.name : "UnknownInstance";

            if (root == null)
            {
                root = new GameObject("Actor[" + instanceName + "]").transform;
            }

            if (Instance != null)
            {
                Instance.transform.SetParent(root);
            }

            foreach (var controller in controllers)
            {
                if (controller != null)
                {
                    controller.transform.SetParent(root);
                }
            }
        }
    }
}