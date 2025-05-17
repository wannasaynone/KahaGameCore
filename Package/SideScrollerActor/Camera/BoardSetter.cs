using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Camera
{
    public class BoardSetter : MonoBehaviour
    {
        private static BoardSetter instance;

        public static float MAX_X { get { if (instance == null) return float.MaxValue; return instance.maxX; } }
        public static float MIN_X { get { if (instance == null) return float.MinValue; return instance.minX; } }

        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 10f;

        private void OnEnable()
        {
            if (instance != null)
            {
                Debug.LogError("There are multiple BoardSetter instances in the scene.");
                return;
            }

            instance = this;
        }

        public static void SetBoard(float minX, float maxX)
        {
            if (instance == null)
            {
                GameObject boardSetter = new GameObject("[BoardSetter]");
                instance = boardSetter.AddComponent<BoardSetter>();
            }

            instance.minX = minX;
            instance.maxX = maxX;
        }
    }
}