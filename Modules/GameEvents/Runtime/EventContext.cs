using System.Threading;
using KahaGameCore.Effects;

namespace KahaGameCore.GameEvents
{
    public sealed class EventContext
    {
        public EventContext(
            CancellationToken cancellationToken,
            EffectExecutionContext effectContext = null)
        {
            CancellationToken = cancellationToken;
            EffectContext = effectContext ?? new EffectExecutionContext();
        }

        public CancellationToken CancellationToken { get; }
        public EffectExecutionContext EffectContext { get; }
    }
}
