using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KahaGameCore.ActorSystem
{
    public abstract class AGameActor : MonoBehaviour
    {
        public string Faction => faction;
        [SerializeField] private string faction;

        private ActorController _controller;
        private bool _isInitialized;

        protected ActorController Controller => _controller;
        public Vector3 FacingDirection => _currentFacingDirection.normalized;
        private Vector3 _currentFacingDirection = Vector3.right;

        // 此 actor 的所有 action 名冊（含未 active 者）。跨專案共用的協調基礎。
        protected readonly List<AActorAction> _allActions = new();

        public void Initialize()
        {
            if (_isInitialized) return;
            _controller = new ActorController();

            // 發掘並初始化所有子 action（OnBind 只填 action 自身 bindings，不碰 controller channel）。
            var actions = GetComponentsInChildren<AActorAction>();
            foreach (var action in actions)
            {
                action.Init(this);
                _allActions.Add(action);
            }

            OnInitialize(_controller);
            _isInitialized = true;
        }

        // ── 通用 action 啟停 / 查詢 API ───────────────────────────────────
        // 型別參數版一律 protected：只有 actor 自身與其子類（orchestrator——玩家輸入 ControlableGameActor、
        // AI 的 BTGameActor）能依具體型別驅動 action。leaf action 非 AGameActor 子類，即使把 actor cast 回
        // BasicGameActor 也無法呼叫 protected 成員（CS0122），故編譯期無法跨 action 啟停。
        // 外部 AI（BehaviorTree 任務）透過 BTGameActor 公開的 Ai* 包裝呼叫（見 BTGameActor）。

        // 純內部啟動管線（保留「若已 active 先停再啟」的重觸發語意）。
        // 不對外公開實例式啟動，避免外部 new 一個 action 傳入破壞隔離。
        internal void ActivateInstance(AActorAction action, ActionContext context)
        {
            if (action.IsActive)
                _controller.SetActionInactive(action, context);
            _controller.SetActionActive(action, context);
        }

        protected void ActiveAction<T>(ActionContext context, string actionId = "") where T : AActorAction
        {
            T action = FindAction<T>(actionId);
            if (action == null)
            {
                Debug.LogWarning($"[{GetType().Name}] {gameObject.name}: Action of type {typeof(T).Name} with id '{actionId}' not found.");
                return;
            }
            ActivateInstance(action, context);
        }

        protected void DeactivateAction<T>(ActionContext context, string actionId = "") where T : AActorAction
        {
            T action = FindAction<T>(actionId);
            if (action == null) return;
            if (action.IsActive)
                _controller.SetActionInactive(action, context);
        }

        protected bool IsActionActive<T>(string actionId = "") where T : AActorAction
        {
            T action = FindAction<T>(actionId);
            return action != null && action.IsActive;
        }

        protected T GetActionOfType<T>(string actionId = "") where T : AActorAction => FindAction<T>(actionId);

        protected T FindAction<T>(string actionId) where T : AActorAction
        {
            if (string.IsNullOrEmpty(actionId))
                return _allActions.OfType<T>().FirstOrDefault();

            return _allActions.OfType<T>().FirstOrDefault(a => a.ActionId == actionId);
        }

        protected void ActivateActionsWith<TRole>(ActionContext context, Func<TRole, bool> filter = null)
            where TRole : IActorActivatable
        {
            for (int i = 0; i < _allActions.Count; i++)
            {
                if (_allActions[i] is TRole role && (filter == null || filter(role)))
                    ActivateInstance(_allActions[i], context);
            }
        }

        protected void DeactivateActionsWith<TRole>(ActionContext context)
            where TRole : IActorYieldable
        {
            for (int i = 0; i < _allActions.Count; i++)
            {
                if (_allActions[i].IsActive && _allActions[i] is TRole)
                    _controller.SetActionInactive(_allActions[i], context);
            }
        }

        public bool IsAnyActionWithRole<TRole>() where TRole : IActorState
        {
            for (int i = 0; i < _allActions.Count; i++)
                if (_allActions[i].IsActive && _allActions[i] is TRole) return true;
            return false;
        }

        public bool TryGetRole<TRole>(out TRole role) where TRole : IActorState
        {
            for (int i = 0; i < _allActions.Count; i++)
            {
                if (_allActions[i].IsActive && _allActions[i] is TRole r)
                {
                    role = r;
                    return true;
                }
            }
            role = default;
            return false;
        }

        protected abstract void OnInitialize(ActorController controller);
        protected abstract void OnFaceDirectionChanged();
        public abstract void PlayAnimation(string animationName);
        public abstract void SetAnimationSpeed(float delta);
        public abstract string GetCurrentAnimation();
        public abstract Transform GetAnimatorRoot();

        public void SetFacingDirection(Vector3 direction)
        {
            _currentFacingDirection = direction.normalized;
            OnFaceDirectionChanged();
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public void Move(Vector3 delta)
        {
            transform.position += delta;
        }

        public virtual void OnHitByBullet(BulletHitContext context) { }

        private readonly List<Func<BulletHitContext, bool>> _bulletHitFilters = new();

        public void AddBulletHitFilter(Func<BulletHitContext, bool> filter)
        {
            if (!_bulletHitFilters.Contains(filter))
                _bulletHitFilters.Add(filter);
        }

        public void RemoveBulletHitFilter(Func<BulletHitContext, bool> filter)
        {
            _bulletHitFilters.Remove(filter);
        }

        public bool CanBeHitByBullet(BulletHitContext context)
        {
            return _bulletHitFilters.All(f => f(context));
        }
    }
}
