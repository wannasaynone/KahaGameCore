using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.UITool
{
    public class TrackCharacterUIBase : MonoBehaviour
    {
        [SerializeField] private Vector2 m_offset = default;

        public GameObject target = null;

        private RectTransform m_rect;

        private void Awake()
        {
            transform.SetParent(MainCanvas.Instance.MainRectTransform);
            transform.localScale = Vector3.one;
            ForceUpdate();
        }

        public void ForceUpdate()
        {
            Update();
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            Vector2 ViewPortPos = Camera.main.WorldToViewportPoint(target.transform.position);
            Vector2 Worldob_ScreenPos = new Vector2(
            ((ViewPortPos.x * MainCanvas.Instance.MainRectTransform.sizeDelta.x) - (MainCanvas.Instance.MainRectTransform.sizeDelta.x * 0.5f)),
            ((ViewPortPos.y * MainCanvas.Instance.MainRectTransform.sizeDelta.y) - (MainCanvas.Instance.MainRectTransform.sizeDelta.y * 0.5f)));

            if (m_rect == null)
                m_rect = GetComponent<RectTransform>();

            m_rect.anchoredPosition = Worldob_ScreenPos + m_offset;
        }
    }
}
