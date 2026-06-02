using System;
using System.Collections.Generic;

namespace KahaGameCore.ActorSystem
{
    public class
    ActorController
    {
        private AGameActor _actor;
        private readonly Dictionary<int, ChannelSlot> _channelIdToSlot = new();
        private readonly List<AActorAction> _activeActions = new();
        private readonly List<AActorAction> _tickSnapshot = new();
        private int _activationCounter;
        private ActionContext _currentContext;

        private class ChannelSlot
        {
            public AActorAction Owner;
            public int Priority;
            public int OwnerActivationOrder;
            public Action<ActionContext> Handler;
            public Action<AGameActor, ActionContext> DefaultHandler;

            public void Clear()
            {
                Owner = null;
                Priority = 0;
                OwnerActivationOrder = -1;
                Handler = null;
            }
        }

        public void SetChannelDefault<TChannel>(TChannel channel, Action<AGameActor, ActionContext> defaultHandler) where TChannel : Enum
        {
            int channelId = Convert.ToInt32(channel);
            if (_channelIdToSlot.TryGetValue(channelId, out var slot))
            {
                slot.DefaultHandler = defaultHandler;
            }
        }

        public void Initialize<TChannel>(AGameActor actor) where TChannel : Enum
        {
            _actor = actor;
            _channelIdToSlot.Clear();
            foreach (var value in Enum.GetValues(typeof(TChannel)))
            {
                _channelIdToSlot[Convert.ToInt32(value)] = new ChannelSlot();
            }
        }

        public void SetActionActive(AActorAction action, ActionContext context)
        {
            if (!action.IsInitialized)
            {
                UnityEngine.Debug.LogError("[ActorController] " + _actor.gameObject.name + " is trying to active uninitialized action " + nameof(action) + ", please make sure initialize action before active it.");
                return;
            }

            if (action.IsActive) return;

            action.Completed += OnActionCompleted;

            action.IsActive = true;
            action.ActivationOrder = _activationCounter++;
            action.OwningController = this;
            action.ResetChannelOwnershipCheck();
            _activeActions.Add(action);
            action.Active(context);
        }

        public void SetActionInactive(AActorAction action, ActionContext context)
        {
            if (!action.IsActive) return;

            action.Completed -= OnActionCompleted;

            action.IsActive = false;
            _activeActions.Remove(action);
            action.Deactivate(context);
        }

        public void Tick(ActionContext context)
        {
            _currentContext = context;
            _tickSnapshot.Clear();
            for (int i = 0; i < _activeActions.Count; i++)
                _tickSnapshot.Add(_activeActions[i]);

            for (int i = 0; i < _tickSnapshot.Count; i++)
                _tickSnapshot[i].Tick(context);

            foreach (var slot in _channelIdToSlot.Values)
            {
                slot.Clear();
            }

            for (int i = 0; i < _activeActions.Count; i++)
            {
                var action = _activeActions[i];
                for (int j = 0; j < action.Bindings.Count; j++)
                {
                    var binding = action.Bindings[j];
                    if (!_channelIdToSlot.TryGetValue(binding.ChannelId, out var slot)) continue;

                    bool canClaim = binding.Priority > slot.Priority ||
                                    (binding.Priority == slot.Priority &&
                                     action.ActivationOrder > slot.OwnerActivationOrder);

                    if (canClaim)
                    {
                        slot.Owner = action;
                        slot.Priority = binding.Priority;
                        slot.OwnerActivationOrder = action.ActivationOrder;
                        slot.Handler = binding.Handler;
                    }
                }
            }

            foreach (var slot in _channelIdToSlot.Values)
            {
                if (slot.Handler != null)
                {
                    slot.Handler.Invoke(context);
                }
                else
                {
                    slot.DefaultHandler?.Invoke(_actor, context);
                }
            }
        }

        public AActorAction GetChannelOwner(int channelId)
        {
            if (_channelIdToSlot.TryGetValue(channelId, out var slot))
                return slot.Owner;
            return null;
        }

        private void OnActionCompleted(AActorAction action)
        {
            SetActionInactive(action, _currentContext);
        }
    }
}
