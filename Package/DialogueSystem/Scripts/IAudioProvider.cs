using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectBSR.DialogueSystem
{
    public interface IAudioProvider
    {
        UniTask<AudioClip> LoadAudioAsync(string audioName);
        void ReleaseAudio(string audioName);
        void ReleaseAll();
    }
}
