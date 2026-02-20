using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ProjectBSR.DialogueSystem.DefaultImplements
{
    public class AddressablesCGProvider : ICGProvider
    {
        private readonly Dictionary<string, AsyncOperationHandle<Texture2D>> loadedHandles = new Dictionary<string, AsyncOperationHandle<Texture2D>>();

        public async UniTask<Texture2D> LoadCGAsync(string cgName)
        {
            if (loadedHandles.TryGetValue(cgName, out AsyncOperationHandle<Texture2D> existingHandle))
            {
                return existingHandle.Result;
            }

            AsyncOperationHandle<Texture2D> handle = Addressables.LoadAssetAsync<Texture2D>(cgName);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedHandles[cgName] = handle;
                return handle.Result;
            }

            Debug.LogError($"[AddressablesCGProvider] Failed to load CG: {cgName}");
            return null;
        }

        public void ReleaseCG(string cgName)
        {
            if (loadedHandles.TryGetValue(cgName, out AsyncOperationHandle<Texture2D> handle))
            {
                Addressables.Release(handle);
                loadedHandles.Remove(cgName);
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
