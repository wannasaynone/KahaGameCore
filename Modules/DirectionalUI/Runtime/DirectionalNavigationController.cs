using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KahaGameCore.DirectionalUI
{
    /// <summary>
    /// Owns focus movement between irregularly positioned uGUI Selectables.
    /// Input adapters call Move and Submit; this module does not poll a device.
    /// </summary>
    public sealed class DirectionalNavigationController : MonoBehaviour
    {
        [SerializeField] private RectTransform navigationRoot;
        [SerializeField] private Selectable initialSelection;
        [SerializeField, Min(1f)] private float directionalBias = 3f;

        private readonly List<Selectable> selectables = new List<Selectable>();

        public event Action<Selectable> SelectionChanged;
        public event Action<GameObject> Submitted;

        public Selectable CurrentSelection
        {
            get
            {
                GameObject selectedObject = EventSystem.current == null
                    ? null
                    : EventSystem.current.currentSelectedGameObject;
                return selectedObject == null ? null : selectedObject.GetComponent<Selectable>();
            }
        }

        private void Start()
        {
            SelectInitial();
        }

        public void Refresh()
        {
            selectables.Clear();

            Transform root = navigationRoot == null ? transform : navigationRoot;
            Selectable[] discovered = root.GetComponentsInChildren<Selectable>(true);
            foreach (Selectable selectable in discovered)
            {
                if (selectable != null)
                {
                    selectables.Add(selectable);
                }
            }
        }

        public bool SelectInitial()
        {
            Refresh();

            Selectable target = IsAvailable(initialSelection)
                ? initialSelection
                : FindFirstAvailable();
            return Select(target);
        }

        public bool Move(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            Refresh();

            Selectable current = CurrentSelection;
            if (!IsAvailable(current) || !selectables.Contains(current))
            {
                return SelectInitial();
            }

            direction.Normalize();
            Vector2 currentCenter = GetWorldCenter(current);
            Selectable best = null;
            float bestScore = float.PositiveInfinity;

            foreach (Selectable candidate in selectables)
            {
                if (candidate == current || !IsAvailable(candidate))
                {
                    continue;
                }

                Vector2 offset = GetWorldCenter(candidate) - currentCenter;
                float distance = offset.magnitude;
                if (distance < 0.001f)
                {
                    continue;
                }

                float alignment = Vector2.Dot(offset / distance, direction);
                if (alignment <= 0.01f)
                {
                    continue;
                }

                // Prefer nearby candidates, but increasingly punish candidates that
                // sit far away from the requested direction.
                float score = distance / Mathf.Pow(alignment, directionalBias);
                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return Select(best);
        }

        public bool Submit()
        {
            Selectable current = CurrentSelection;
            if (!IsAvailable(current) || EventSystem.current == null)
            {
                return false;
            }

            ExecuteEvents.Execute(
                current.gameObject,
                new BaseEventData(EventSystem.current),
                ExecuteEvents.submitHandler);
            Submitted?.Invoke(current.gameObject);
            return true;
        }

        private bool Select(Selectable target)
        {
            if (!IsAvailable(target) || EventSystem.current == null)
            {
                return false;
            }

            EventSystem.current.SetSelectedGameObject(target.gameObject);
            SelectionChanged?.Invoke(target);
            return true;
        }

        private Selectable FindFirstAvailable()
        {
            foreach (Selectable selectable in selectables)
            {
                if (IsAvailable(selectable))
                {
                    return selectable;
                }
            }

            return null;
        }

        private static bool IsAvailable(Selectable selectable)
        {
            return selectable != null
                && selectable.gameObject.activeInHierarchy
                && selectable.IsActive()
                && selectable.IsInteractable();
        }

        private static Vector2 GetWorldCenter(Selectable selectable)
        {
            RectTransform rectTransform = selectable.transform as RectTransform;
            if (rectTransform == null)
            {
                Vector3 position = selectable.transform.position;
                return new Vector2(position.x, position.y);
            }

            Vector3 center = rectTransform.TransformPoint(rectTransform.rect.center);
            return new Vector2(center.x, center.y);
        }
    }
}
