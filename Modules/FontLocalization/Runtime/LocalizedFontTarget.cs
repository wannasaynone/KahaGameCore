using System;
using TMPro;
using UnityEngine;

namespace KahaGameCore.FontLocalization
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class LocalizedFontTarget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI target;

        public TextMeshProUGUI Target
        {
            get
            {
                if (target == null)
                {
                    target = GetComponent<TextMeshProUGUI>();
                }

                return target;
            }
        }

        public void ApplyFont(TMP_FontAsset font)
        {
            if (font == null)
            {
                throw new ArgumentNullException(nameof(font));
            }

            Target.font = font;
            Target.SetAllDirty();
        }

        private void Reset()
        {
            target = GetComponent<TextMeshProUGUI>();
        }

        private void OnValidate()
        {
            if (target == null)
            {
                target = GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
