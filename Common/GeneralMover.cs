using UnityEngine;

namespace KahaGameCore.Common
{
    public class GeneralMover : MonoBehaviour
    {
        private const float ADJUST_VALUE = 0.1f;

        public Vector3 DirectionMotion { get { return m_inputDirectionMotion; } }

        [Header("Input Key")]
        [SerializeField] private string m_forwardKey = "w";
        [SerializeField] private string m_backwardKey = "s";
        [SerializeField] private string m_rightKey = "d";
        [SerializeField] private string m_leftKey = "a";
        [Header("Mover Properties")]
        [SerializeField] private float m_maxSpeed = 3f;
        [SerializeField] private float m_speedUpTime = 0.5f;
        [SerializeField] private float m_jumpUpSpeed = 3f;
        [SerializeField] private float m_fallingAddWeight = 5f;
        [SerializeField] private float m_maxJumpSpeedTime = 0.2f;
        [SerializeField] private bool m_blockMoveInAir = false;
        [Header("Physics Properties")]
        [SerializeField] private string[] m_layerMaskName = new string[] { "Default" };
        [SerializeField] private float m_rayLength = 1.05f;
        [SerializeField] private float m_gravity = 1f;

        private Vector3 m_inputDirectionMotion = Vector3.zero;
        private IGeneralMoverState m_state = null;

        private void OnEnable()
        {
            m_state = new IdleState(this);
        }

        private void Update()
        {
            if(m_state != null)
            {
                m_state.Update();
            }

            transform.position += GetPositionChangeValue() * Time.deltaTime;
        }

        private void GetInput(string addKey, string minusKey, ref float setTo)
        {
            if (Input.GetKey(addKey))
            {
                if (setTo < m_maxSpeed)
                {
                    setTo += (m_maxSpeed / m_speedUpTime) * Time.deltaTime;
                }
                else
                {
                    setTo = m_maxSpeed;
                }
            }
            else if (Input.GetKey(minusKey))
            {
                if (setTo > -m_maxSpeed)
                {
                    setTo -= (m_maxSpeed / m_speedUpTime) * Time.deltaTime;
                }
                else
                {
                    setTo = -m_maxSpeed;
                }
            }
            else
            {
                if (setTo > 0f)
                {
                    setTo -= (m_maxSpeed / m_speedUpTime) * Time.deltaTime;
                }
                else if (setTo < 0f)
                {
                    setTo += (m_maxSpeed / m_speedUpTime) * Time.deltaTime;
                }
            }
        }

        private Vector3 GetPositionChangeValue()
        {
            AdjustFloat(ref m_inputDirectionMotion.x);
            AdjustFloat(ref m_inputDirectionMotion.y);
            AdjustFloat(ref m_inputDirectionMotion.z);

            Vector3 _rightVector = transform.right * m_inputDirectionMotion.x;
            Vector3 _forwardVector = transform.forward * m_inputDirectionMotion.z;
            Vector3 _upVector = transform.up * m_inputDirectionMotion.y;

            return _forwardVector + _rightVector + _upVector;
        }

        private void AdjustFloat(ref float value)
        {
            if (Mathf.Abs(value) <= ADJUST_VALUE)
            {
                value = 0f;
            }
        }

        private bool IsGrounded()
        {
            Ray _downRay = new Ray(transform.position, -transform.up);
            return Physics.Raycast(_downRay, m_rayLength, LayerMask.GetMask(m_layerMaskName));
        }

        ////////////////////////////////////////////
        ////////////////////// General Mover States
        ////////////////////////////////////////////

        private interface IGeneralMoverState
        {
            void Update();
        }

        private class IdleState : IGeneralMoverState
        {
            private readonly GeneralMover m_generalMover = null;

            public IdleState(GeneralMover generalMover)
            {
                m_generalMover = generalMover;
            }

            public void Update()
            {
                if(!m_generalMover.IsGrounded())
                {
                    m_generalMover.m_state = new FlyState(m_generalMover);
                    return;
                }
                m_generalMover.GetInput(m_generalMover.m_forwardKey, m_generalMover.m_backwardKey, ref m_generalMover.m_inputDirectionMotion.z);
                m_generalMover.GetInput(m_generalMover.m_rightKey, m_generalMover.m_leftKey, ref m_generalMover.m_inputDirectionMotion.x);
                GetJumpInput();
            }

            private void GetJumpInput()
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    m_generalMover.m_state = new JumpState(m_generalMover);
                }
            }
        }

        private class JumpState : IGeneralMoverState
        {
            private readonly GeneralMover m_generalMover = null;

            public JumpState(GeneralMover generalMover)
            {
                m_generalMover = generalMover;
            }

            public void Update()
            {
                if (!m_generalMover.m_blockMoveInAir)
                {
                    m_generalMover.GetInput(m_generalMover.m_forwardKey, m_generalMover.m_backwardKey, ref m_generalMover.m_inputDirectionMotion.z);
                    m_generalMover.GetInput(m_generalMover.m_rightKey, m_generalMover.m_leftKey, ref m_generalMover.m_inputDirectionMotion.x);
                }

                if (Input.GetKey(KeyCode.Space) && m_generalMover.m_inputDirectionMotion.y < m_generalMover.m_jumpUpSpeed)
                {
                    m_generalMover.m_inputDirectionMotion.y += (m_generalMover.m_jumpUpSpeed / m_generalMover.m_maxJumpSpeedTime) * Time.deltaTime;
                }
                else
                {
                    m_generalMover.m_state = new FlyState(m_generalMover);
                }
            }
        }

        private class FlyState : IGeneralMoverState
        {
            private readonly GeneralMover m_generalMover = null;

            public FlyState(GeneralMover generalMover)
            {
                m_generalMover = generalMover;
            }

            public void Update()
            {
                if (!m_generalMover.m_blockMoveInAir)
                {
                    m_generalMover.GetInput(m_generalMover.m_forwardKey, m_generalMover.m_backwardKey, ref m_generalMover.m_inputDirectionMotion.z);
                    m_generalMover.GetInput(m_generalMover.m_rightKey, m_generalMover.m_leftKey, ref m_generalMover.m_inputDirectionMotion.x);
                }

                m_generalMover.m_inputDirectionMotion.y -= m_generalMover.m_gravity * Time.deltaTime;
                m_generalMover.m_inputDirectionMotion.y -= m_generalMover.m_gravity * m_generalMover.m_fallingAddWeight * Time.deltaTime;

                if (m_generalMover.IsGrounded())
                {
                    m_generalMover.m_inputDirectionMotion.y = 0f;
                    m_generalMover.m_state = new IdleState(m_generalMover);
                }
            }


        }
    } 
}