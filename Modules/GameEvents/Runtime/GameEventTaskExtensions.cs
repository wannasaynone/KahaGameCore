using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    public static class GameEventTaskExtensions
    {
        /// <summary>
        /// Like <c>Forget()</c>, but a failure is logged with a Console context:
        /// the failing event file when the runner knows it, otherwise
        /// <paramref name="fallbackContext"/> (usually the trigger). Clicking the
        /// Console entry then pings that object. Cancellation is not logged.
        /// </summary>
        public static void ForgetAndLog(this UniTask task, UnityEngine.Object fallbackContext)
        {
            LogFailureAsync(task, fallbackContext).Forget();
        }

        private static async UniTaskVoid LogFailureAsync(
            UniTask task,
            UnityEngine.Object fallbackContext)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                UnityEngine.Object context = exception is GameEventException gameEventException &&
                    gameEventException.EventFile != null
                        ? gameEventException.EventFile
                        : fallbackContext;
                Debug.LogException(exception, context);
            }
        }
    }
}
