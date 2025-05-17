#if USING_URP
using CustomBlendModes;
#endif
using UnityEngine;

namespace ProjectTentacle.Tools
{
    public enum GradientRenderMode
    {
        Linear,
        Radius
    }

#if USING_URP
    [RequireComponent(typeof(SpriteRenderer), typeof(SpriteBlendingMode))]
#else
    [RequireComponent(typeof(SpriteRenderer))]
#endif
    public class GradientTextureComponent : MonoBehaviour
    {
        [SerializeField] private Gradient gradient = new Gradient();
        [SerializeField] private Vector2 startPoint = new Vector2(0.2f, 0.5f);
        [SerializeField] private Vector2 endPoint = new Vector2(0.8f, 0.5f);
        [SerializeField] private int textureWidth = 256;
        [SerializeField] private int textureHeight = 256;
        [SerializeField] private GradientRenderMode renderMode = GradientRenderMode.Linear;
        [SerializeField] private bool showPreviewInGameScene = false;
        [SerializeField] private float previewSize = 100f;

        private Texture2D generatedTexture;
        private Sprite generatedSprite;
        private SpriteRenderer spriteRenderer;

        // Initialize default gradient in Awake
        private void Awake()
        {
            // Only initialize if the gradient has no keys (is empty)
            if (gradient.colorKeys.Length == 0)
            {
                GradientColorKey[] colorKeys = new GradientColorKey[2];
                colorKeys[0] = new GradientColorKey(Color.white, 0.0f);
                colorKeys[1] = new GradientColorKey(Color.black, 1.0f);

                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
                alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

                gradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        // Optional preview in game scene
        private void OnGUI()
        {
            if (Application.isPlaying && showPreviewInGameScene && generatedTexture != null)
            {
                // Calculate screen position based on the object's position
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);

                // Convert to GUI coordinates (flip Y)
                screenPos.y = Screen.height - screenPos.y;

                // Draw the preview
                Rect previewRect = new Rect(
                    screenPos.x - previewSize / 2,
                    screenPos.y - previewSize / 2,
                    previewSize,
                    previewSize
                );

                // Draw the texture preview
                // Note: GUI.DrawTexture doesn't support custom materials directly
                GUI.DrawTexture(previewRect, generatedTexture);
            }
        }

        // Generate sprite at runtime
        private void Start()
        {
            // Get the sprite renderer component
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("No SpriteRenderer component found on this GameObject!");
                return;
            }

            // Generate texture
            generatedTexture = GenerateTexture();

            // Create sprite from texture with no border
            generatedSprite = Sprite.Create(
                generatedTexture,
                new Rect(0, 0, generatedTexture.width, generatedTexture.height),
                new Vector2(0.5f, 0.5f)
            );

            // Apply sprite to the sprite renderer
            spriteRenderer.sprite = generatedSprite;
        }

        // Clean up resources when destroyed
        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (generatedTexture != null)
                {
                    Destroy(generatedTexture);
                }
                if (generatedSprite != null)
                {
                    Destroy(generatedSprite);
                }
            }
            else
            {
                if (generatedTexture != null)
                {
                    DestroyImmediate(generatedTexture);
                }
                if (generatedSprite != null)
                {
                    DestroyImmediate(generatedSprite);
                }
            }
        }

        // Generate texture based on gradient and points
        public Texture2D GenerateTexture()
        {
            // Create or recreate the texture
            if (generatedTexture == null || generatedTexture.width != textureWidth || generatedTexture.height != textureHeight)
            {
                if (generatedTexture != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(generatedTexture);
                    }
                    else
                    {
                        DestroyImmediate(generatedTexture);
                    }
                }
                generatedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
                generatedTexture.filterMode = FilterMode.Bilinear;
                generatedTexture.wrapMode = TextureWrapMode.Clamp;
            }

            // Generate the gradient texture based on render mode
            float length = Vector2.Distance(startPoint, endPoint);
            Vector2 direction = (endPoint - startPoint).normalized;

            for (int y = 0; y < generatedTexture.height; y++)
            {
                for (int x = 0; x < generatedTexture.width; x++)
                {
                    // Convert pixel coordinates to normalized space (0-1)
                    Vector2 pixelPos = new Vector2((float)x / generatedTexture.width, (float)y / generatedTexture.height);

                    float gradientPos;

                    if (renderMode == GradientRenderMode.Linear)
                    {
                        // Linear mode - project pixel onto gradient line

                        // Calculate vector from start to pixel
                        Vector2 pixelToStart = pixelPos - startPoint;

                        // Project this vector onto the direction vector
                        float projection = Vector2.Dot(pixelToStart, direction);

                        // Normalize by the length of the gradient line
                        gradientPos = Mathf.Clamp01(projection / length);
                    }
                    else // Radius mode
                    {
                        // Radius mode - use distance from center (startPoint)
                        // Calculate distance from pixel to center (startPoint)
                        float distance = Vector2.Distance(pixelPos, startPoint);

                        // Normalize by the length of the gradient line (distance from startPoint to endPoint)
                        gradientPos = Mathf.Clamp01(distance / length);
                    }

                    // Sample the gradient at this position
                    Color color = gradient.Evaluate(gradientPos);

                    // Set the pixel color
                    generatedTexture.SetPixel(x, y, color);
                }
            }

            // Apply changes
            generatedTexture.Apply();
            return generatedTexture;
        }

        // Public getters for editor use
        public Gradient GetGradient() => gradient;
        public Vector2 GetStartPoint() => startPoint;
        public Vector2 GetEndPoint() => endPoint;
        public int GetTextureWidth() => textureWidth;
        public int GetTextureHeight() => textureHeight;
        public GradientRenderMode GetRenderMode() => renderMode;

        // Public setters for editor use
        public void SetGradient(Gradient newGradient)
        {
            gradient = newGradient;
        }

        public void SetStartPoint(Vector2 newStartPoint)
        {
            startPoint = newStartPoint;
        }

        public void SetEndPoint(Vector2 newEndPoint)
        {
            endPoint = newEndPoint;
        }

        public void SetTextureWidth(int newWidth)
        {
            textureWidth = newWidth;
        }

        public void SetTextureHeight(int newHeight)
        {
            textureHeight = newHeight;
        }

        public void SetRenderMode(GradientRenderMode newMode)
        {
            renderMode = newMode;
        }
    }
}
