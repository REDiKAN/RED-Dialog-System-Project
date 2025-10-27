Shader "UI/SoftClip"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _ClipLeft("Clip Left", Range(0,1)) = 0
        _ClipRight("Clip Right", Range(0,1)) = 1
        _ClipTop("Clip Top", Range(0,1)) = 1
        _ClipBottom("Clip Bottom", Range(0,1)) = 0
        _Feather("Feather", Range(0,0.5)) = 0.1
    }

        SubShader
        {
            Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
            Cull Off Lighting Off ZWrite Off ZTest[unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"
                #include "UnityUI.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 worldPosition : TEXCOORD1;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                fixed4 _Color;
                fixed4 _TextureSampleAdd;
                float4 _ClipRect;
                float _ClipLeft, _ClipRight, _ClipTop, _ClipBottom;
                float _Feather;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.worldPosition = v.vertex;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = v.texcoord;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    half2 uv = i.texcoord;
                    half2 clipUV = uv;

                    // Применяем обрезку с feather
                    float leftAlpha = smoothstep(_ClipLeft, _ClipLeft + _Feather, clipUV.x);
                    float rightAlpha = smoothstep(_ClipRight - _Feather, _ClipRight, 1 - clipUV.x);
                    float bottomAlpha = smoothstep(_ClipBottom, _ClipBottom + _Feather, clipUV.y);
                    float topAlpha = smoothstep(_ClipTop - _Feather, _ClipTop, 1 - clipUV.y);

                    float alpha = min(min(leftAlpha, rightAlpha), min(bottomAlpha, topAlpha));

                    fixed4 color = (tex2D(_MainTex, uv) + _TextureSampleAdd) * _Color;
                    color.a *= alpha;

                    return color;
                }
                ENDCG
            }
        }
}