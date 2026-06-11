using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    public class PerformanceRegistry : IPerformancePlayer
    {
        /// <summary>未註冊演出的佔位停頓時間（秒），讓流程節奏接近實裝後的感覺。</summary>
        private const float PLACEHOLDER_DURATION_SECONDS = 0.5f;

        private readonly Dictionary<string, IStagePerformance> idToPerformance = new Dictionary<string, IStagePerformance>();

        public void Register(string performanceId, IStagePerformance performance)
        {
            if (string.IsNullOrEmpty(performanceId))
            {
                throw new ArgumentException("演出 ID 不可為空。", nameof(performanceId));
            }

            idToPerformance[performanceId] = performance ?? throw new ArgumentNullException(nameof(performance));
        }

        public async UniTask PlayAsync(string performanceId)
        {
            if (string.IsNullOrWhiteSpace(performanceId))
            {
                return;
            }

            if (idToPerformance.TryGetValue(performanceId, out IStagePerformance performance))
            {
                await performance.PlayAsync();
                return;
            }

            Debug.Log($"[演出預留] {performanceId}（尚未註冊 IStagePerformance，先以佔位停頓代替）");
            await UniTask.Delay(TimeSpan.FromSeconds(PLACEHOLDER_DURATION_SECONDS), ignoreTimeScale: true);
        }
    }
}
