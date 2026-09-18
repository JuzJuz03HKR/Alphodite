using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Core;

namespace Tacetno433.Battle
{
    //DuelEffects : the sound waves and the numbers that pop up during a duel.
    //
    //Everything is made ONCE when the duel opens and then reused (an "object pool").
    //Spawning a wave just switches on a slot that is currently off, so a long fight never
    //creates new objects and never feeds the garbage collector.
    public class DuelEffects
    {
        //Wave : one arc of sound travelling across the stage
        private class Wave
        {
            public bool On;
            public float X, Y, Radius, Life, Dir;
        }

        //Pop : one short piece of text that floats up and fades
        private class Pop
        {
            public bool On;
            public float X, Y, Life, Scale;
            public string Text = "";
            public Color Color;
        }

        private const float WaveLife = 1.1f;
        private const float WaveSpeed = 520f;
        private const float PopLife = 1.2f;

        private Wave[] waves = new Wave[24];
        private Pop[] pops = new Pop[12];

        public DuelEffects()
        {
            for (int i = 0; i < waves.Length; i++) waves[i] = new Wave();
            for (int i = 0; i < pops.Length; i++) pops[i] = new Pop();
        }

        //Wave Spawn : dir +1 travels right (our sound), -1 travels left (TACET)
        public void SpawnWave(float x, float y, float dir)
        {
            for (int i = 0; i < waves.Length; i++)
            {
                if (waves[i].On) continue;
                waves[i].On = true;
                waves[i].X = x;
                waves[i].Y = y;
                waves[i].Radius = 18f;
                waves[i].Life = WaveLife;
                waves[i].Dir = dir;
                return;
            }
        }

        //Pop Spawn : text should come from NumberText or a fixed word, never built on the spot
        public void SpawnPop(float x, float y, string text, Color color, float scale)
        {
            for (int i = 0; i < pops.Length; i++)
            {
                if (pops[i].On) continue;
                pops[i].On = true;
                pops[i].X = x;
                pops[i].Y = y;
                pops[i].Life = PopLife;
                pops[i].Text = text;
                pops[i].Color = color;
                pops[i].Scale = scale;
                return;
            }
        }

        public void Update(float dt)
        {
            for (int i = 0; i < waves.Length; i++)
            {
                Wave w = waves[i];
                if (!w.On) continue;
                w.X += w.Dir * WaveSpeed * dt;
                w.Radius += 90f * dt;
                w.Life -= dt;
                if (w.Life <= 0f) w.On = false;
            }

            for (int i = 0; i < pops.Length; i++)
            {
                Pop p = pops[i];
                if (!p.On) continue;
                p.Y -= 38f * dt;
                p.Life -= dt;
                if (p.Life <= 0f) p.On = false;
            }
        }

        //Waves Draw : each arc is drawn twice, dark then light, so it reads on the bright
        //stage and on TACET's black alike
        public void DrawWaves(SpriteBatch sb)
        {
            for (int i = 0; i < waves.Length; i++)
            {
                Wave w = waves[i];
                if (!w.On) continue;

                float a = w.Life / WaveLife;
                float centre = w.Dir > 0f ? 0f : MathHelper.Pi;
                Gfx.Arc(sb, w.X, w.Y, w.Radius, centre - 0.8f, centre + 0.8f, Palette.Ink * (0.8f * a), 5f);
                Gfx.Arc(sb, w.X, w.Y, w.Radius, centre - 0.8f, centre + 0.8f, Palette.Highlight * a, 2f);
            }
        }

        //Pops Draw : white text with a black shadow, readable on any background
        public void DrawPops(SpriteBatch sb, SpriteFont font)
        {
            for (int i = 0; i < pops.Length; i++)
            {
                Pop p = pops[i];
                if (!p.On) continue;

                float a = Math.Min(1f, p.Life * 2.5f);
                Gfx.TextCentered(sb, font, p.Text, p.X + 2, p.Y + 2, Color.Black * (0.8f * a), p.Scale);
                Gfx.TextCentered(sb, font, p.Text, p.X, p.Y, p.Color * a, p.Scale);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < waves.Length; i++) waves[i].On = false;
            for (int i = 0; i < pops.Length; i++) pops[i].On = false;
        }
    }
}
