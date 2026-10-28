// Post.fx : the battle picture's finishing passes (Beta1). Used by Core/PostChain.cs.
//
// ADVANCED PART : a shader is a small program the graphics card runs once for every pixel it draws.
// SpriteBatch hands each pass a picture (SpriteTexture) and the pixel's place in it (UV, 0..1).
// The world is drawn into its own picture first (the SCENE), the lights into another (the LIGHT MAP),
// and the effects that give off their own light (sparks, slashes, flares) into a third (the GLOW, never
// darkened by the light map). Then these passes mix them:
//   Down     : SCENE x LIGHT + GLOW, shrunk to a quarter of the screen (cheap to blur)
//   Blur     : a soft smear along one direction, run twice (across, then down)
//   Composite: SCENE x LIGHT + GLOW, plus the soft glow (BLOOM) taken from the blurred copy, then the QTE FOCUS,
//              the dark corners (VIGNETTE) and the film grain
//
// LIGHT x 2 : the light map is stored at half strength so it can go above 1. 0.5 in the picture
// means "lit normally", 1.0 means "twice as bright" (a hit flash).

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_3
    #define PS_SHADERMODEL ps_4_0_level_9_3
#endif

Texture2D SpriteTexture;
sampler2D SceneSampler : register(s0) = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Point;
    AddressU = Clamp; AddressV = Clamp;
};

Texture2D LightTexture;
sampler2D LightSampler : register(s1) = sampler_state
{
    Texture = <LightTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Point;
    AddressU = Clamp; AddressV = Clamp;
};

Texture2D SoftTexture;
sampler2D SoftSampler : register(s2) = sampler_state
{
    Texture = <SoftTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Point;
    AddressU = Clamp; AddressV = Clamp;
};

Texture2D GlowTexture;
sampler2D GlowSampler : register(s3) = sampler_state
{
    Texture = <GlowTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Point;
    AddressU = Clamp; AddressV = Clamp;
};

float2 Step;            // Blur : one pixel of the source, along the blur's direction (in UV)
float BloomThreshold;   // Composite : how bright a pixel must be before it glows
float BloomStrength;    //             how strong the glow is
float Focus;            //             QTE FOCUS : 0 = normal, 1 = the world dark, grey and soft behind the ring
float Vignette;         //             how dark the corners get
float Grain;            //             film grain strength
float Time;             //             keeps the grain moving

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};

float Luma(float3 c)
{
    return dot(c, float3(0.299, 0.587, 0.114));
}

// Hash : a steady "random" number from a position, the same sine trick as TacetField.Hash
float Hash(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

//Down : the lit world, a quarter of the size
float4 DownPS(VertexShaderOutput input) : COLOR0
{
    float3 scene = tex2D(SceneSampler, input.UV).rgb;
    float3 light = tex2D(LightSampler, input.UV).rgb * 2.0;
    float3 glow = tex2D(GlowSampler, input.UV).rgb;
    return float4(scene * light + glow, 1.0);
}

//Blur : nine taps along Step, the middle one counting most (a Gaussian bell)
float4 BlurPS(VertexShaderOutput input) : COLOR0
{
    float3 c = tex2D(SceneSampler, input.UV).rgb * 0.2270270;
    c += tex2D(SceneSampler, input.UV + Step * 1.0).rgb * 0.1945946;
    c += tex2D(SceneSampler, input.UV - Step * 1.0).rgb * 0.1945946;
    c += tex2D(SceneSampler, input.UV + Step * 2.0).rgb * 0.1216216;
    c += tex2D(SceneSampler, input.UV - Step * 2.0).rgb * 0.1216216;
    c += tex2D(SceneSampler, input.UV + Step * 3.0).rgb * 0.0540541;
    c += tex2D(SceneSampler, input.UV - Step * 3.0).rgb * 0.0540541;
    c += tex2D(SceneSampler, input.UV + Step * 4.0).rgb * 0.0162162;
    c += tex2D(SceneSampler, input.UV - Step * 4.0).rgb * 0.0162162;
    return float4(c, 1.0);
}

//Composite : the finished picture of the world
float4 CompositePS(VertexShaderOutput input) : COLOR0
{
    float3 scene = tex2D(SceneSampler, input.UV).rgb;
    float3 light = tex2D(LightSampler, input.UV).rgb * 2.0;
    float3 soft = tex2D(SoftSampler, input.UV).rgb;
    float3 glow = tex2D(GlowSampler, input.UV).rgb;

    float3 lit = scene * light + glow;

    //BLOOM : only what is brighter than the threshold glows, and it glows from the blurred copy
    float3 bloom = saturate((soft - BloomThreshold) / max(1.0 - BloomThreshold, 0.001));
    float3 c = lit + bloom * BloomStrength;

    //QTE FOCUS : the world behind the ring goes soft, grey and dark, but stays readable
    float grey = Luma(soft) * 0.55;
    c = lerp(c, float3(grey, grey, grey), Focus * 0.85);

    //VIGNETTE : darker towards the corners, more so in FOCUS
    float2 d = (input.UV - 0.5) * float2(1.25, 1.0);
    float v = saturate(1.0 - dot(d, d) * Vignette * (1.0 + Focus * 1.5));
    c *= v;

    //Grain : a little moving noise so the flat greys feel like film
    float n = Hash(floor(input.UV * float2(640.0, 360.0)) + floor(Time * 24.0) * 7.31) - 0.5;
    c += n * Grain;

    return float4(c, 1.0);
}

technique Down
{
    pass P0 { PixelShader = compile PS_SHADERMODEL DownPS(); }
};

technique Blur
{
    pass P0 { PixelShader = compile PS_SHADERMODEL BlurPS(); }
};

technique Composite
{
    pass P0 { PixelShader = compile PS_SHADERMODEL CompositePS(); }
};
