Shader "Custom/2D_Sprite_Shader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // These are the things that decide the blending mode.
        [Enum(UnityEngine.Rendering.BlendMode)] _SourceBlend ("Source blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DestinationBlend ("Destination blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend mode operation", Float) = 29
        
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        
        // Per instance
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
    
        Cull Off
        Lighting Off
        ZWrite Off
        Blend [_SourceBlend] [_DestinationBlend]
        BlendOp [_BlendOp]
    
        Pass
        {
        CGPROGRAM
            //#pragma fragment frag
            #pragma fragment frag;
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            
            sampler2D _GrabTexture;
            
            float _BlendOp;
            
            fixed4 darken(v2f i);
            fixed4 hardLight(v2f i);
            fixed4 softLight(v2f i);
            
            fixed4 frag(v2f i) : SV_Target {

                // Deciding which custom blending mode to use. (There might be a better way to do this.)
                int blendOp = (int)_BlendOp;
                if (blendOp == 3) // Darken inside BlendOp enum
                {
                    fixed4 result = darken(i);
                    return result;
                }
                else if (blendOp == 28) // Hard light inside BlendOp enum
                {
                    fixed4 result = hardLight(i);
                    return result;
                }
                else if (blendOp == 29) // Soft light inside BlendOp enum
                {
                    fixed4 result = softLight(i);
                    return result;
                }
                
                return SpriteFrag(i); // Return the default sprite.
            }
            
            fixed4 darken(v2f i)
            {
                fixed4 spriteCol = SpriteFrag(i);
                fixed4 destCol = tex2D(_GrabTexture, i.texcoord);
                fixed3 darkened = min(spriteCol.rgb, destCol.rgb);

                fixed4 outColor;
                outColor.a = spriteCol.a;
                // Interpolate between destination and darkened result based on sprite alpha.
                outColor.rgb = lerp(destCol.rgb, darkened, spriteCol.a);
                return outColor;
            }
            
            // Hard Light blend function.
            fixed4 hardLight(v2f i)
            {
                fixed4 spriteCol = SpriteFrag(i);    // The sprite’s color (blend)
                
                fixed4 result = spriteCol;
                
                // Apply brightness compensation.
                float brightnessFactor = 5; // Adjust this factor as needed. (Currently fits my tests.)
                result *= brightnessFactor;

                fixed4 outColor;
                outColor.rgb = result;
                outColor.a   = spriteCol.a;
                return outColor;
            }

                        // Hard Light blend function.
            fixed4 softLight(v2f i)
            {
                fixed4 spriteCol = SpriteFrag(i);
                
                fixed4 result = spriteCol;
                
                // Apply brightness compensation.
                float brightnessFactor = 9; // Adjust this factor as needed. (Currently fits my tests.)
                result *= brightnessFactor;

                fixed luminance = dot(result.rgb, fixed3(0.299, 0.587, 0.114));
                fixed3 gray = fixed3(luminance, luminance, luminance);
                
                // Lerp between the original color and its grayscale equivalent.
                // amount = 0 gives original color, amount = 1 gives full grayscale.
                fixed3 desaturated = lerp(result.rgb, gray, 0.25f);
                
                // Preserve the alpha channel.
                result = fixed4(desaturated, result.a);

                fixed4 outColor;
                outColor.rgb = result;
                outColor.a   = spriteCol.a;
                return outColor;
            }
        ENDCG
        }
    }
}