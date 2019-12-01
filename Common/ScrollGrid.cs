using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace KahaGameCore.Common
{
    // TODO: add infinite scroll options...

    [RequireComponent(typeof(ScrollRect))]
    public class ScrollGrid : MonoBehaviour
    {
        public RectTransform ContentParent { get { return m_scroll.content; } }

        [SerializeField] private float m_gap = 20f;

        private ScrollRect m_scroll = null;
        private RectTransform[] m_allChildren = null;
        private float m_minHeight = 0f;

        private void Awake()
        {
            m_scroll = GetComponent<ScrollRect>();
            m_scroll.content.anchorMax = new Vector2(0.5f, 1);
            m_scroll.content.anchorMin = new Vector2(0.5f, 0);
            m_minHeight = m_scroll.viewport.rect.height;
            m_scroll.normalizedPosition = new Vector2(0, 1);

            Sort();
        }

        public void UpdateChildrenInfo()
        {
            RectTransform[] _allRect = m_scroll.content.GetComponentsInChildren<RectTransform>();
            m_allChildren = new RectTransform[_allRect.Length - 1];
            for (int i = 0; i < m_allChildren.Length; i++)
            {
                m_allChildren[i] = _allRect[i + 1];
                m_allChildren[i].localScale = Vector3.one;
                m_allChildren[i].anchorMax = new Vector2(0.5f, 1);
                m_allChildren[i].anchorMin = new Vector2(0.5f, 1);
                m_allChildren[i].pivot = new Vector2(0.5f, 1);
            }
        }

        public void Sort()
        {
            float _height = -m_gap;

            if(m_allChildren == null)
            {
                UpdateChildrenInfo();
            }

            for (int i = 0; i < m_allChildren.Length; i++)
            {
                m_allChildren[i].anchoredPosition = new Vector3(0f, _height, 0f);
                _height -= (m_allChildren[i].rect.height + m_gap);
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
