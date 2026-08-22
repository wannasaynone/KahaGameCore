using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Effects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace KahaGameCore.GameEvents.Editor
{
    internal sealed class GameEventEditorPreferencesProvider : SettingsProvider
    {
        private sealed class CategoryColorDraft
        {
            public string Category;
            public Color Color;
        }

        public const string SettingsPath =
            "Project/Kaha Game Core/Game Event Editor";

        private readonly List<CategoryColorDraft> drafts =
            new List<CategoryColorDraft>();
        private string[] availableCategories = Array.Empty<string>();
        private string categoryDiscoveryWarning;
        private string validationError;

        private GameEventEditorPreferencesProvider() :
            base(SettingsPath, SettingsScope.Project)
        {
            label = "Game Event Editor";
            keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Game Event",
                "Command",
                "Category",
                "Color",
                "指令",
                "分類",
                "顏色"
            };
        }

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new GameEventEditorPreferencesProvider();
        }

        public override void OnActivate(
            string searchContext,
            VisualElement rootElement)
        {
            ReloadAvailableCategories();
            ReloadDrafts();
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.LabelField("指令分類顏色", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "從目前事件目錄已註冊的指令分類中選擇顏色；沒有設定的分類維持原本外觀。",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8f);

            if (!string.IsNullOrWhiteSpace(categoryDiscoveryWarning))
            {
                EditorGUILayout.HelpBox(
                    categoryDiscoveryWarning,
                    MessageType.Warning);
                EditorGUILayout.Space(4f);
            }

            if (availableCategories.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "目前事件目錄沒有可用的指令分類。請先在 Game Event 編輯器的「指令設定」選擇來源並啟用指令。",
                    MessageType.Info);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    "分類",
                    EditorStyles.miniBoldLabel,
                    GUILayout.MinWidth(180f));
                EditorGUILayout.LabelField(
                    "顏色",
                    EditorStyles.miniBoldLabel,
                    GUILayout.Width(90f));
                GUILayout.Space(52f);
            }

            int removeIndex = -1;
            bool changed = false;
            for (int index = 0; index < drafts.Count; index++)
            {
                CategoryColorDraft draft = drafts[index];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    DrawCategoryPopup(draft);
                    draft.Color = EditorGUILayout.ColorField(
                        GUIContent.none,
                        draft.Color,
                        true,
                        true,
                        false,
                        GUILayout.Width(90f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        changed = true;
                    }

                    if (GUILayout.Button("移除", GUILayout.Width(52f)))
                    {
                        removeIndex = index;
                    }
                }
            }

            if (removeIndex >= 0)
            {
                drafts.RemoveAt(removeIndex);
                changed = true;
            }

            EditorGUILayout.Space(4f);
            bool hasUnconfiguredCategory =
                drafts.All(draft => !string.IsNullOrWhiteSpace(draft.Category)) &&
                availableCategories.Any(
                    category => drafts.All(
                        draft => !string.Equals(
                            draft.Category,
                            category,
                            StringComparison.Ordinal)));
            using (new EditorGUI.DisabledScope(!hasUnconfiguredCategory))
            {
                if (GUILayout.Button("新增分類顏色", GUILayout.Width(112f)))
                {
                    drafts.Add(new CategoryColorDraft
                    {
                        Category = string.Empty,
                        Color = Color.white
                    });
                    validationError = "請選擇分類。";
                }
            }

            if (changed)
            {
                SaveDraftsIfValid();
            }

            if (!string.IsNullOrWhiteSpace(validationError))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(validationError, MessageType.Warning);
            }
        }

        private void DrawCategoryPopup(CategoryColorDraft draft)
        {
            string currentCategory = draft.Category ?? string.Empty;
            string[] selectableCategories = availableCategories
                .Where(category =>
                    string.Equals(
                        category,
                        currentCategory,
                        StringComparison.Ordinal) ||
                    drafts.All(other =>
                        ReferenceEquals(other, draft) ||
                        !string.Equals(
                            other.Category,
                            category,
                            StringComparison.Ordinal)))
                .ToArray();
            int availableIndex = Array.IndexOf(
                selectableCategories,
                currentCategory);
            bool needsPlaceholder = availableIndex < 0;
            string[] labels;
            int selectedIndex;
            if (needsPlaceholder)
            {
                string placeholder = string.IsNullOrWhiteSpace(currentCategory)
                    ? "請選擇分類…"
                    : $"遺失／{currentCategory}";
                labels = new[] { placeholder }
                    .Concat(selectableCategories)
                    .ToArray();
                selectedIndex = 0;
            }
            else
            {
                labels = selectableCategories;
                selectedIndex = availableIndex;
            }

            int newIndex = EditorGUILayout.Popup(
                selectedIndex,
                labels,
                GUILayout.MinWidth(180f));
            if (newIndex == selectedIndex)
            {
                return;
            }

            draft.Category = selectableCategories[
                needsPlaceholder ? newIndex - 1 : newIndex];
        }

        private void ReloadAvailableCategories()
        {
            GameEventCatalogAsset eventCatalog =
                GameEventEditorProjectSettings.instance.LoadEventCatalog();
            if (eventCatalog == null)
            {
                availableCategories = Array.Empty<string>();
                categoryDiscoveryWarning = null;
                return;
            }

            var warnings = new List<string>();
            IReadOnlyList<EffectCommandDescriptor> registeredCommands =
                EffectCommandAssemblyCatalog.GetDescriptors(
                    eventCatalog.CommandAssemblyNames,
                    warnings);
            availableCategories = GameEventProjectAuthoringCatalog
                .SelectCommands(
                    registeredCommands,
                    eventCatalog.EnabledCommandNames,
                    warnings)
                .Select(descriptor => descriptor.Category?.Trim())
                .Where(category => !string.IsNullOrEmpty(category))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(category => category, StringComparer.Ordinal)
                .ToArray();
            categoryDiscoveryWarning = warnings.Count == 0
                ? null
                : string.Join("\n", warnings);
        }

        private void ReloadDrafts()
        {
            drafts.Clear();
            foreach (EffectCommandCategoryColorEntry entry in
                     GameEventEditorProjectSettings.instance.CommandCategoryColors)
            {
                drafts.Add(new CategoryColorDraft
                {
                    Category = entry.Category,
                    Color = entry.Color
                });
            }

            validationError = null;
        }

        private void SaveDraftsIfValid()
        {
            var entries = new List<EffectCommandCategoryColorEntry>(drafts.Count);
            var categories = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < drafts.Count; index++)
            {
                CategoryColorDraft draft = drafts[index];
                if (string.IsNullOrWhiteSpace(draft.Category))
                {
                    validationError = $"第 {index + 1} 列尚未選擇分類。";
                    return;
                }

                string category = draft.Category.Trim();
                if (!categories.Add(category))
                {
                    validationError = $"分類 key「{category}」重複。";
                    return;
                }

                entries.Add(new EffectCommandCategoryColorEntry(category, draft.Color));
            }

            GameEventEditorProjectSettings.instance.SetCommandCategoryColors(entries);
            validationError = null;
            GameEventDocumentEditorWindow.RepaintOpenWindows();
        }
    }
}
