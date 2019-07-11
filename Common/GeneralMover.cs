using UnityEngine;

namespace KahaGameCore.Common
{
    public class GeneralMover : MonoBehaviour
    {
        private const float ADJUST_VALUE = 0.1f;

        public Vector3 DirectionMotion { get { return m_inputDirectionMotion; } }

        [SerializeField] private Transform m_directionBase = null;
        [SerializeField] private Transform m_actorRoot = null;
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
        [SerializeField] private float m_boxHeight = 1f;
        [SerializeField] private float m_boxWidth = 0.5f;
        [SerializeField] private float m_boxDensity = 0.1f;
        [SerializeField] private float m_gravity = 1f;
        [Header("Test Tools")]
        [SerializeField] private bool m_showPhysicsRays = false;

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
            m_actorRoot.rotation = m_directionBase.rotation;
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

            Vector3 _rightVector = m_directionBase.right * m_inputDirectionMotion.x;
            Vector3 _forwardVector = m_directionBase.forward * m_inputDirectionMotion.z;
            Vector3 _upVector = m_directionBase.up * m_inputDirectionMotion.y;

            Vector3 _direction = _forwardVector + _rightVector + _upVector;

            if(IsHitWall(_direction.normalized))
            {
                _direction.x = _direction.z = 0f;
            }

            return _direction;
        }

        private void AdjustFloat(ref float value)
        {
            if (Mathf.Abs(value) <= ADJUST_VALUE)
            {
                value = 0f;
            }
        }

        private bool IsHitWall(Vector3 direction)
        {
            Vector3 _startPoint = transform.position - (transform.right * m_boxWidth / 2f);

            float _each_height = m_boxDensity / m_boxHeight;
            float _each_width = m_boxDensity / m_boxWidth;

            for(float _currentHeight = 0f; _currentHeight < m_boxHeight; _currentHeight += _each_height)
            {
                for(float _currentWidth = 0f; _currentWidth < m_boxWidth; _currentWidth += _each_width)
                {
                    Ray _ray = new Ray(_startPoint + new Vector3(_currentWidth, _currentHeight, 0f), direction);
                    if(m_showPhysicsRays)
                    {
                        Debug.DrawRay(_startPoint + new Vector3(_currentWidth, _currentHeight, 0f), direction, Color.red);
                    }
                    if (Physics.Raycast(_ray, m_rayLength, LayerMask.GetMask(m_layerMaskName)))
                    {
                        return true;
                    }
                }
            }

            return false;
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
                if (!m_generalMover.IsHitWall(-m_generalMover.transform.up))
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

                if(m_generalMover.IsHitWall(m_generalMover.transform.up) && m_generalMover.m_inputDirectionMotion.y > 0f)
                {
                    m_generalMover.m_inputDirectionMotion.y = 0f;
                }

                m_generalMover.m_inputDirectionMotion.y -= m_generalMover.m_gravity * Time.deltaTime;
                m_generalMover.m_inputDirectionMotion.y -= m_generalMover.m_gravity * m_generalMover.m_fallingAddWeight * Time.deltaTime;

                if (m_generalMover.IsHitWall(-m_generalMover.transform.up))
                {
                    m_generalMover.m_inputDirectionMotion.y = 0f;
                    m_generalMover.m_state = new IdleState(m_generalMover);
                }
            }
        }
    }
}
