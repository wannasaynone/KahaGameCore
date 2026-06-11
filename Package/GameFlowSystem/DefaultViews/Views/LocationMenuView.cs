using System;
using System.Collections.Generic;
using KahaGameCore.UserInterfaceSystem;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;
using UnityEngine;
using UnityEngine.UI;

namespace KahaGameCore.Package.GameFlowSystem.DefaultViews
{
    /// <summary>移動選單：列出可前往的地點，附取消（返回）按鈕。</summary>
    public class LocationMenuView : AView
    {
        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private LocationButtonItem buttonPrefab;
        [SerializeField] private Button cancelButton;

        private readonly List<LocationButtonItem> spawnedButtons = new List<LocationButtonItem>();
        private Action onCancelled;

        private void Awake()
        {
            cancelButton.onClick.AddListener(() => onCancelled?.Invoke());
        }

        public void Bind(IReadOnlyList<LocationData> locations, Action<LocationData> onSelected, Action onCancelled)
        {
            this.onCancelled = onCancelled;
            ClearButtons();

            foreach (LocationData location in locations)
            {
                LocationButtonItem button = Instantiate(buttonPrefab, buttonContainer);
                button.Bind(location, onSelected);
                spawnedButtons.Add(button);
            }
        }

        private void ClearButtons()
        {
            foreach (LocationButtonItem button in spawnedButtons)
            {
                Destroy(button.gameObject);
            }
            spawnedButtons.Clear();
        }
    }
}
