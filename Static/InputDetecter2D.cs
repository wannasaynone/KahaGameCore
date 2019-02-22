using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

        public List<int> TouchIds { get { return new List<int>(m_touchFingerIDs); } }

        private Camera m_camera = null;
        private State m_state = State.None;
        private Touch[] m_touchs = null;
        private List<int> m_touchFingerIDs = new List<int>();

        public InputInfo DetectInput()
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

        private InputInfo DetectComputerInput()
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

        private bool m_tempRecordIsOnUGUIState = false;
        private InputInfo DetectMobileInput()
        {
            m_touchs = UnityEngine.Input.touches;
            if (m_touchs.Length > 0)
            {
                for (int i = 0; i < m_touchs.Length; i++)
                {
                    switch(m_touchs[i].phase)
                    {
                        case TouchPhase.Began:
                            {
                                if (m_touchFingerIDs.Count <= 0)
                                {
                                    m_state = State.Down;
                                    m_touchFingerIDs.Add(m_touchs[i].fingerId);
                                }
                                else if (!m_touchFingerIDs.Contains(m_touchs[i].fingerId))
                                {
                                    m_touchFingerIDs.Add(m_touchs[i].fingerId);
                                }
                                break;
                            }
                        case TouchPhase.Ended:
                            {
                                Debug.Log("TouchPhase.Ended, m_touchFingerIDs.Count=" + m_touchFingerIDs.Count);
                                if (m_touchFingerIDs[0] == m_touchs[i].fingerId)
                                {
                                    m_state = State.Up;
                                }
                                break;
                            }
                        default:
                            {
                                if (m_touchFingerIDs[0] == m_touchs[i].fingerId)
                                {
                                    m_state = State.Pressing;
                                }
                                break;
                            }
                    }
                }
            }
            else
            {
                m_state = State.None;
            }

            InputInfo _info = CheckRaycast();

            if(m_state != State.Up)
            {
                m_tempRecordIsOnUGUIState = _info.isOnUGUI;
            }
            else
            {
                _info.isOnUGUI = m_tempRecordIsOnUGUIState;
                m_touchFingerIDs.RemoveAt(0); // remove after check Raycast
                Debug.Log(m_touchFingerIDs.Count);
                Debug.Log(_info.InputState);
            }

            return _info;
        }

        private InputInfo CheckRaycast()
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
            for (int i = 0; i < m_touchs.Length; i++)
            {
                if (m_touchs[i].fingerId == m_touchFingerIDs[0])
                {
                    Vector2 _touchPos = m_camera.ScreenToWorldPoint(new Vector2(m_touchs[i].position.x, m_touchs[i].position.y));
                    _rayHit = Physics2D.Raycast(_touchPos, Vector2.zero);

                    if (EventSystem.current == null)
                    {
                        Debug.LogWarning("Unity EventSystem is not exist");
                    }
                    else if (EventSystem.current.IsPointerOverGameObject(m_touchFingerIDs[0]))
                    {
                        _info.isOnUGUI = true;
                    }
                    else
                    {
                        _info.isOnUGUI = false;
                    }

                    _info.InputPosition = _touchPos;
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
