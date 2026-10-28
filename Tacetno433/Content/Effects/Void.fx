// Void.fx : TACET's dark on the battle page (Beta1 round 4). Used by Core/TacetVoid.cs.
//
// ADVANCED PART : TACET is a tear in space behind the enemies (a black hole) that pours its dark over the
// stage floor towards the camera. It is not a picture : every pixel works out from its place on the screen
// whether it is inside the hole, inside the pool on the floor, on their glowing edge, or outside, where the
// world only bends around the hole.
//   Void     : into the SCENE. The backdrop (BACK) seen through the bend (LENSING) and the shock wave of a
//              hit, then the black of the hole and of the pool laid over it
//   VoidGlow : into the GLOW. Only the light : the hole's white rim, its rays (CORONA), the tilted ring of
//              light turning around it (DISC), broken rings drifting out (ARCS), the seam on the floor drawn as a
//              sound meter (SIGNAL), the hole seen upside down in the black pool (MIRROR), grains of light
//              (STIPPLE) and the ring of the shock wave
// Both passes work out the same shapes, so the light always sits exactly on the edge of the dark.
// Everything is worked out on a grid of 2 x 2 screen pixels, one pixel of the art (PIXEL x2).
//
// The noise picture (Fx.Noise, made once on the CPU) gives the wobble, the rays and the currents :
// reading a picture is cheaper for the card than working noise out with maths for every pixel.

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0
    #define PS_SHADERMODEL ps_4_0
#endif

Texture2D SpriteTexture;
sampler2D BackSampler : register(s0) = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Point; MagFilter = Point; MipFilter = Point;
    AddressU = Clamp; AddressV = Clamp;
};

Texture2D NoiseTexture;
sampler2D NoiseSampler : register(s1) = sampler_state
{
    Texture = <NoiseTexture>;
    MinFilter = Linear; MagFilter = Linear; MipFilter = Point;
    AddressU = Wrap; AddressV = Wrap;
};

float2 Screen;      // the screen in pixels (1280, 720)
float2 Hole;        // HOLE : its middle, in screen pixels
float Radius;       //        its size in pixels
float2 PoolBase;    // POOL : x of its middle, y of the line where it starts on the floor (pixels)
float2 PoolSize;    //        how far it reaches across (x) and towards the camera (y)
float Spike;        //        how tall the waveform on its edge gets (pixels)
float Swirl;        // LENSING : how far the world turns around the hole at its rim (radians)
float Danger;       // 0..1 how close the band is to losing : wilder and brighter
float Time;
float4 Wave;        // SHOCK WAVE : x, y (pixels), its radius now (pixels), strength 0..1

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};

//Noise : the four layers of the noise picture (r and g : soft clouds, b : grains)
float4 Noise(float2 uv)
{
    return tex2Dlod(NoiseSampler, float4(uv, 0.0, 0.0));
}

//Rand : a steady random number from a position (the sine trick of TacetField.Hash)
float Rand(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

//Art Pixel : the middle of the 2 x 2 block this screen pixel belongs to
float2 ArtPixel(float2 uv)
{
    return (floor(uv * Screen * 0.5) + 0.5) * 2.0;
}

//Hole Edge : how far the hole reaches in the direction of v. The edge wobbles slowly, and is torn finely
//(the grain layer of the noise). turn runs -0.5 .. 0.5 around the circle and the noise tiles, so the two
//ends meet without a seam.
float HoleEdge(float2 v)
{
    float turn = atan2(v.y, v.x) * 0.15915;
    float w = Noise(float2(turn, Time * 0.05)).r;
    float torn = Noise(float2(turn * 2.0, Time * 0.15)).b;
    return Radius * (0.88 + w * 0.24 + (torn - 0.5) * 0.07);
}

//Spike At : the waveform on the pool's edge, in bars 4 pixels wide (2 pixels of art) like a sound meter.
//A slow layer makes the swell, a fast one the flicker. Mostly short, now and then a tall one.
float SpikeAt(float x)
{
    float bar = floor(x * 0.25);
    float slow = Noise(float2(bar * 0.0123, Time * (0.08 + Danger * 0.1))).r;
    float fast = Noise(float2(bar * 0.0917 + 0.31, Time * (0.6 + Danger * 0.9))).g;
    float h = saturate((slow * 0.7 + fast * 0.7 - 0.55) * 2.2);
    return h * h * Spike;
}

//Pool Edge : the row where the pool ends at column x. An ellipse on the floor, seen from the band :
//wide across, short towards the camera. The waveform rides on it and shrinks towards the far sides.
float PoolEdge(float x)
{
    float dx = (x - PoolBase.x) / PoolSize.x;
    float k = sqrt(saturate(1.0 - dx * dx));
    return PoolBase.y + (PoolSize.y + SpikeAt(x)) * k;
}

//Void : the world bent around the hole, then the dark
float4 VoidPS(VertexShaderOutput input) : COLOR0
{
    float2 p = ArtPixel(input.UV);

    //LENSING : around the hole the world turns and is pulled in, the closer the more
    float2 v = p - Hole;
    float d = max(length(v), 0.001);
    float r = HoleEdge(v);
    float bend = saturate(r / d);
    bend = bend * bend;
    float turn = Swirl * bend;
    float s = sin(turn);
    float c = cos(turn);
    float2 q = Hole + float2(v.x * c - v.y * s, v.x * s + v.y * c) * (1.0 - 0.3 * bend);

    //SHOCK WAVE : a ring running out from a hit pushes the picture outwards as it passes
    float2 w = p - Wave.xy;
    float wd = max(length(w), 0.001);
    float ring = saturate(1.0 - abs(wd - Wave.z) / 24.0);
    q += w / wd * ring * ring * 16.0 * Wave.w;

    float3 col = tex2D(BackSampler, q / Screen).rgb;

    //THE POOL : the floor goes black from under the hole to its edge, slow currents flowing towards the camera
    float edge = PoolEdge(p.x);
    float onFloor = step(PoolBase.y, p.y);
    float inPool = onFloor * step(p.y, edge);
    float2 f = (p - PoolBase) / PoolSize;
    float flow = Noise(float2(f.x * 0.9, f.y * 1.3 - Time * 0.05)).g;
    float3 pool = (0.006 + pow(flow, 5.0) * 0.12) * float3(1.0, 1.0, 1.0);

    //Shadow : the floor just in front of the edge is already dimmer
    float shade = saturate(1.0 - (p.y - edge) / 48.0) * step(edge, p.y) * onFloor;
    col *= 1.0 - shade * 0.45;

    //THE HOLE : black, with faint grey currents turning inside, faster towards the middle (a whirlpool)
    float inHole = step(d, r);
    float2 m = v / Radius;
    float spin = Time * 0.12 + (1.0 - saturate(d / r)) * 2.2;
    float ss = sin(spin);
    float cc = cos(spin);
    float2 mr = float2(m.x * cc - m.y * ss, m.x * ss + m.y * cc);
    float marble = Noise(mr * 0.3 + 0.5).r;
    float3 hole = pow(marble, 4.0) * 0.18 * saturate(d / r) * float3(1.0, 1.0, 1.0);

    col = lerp(col, pool, inPool);
    col = lerp(col, hole, inHole);
    return float4(col, 1.0);
}

//Void Glow : only the light of the dark, added into the GLOW (it blooms and the light map never dims it)
float4 VoidGlowPS(VertexShaderOutput input) : COLOR0
{
    float2 p = ArtPixel(input.UV);
    float2 v = p - Hole;
    float d = max(length(v), 0.001);
    float r = HoleEdge(v);
    float outside = d - r;                                  // > 0 outside the rim
    float turn = atan2(v.y, v.x) * 0.15915;
    float g = 0.0;

    //RIM : the edge of the hole, white hot, with its glow on both sides (thinner inside)
    g += saturate(1.0 - abs(outside) / (2.0 + Danger * 2.0)) * 1.3;
    g += exp(-max(outside, 0.0) / (5.0 + Danger * 5.0)) * step(0.0, outside) * 0.45;
    g += exp(min(outside, 0.0) / 3.0) * step(outside, 0.0) * 0.25;

    //CORONA : rays of light out of the rim, each its own length, slowly changing
    float ray = Noise(float2(turn * 8.0, Time * 0.04)).g;
    ray = pow(saturate(ray * 1.6 - 0.35), 3.0);
    float reach = Radius * (0.25 + ray * (1.3 + Danger));
    g += ray * saturate(1.0 - outside / reach) * step(0.0, outside) * 0.6;

    //DISC : a tilted ring of light turning around the hole. Its far half passes behind the black.
    float2 dv = float2(v.x, v.y * 4.5);
    float dd = length(dv);
    float band = saturate(1.0 - abs(dd - Radius * 1.6) / (Radius * 0.35));
    band = band * band;
    float dturn = atan2(dv.y, dv.x) * 0.15915;
    float streak = Noise(float2(dturn * 3.0 - Time * 0.18, dd / Radius * 0.25)).r;
    float hidden = step(v.y, 0.0) * step(outside, 0.0);
    g += band * (0.15 + pow(streak, 3.0) * 1.6) * (1.0 - hidden) * 0.7;

    //ARCS : thin rings drifting outwards, broken into pieces, fading as they go
    [unroll] for (int k = 0; k < 3; k++)
    {
        float t = frac(Time * 0.07 + k * 0.3333);
        float ar = Radius * (1.3 + t * 2.4);
        float arc = saturate(1.0 - abs(d - ar) / 1.6);
        float piece = step(0.52, Noise(float2(turn * 2.0 + k * 0.37, k * 0.29 + 0.11)).r);
        g += arc * piece * (1.0 - t) * 0.4;
    }

    //POOL SEAM : where the dark meets the lit floor, white, the waveform pushing out of it
    float edge = PoolEdge(p.x);
    float onFloor = step(PoolBase.y, p.y);
    float e = p.y - edge;                                   // > 0 in front of the seam
    g += saturate(1.0 - abs(e) / 2.5) * onFloor * 1.1;
    g += exp(-max(e, 0.0) / 12.0) * step(0.0, e) * onFloor * 0.22;

    //SIGNAL : the seam is a sound meter, white bars standing up into the dark from the edge (the last of the music)
    float level = SpikeAt(p.x + 613.0) * 0.7 + 2.0;
    g += step(-level, e) * step(e, 0.0) * onFloor * 0.45;

    //MIRROR : the pool is still and black like ink. The hole's rim shows in it upside down, broken into
    //ripples, fading as the pool comes towards the camera.
    float row = floor(p.y * 0.5);
    float ripple = (Noise(float2(row * 0.021, Time * 0.07)).g - 0.5) * 26.0;
    float2 mv = float2(p.x + ripple, 2.0 * PoolBase.y - p.y) - Hole;
    float md = max(length(mv), 0.001);
    float mr = HoleEdge(mv);
    float mirror = saturate(1.0 - abs(md - mr) / 3.0) + exp(-max(md - mr, 0.0) / 8.0) * step(mr, md) * 0.3;
    float fade = 1.0 - saturate((p.y - PoolBase.y) / 170.0);
    g += mirror * step(e, -level) * onFloor * fade * 0.32;

    //STIPPLE : grains of light thrown around the rim, thicker close to it, like an engraving
    //(strictly below the threshold : where there is no halo there is no grain at all. A step that also let an exact 0
    //through made white specks all over the screen, round 4)
    float halo = saturate(1.0 - outside / (Radius * 0.6)) * step(0.0, outside);
    float grain = 1.0 - step(halo * halo * 0.2, Rand(floor(p * 0.5) + floor(Time * 10.0) * 0.731));
    g += grain * 0.6;

    //SHOCK WAVE : the ring of a hit
    float wd = length(p - Wave.xy);
    g += saturate(1.0 - abs(wd - Wave.z) / 3.0) * Wave.w * 0.8;

    return float4(g, g, g, 1.0);
}

technique Void
{
    pass P0 { PixelShader = compile PS_SHADERMODEL VoidPS(); }
};

technique VoidGlow
{
    pass P0 { PixelShader = compile PS_SHADERMODEL VoidGlowPS(); }
};
