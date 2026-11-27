using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.Parameters;
using UnityEditor;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    /// <summary>
    /// The structured condition editor the event Condition and Parameter State
    /// Binder use (parameter dropdown, operator, value, AND/OR groups), for
    /// editor windows outside this assembly that store a condition string.
    /// One instance can draw many conditions; each is identified by a key.
    /// </summary>
    public sealed class GameEventConditionField
    {
        private sealed class Cache
        {
            public string Source;
            public GameEventConditionGroupDraft Root;
            public string Error;
            public string PendingSource;
        }

        private readonly Action repaint;
        private readonly ParameterTableWorkspace parameterWorkspace = new ParameterTableWorkspace();
        private readonly Dictionary<string, Cache> caches = new Dictionary<string, Cache>(StringComparer.Ordinal);
        private GameEventCatalogAsset eventCatalog;
        private GameEventProjectAuthoringCatalog catalog;
        private ParameterAuthoringEntry[] parameters = Array.Empty<ParameterAuthoringEntry>();
        private string parameterTablesRevision;

        /// <param name="repaint">Repaints the hosting window after a dropdown selection.</param>
        public GameEventConditionField(Action repaint)
        {
            this.repaint = repaint ?? throw new ArgumentNullException(nameof(repaint));
        }

        /// <summary>
        /// Draws the condition and returns its new source. <paramref name="changed"/>
        /// is true when the user edited it this frame, or a dropdown selection
        /// made since the previous draw changed it.
        /// </summary>
        public string Draw(
            GameEventCatalogAsset catalogAsset,
            string key,
            string condition,
            string emptyMessage,
            out bool changed)
        {
            changed = false;
            condition ??= string.Empty;
            EnsureCatalog(catalogAsset);

            if (!caches.TryGetValue(key, out Cache cache))
            {
                cache = new Cache();
                caches[key] = cache;
            }

            if (cache.PendingSource != null)
            {
                condition = cache.PendingSource;
                cache.PendingSource = null;
                changed = true;
            }
            else if (!string.Equals(cache.Source, condition, StringComparison.Ordinal) || (cache.Root == null && cache.Error == null))
            {
                Parse(cache, condition);
            }

            if (cache.Error != null)
            {
                EditorGUILayout.HelpBox(
                    "此條件使用了圖形介面不支援的語法，只能直接編輯公式：" + cache.Error,
                    MessageType.Error);
                string edited = EditorGUILayout.TextField("條件公式", condition);
                changed |= !string.Equals(edited, condition, StringComparison.Ordinal);
                return edited;
            }

            if (parameters.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "事件目錄的參數表中沒有布林或數值參數，無法建立條件。",
                    MessageType.Warning);
                return condition;
            }

            bool structureEdited = GameEventConditionGui.DrawGroup(
                cache.Root,
                parameters,
                () => catalog,
                true,
                emptyMessage,
                ShowCreateParameterPopup,
                () =>
                {
                    cache.PendingSource = GameEventConditionDraftCodec.Serialize(cache.Root);
                    cache.Source = cache.PendingSource;
                    repaint();
                },
                out bool _);
            if (structureEdited)
            {
                condition = GameEventConditionDraftCodec.Serialize(cache.Root);
                cache.Source = condition;
                changed = true;
            }

            return condition;
        }

        /// <summary>Reloads the parameter list and drops cached drafts, e.g. after the host reloads its data.</summary>
        public void Refresh()
        {
            caches.Clear();
            parameterTablesRevision = null;
        }

        private static void Parse(Cache cache, string condition)
        {
            cache.Source = condition;
            cache.Root = null;
            cache.Error = null;
            try
            {
                cache.Root = GameEventConditionDraftCodec.Parse(condition);
            }
            catch (Exception exception)
            {
                cache.Error = exception.Message;
            }
        }

        private void EnsureCatalog(GameEventCatalogAsset selected)
        {
            string revision = GetParameterTablesRevision(selected);
            if (catalog != null &&
                selected == eventCatalog &&
                string.Equals(revision, parameterTablesRevision, StringComparison.Ordinal))
            {
                return;
            }

            eventCatalog = selected;
            parameterWorkspace.Bind(selected != null ? selected.ParameterTables : Array.Empty<TextAsset>());
            catalog = GameEventProjectAuthoringCatalog.Load(selected);
            parameters = catalog.ParameterEntries
                .Where(entry => GameEventConditionGui.IsSupportedParameterType(entry.Definition.Type))
                .ToArray();
            parameterTablesRevision = revision;
        }

        private void ShowCreateParameterPopup(
            Rect anchorRect,
            IReadOnlyList<ParameterType> creatableTypes,
            Action<string> onSelected)
        {
            if (GameEventDocumentEditorWindow.HasUnsavedParameterChangesInOpenWindows())
            {
                EditorUtility.DisplayDialog(
                    "Game Event Editor 有尚未儲存的參數",
                    "請先在 Game Event Editor 儲存或捨棄參數表變更，再新增參數，避免覆蓋資料。",
                    "開啟 Game Event Editor");
                GameEventDocumentEditorWindow.OpenWindow();
                return;
            }

            PopupWindow.Show(
                anchorRect,
                new CreateParameterPopupContent(
                    parameterWorkspace,
                    creatableTypes,
                    createdKey =>
                    {
                        try
                        {
                            parameterWorkspace.SaveAll();
                        }
                        catch
                        {
                            parameterWorkspace.ReloadAll();
                            throw;
                        }

                        GameEventDocumentEditorWindow.ReloadParameterWorkspacesAfterExternalSave();
                        parameterTablesRevision = null;
                        EnsureCatalog(eventCatalog);
                        onSelected(createdKey);
                        repaint();
                    }));
        }

        private static string GetParameterTablesRevision(GameEventCatalogAsset selected)
        {
            if (selected == null)
                return string.Empty;

            return string.Join(
                "|",
                selected.ParameterTables
                    .Where(asset => asset != null)
                    .Select(AssetDatabase.GetAssetPath)
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Select(path => path + ":" + AssetDatabase.GetAssetDependencyHash(path)));
        }
    }
}
