using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>Wait(秒數)：暫停指令串指定秒數（演出節奏調整用）。</summary>
    public class WaitCommand : KahaGameCore.Package.EffectProcessor.EffectCommandBase
    {
        public override void Process(string[] vars, Action onCompleted, Action onForceQuit)
        {
            if (!float.TryParse(vars[0], out float seconds))
            {
                Debug.LogError($"[WaitCommand] 無法解析秒數：{vars[0]}");
                onCompleted?.Invoke();
                return;
            }

            WaitAsync(seconds, onCompleted).Forget();
        }

        private async UniTaskVoid WaitAsync(float seconds, Action onCompleted)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(seconds));
            onCompleted?.Invoke();
        }
    }
}
