using UnityEngine;
using UnityEngine.UI;
using KahaGameCore.Interface;
using System;

namespace KahaGameCore.Common
{
    public abstract class ConfirmWindowBase : UIView
    {
        [SerializeField] protected GameObject m_root = null;
        [SerializeField] private Text m_text = null;
        [SerializeField] private Text m_title = null;

        private Action m_onConfirmed = null;

        public override bool IsShowing
        {
            get
            {
                return m_root.activeSelf;
            }
        }

        private Manager m_manager = null;

        public override void Show(Manager manager, bool show, Action onShown)
        {
            if (m_manager != null && m_manager != manager)
            {
                return;
            }

            if (show)
            {
                if (manager == null)
                {
                    return;
                }

                m_manager = manager;
            }
            else
            {
                m_manager = null;
            }

            OnStartToShow(show, onShown);
        }

        public override void ForceShow(Manager manager, bool show)
        {
            if (m_manager != null && m_manager != manager)
            {
                return;
            }

            if (show)
            {
                if (manager == null)
                {
                    return;
                }

                m_manager = manager;
            }
            else
            {
                m_manager = null;
            }

            m_root.SetActive(show);
        }

        protected abstract void OnStartToShow(bool show, Action onShown);

        public void SetMessage(string content, string title, Action onConfirmed)
        {
            m_text.text = content;
            m_title.text = title;
            m_onConfirmed = onConfirmed;
        }

        public void Button_Confirm()
        {
            m_onConfirmed?.Invoke();
        }
    }
}
