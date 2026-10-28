// Sprite.fx : effects drawn on one picture at a time with SpriteBatch (Beta1). Used through Core/Fx.cs.
//
// ADVANCED PART : SpriteBatch already knows how to place a picture on the screen. Passing this
// effect to SpriteBatch.Begin only changes how each pixel of the picture is coloured.
//   Flash      : the picture turns towards one colour, keeping its shape (a hit makes it go white)
//   Silhouette : the picture becomes one flat colour (the black and white IMPACT FRAME)
//   Dissolve   : the picture eats away from noise, with a glowing rim (an enemy SILENCED into the dark)

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_3
    #define PS_SHADERMODEL ps_4_0_level_9_3
#endif

Texture2D SpriteTexture;
sampler2D SpriteSampler : register(s0) = sampler_state
{
    Texture = <SpriteTexture>;
};

float4 FlashColor;      // Flash : the colour, its alpha is how far to go
float4 SolidColor;      // Silhouette : the flat colour
float Amount;           // Dissolve : 0 = whole, 1 = gone
float4 RimColor;        // Dissolve : the glow along the edge being eaten
float NoiseScale;       // Dissolve : how big the holes are, in pixels of the picture

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};

float Hash(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

// Value Noise : smooth random hills, made by blending the hash of the four corners around a point
float Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = Hash(i);
    float b = Hash(i + float2(1.0, 0.0));
    float c = Hash(i + float2(0.0, 1.0));
    float d = Hash(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float4 FlashPS(VertexShaderOutput input) : COLOR0
{
    float4 c = tex2D(SpriteSampler, input.UV) * input.Color;
    c.rgb = lerp(c.rgb, FlashColor.rgb * c.a, FlashColor.a);
    return c;
}

float4 SilhouettePS(VertexShaderOutput input) : COLOR0
{
    float a = tex2D(SpriteSampler, input.UV).a * input.Color.a;
    return float4(SolidColor.rgb * a, a);
}

float4 DissolvePS(VertexShaderOutput input) : COLOR0
{
    float4 c = tex2D(SpriteSampler, input.UV) * input.Color;
    float n = Noise(input.UV * NoiseScale) * 0.7 + Noise(input.UV * NoiseScale * 2.7) * 0.3;
    float cut = n - Amount;
    if (cut < 0.0) return float4(0, 0, 0, 0);
    float rim = 1.0 - saturate(cut / 0.08);
    c.rgb = lerp(c.rgb, RimColor.rgb * c.a, rim * RimColor.a);
    return c;
}

technique Flash
{
    pass P0 { PixelShader = compile PS_SHADERMODEL FlashPS(); }
};

technique Silhouette
{
    pass P0 { PixelShader = compile PS_SHADERMODEL SilhouettePS(); }
};

technique Dissolve
{
    pass P0 { PixelShader = compile PS_SHADERMODEL DissolvePS(); }
};
