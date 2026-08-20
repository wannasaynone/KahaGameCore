using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace KahaGameCore.GameEvents.Editor
{
    internal sealed class ParameterKeyAdvancedDropdown : AdvancedDropdown
    {
        private sealed class ParameterItem : AdvancedDropdownItem
        {
            public ParameterItem(string name, string key)
                : base(name)
            {
                Key = key;
            }

            public string Key { get; }
        }

        private sealed class CreateItem : AdvancedDropdownItem
        {
            public CreateItem()
                : base("＋ 新增參數…")
            {
            }
        }

        private readonly IReadOnlyList<ParameterAuthoringEntry> entries;
        private readonly Action<string> onSelected;
        private readonly Action onCreate;

        public ParameterKeyAdvancedDropdown(
            AdvancedDropdownState state,
            IReadOnlyList<ParameterAuthoringEntry> entries,
            Action<string> onSelected,
            Action onCreate)
            : base(state)
        {
            this.entries = entries ?? throw new ArgumentNullException(nameof(entries));
            this.onSelected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
            this.onCreate = onCreate ?? throw new ArgumentNullException(nameof(onCreate));
            minimumSize = new Vector2(420f, 320f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("選擇參數");
            root.AddChild(new CreateItem());
            HashSet<string> duplicateDisplayNames = entries
                .GroupBy(entry => entry.TableDisplayName, StringComparer.Ordinal)
                .Where(group => group.Select(entry => entry.AssetPath).Distinct().Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.Ordinal);

            foreach (IGrouping<string, ParameterAuthoringEntry> tableEntries in
                     entries.GroupBy(entry => entry.AssetPath))
            {
                ParameterAuthoringEntry first = tableEntries.First();
                string tableLabel = first.TableDisplayName;
                if (duplicateDisplayNames.Contains(tableLabel))
                {
                    tableLabel += " — " + Path.GetFileName(first.AssetPath);
                }

                AdvancedDropdownItem tableItem = new AdvancedDropdownItem(tableLabel);
                foreach (ParameterAuthoringEntry entry in tableEntries
                             .OrderBy(item => item.Definition.Key, StringComparer.Ordinal))
                {
                    tableItem.AddChild(new ParameterItem(
                        FormatEntryLabel(entry),
                        entry.Definition.Key));
                }

                root.AddChild(tableItem);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is ParameterItem parameterItem)
            {
                onSelected(parameterItem.Key);
            }
            else if (item is CreateItem)
            {
                onCreate();
            }
        }

        internal static string FormatEntryLabel(ParameterAuthoringEntry entry)
        {
            string displayName = string.IsNullOrWhiteSpace(entry.Definition.DisplayName)
                ? entry.Definition.Key
                : entry.Definition.DisplayName;
            return $"{displayName}　（${entry.Definition.Key}）";
        }
    }
}
