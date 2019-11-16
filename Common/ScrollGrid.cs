using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace KahaGameCore.Common
{
    // TODO: add infinite scroll options...

    [RequireComponent(typeof(ScrollRect))]
    public class ScrollGrid : MonoBehaviour
    {
        [SerializeField] private List<RectTransform> m_items = null;
        [SerializeField] private float m_gap = 20f;

        private ScrollRect m_scroll = null;
        private float m_minHeight = 0f;

        private void Awake()
        {
            m_scroll = GetComponent<ScrollRect>();
            m_scroll.content.anchorMax = new Vector2(0.5f, 1);
            m_scroll.content.anchorMin = new Vector2(0.5f, 0);
            m_minHeight = m_scroll.viewport.rect.height;
            m_scroll.normalizedPosition = new Vector2(0, 1);

            if (m_items == null)
            {
                return;
            }

            for(int i = 0; i < m_items.Count; i++)
            {
                m_items[i].anchorMax = new Vector2(0.5f, 1);
                m_items[i].anchorMin = new Vector2(0.5f, 1);
                m_items[i].pivot = new Vector2(0.5f, 1);
            }
        }

        public void AddItem(RectTransform item)
        {
            if(m_items.Contains(item))
            {
                return;
            }

            item.SetParent(m_scroll.content);
            item.localScale = Vector3.one;

            item.anchorMax = new Vector2(0.5f, 1);
            item.anchorMin = new Vector2(0.5f, 1);
            item.pivot = new Vector2(0.5f, 1);
            m_items.Add(item);
        }

        public void RemoveItem(RectTransform item)
        {
            m_items.Remove(item);
        }

        public void Sort()
        {
            float _height = -m_gap;

            for (int i = 0; i < m_items.Count; i++)
            {
                m_items[i].anchoredPosition = new Vector3(0f, _height, 0f);
                _height -= (m_items[i].rect.height + m_gap);
            }

            if (Mathf.Abs(_height) > m_minHeight)
            {
                m_scroll.normalizedPosition = new Vector2(0, 1);
                m_scroll.content.offsetMin = new Vector2(m_scroll.content.offsetMin.x, -(Mathf.Abs(_height) - m_minHeight));
                m_scroll.normalizedPosition = new Vector2(0, 1);
            }
            else
            {
                m_scroll.content.offsetMin = new Vector2(m_scroll.content.offsetMin.x, 0f);
            }
        }
    }
}
