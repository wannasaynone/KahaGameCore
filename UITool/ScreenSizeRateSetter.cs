using UnityEngine;
using UnityEngine.UI;

namespace KahaGameCore.UITool
{
    [RequireComponent(typeof(CanvasScaler))]
    public class ScreenSizeRateSetter : MonoBehaviour
    {
        private void Awake()
        {
            CanvasScaler _canvasScaler = GetComponent<CanvasScaler>();

            float _screenWidthScale = Screen.width / _canvasScaler.referenceResolution.x;
            float _screenHeightScale = Screen.height / _canvasScaler.referenceResolution.y;
            _canvasScaler.matchWidthOrHeight = _screenWidthScale > _screenHeightScale ? 1 : 0;

            Debug.Log("[ScreenSizeRateSetter] Worked on " + gameObject.name + "(Game Object Instance ID=" + gameObject.GetInstanceID() + ")");
        }
    }
}

