using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace KahaGameCore.Dialogue.View
{
    public interface IDialogueTextDisplayRule
    {
        UniTask DisplayAsync(
            string text,
            Action<string> setVisibleText,
            CancellationToken cancellationToken);
    }

    public sealed class ImmediateDialogueTextDisplayRule : IDialogueTextDisplayRule
    {
        public UniTask DisplayAsync(
            string text,
            Action<string> setVisibleText,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            setVisibleText(text);
            return UniTask.CompletedTask;
        }
    }
}
