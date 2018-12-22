using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KahaGameCore
{
    public class InputDetecter2D
    {
        public struct InputInfo
        {
            public Vector3 InputPosition;
            public Transform RayCastTranform;
            public Collider2D RayCastCollider;
            public State InputState;
            public bool isOnUGUI;
        }

        public enum State
        {
            None,
            Down,
            Pressing,
            Up
        }

        private static Camera m_camera = null;
        private static State m_state = State.None;
        public static int StartFingerID { get; private set; }

        public InputDetecter2D()
        {
            StartFingerID = -1;
        }

        public static InputInfo DetectInput()
        {
            if (m_camera == null)
            {
                m_camera = Camera.main;
            }

#if UNITY_STANDALON || UNITY_EDITOR || UNITY_WEBGL
           return DetectComputerInput();
#elif UNITY_ANDROID
           return DetectMobileInput();
#else
           return DetectComputerInput();
#endif
        }

        private static InputInfo DetectComputerInput()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                m_state = State.Down;
            }
            else if (UnityEngine.Input.GetMouseButton(0))
            {
                m_state = State.Pressing;
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                m_state = State.Up;
            }
            else
            {
                m_state = State.None;
            }

            InputInfo info = CheckRaycast();

            return info;
        }

        private static InputInfo DetectMobileInput()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                if (StartFingerID == -1)
                {
                    if (UnityEngine.Input.GetTouch(0).phase == TouchPhase.Began)
                    {
                        m_state = State.Down;
                        StartFingerID = UnityEngine.Input.GetTouch(0).fingerId;
                    }
                }
                else
                {
                    for (int i = 0; i < UnityEngine.Input.touches.Length; i++)
                    {
                        if (UnityEngine.Input.touches[i].fingerId == StartFingerID)
                        {
                            if (UnityEngine.Input.touches[i].phase == TouchPhase.Stationary || UnityEngine.Input.touches[i].phase == TouchPhase.Moved)
                            {
                                m_state = State.Pressing;
                            }
                            else if (UnityEngine.Input.touches[i].phase == TouchPhase.Ended)
                            {
                                m_state = State.Up;
                            }
                        }
                    }

                }
            }
            else
            {
                m_state = State.None;
                StartFingerID = -1;
            }

            InputInfo info = CheckRaycast();

            return info;
        }

        private static InputInfo CheckRaycast()
        {
            RaycastHit2D rayHit = new RaycastHit2D();
            InputInfo info = new InputInfo();
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            Vector2 mousePos = m_camera.ScreenToWorldPoint(new Vector2(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y));
            rayHit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (EventSystem.current == null)
            {
                Debug.LogWarning("Unity EventSystem is not exist");
            }
            else if (EventSystem.current.IsPointerOverGameObject())
            {
                info.isOnUGUI = true;
            }
            else
            {
                info.isOnUGUI = false;
            }
            info.InputPosition = mousePos;
#elif UNITY_ANDROID
        for (int i = 0; i < UnityEngine.Input.touches.Length; i++)
        {
            if (UnityEngine.Input.touches[i].fingerId == StartFingerID)
            {
                Vector2 touchPos = m_camera.ScreenToWorldPoint(new Vector2(UnityEngine.Input.touches[i].position.x, UnityEngine.Input.touches[i].position.y));
                rayHit = Physics2D.Raycast(touchPos, Vector2.zero);
        
                 if(EventSystem.current.IsPointerOverGameObject(StartFingerID))
                 {
                    info.isOnUGUI = true;
                 }
                  else
                 {
                     info.isOnUGUI = false;
                 }
                info.InputPosition = touchPos;
            }
        }
#endif
            info.InputState = m_state;
            info.RayCastTranform = rayHit.transform;
            info.RayCastCollider = rayHit.collider;
            return info;
        }
    }
}
