// Post.fx : the battle picture's finishing passes (Beta1). Used by Core/PostChain.cs.
//
// ADVANCED PART : a shader is a small program the graphics card runs once for every pixel it draws.
// SpriteBatch hands each pass a picture (SpriteTexture) and the pixel's place in it (UV, 0..1).
// The world is drawn into its own picture first (the SCENE), the lights into another (the LIGHT MAP),
// and the effects that give off their own light (sparks, slashes, flares) into a third (the GLOW, never
// darkened by the light map). Then these passes mix them:
//   Down     : SCENE x LIGHT + GLOW, shrunk to a quarter of the screen (cheap to blur)
//   Blur     : a soft smear along one direction, run twice (across, then down)
//   Composite: SCENE x LIGHT + GLOW (read through the SPACE CUTS, see CutUV), plus the soft glow (BLOOM) taken from the blurred copy, then the QTE FOCUS,
//              TACET's dark creeping in from the edges (ENGULF), the dark corners (VIGNETTE), the IMPACT FRAME of a
//              big blow and the film grain
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

// SHARP SCREEN (Core/Sharp.cs) : on a screen bigger than the page, SCENE and GLOW are as big as the screen and keep
// smaller copies of themselves (MIPMAPS). MipFilter Linear lets Down read between two copies (Lod 0.58 at 1.5).
Texture2D SpriteTexture;
sampler2D SceneSampler : register(s0) = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
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
    MinFilter = Linear; MagFilter = Linear; MipFilter = Linear;
    AddressU = Clamp; AddressV = Clamp;
};

Texture2D NoiseTexture;
sampler2D NoiseSampler : register(s4) = sampler_state
{
    Texture = <NoiseTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Point;
    AddressU = Wrap; AddressV = Wrap;
};

float2 Step;            // Blur : one pixel of the source, along the blur's direction (in UV)
float BloomThreshold;   // Composite : how bright a pixel must be before it glows
float BloomStrength;    //             how strong the glow is
float Focus;            //             QTE FOCUS : 0 = normal, 1 = the world dark, grey and soft behind the ring
float Vignette;         //             how dark the corners get
float Grain;            //             film grain strength
float Time;             //             keeps the grain moving
float Engulf;           //             ENGULF : 0 = nothing, 1 = TACET's dark has crept far in from the edges
float Impact;           //             IMPACT FRAME : 1 = the picture turned over for a frame or two
float Flash;            //             IMPACT FRAME : the frame of white, then its glare fading (0..1)
float Blur;             //             IMPACT FRAME : the world smeared towards the blow (ZOOM BLUR, 0..1)
float2 Center;          //             IMPACT FRAME : where the blow landed (0..1 of the screen)
float Lod;              // Down : which copy of SCENE and GLOW is the page's own size (0 in the 1280 x 720 window)

// SPACE CUT (Beta1 block D, 2 Nov) : up to four cuts that split the picture of the world for a moment (Core/HitFx.Skills.cs,
// FANNY's FINALE in Core/HitFx.Finale.cs)
float4 Cuts[4];         //             xy where it is (0..1 of the screen), zw its way (the cosine and sine of its angle, worked out once by the game)
float4 CutTints[4];     //             rgb the colour of its edges, a its half length (in screen heights)
float4 CutShapes[4];    //             x how far the two halves slide along it, y how wide the black gap is (screen heights),
                        //             z how open (0 = closed, 1 = wide), w PRISM : 0 = its edges in its own colour, 1 = the rainbow runs along them
float CutFall;          //             THE BREAK : how far the pieces between whole cuts have dropped (screen heights, 0 = none)

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

//Hue : the rainbow, red at 0 and round every colour once as h grows by 1 (FANNY's PRISM)
float3 Hue(float h)
{
    return saturate(abs(frac(h + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 1.0);
}

//Piece Of : which piece of the broken world a point is in (THE BREAK). Every WHOLE cut (longer than the screen) splits the
//world in two, so the sides a point is on, one bit for each cut, name its piece : 0 to 15.
float PieceOf(float2 uv)
{
    const float aspect = 1280.0 / 720.0;
    float piece = 0.0;
    float bit = 1.0;
    [unroll] for (int i = 0; i < 4; i++)
    {
        float2 d = (uv - Cuts[i].xy) * float2(aspect, 1.0);
        float whole = step(1.5, CutTints[i].a) * step(0.0001, CutShapes[i].z);
        piece += step(0.0, dot(d, float2(-Cuts[i].w, Cuts[i].z))) * bit * whole;
        bit *= 2.0;
    }
    return piece;
}

//SPACE CUT : where this pixel reads the world from while the cuts are open, how deep it is in a cut's black gap (dark),
//and the light of the cut's edges on it (edge).
//ADVANCED PART : a cut is a line through Cuts[i].xy going the way Cuts[i].zw. For each pixel, side is how far it is from
//the line (plus on one side, minus on the other) and along is how far down the line. Every pixel on one side reads the
//picture from a little further along the line and away from it, the other side the other way, so the two halves of the
//world slide apart like the two halves of something cut (DMC's Judgement Cut, ZZZ's Miyabi). span fades the slide out
//towards the cut's ends, so it is a tear in the picture, not a line across the whole screen. What shows in the gap is
//black : the silence behind the world. A PRISM cut's edges run through the rainbow along it (FANNY's crystal).
//THE BREAK (FANNY's FINALE) : while CutFall is above 0, the pieces the whole cuts make also drop, each by its own amount
//(a steady random number from its piece's name). A pixel reads the world from higher up; if that point is in another
//piece, nothing of this pixel's piece has fallen here : black, the silence between the pieces of glass. A pixel whose
//point is near its piece's own edge is that piece of glass's edge : it catches the rainbow.
float2 CutUV(float2 uv, out float dark, out float3 edge)
{
    const float aspect = 1280.0 / 720.0;
    float2 square = float2(aspect, 1.0);
    float2 moved = uv;
    dark = 0.0;
    edge = float3(0.0, 0.0, 0.0);
    [unroll] for (int i = 0; i < 4; i++)
    {
        float2 dir = Cuts[i].zw;
        float2 across = float2(-dir.y, dir.x);
        float2 d = (uv - Cuts[i].xy) * square;
        float along = dot(d, dir);
        float side = dot(d, across);
        float span = saturate(1.0 - abs(along) / max(CutTints[i].a, 0.001));
        span = span * span * (3.0 - 2.0 * span);
        float k = CutShapes[i].z * span;
        float s = side >= 0.0 ? 1.0 : -1.0;
        float gap = CutShapes[i].y * k;
        moved -= (dir * (s * CutShapes[i].x * k) + across * (s * gap)) / square;
        dark = max(dark, (1.0 - smoothstep(gap * 0.55, gap + 0.0005, abs(side))) * step(0.0001, k));
        float rim = saturate(1.0 - abs(abs(side) - gap) / 0.0035) * k;
        float haze = saturate(1.0 - abs(side) / 0.045) * k;
        float3 tint = lerp(CutTints[i].rgb, Hue(along * 1.4 + s * 0.1 + Time * 0.3), CutShapes[i].w);
        edge += tint * (rim * (1.4 - 0.4 * CutShapes[i].w) + haze * (0.3 - 0.2 * CutShapes[i].w)) + float3(1.0, 1.0, 1.0) * (rim * rim * 0.9);
    }

    [branch] if (CutFall > 0.0001)
    {
        float piece = PieceOf(uv);
        float drop = 0.55 + 0.9 * Hash(float2(piece, 3.7));
        float drift = Hash(float2(piece, 9.1)) - 0.5;
        moved -= float2(drift * 0.4, drop) * CutFall / square;
        float there = PieceOf(moved);
        float apart = step(0.5, abs(there - piece));
        dark = max(dark, apart);
        float near = 1.0;
        float prism = 0.0;
        [unroll] for (int j = 0; j < 4; j++)
        {
            float2 d2 = (moved - Cuts[j].xy) * square;
            float whole = step(1.5, CutTints[j].a) * step(0.0001, CutShapes[j].z);
            near = min(near, lerp(1.0, abs(dot(d2, float2(-Cuts[j].w, Cuts[j].z))), whole));
            prism = max(prism, CutShapes[j].w * whole);
        }
        float glint = saturate(1.0 - near / 0.004) * (1.0 - apart) * saturate(CutFall * 60.0);
        float3 glass = lerp(float3(0.85, 0.85, 0.9), Hue(moved.x * 1.7 + moved.y * 0.8 + Time * 0.4), prism);   // TACET's glass is white
        edge += glass * (glint * 1.3) + float3(1.0, 1.0, 1.0) * (glint * glint * 0.7);
    }
    return moved;
}

//Down : the lit world, a quarter of the size. SCENE and GLOW are read from their copy the page's size (Lod), so on every
//screen the glow is made from the same picture.
float4 DownPS(VertexShaderOutput input) : COLOR0
{
    float3 scene = tex2Dlod(SceneSampler, float4(input.UV, 0.0, Lod)).rgb;
    float3 light = tex2D(LightSampler, input.UV).rgb * 2.0;
    float3 glow = tex2Dlod(GlowSampler, float4(input.UV, 0.0, Lod)).rgb;
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
    float cutDark;
    float3 cutEdge;
    float2 uv = CutUV(input.UV, cutDark, cutEdge);                                // SPACE CUT : the halves of the world apart
    float3 scene = tex2Dlod(SceneSampler, float4(uv, 0.0, 0.0)).rgb;             // the full size copy, pixel for pixel
    float3 light = tex2D(LightSampler, uv).rgb * 2.0;
    float3 soft = tex2D(SoftSampler, uv).rgb;
    float3 glow = tex2Dlod(GlowSampler, float4(uv, 0.0, 0.0)).rgb;

    float3 lit = scene * light + glow;

    //BLOOM : only what is brighter than the threshold glows, and it glows from the blurred copy
    float3 bloom = saturate((soft - BloomThreshold) / max(1.0 - BloomThreshold, 0.001));
    float3 c = lit + bloom * BloomStrength;

    //QTE FOCUS : the world behind the ring goes soft, grey and dark, but stays readable
    float grey = Luma(soft) * 0.55;
    c = lerp(c, float3(grey, grey, grey), Focus * 0.85);

    //ENGULF : when TACET is winning its dark creeps in from the bottom and the right, ragged, with a white seam.
    //fromEdge is how far this pixel is from those edges, reach how far the dark has come here.
    float2 eu = floor(input.UV * float2(640.0, 360.0)) / float2(640.0, 360.0);
    float fromEdge = min((1.0 - eu.y) * 1.25, (1.0 - eu.x) * 1.9);
    float ragged = tex2Dlod(NoiseSampler, float4(eu.x * 1.7 + Time * 0.01, eu.y * 1.7 - Time * 0.03, 0.0, 0.0)).r;
    float reach = Engulf * 0.42 * (0.55 + 0.9 * ragged);
    float swallowed = step(fromEdge, reach);
    float nearSeam = saturate(1.0 - (fromEdge - reach) * 30.0) * (1.0 - swallowed);
    c *= 1.0 - nearSeam * 0.5 * step(0.001, Engulf);
    c = lerp(c, float3(0.0, 0.0, 0.0), swallowed);
    c += saturate(1.0 - abs(fromEdge - reach) * 450.0) * step(0.001, Engulf) * 0.9;

    //ZOOM BLUR : for a moment after a big blow the world is smeared towards where it landed. The blurred copy is
    //read six times along the line to the blow and the picture leans towards that average.
    [branch] if (Blur > 0.001)
    {
        float2 toward = (Center - input.UV) * Blur * 0.2;
        float3 smear = float3(0.0, 0.0, 0.0);
        [unroll] for (int i = 1; i <= 6; i++)
            smear += tex2Dlod(SoftSampler, float4(input.UV + toward * (i / 6.0), 0.0, 0.0)).rgb;
        c = lerp(c, smear / 6.0 * 1.35, Blur * 0.65);
    }

    //SPACE CUT : black where the world is cut open, the cut's edges burning in its colour
    c = lerp(c, float3(0.0, 0.0, 0.0), cutDark);
    c += cutEdge;

    //VIGNETTE : darker towards the corners, more so in FOCUS
    float2 d = (input.UV - 0.5) * float2(1.25, 1.0);
    float v = saturate(1.0 - dot(d, d) * Vignette * (1.0 + Focus * 1.5));
    c *= v;

    //IMPACT FRAME : the moment of a big blow, like a manga panel : white paper, and black where something bright was
    //(the lightning, the cuts, the hole's rim, the lines of the fighters). It reads the world before its lights
    //and bloom, or the flash of the blow itself would turn into one big black shape (round 6).
    //Between the two turns a frame of white, and its glare fading after (Flash).
    float ink = Luma(saturate(scene * 0.6 + glow));
    float turned = 1.0 - step(0.32, ink);
    c = lerp(c, float3(turned, turned, turned), Impact);
    c = lerp(c, float3(1.0, 1.0, 1.0), Flash * 0.85);

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
