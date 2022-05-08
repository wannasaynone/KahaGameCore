using UnityEngine;

namespace KahaGameCore.UITool
{
    public class MainCanvas
    {
        public static MainCanvas Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new MainCanvas();
                }
                return m_instance;
            }
        }
        private static MainCanvas m_instance = null;

        public RectTransform MainRectTransform { get; private set; }

        private MainCanvas()
        {
            GameObject _mainCanvasGO = GameObject.FindGameObjectWithTag("MainCanvas");
            if (_mainCanvasGO == null)
            {
                Debug.LogError("[TrackCharacterUIBase][Awake] Can't find MainCanvas, need to create a canvas with tag \"MainCanvas\"");
                return;
            }
            MainRectTransform = _mainCanvasGO.GetComponent<RectTransform>();
        }
    }
}
