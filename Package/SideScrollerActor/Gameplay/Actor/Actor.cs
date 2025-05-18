using System.Collections;
using UnityEngine;
using KahaGameCore.GameEvent;
using KahaGameCore.Common;
using System.Collections.Generic;
using KahaGameCore.Package.SideScrollerActor.Gameplay.Extension;
using KahaGameCore.Package.SideScrollerActor.InGameEvent;
using KahaGameCore.Package.SideScrollerActor.Camera;
using KahaGameCore.Package.SideScrollerActor.WeaponScripts;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    [RequireComponent(typeof(BoxCollider2D))]
    public partial class Actor : MonoBehaviour
    {
        public enum Camp
        {
            Monster,
            Hero,
        }

        [Header("滑鼠長放可以看到說明")]
        [Space(10)]
        [Tooltip("目前只有Monster和Hero，互相敵對；遊戲可能允許玩家操作Monster所以不適用Player/Enemy。")]
        [Rename("角色陣營")] public Camp camp = Camp.Monster;

        public enum LockDirectionType
        {
            None,
            LookAtOpponent,
            LookAtMouse
        }

        [Tooltip("角色將永遠看向敵人或滑鼠，None表示不固定。")]
        [Rename("角色面向追蹤種類")] public LockDirectionType lockDirectionType = LockDirectionType.None;

        public enum LockLookDirection
        {
            None,
            Left,
            Right
        }
        [Tooltip("鎖定角色永遠看向左或右，None則不鎖定。受角色面向追蹤種類影響，若種類不是None，種類會修改這個參數。")]
        [Rename("鎖定面向方向")] public LockLookDirection lockLookDirection = LockLookDirection.None;

        public enum State
        {
            Normal,
            Dashing,
            Attacking,
            Extension,
            Hurting,
            Dead,
            JumpingToTargetX,
            WaitInitialize,
            Flying,
            Recovering,
            Defending,
            Stunned,
            SimpleJumping,
            Running,
            Reloading,
            SimplePauseMove
        }

        public enum JumpState
        {
            None,
            Prepare,
            JumpingUp,
            Hover,
            Falling
        }

        [Space(10)]
        [Header("Actor設置")]
        [Space(10)]
        [Tooltip("目前最大生命值，初始化後會自動填入。恢復生命時無法超過這個數值。")]
        [Rename("目前最大生命值")] public int currentMaxHealth;
        [Tooltip("目前生命值，初始化後會自動填入。歸零後進入死亡流程。")]
        [Rename("目前生命值")] public int currentHealth;
        [Tooltip("目前最大體力，初始化後會自動填入。恢復體力時無法超過這個數值。")]
        [Rename("目前最大體力")] public int currentMaxStamina;
        [Tooltip("目前體力，初始化後會自動填入。消耗體力時無法低於零。")]
        [Rename("目前體力")] public float currentStamina;
        public ActorSetting actorSetting;

        [Space(10)]
        [Header("武器")]
        [Space(10)]
        [Header("Actor一定要有一個武器，否則無法進行攻擊")]
        [Header("攻擊相關的設定都在武器身上")]
        [Header("目前的結構為 Actor -> 武器Weapon -> 攻擊資訊AttackInfo")]
        private IWeaponCapability weaponCapability;

        [Space(10)]
        [Header("其他設置")]
        [Space(10)]
        [Tooltip("允許跳躍的開關，設定為false則不會響應跳躍輸入以及外部呼叫跳躍功能。")]
        [Rename("允許跳躍")][SerializeField] private bool allowJump = true;
        [Tooltip("允許衝刺的開關，設定為false則不會響應衝刺輸入以及外部呼叫衝刺功能。")]
        [Rename("允許衝刺")][SerializeField] private bool allowDash = true;
        [Tooltip("受傷後若體力為0時將會進入被擊倒的狀態，設定受傷後若被擊倒是否倒地。若是，則會播放down animation。")]
        [Rename("被擊倒後倒地")][SerializeField] private bool downWhenStunned = true;
        [Tooltip("允許受到攻擊後會根據 attack info 的 force 被擊退。")]
        [Rename("允許擊退")][SerializeField] private bool allowKnockBack = true;
        [Space(10)]
        [Header("擴充功能用，為特殊使用機制，要使用時與KAHA討論")]
        [Tooltip("擴充功能用，為特殊使用機制，要使用時與KAHA討論。")]
        [SerializeField] private ActorExtension[] actorExtensions;

        [Space(10)]
        [Header("視覺相關組件設置")]
        [Space(10)]
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystemRenderer dashEffect_front;
        [SerializeField] private ParticleSystemRenderer dashEffect_back;
        [SerializeField] private GameObject perfectGuardEffect;
        [SerializeField] private float cameraOffset_normal = 0.5f;
        [SerializeField] private float cameraOffset_y = 0f;
        [SerializeField] private Transform defaultFirePoint;
        [SerializeField] private string groundLayerName = "Ground";
        [Header("Observable")]
        [Observerable][SerializeField] private SpriteRenderer[] spriteRenderers;

        /// cache parameter
        private BoxCollider2D boxCollider2D;
        private IAttackInfo currentAttackInfo;
        private GameObject cachedEnabledPrepareGameObject;
        private GameObject cachedEnabledAttackGameObject;
        private ActorExtension currentActorExtension;
        private float dashCooldownTimer = 0f;
        private bool isPressingRight = false;
        private bool isPressingLeft = false;
        private IInteractableObject waitInteractableObject = null;
        private LockLookDirection lastLockLookDirection = LockLookDirection.None;
        private UnityEngine.Camera mainCamera;
        private List<object> pauseLocks = new List<object>();
        private IEnumerator currentHurtCoroutine;
        private float attackTimer = 0f;
        private float defenseTimer = 0f;
        private bool isForceInvincible = false;
        private bool isPreparingAttack = false;
        private bool isHoldingPrepareAttackButton = false;
        [SerializeField] private State state = State.WaitInitialize;
        private JumpState jumpState = JumpState.None;
        ///

        public bool IsGrounded { get; private set; }

        public IWeaponCapability Weapon
        {
            get
            {
                return weaponCapability;
            }
        }

        public Animator Animator { get { return animator; } }
        public SpriteRenderer[] SpriteRenderers { get { return spriteRenderers; } }

        public bool IsFacingRight
        {
            get
            {
                return Mathf.Approximately(animator.transform.eulerAngles.y, 0f);
            }
        }

        public bool IsInvincible
        {
            get
            {
                return state == State.Dead
                    || state == State.Flying
                    || state == State.WaitInitialize
                    || state == State.Dashing
                    || state == State.Recovering
                    || state == State.Defending
                    || state == State.JumpingToTargetX
                    || state == State.Stunned
                    || isForceInvincible;
            }
        }

        public bool IsControlable
        {
            get
            {
                return state == State.Normal || state == State.Running;
            }
        }

        public bool IsStunned
        {
            get
            {
                return state == State.Stunned;
            }
        }

        public bool IsOverride
        {
            get
            {
                return state == State.Extension;
            }
        }

        public bool IsWaitingInitialize
        {
            get
            {
                return state == State.WaitInitialize;
            }
        }

        public string GetStateName()
        {
            return state.ToString();
        }

        private void Awake()
        {
            boxCollider2D = GetComponent<BoxCollider2D>();
            boxCollider2D.isTrigger = true;

            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                Debug.LogError("Actor: " + gameObject.name + " has no sprite renderer.");
            }

            EventBus.Subscribe<WeaponSwitcher_OnWeaponRequested>(WeaponSwitcher_OnWeaponRequested);
        }

        private void WeaponSwitcher_OnWeaponRequested(WeaponSwitcher_OnWeaponRequested e)
        {
            if (e.actorInstanceID != GetInstanceID())
                return;

            if (e.weapon != null)
            {
                SetWeapon(e.weapon);
            }
        }

        private void OnEnable()
        {
            ActorContainer.RegisterActor(this);
            EventBus.Subscribe<RangeWeapon_OnReloading>(OnReloading);
        }

        private void OnDisable()
        {
            ActorContainer.UnregisterActor(this);

            if (currentActorExtension != null)
            {
                currentActorExtension.ForceEnd();
            }

            currentActorExtension = null;
            isHoldingPrepareAttackButton = false;
            isPreparingAttack = false;

            if (cachedEnabledPrepareGameObject != null)
            {
                cachedEnabledPrepareGameObject.gameObject.SetActive(false);
                cachedEnabledPrepareGameObject = null;
            }

            EventBus.Unsubscribe<RangeWeapon_OnReloading>(OnReloading);
            state = State.WaitInitialize;
        }

        private void Update()
        {
            float groundCheckDistance = 0.1f;
            LayerMask groundLayer = 1 << LayerMask.NameToLayer(groundLayerName);
            RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.up * groundCheckDistance, Vector2.down, groundCheckDistance, groundLayer);
            IsGrounded = hit.collider != null;

            if (!IsGrounded && (IsControlable || jumpState == JumpState.Hover))
            {
                ContinueSimpleJump();
            }

            if (!IsControlable)
            {
                return;
            }

            dashCooldownTimer -= Time.deltaTime;
            TickLockDirection();

            SetStamina(currentStamina + actorSetting.restoreStaminaPerSecond * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (CameraController.Instance == null || CameraController.Instance.target != transform)
            {
                return;
            }

            if (state == State.Dead || state == State.Extension) // will let extension handle camera
            {
                return;
            }

            ForceUpdateCameraSettingWithThisActor();
        }

        public void Initialize(IWeaponCapability defaultWeaponCapability = null)
        {
            if (defaultWeaponCapability != null)
            {
                weaponCapability = defaultWeaponCapability;
                EventBus.Publish(new Actor_OnWeaponChanged() { actorInstanceID = GetInstanceID(), weapon = defaultWeaponCapability });
            }

            if (weaponCapability == null)
            {
                Debug.LogError("No weapon found in Actor: " + gameObject.name + ". Please set weapon before initialize.");
                return;
            }

            currentMaxHealth = actorSetting.health;
            currentHealth = currentMaxHealth;
            currentMaxStamina = actorSetting.stamina;
            currentStamina = currentMaxStamina;
            state = State.Normal;

            animator.Play(weaponCapability.GetIdleAnimationName());
            EventBus.Publish(new Actor_OnHealthChanged() { instanceID = GetInstanceID(), currentHealth = currentHealth, maxHealth = currentMaxHealth });
            EventBus.Publish(new Actor_OnStaminaChanged() { instanceID = GetInstanceID(), currentStamina = System.Convert.ToInt32(currentStamina), maxStamina = currentMaxStamina });
        }

        public class JumpInfo
        {
            public float targetX;
            public float height;
            public float duration;
        }
    }
}
