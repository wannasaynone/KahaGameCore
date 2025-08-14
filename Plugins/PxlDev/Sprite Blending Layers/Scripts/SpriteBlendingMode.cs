using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CustomBlendModes
{
    public enum BlendModes
    {
        Opaque,
        Add,
        Multiply,
        Screen,
        Overlay,
        Darken,
        Lighten,
        Subtract,
        HardLightApproximation,
        SoftLightApproximation,
    }

    [ExecuteAlways]
    public class SpriteBlendingMode : MonoBehaviour
    {
        [Header("Select Blend Mode")]
        public BlendModes blendMode = BlendModes.Opaque;
        
        // Store the last selected blend mode to only update when it changes
        private BlendModes lastBlendMode = BlendModes.Opaque;
        
        // References to the SpriteRenderer and its material instance
        private SpriteRenderer spriteRenderer;
        private Material materialInstance;

        private void Awake()
        {
            SetupMaterial();
        }

        private void OnEnable()
        {
            SetupMaterial();
        }

        private void OnValidate()
        {
            SetupMaterial();
            UpdateBlendMode();
            lastBlendMode = blendMode;
        }

        // Called once per frame
        private void Update()
        {
            // Only update material if the blend mode enum has changed
            if (lastBlendMode != blendMode)
            {
                UpdateBlendMode();
                lastBlendMode = blendMode;
            }
        }

        private void SetupMaterial()
        {
            // Get the SpriteRenderer on this GameObject
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            // Create a new instance of the material if needed
            if (spriteRenderer != null && (materialInstance == null || spriteRenderer.sharedMaterial != materialInstance))
            {
                materialInstance = new Material(Shader.Find("Custom/2D_Sprite_Shader"));
                spriteRenderer.material = materialInstance;
            }
        }

        // Set the shader properties (_SourceBlend, _DestinationBlend, _BlendOp) based on the selected blend mode
        private void UpdateBlendMode()
        {
            if (materialInstance == null)
                return;

            switch (blendMode) {
                
                case BlendModes.Opaque:
                    materialInstance.SetInt("_SourceBlend", (int)BlendMode.SrcAlpha);
                    materialInstance.SetInt("_DestinationBlend", (int)BlendMode.OneMinusSrcAlpha);
                    materialInstance.SetInt("_BlendOp", (int)BlendOp.Add);
                    break;
                case BlendModes.Add:
                    materialInstance.SetInt("_SourceBlend", (int)BlendMode.One);
                    materialInstance.SetInt("_DestinationBlend", (int)BlendMode.One);
                    materialInstance.SetInt("_BlendOp", (int)BlendOp.Add);
                    break;
                case BlendModes.Multiply:
                    // Multiply: use destination color as source factor
                    materialInstance.SetInt("_SourceBlend", (int)BlendMode.DstColor);
                    materialInstance.SetInt("_DestinationBlend", (int)BlendMode.OneMinusSrcAlpha);
                    materialInstance.SetInt("_BlendOp", (int)BlendOp.Add);
                    break;
                case BlendModes.Screen:
                    // Screen: one minus destination color
                    materialInstance.SetInt("_SourceBlend", (int)BlendMode.OneMinusDstColor);
                    materialInstance.SetInt("_DestinationBlend", (int)BlendMode.One);
                    materialInstance.SetInt("_BlendOp", (int)BlendOp.Add);
                    break;
                case BlendModes.Overlay:
                    // Overlay: use typical approximation.
                    materialInstance.SetInt("_SourceBlend", (int)BlendMode.One);
                    materialInstance.SetInt("_DestinationBlend", (int)BlendMode.OneMinusSrcColor);
                    materialInstance.SetInt("_BlendOp", (int)BlendOp.Add);
                    break;
                case BlendModes.Darken:
                    // Darken: using the minimum of source and destination
                    materialInstance.SetInt("_SourceBlend", (int)BlendMode.SrcAlpha);
                    materialInstance.SetInt("_DestinationBlend", (int)BlendMode.OneMinusSrcAlpha);
                    materialInstance.SetInt("_BlendOp", (int)BlendOp.Min);
                    break;
                case BlendModes.Lighten:
                    // Lighten: using the maximum of source and destination
                    materialInstance.SetInt("_SourceBlend", (int)BlendMode.One);
                    materialInstance.SetInt("_DestinationBlend", (int)BlendMode.One);
                    materialInstance.SetInt("_BlendOp", (int)BlendOp.Max);
                    break;
                case BlendModes.Subtract:
                    // Subtract: using reverse subtraction to achieve the effect
                    materialInstance.SetInt("_SourceBlend", (int)BlendMode.One);
                    materialInstance.SetInt("_DestinationBlend", (int)BlendMode.One);
                    materialInstance.SetInt("_BlendOp", (int)BlendOp.ReverseSubtract);
                    break;
                case BlendModes.HardLightApproximation:
                    // Subtract: using reverse subtraction to achieve the effect
                    materialInstance.SetInt("_SourceBlend", (int)BlendMode.DstColor);
                    materialInstance.SetInt("_DestinationBlend", (int)BlendMode.OneMinusSrcAlpha);
                    materialInstance.SetInt("_BlendOp", (int)BlendOp.HardLight);
                    break;
                
                case BlendModes.SoftLightApproximation:
                    // Subtract: using reverse subtraction to achieve the effect
                    materialInstance.SetInt("_SourceBlend", (int)BlendMode.DstColor);
                    materialInstance.SetInt("_DestinationBlend", (int)BlendMode.OneMinusSrcAlpha);
                    materialInstance.SetInt("_BlendOp", (int)BlendOp.SoftLight);
                    break;
            }
        }
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(SpriteBlendingMode))]
    public class SpriteBlendingModeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector (all serialized properties)
            DrawDefaultInspector();

            // Get a reference to our target script
            SpriteBlendingMode sbm = (SpriteBlendingMode)target;
            
            // Prepare a custom help message based on the selected blend mode
            string customTextNormal = "";
            string customTextWarning = "";
            
            switch (sbm.blendMode)
            {
                case BlendModes.Opaque:
                    customTextNormal = "Opaque: Uses normal alpha blending.";
                    break;
                case BlendModes.Add:
                    customTextNormal = "Add: Colors are added together.";
                    break;
                case BlendModes.Multiply:
                    customTextNormal = "Multiply: Multiplies the sprite with the background.";
                    break;
                case BlendModes.Screen:
                    customTextNormal = "Screen: Inverts, multiplies, and then inverts the colors.";
                    break;
                case BlendModes.Overlay:
                    customTextNormal = "Overlay: Combines Multiply and Screen for a dynamic effect.";
                    break;
                case BlendModes.Darken:
                    customTextNormal = "Darken: Chooses the darker of the sprite and background colors.";
                    break;
                case BlendModes.Lighten:
                    customTextNormal = "Lighten: Chooses the lighter of the sprite and background colors.";
                    break;
                case BlendModes.Subtract:
                    customTextNormal = "Subtract: Subtracts the sprite's color from the background.";
                    break;
                case BlendModes.HardLightApproximation:
                    customTextNormal = "Hard Light Approximation: A loose approximation of the actual Hard Light blend.";
                    customTextWarning = "The approximation is highly imprecise but still attempts to replicate the intended colors as closely as possible.";
                    break;
                case BlendModes.SoftLightApproximation:
                    customTextNormal = "Soft Light Approximation: A loose approximation of the actual Soft Light blend.";
                    customTextWarning = "The approximation is highly imprecise but still attempts to replicate the intended colors as closely as possible.";
                    break;
            }
            
            // If custom text is set, display it in a HelpBox
            if (!string.IsNullOrEmpty(customTextNormal))
            {
                EditorGUILayout.Separator();
                GUILayout.Label(customTextNormal, EditorStyles.selectionRect);
            }

            if (!string.IsNullOrEmpty(customTextWarning)) {
                EditorGUILayout.HelpBox(customTextWarning, MessageType.Warning);
            }
        }
    }
    #endif
}