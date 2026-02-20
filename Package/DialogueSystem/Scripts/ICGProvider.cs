using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectBSR.DialogueSystem
{
    public interface ICGProvider
    {
        UniTask<Texture2D> LoadCGAsync(string cgName);
        void ReleaseCG(string cgName);
        void ReleaseAll();
    }
}
