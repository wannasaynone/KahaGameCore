using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using KahaGameCore.Effects;

namespace KahaGameCore.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>Wait(秒數)：暫停指令串指定秒數（演出節奏調整用）。</summary>
    public class WaitCommand : IEffectCommand
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
