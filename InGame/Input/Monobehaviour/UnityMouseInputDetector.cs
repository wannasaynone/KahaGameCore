using UnityEngine;

namespace KahaGameCore.Input.Mono
{
    public class UnityMouseInputDetector : MonoBehaviour
    {
        public class Setting
        {
            public float doubleClick_secondDownWaitTime = 0.25f;
        }

        public static void Initial(Setting setting = null)
        {
            m_settingInstance = setting;
            if (m_settingInstance == null)
            {
                m_settingInstance = new Setting();
            }

            GameObject go = new GameObject("[UnityMouseInputDetector]");
            go.AddComponent<UnityMouseInputDetector>();
        }

        private static UnityMouseInputDetector m_instance;
        private static Setting m_settingInstance;

        private void Awake()
        {
            if (m_instance != null)
            {
                Destroy(this);
            }
            else
            {
                if (m_settingInstance == null)
                {
                    Debug.Log("[UnityMouseInputDetector][Awake] use UnityMouseInputDetector.Initial(Setting) to initial this");
                    Destroy(this);
                }
                else
                {
                    m_instance = this;
                }
            }
        }

        private float m_doubleClick_waitSecondDownTimer;

        private void Update()
        {
            if (m_settingInstance == null)
                return;

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
            }

            if (UnityEngine.Input.GetMouseButton(0))
            {
            }

            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                if (m_doubleClick_waitSecondDownTimer <= 0f)
                {
                    m_doubleClick_waitSecondDownTimer = m_settingInstance.doubleClick_secondDownWaitTime;
                }
                else
                {
                    m_doubleClick_waitSecondDownTimer = 0f;
                    InputEventHanlder.SendOnDoubleTapped();
                }
            }

            if (m_doubleClick_waitSecondDownTimer > 0f)
            {
                m_doubleClick_waitSecondDownTimer -= Time.deltaTime;
                if (m_doubleClick_waitSecondDownTimer <= 0f)
                {
                    InputEventHanlder.SendOnSingleTapped();
                }
            }
        }
    }
}
