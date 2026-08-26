using System;
using System.Collections.Generic;
using UnityEditor;

namespace KahaGameCore.GameEvents.Editor
{
    public sealed class GameEventArgumentOptionProvider :
        IEffectCommandArgumentOptionProvider
    {
        private static readonly GameEventDocumentJsonCodec Codec =
            new GameEventDocumentJsonCodec();

        public string SourceKey =>
            GameEventEffectCommandModule.EventOptionSourceKey;

        public IReadOnlyList<EffectCommandArgumentOption> GetOptions(
            EffectCommandArgumentOptionContext context)
        {
            return BuildOptions(
                GameEventEditorProjectSettings.instance.LoadEventCatalog());
        }

        public void StopPreview()
        {
        }

        internal static IReadOnlyList<EffectCommandArgumentOption> BuildOptions(
            GameEventCatalogAsset catalog)
        {
            var options = new List<EffectCommandArgumentOption>();
            if (catalog == null)
            {
                return options;
            }

            for (int index = 0; index < catalog.Files.Count; index++)
            {
                UnityEngine.TextAsset asset = catalog.Files[index];
                if (asset == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(asset);
                EffectCommandArgumentOption option = CreateOption(asset, assetPath);
                if (option != null)
                {
                    options.Add(option);
                }
            }

            return options;
        }

        internal static EffectCommandArgumentOption CreateOption(
            UnityEngine.TextAsset asset,
            string assetPath)
        {
            if (asset == null)
            {
                return null;
            }

            GameEventDocument document;
            try
            {
                document = Codec.Read(asset.text);
            }
            catch (Exception)
            {
                return null;
            }

            return new EffectCommandArgumentOption(
                document.DocumentGuid.ToString("D"),
                $"{document.DisplayName} ({asset.name})",
                GameEventEditorAssetUtility.GetCategoryFromAssetPath(assetPath),
                assetPath,
                asset);
        }
    }
}
