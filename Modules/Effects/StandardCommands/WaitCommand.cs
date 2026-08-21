using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace KahaGameCore.Effects.StandardCommands
{
    /// <summary>Wait(seconds): pauses an effect command sequence.</summary>
    public sealed class WaitCommand : IEffectCommand
    {
        public async UniTask ExecuteAsync(
            EffectExecutionContext context,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!float.TryParse(
                    arguments[0],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float seconds))
            {
                throw new FormatException($"Wait seconds is invalid: '{arguments[0]}'.");
            }

            await UniTask.Delay(
                TimeSpan.FromSeconds(seconds),
                cancellationToken: cancellationToken);
        }
    }
}
