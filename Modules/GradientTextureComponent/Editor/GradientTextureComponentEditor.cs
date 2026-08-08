using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ProjectTentacle.Tools
{
    public class GradientTextureComponentEditorUtils
    {
        [MenuItem("GameObject/Kaha Game Core/Gradient Texture Component")]
        private static void CreateGradientTextureComponent()
        {
            GameObject go = new GameObject("Gradient Texture Component");
            go.AddComponent<GradientTextureComponent>();
            Selection.activeGameObject = go;
        }
    }

    [CustomEditor(typeof(GradientTextureComponent))]
    public class GradientTextureComponentEditor : Editor
    {
        private Texture2D previewTexture;
        private bool isDraggingStart = false;
        private bool isDraggingEnd = false;
        private readonly Color handleColor = new Color(1, 1, 1, 0.8f);
        private readonly Color handleSelectedColor = new Color(1, 0.8f, 0, 1);
        private readonly float handleRadius = 8f;
        private SerializedProperty gradientProp;
        private SerializedProperty startPointProp;
        private SerializedProperty endPointProp;
        private SerializedProperty textureWidthProp;
        private SerializedProperty textureHeightProp;
        private SerializedProperty renderModeProp;

        private int lastTextureWidth;
        private int lastTextureHeight;

        private bool isEditing = false;

        private void OnEnable()
        {
            // Get serialized properties
            gradientProp = serializedObject.FindProperty("gradient");
            startPointProp = serializedObject.FindProperty("startPoint");
            endPointProp = serializedObject.FindProperty("endPoint");
            textureWidthProp = serializedObject.FindProperty("textureWidth");
            textureHeightProp = serializedObject.FindProperty("textureHeight");
            renderModeProp = serializedObject.FindProperty("renderMode");

            // Generate initial preview
            GeneratePreview();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Gradient Texture Component", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Gradient editor
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(gradientProp, new GUIContent("Gradient"));
            if (EditorGUI.EndChangeCheck())
            {
                GeneratePreview();
            }

            EditorGUILayout.Space();

            // Texture settings
            EditorGUILayout.LabelField("Texture Settings", EditorStyles.boldLabel);

            // Render mode selection
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(renderModeProp, new GUIContent("Render Mode"));
            if (EditorGUI.EndChangeCheck())
            {
                isEditing = false;
                GeneratePreview();
            }

            // Texture size input
            EditorGUI.BeginChangeCheck();
            int newWidth = EditorGUILayout.IntField("Texture Width", textureWidthProp.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                textureWidthProp.intValue = Mathf.Clamp(newWidth, 1, 4096);

                if (lastTextureWidth == 0)
                {
                    lastTextureWidth = newWidth;
                }
                else
                {
                    lastTextureWidth = newWidth;
                    isEditing = false;
                }

                GeneratePreview();
            }

            EditorGUI.BeginChangeCheck();
            int newHeight = EditorGUILayout.IntField("Texture Height", textureHeightProp.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                textureHeightProp.intValue = Mathf.Clamp(newHeight, 1, 4096);

                if (lastTextureHeight == 0)
                {
                    lastTextureHeight = newHeight;
                }
                else
                {
                    lastTextureHeight = newHeight;
                    isEditing = false;
                }

                GeneratePreview();
            }

            // Preview with control points
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview and Direction Control", EditorStyles.boldLabel);
            // Help text based on render mode
            GradientRenderMode mode = (GradientRenderMode)renderModeProp.enumValueIndex;
            if (mode == GradientRenderMode.Linear)
            {
                EditorGUILayout.HelpBox("Drag the white circles to set gradient direction", MessageType.Info);
            }
            else // Radius mode
            {
                EditorGUILayout.HelpBox("Drag the white circles to set center (first circle) and radius (second circle)", MessageType.Info);
            }

            if (previewTexture != null)
            {
                // Create a preview area with proper aspect ratio
                float maxSize = Mathf.Min(EditorGUIUtility.currentViewWidth - 40, 300);
                float aspectRatio = (float)textureWidthProp.intValue / textureHeightProp.intValue;

                float width, height;
                if (aspectRatio >= 1.0f)
                {
                    // Wider than tall
                    width = maxSize;
                    height = width / aspectRatio;
                }
                else
                {
                    // Taller than wide
                    height = maxSize;
                    width = height * aspectRatio;
                }

                Rect previewRect = EditorGUILayout.GetControlRect(false, height);
                previewRect.width = width;

                // Draw the preview texture
                EditorGUI.DrawPreviewTexture(previewRect, previewTexture);

                // Handle control points
                HandleControlPoints(previewRect);

                // Draw the control points
                Vector2 startPoint = startPointProp.vector2Value;
                Vector2 endPoint = endPointProp.vector2Value;

                // In Unity's editor GUI, Y increases downward, but we want to display the control points
                // in a way that matches the texture coordinates where Y increases upward
                Vector2 startPos = new Vector2(
                    previewRect.x + startPoint.x * previewRect.width,
                    previewRect.y + (1.0f - startPoint.y) * previewRect.height
                );
                Vector2 endPos = new Vector2(
                    previewRect.x + endPoint.x * previewRect.width,
                    previewRect.y + (1.0f - endPoint.y) * previewRect.height
                );

                // Draw black outlines
                Handles.color = Color.black;
                Handles.DrawSolidDisc(startPos, Vector3.forward, handleRadius + 2);
                Handles.DrawSolidDisc(endPos, Vector3.forward, handleRadius + 2);

                // Draw the handles
                Handles.color = isDraggingStart ? handleSelectedColor : handleColor;
                Handles.DrawSolidDisc(startPos, Vector3.forward, handleRadius);
                Handles.color = isDraggingEnd ? handleSelectedColor : handleColor;
                Handles.DrawSolidDisc(endPos, Vector3.forward, handleRadius);
            }

            // Apply changes
            serializedObject.ApplyModifiedProperties();

            // Regenerate button
            EditorGUILayout.Space();
            if (!isEditing)
            {
                isEditing = true;
                GeneratePreview();

                // Also apply to SpriteRenderer if available
                GradientTextureComponent component = (GradientTextureComponent)target;
                SpriteRenderer spriteRenderer = component.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && previewTexture != null)
                {
                    // Create a sprite from the texture with no border
                    Sprite generatedSprite = Sprite.Create(
                        previewTexture,
                        new Rect(0, 0, previewTexture.width, previewTexture.height),
                        new Vector2(0.5f, 0.5f)
                    );

                    // Apply sprite to the sprite renderer
                    Undo.RecordObject(spriteRenderer, "Apply Gradient Sprite");
                    spriteRenderer.sprite = generatedSprite;

                    // Mark the scene as dirty
                    EditorUtility.SetDirty(spriteRenderer);
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }
        }

        public void OnSceneGUI()
        {
            GradientTextureComponent component = (GradientTextureComponent)target;
            Transform transform = component.transform;

            // Generate preview texture if needed
            if (previewTexture == null)
            {
                GeneratePreview();
            }

            // Calculate the size based on texture dimensions and pixels per unit
            float pixelsPerUnit = 100f; // Same as in the runtime Sprite.Create
            float worldWidth = textureWidthProp.intValue / pixelsPerUnit;
            float worldHeight = textureHeightProp.intValue / pixelsPerUnit;

            // Calculate world positions for handles
            Vector3 worldPos = transform.position;

            // Draw resize handles in the scene view
            EditorGUI.BeginChangeCheck();

            // Draw width handle (right side)
            Vector3 rightHandlePos = transform.position + new Vector3(worldWidth / 2, 0, 0);
            float handleSize = HandleUtility.GetHandleSize(rightHandlePos) * 0.1f;
            Vector3 newRightPos = Handles.Slider(
                rightHandlePos,
                Vector3.right,
                handleSize,
                Handles.ArrowHandleCap,
                0.1f
            );

            // Draw height handle (top side)
            Vector3 topHandlePos = transform.position + new Vector3(0, worldHeight / 2, 0);
            Vector3 newTopPos = Handles.Slider(
                topHandlePos,
                Vector3.up,
                handleSize,
                Handles.ArrowHandleCap,
                0.1f
            );

            // Draw corner handle (top-right corner)
            Vector3 cornerHandlePos = transform.position + new Vector3(worldWidth / 2, worldHeight / 2, 0);
            Vector3 newCornerPos = Handles.FreeMoveHandle(
                cornerHandlePos,
                handleSize,
                Vector3.zero,
                Handles.RectangleHandleCap
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(component, "Resize Gradient Texture");

                // Calculate new width and height based on handle positions
                float newWidth = 0;
                float newHeight = 0;

                // Handle right side drag
                if (newRightPos != rightHandlePos)
                {
                    newWidth = (newRightPos.x - transform.position.x) * 2 * pixelsPerUnit;
                    textureWidthProp.intValue = Mathf.Clamp(Mathf.RoundToInt(newWidth), 1, 4096);
                }

                // Handle top side drag
                if (newTopPos != topHandlePos)
                {
                    newHeight = (newTopPos.y - transform.position.y) * 2 * pixelsPerUnit;
                    textureHeightProp.intValue = Mathf.Clamp(Mathf.RoundToInt(newHeight), 1, 4096);
                }

                // Handle corner drag
                if (newCornerPos != cornerHandlePos)
                {
                    newWidth = (newCornerPos.x - transform.position.x) * 2 * pixelsPerUnit;
                    newHeight = (newCornerPos.y - transform.position.y) * 2 * pixelsPerUnit;
                    textureWidthProp.intValue = Mathf.Clamp(Mathf.RoundToInt(newWidth), 1, 4096);
                    textureHeightProp.intValue = Mathf.Clamp(Mathf.RoundToInt(newHeight), 1, 4096);
                }

                // Apply changes
                serializedObject.ApplyModifiedProperties();
                GeneratePreview();

                // Update sprite renderer
                SpriteRenderer spriteRenderer = component.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && previewTexture != null)
                {
                    Sprite generatedSprite = Sprite.Create(
                        previewTexture,
                        new Rect(0, 0, previewTexture.width, previewTexture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    spriteRenderer.sprite = generatedSprite;
                }
            }
        }

        private void HandleControlPoints(Rect previewRect)
        {
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            // Check if mouse is inside the preview rect
            if (previewRect.Contains(mousePos))
            {
                // Calculate normalized positions
                Vector2 normalizedPos = new Vector2(
                    (mousePos.x - previewRect.x) / previewRect.width,
                    (mousePos.y - previewRect.y) / previewRect.height
                );
                normalizedPos.x = Mathf.Clamp01(normalizedPos.x);
                normalizedPos.y = Mathf.Clamp01(normalizedPos.y);

                // Calculate distances to handles
                Vector2 startPoint = startPointProp.vector2Value;
                Vector2 endPoint = endPointProp.vector2Value;

                // In Unity's editor GUI, Y increases downward, but we want to display the control points
                // in a way that matches the texture coordinates where Y increases upward
                Vector2 startPos = new Vector2(
                    previewRect.x + startPoint.x * previewRect.width,
                    previewRect.y + (1.0f - startPoint.y) * previewRect.height
                );
                Vector2 endPos = new Vector2(
                    previewRect.x + endPoint.x * previewRect.width,
                    previewRect.y + (1.0f - endPoint.y) * previewRect.height
                );
                float distToStart = Vector2.Distance(mousePos, startPos);
                float distToEnd = Vector2.Distance(mousePos, endPos);

                // Handle mouse down
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (distToStart < handleRadius)
                    {
                        isDraggingStart = true;
                        e.Use();
                    }
                    else if (distToEnd < handleRadius)
                    {
                        isDraggingEnd = true;
                        e.Use();
                    }
                }
                // Handle mouse drag
                else if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    if (isDraggingStart)
                    {
                        // Flip the Y coordinate to match the texture coordinates
                        Vector2 flippedPos = new Vector2(normalizedPos.x, 1.0f - normalizedPos.y);
                        startPointProp.vector2Value = flippedPos;
                        GeneratePreview();
                        e.Use();
                    }
                    else if (isDraggingEnd)
                    {
                        // Flip the Y coordinate to match the texture coordinates
                        Vector2 flippedPos = new Vector2(normalizedPos.x, 1.0f - normalizedPos.y);
                        endPointProp.vector2Value = flippedPos;
                        GeneratePreview();
                        e.Use();
                    }
                }
            }

            // Handle mouse up (even outside the preview rect)
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (isDraggingStart || isDraggingEnd)
                {
                    isDraggingStart = false;
                    isDraggingEnd = false;
                    e.Use();
                }
            }
        }

        private void GeneratePreview()
        {
            GradientTextureComponent component = (GradientTextureComponent)target;

            // Create or recreate the preview texture
            int textureWidth = textureWidthProp.intValue;
            int textureHeight = textureHeightProp.intValue;

            if (previewTexture == null || previewTexture.width != textureWidth || previewTexture.height != textureHeight)
            {
                if (previewTexture != null)
                {
                    DestroyImmediate(previewTexture);
                }
                previewTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
                previewTexture.filterMode = FilterMode.Bilinear;
                previewTexture.wrapMode = TextureWrapMode.Clamp;
            }

            // Generate the gradient texture
            Vector2 startPoint = startPointProp.vector2Value;
            Vector2 endPoint = endPointProp.vector2Value;
            Vector2 direction = (endPoint - startPoint).normalized;
            float length = Vector2.Distance(startPoint, endPoint);

            // Get the gradient from the component
            Gradient gradient = component.GetGradient();

            // If the gradient has no keys, initialize it with default values
            if (gradient.colorKeys.Length == 0)
            {
                // Create a default gradient
                GradientColorKey[] colorKeys = new GradientColorKey[2];
                colorKeys[0] = new GradientColorKey(Color.white, 0.0f);
                colorKeys[1] = new GradientColorKey(Color.black, 1.0f);

                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
                alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

                gradient.SetKeys(colorKeys, alphaKeys);

                // Update the component's gradient
                component.SetGradient(gradient);
            }

            // Get the render mode
            GradientRenderMode renderMode = (GradientRenderMode)renderModeProp.enumValueIndex;

            for (int y = 0; y < previewTexture.height; y++)
            {
                for (int x = 0; x < previewTexture.width; x++)
                {
                    // Convert pixel coordinates to normalized space (0-1)
                    Vector2 pixelPos = new Vector2((float)x / previewTexture.width, (float)y / previewTexture.height);

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
                    previewTexture.SetPixel(x, y, color);
                }
            }

            // Apply changes
            previewTexture.Apply();
            Repaint();
        }
    }
}
