using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.PlayerControlable
{
    public class InputDetectorStateMachine : MonoBehaviour
    {
        public static InputDetectorStateMachine Instance { get; private set; }

        [Serializable]
        private class StateInfo
        {
            public string stateName;
            public InputDetector inputDetector;
        }

        [SerializeField] private List<StateInfo> stateInfos = new List<StateInfo>();

        private StateInfo currentStateInfo;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                currentStateInfo = stateInfos[0];
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetState(string stateName)
        {
            skipUpdate = true;

            if (currentStateInfo != null)
            {
                currentStateInfo.inputDetector.Reset();
            }

            currentStateInfo = stateInfos.Find(x => x.stateName == stateName);
        }

        private bool skipUpdate = false;

        private void Update()
        {
            if (skipUpdate)
            {
                skipUpdate = false;
                return;
            }

            if (currentStateInfo == null)
            {
                return;
            }

            currentStateInfo.inputDetector.Tick();
        }
    }
}