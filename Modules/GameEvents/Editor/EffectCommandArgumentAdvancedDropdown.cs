using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace KahaGameCore.GameEvents.Editor
{
    internal sealed class EffectCommandArgumentAdvancedDropdown : AdvancedDropdown
    {
        private sealed class OptionItem : AdvancedDropdownItem
        {
            public OptionItem(EffectCommandArgumentOption option)
                : base(option.Label)
            {
                Option = option;
            }

            public EffectCommandArgumentOption Option { get; }
        }

        private readonly string title;
        private readonly IReadOnlyList<EffectCommandArgumentOption> options;
        private readonly Action<string> onSelected;

        public EffectCommandArgumentAdvancedDropdown(
            AdvancedDropdownState state,
            string title,
            IReadOnlyList<EffectCommandArgumentOption> options,
            Action<string> onSelected)
            : base(state)
        {
            this.title = string.IsNullOrWhiteSpace(title) ? "選擇項目" : title;
            this.options = options ?? Array.Empty<EffectCommandArgumentOption>();
            this.onSelected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
            minimumSize = new UnityEngine.Vector2(420f, 320f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(title);
            var groups = new Dictionary<string, AdvancedDropdownItem>(
                StringComparer.Ordinal);
            foreach (EffectCommandArgumentOption option in options
                         .OrderBy(item => item.Group, StringComparer.Ordinal)
                         .ThenBy(item => item.Label, StringComparer.Ordinal))
            {
                AdvancedDropdownItem parent = root;
                string currentPath = string.Empty;
                foreach (string segment in SplitGroup(option.Group))
                {
                    currentPath = currentPath.Length == 0
                        ? segment
                        : currentPath + "/" + segment;
                    if (!groups.TryGetValue(currentPath, out AdvancedDropdownItem group))
                    {
                        group = new AdvancedDropdownItem(segment);
                        groups.Add(currentPath, group);
                        parent.AddChild(group);
                    }

                    parent = group;
                }

                parent.AddChild(new OptionItem(option));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is OptionItem optionItem)
            {
                onSelected(optionItem.Option.Value);
            }
        }

        private static IEnumerable<string> SplitGroup(string group)
        {
            return (group ?? string.Empty)
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => segment.Trim())
                .Where(segment => segment.Length > 0);
        }
    }
}
