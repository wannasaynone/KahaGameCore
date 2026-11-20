using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    [Serializable]
    internal sealed class EffectCommandCategoryColorEntry
    {
        [SerializeField] private string category;
        [SerializeField] private Color color;

        public EffectCommandCategoryColorEntry(string category, Color color)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException(
                    "Command category is required.",
                    nameof(category));
            }

            this.category = category.Trim();
            this.color = color;
        }

        public string Category => category;
        public Color Color => color;
    }

    [Serializable]
    internal sealed class EffectCommandCategoryColorPalette
    {
        [SerializeField] private List<EffectCommandCategoryColorEntry> entries =
            new List<EffectCommandCategoryColorEntry>();

        public IReadOnlyList<EffectCommandCategoryColorEntry> Entries =>
            entries ?? (IReadOnlyList<EffectCommandCategoryColorEntry>)
            Array.Empty<EffectCommandCategoryColorEntry>();

        public bool TryGetColor(string category, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(category) || entries == null)
            {
                return false;
            }

            string normalizedCategory = category.Trim();
            foreach (EffectCommandCategoryColorEntry entry in entries)
            {
                if (entry != null &&
                    string.Equals(
                        entry.Category,
                        normalizedCategory,
                        StringComparison.Ordinal))
                {
                    color = entry.Color;
                    return true;
                }
            }

            return false;
        }

        public void Replace(IEnumerable<EffectCommandCategoryColorEntry> replacements)
        {
            if (replacements == null)
            {
                throw new ArgumentNullException(nameof(replacements));
            }

            var replacementEntries = new List<EffectCommandCategoryColorEntry>();
            var categories = new HashSet<string>(StringComparer.Ordinal);
            foreach (EffectCommandCategoryColorEntry replacement in replacements)
            {
                if (replacement == null)
                {
                    throw new ArgumentException(
                        "Command category colors cannot contain null entries.",
                        nameof(replacements));
                }

                var entry = new EffectCommandCategoryColorEntry(
                    replacement.Category,
                    replacement.Color);
                if (!categories.Add(entry.Category))
                {
                    throw new ArgumentException(
                        $"Command category '{entry.Category}' has more than one color.",
                        nameof(replacements));
                }

                replacementEntries.Add(entry);
            }

            entries = replacementEntries;
        }
    }

    /// <summary>
    /// Colors of the If／Else／End If／Quit rows in the command list. Each If
    /// nesting level cycles through <see cref="BlockColors"/>; the level's color
    /// is used for its If panel, End If bar and frame border.
    /// </summary>
    [Serializable]
    internal sealed class ControlFlowColorSettings
    {
        public static readonly Color[] DefaultBlockColors =
        {
            new Color(0.22f, 0.50f, 0.32f),
            new Color(0.24f, 0.42f, 0.66f),
            new Color(0.50f, 0.32f, 0.62f)
        };

        public static readonly Color DefaultElseColor = new Color(0.62f, 0.44f, 0.12f);
        public static readonly Color DefaultQuitColor = new Color(0.62f, 0.20f, 0.20f);
        public const float DefaultFrameBackgroundAlpha = 0.14f;

        [SerializeField] private Color[] blockColors = (Color[])DefaultBlockColors.Clone();
        [SerializeField] private Color elseColor = DefaultElseColor;
        [SerializeField] private Color quitColor = DefaultQuitColor;
        [SerializeField] private float frameBackgroundAlpha = DefaultFrameBackgroundAlpha;

        public ControlFlowColorSettings()
        {
        }

        public ControlFlowColorSettings(
            IReadOnlyList<Color> blockColors,
            Color elseColor,
            Color quitColor,
            float frameBackgroundAlpha)
        {
            if (blockColors == null || blockColors.Count == 0)
            {
                throw new ArgumentException(
                    "At least one block color is required.",
                    nameof(blockColors));
            }

            this.blockColors = new Color[blockColors.Count];
            for (int index = 0; index < blockColors.Count; index++)
            {
                this.blockColors[index] = blockColors[index];
            }

            this.elseColor = elseColor;
            this.quitColor = quitColor;
            this.frameBackgroundAlpha = Mathf.Clamp01(frameBackgroundAlpha);
        }

        public IReadOnlyList<Color> BlockColors =>
            blockColors != null && blockColors.Length > 0
                ? blockColors
                : DefaultBlockColors;

        public Color ElseColor => elseColor;
        public Color QuitColor => quitColor;
        public float FrameBackgroundAlpha => frameBackgroundAlpha;

        public Color GetBlockColor(int depth)
        {
            IReadOnlyList<Color> colors = BlockColors;
            return colors[Math.Max(0, depth) % colors.Count];
        }
    }

    [FilePath(
        "ProjectSettings/KahaGameCore.GameEventEditorSettings.asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal sealed class GameEventEditorProjectSettings :
        ScriptableSingleton<GameEventEditorProjectSettings>
    {
        [SerializeField] private string eventCatalogGuid;
        [SerializeField] private EffectCommandCategoryColorPalette commandCategoryColors =
            new EffectCommandCategoryColorPalette();
        [SerializeField] private ControlFlowColorSettings controlFlowColors =
            new ControlFlowColorSettings();

        public IReadOnlyList<EffectCommandCategoryColorEntry> CommandCategoryColors =>
            GetCommandCategoryColors().Entries;

        public ControlFlowColorSettings ControlFlowColors =>
            controlFlowColors ?? (controlFlowColors = new ControlFlowColorSettings());

        public void SetControlFlowColors(ControlFlowColorSettings colors)
        {
            controlFlowColors = colors ?? throw new ArgumentNullException(nameof(colors));
            Save(true);
        }

        public GameEventCatalogAsset LoadEventCatalog()
        {
            if (string.IsNullOrEmpty(eventCatalogGuid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(eventCatalogGuid);
            GameEventCatalogAsset catalog = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameEventCatalogAsset>(path);
            if (catalog != null)
            {
                return catalog;
            }

            eventCatalogGuid = string.Empty;
            Save(true);
            return null;
        }

        public void SetEventCatalog(GameEventCatalogAsset catalog)
        {
            string newGuid = catalog == null
                ? string.Empty
                : GetValidatedGuid(catalog);
            if (string.Equals(eventCatalogGuid, newGuid, StringComparison.Ordinal))
            {
                return;
            }

            eventCatalogGuid = newGuid;
            Save(true);
        }

        public bool TryGetCommandCategoryColor(string category, out Color color)
        {
            return GetCommandCategoryColors().TryGetColor(category, out color);
        }

        public void SetCommandCategoryColors(
            IEnumerable<EffectCommandCategoryColorEntry> entries)
        {
            GetCommandCategoryColors().Replace(entries);
            Save(true);
        }

        private EffectCommandCategoryColorPalette GetCommandCategoryColors()
        {
            if (commandCategoryColors == null)
            {
                commandCategoryColors = new EffectCommandCategoryColorPalette();
            }

            return commandCategoryColors;
        }

        private static string GetValidatedGuid(GameEventCatalogAsset catalog)
        {
            string path = AssetDatabase.GetAssetPath(catalog);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                throw new ArgumentException(
                    $"Cannot resolve an asset GUID for '{path}'.",
                    nameof(catalog));
            }

            return guid;
        }
    }
}
