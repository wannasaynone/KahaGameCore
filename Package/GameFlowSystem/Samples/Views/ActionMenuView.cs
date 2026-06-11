using System;
using System.Collections.Generic;
using KahaGameCore.Package.GameFlowSystem;
using KahaGameCore.UserInterfaceSystem;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.Samples.Views
{
    /// <summary>行動選單：依表格資料動態產生行動按鈕。</summary>
    public class ActionMenuView : AView
    {
        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private ActionButtonItem buttonPrefab;

        private readonly List<ActionButtonItem> spawnedButtons = new List<ActionButtonItem>();

        public void Bind(IReadOnlyList<ActionMenuEntry> entries, Action<ActionMenuEntry> onSelected)
        {
            ClearButtons();

            foreach (ActionMenuEntry entry in entries)
            {
                ActionButtonItem button = Instantiate(buttonPrefab, buttonContainer);
                button.Bind(entry, onSelected);
                spawnedButtons.Add(button);
            }
        }

        private void ClearButtons()
        {
            foreach (ActionButtonItem button in spawnedButtons)
            {
                Destroy(button.gameObject);
            }
            spawnedButtons.Clear();
        }
    }
}
