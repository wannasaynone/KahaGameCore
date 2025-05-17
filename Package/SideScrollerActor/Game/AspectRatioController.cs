using UnityEngine;

public class AspectRatioController : MonoBehaviour
{
    [SerializeField] private float targetAspectRatio = 16f / 9f;
    private Camera mainCamera;

    // 添加這兩個變數
    private Camera backgroundCamera;
    private Rect lastRect;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
            mainCamera = Camera.main;

        // 創建背景Camera
        GameObject bgCam = new GameObject("BackgroundCamera");
        backgroundCamera = bgCam.AddComponent<Camera>();
        backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
        backgroundCamera.backgroundColor = Color.black;
        backgroundCamera.depth = mainCamera.depth - 1;
        backgroundCamera.cullingMask = 0;
    }

    void Update()
    {
        // 計算當前視窗的長寬比
        float currentAspectRatio = (float)Screen.width / Screen.height;

        // 計算適配比例
        float scaleHeight = currentAspectRatio / targetAspectRatio;

        Rect rect = new Rect();

        // 調整相機的視圖矩形
        if (scaleHeight < 1.0f)
        {
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
        }

        // 檢查是否變更
        if (rect != lastRect)
        {
            mainCamera.rect = rect;
            lastRect = rect;

            // 強制清理整個螢幕
            GL.Clear(true, true, Color.black);
        }
    }

    // 確保在切換場景時不會出現殘影
    void OnPreRender()
    {
        GL.Clear(true, true, Color.black);
    }
}