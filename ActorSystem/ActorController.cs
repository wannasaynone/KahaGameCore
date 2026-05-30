using System;
using System.Collections.Generic;

namespace KahaGameCore.ActorSystem
{
    public class ActorController
    {
        private IActor _actor;
        private readonly Dictionary<int, ChannelSlot> _channelIdToSlot = new();
        private readonly List<AActorAction> _activeActions = new();
        private readonly List<AActorAction> _tickSnapshot = new();
        private int _activationCounter;

        private class ChannelSlot
        {
            public AActorAction Owner;
            public int Priority;
            public int OwnerActivationOrder;
            public Action<IActor> Handler;

            public void Clear()
            {
                Owner = null;
                Priority = 0;
                OwnerActivationOrder = -1;
                Handler = null;
            }
        }

        public void Initialize<TChannel>(IActor actor) where TChannel : Enum
        {
            _actor = actor;
            _channelIdToSlot.Clear();
            foreach (var value in Enum.GetValues(typeof(TChannel)))
            {
                _channelIdToSlot[Convert.ToInt32(value)] = new ChannelSlot();
            }
        }

        public void SetActionActive(AActorAction action)
        {
            if (action.IsActive) return;

            action.Init();
            action.Completed += OnActionCompleted;

            action.IsActive = true;
            action.ActivationOrder = _activationCounter++;
            _activeActions.Add(action);
            action.OnStart(_actor);
        }

        public void SetActionInactive(AActorAction action)
        {
            if (!action.IsActive) return;

            action.Completed -= OnActionCompleted;

            action.IsActive = false;
            _activeActions.Remove(action);
            action.OnEnd(_actor);
        }

        public void Tick()
        {
            _tickSnapshot.Clear();
            for (int i = 0; i < _activeActions.Count; i++)
                _tickSnapshot.Add(_activeActions[i]);

            for (int i = 0; i < _tickSnapshot.Count; i++)
                _tickSnapshot[i].OnTick();

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
                slot.Handler?.Invoke(_actor);
            }
        }

        private void OnActionCompleted(AActorAction action)
        {
            SetActionInactive(action);
        }
    }
}
