using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectBSR.DialogueSystem.View
{
    public class CGDisplayer : MonoBehaviour
    {
        [SerializeField] private CGLayer cgLayerPrefab;
        [SerializeField] private Transform layerContainer;

        private readonly Dictionary<string, CGLayer> activeLayers = new Dictionary<string, CGLayer>();

        public bool HasCG(string cgName)
        {
            return activeLayers.ContainsKey(cgName);
        }

        public async UniTask ShowCG(Texture2D texture, string cgName, float fadeInTime)
        {
            if (activeLayers.TryGetValue(cgName, out CGLayer existingLayer))
            {
                existingLayer.Setup(texture, cgName);
                await existingLayer.FadeIn(fadeInTime);
                return;
            }

            CGLayer layer = Instantiate(cgLayerPrefab, layerContainer);
            layer.gameObject.SetActive(true);
            layer.Setup(texture, cgName);
            activeLayers[cgName] = layer;

            await layer.FadeIn(fadeInTime);
        }

        public async UniTask HideCG(string cgName, float fadeOutTime)
        {
            if (!activeLayers.TryGetValue(cgName, out CGLayer layer))
            {
                return;
            }

            activeLayers.Remove(cgName);
            await layer.FadeOut(fadeOutTime);
            Destroy(layer.gameObject);
        }

        public void RemoveAllLayers()
        {
            foreach (var layer in activeLayers.Values)
            {
                if (layer != null)
                {
                    Destroy(layer.gameObject);
                }
            }
            activeLayers.Clear();
        }

        private void OnDisable()
        {
            RemoveAllLayers();
        }
    }
}
