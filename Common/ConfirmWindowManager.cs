using System.Collections.Generic;
using System;

namespace KahaGameCore.Common
{
    public class ConfirmWindowManager : Interface.Manager
    {
        private ConfirmWindowBase m_window = null;

        private struct MessageObject
        {
            public string title;
            public string content;
            public Action onConfirmed;
        }
        private Queue<MessageObject> m_messageQueue = new Queue<MessageObject>();

        public ConfirmWindowManager(ConfirmWindowBase window)
        {
            m_window = window;
        }

        public void ShowCommonMessage(string content, string title, Action onConfirmed)
        {
            if(m_window == null)
            {
                return;
            }

            MessageObject _messageObject = new MessageObject()
            {
                title = title,
                content = content,
                onConfirmed = onConfirmed
            };

            _messageObject.onConfirmed += CloseWindow;
            m_messageQueue.Enqueue(_messageObject);

            if(!m_window.IsShowing)
            {
                ShowNext();
            }
        }

        private void ShowNext()
        {
            MessageObject _messageObject = m_messageQueue.Dequeue();
            m_window.SetMessage(_messageObject.content, _messageObject.title, _messageObject.onConfirmed);
            if (!m_window.IsShowing)
            {
                m_window.Show(this, true, null);
            }
        }

        private void CloseWindow()
        {
            if(m_messageQueue.Count <= 0)
            {
                m_window.Show(this, false, null);
            }
            else
            {
                ShowNext();
            }
        }
    }
}

