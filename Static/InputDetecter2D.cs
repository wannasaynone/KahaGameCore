using UnityEngine;
using UnityEngine.EventSystems;

namespace KahaGameCore.Static
{
    public static class InputDetecter2D
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
        public static int StartFingerID { get { return m_startFingerID; } }
        private static int m_startFingerID = -1;

        public static InputInfo DetectInput()
        {
            if (m_camera == null)
            {
                m_camera = Camera.main;
            }

#if UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL
            return DetectComputerInput();
#elif UNITY_ANDROID
            return DetectMobileInput();
#else
            return DetectComputerInput();
#endif
        }

        public static bool ClickOn(Collider2D collider, bool checkUGUI = true)
        {
            InputInfo _info = DetectInput();
            if (_info.InputState == State.Down
                && _info.RayCastCollider != null
                && _info.RayCastCollider == collider
                && (checkUGUI && !_info.isOnUGUI))
            {
                return true;
            }

            return false;
        }

        public static bool ClickUp(Collider2D collider, bool checkUGUI = true)
        {
            InputInfo _info = DetectInput();
            if (_info.InputState == State.Up
                && _info.RayCastCollider != null
                && _info.RayCastCollider == collider
                && (checkUGUI && !_info.isOnUGUI))
            {
                return true;
            }

            return false;
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

            InputInfo _info = CheckRaycast();

            return _info;
        }

        private static bool m_tempRecordIsOnUGUIState = false;
        private static InputInfo DetectMobileInput()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                if (StartFingerID == -1)
                {
                    if (UnityEngine.Input.GetTouch(0).phase == TouchPhase.Began)
                    {
                        m_state = State.Down;
                        m_startFingerID = UnityEngine.Input.GetTouch(0).fingerId;
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
                m_startFingerID = -1;
            }

            InputInfo _info = CheckRaycast();
            if(m_state != State.Up)
            {
                m_tempRecordIsOnUGUIState = _info.isOnUGUI;
            }
            else
            {
                _info.isOnUGUI = m_tempRecordIsOnUGUIState;
            }

            return _info;
        }

        private static InputInfo CheckRaycast()
        {
            RaycastHit2D _rayHit = new RaycastHit2D();
            InputInfo _info = new InputInfo();
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            Vector2 mousePos = m_camera.ScreenToWorldPoint(new Vector2(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y));
            _rayHit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (EventSystem.current == null)
            {
                Debug.LogWarning("Unity EventSystem is not exist");
            }
            else if (EventSystem.current.IsPointerOverGameObject())
            {
                _info.isOnUGUI = true;
            }
            else
            {
                _info.isOnUGUI = false;
            }

            _info.InputPosition = mousePos;
#elif UNITY_ANDROID
            for (int i = 0; i < UnityEngine.Input.touches.Length; i++)
            {
                if (UnityEngine.Input.touches[i].fingerId == StartFingerID)
                {
                    Vector2 touchPos = m_camera.ScreenToWorldPoint(new Vector2(UnityEngine.Input.touches[i].position.x, UnityEngine.Input.touches[i].position.y));
                    _rayHit = Physics2D.Raycast(touchPos, Vector2.zero);

                    if (EventSystem.current == null)
                    {
                        Debug.LogWarning("Unity EventSystem is not exist");
                    }
                    else if (EventSystem.current.IsPointerOverGameObject(StartFingerID))
                    {
                        _info.isOnUGUI = true;
                    }
                    else
                    {
                        _info.isOnUGUI = false;
                    }

                    _info.InputPosition = touchPos;
                }
            }
#endif
            _info.InputState = m_state;
            _info.RayCastTranform = _rayHit.transform;
            _info.RayCastCollider = _rayHit.collider;
            return _info;
        }
    }
}
