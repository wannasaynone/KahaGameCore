using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ProjectBSR.DialogueSystem.DefaultImplements
{
    public class AddressablesAudioProvider : IAudioProvider
    {
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> loadedHandles = new Dictionary<string, AsyncOperationHandle<AudioClip>>();

        public async UniTask<AudioClip> LoadAudioAsync(string audioName)
        {
            if (loadedHandles.TryGetValue(audioName, out AsyncOperationHandle<AudioClip> existingHandle))
            {
                return existingHandle.Result;
            }

            try
            {
                AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(audioName);
                await handle.ToUniTask();

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    loadedHandles[audioName] = handle;
                    return handle.Result;
                }

                Debug.LogError($"[AddressablesAudioProvider] Failed to load audio: {audioName}");
                return null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AddressablesAudioProvider] Exception loading audio: {audioName}, {ex.Message}");
                return null;
            }
        }

        public void ReleaseAudio(string audioName)
        {
            if (loadedHandles.TryGetValue(audioName, out AsyncOperationHandle<AudioClip> handle))
            {
                Addressables.Release(handle);
                loadedHandles.Remove(audioName);
            }
        }

        public void ReleaseAll()
        {
            foreach (var handle in loadedHandles.Values)
            {
                Addressables.Release(handle);
            }
            loadedHandles.Clear();
        }
    }
}
