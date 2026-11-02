using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //HitFx Stars (Beta1 block D item 3, 2 Nov, docs/BETA1_DIRECTION.md 40) : the stars a move has from REHEARSAL are seen
    //where it lands. The user (29 Oct) : a sharper weapon's blow was not clear enough.
    //   STAR MARKS  as a move's blow lands (an ATTACK's blow, a skill's last note), its row of stars shows over the blow, the
    //               same row as on its button (Ui.Stars) : the player sees at once what the rehearsal bought (drawn with the
    //               damage numbers, Screens/BattleScreen.Show.cs)
    //   FLOURISH    at three stars (the legend form) every blow ends in its legend's element (round 6 ELEMENTS) :
    //                  FIRE   PAGANINI : a ring of fire opening and flames leaping up off the blow
    //                  WIND   QUANTZ : two gusts swirling round the blow the way a whirlwind turns
    //                  EARTH  PFUNDT : spikes of rock bursting up under the enemy, stones thrown, a ring on the floor
    //                  WATER  DUPORT : a crown of drops thrown up and falling back, a flat ring of a splash
    //                  LIGHT  PUNTO : long thin rays all round, a sunburst
    //The shapes say what the element is (37.2 : shape first), the colour is the legend's own. Every piece is from the pools
    //of HitFx (no garbage). Numbers : Data/BattleRules.cs, Hit Feel (HitTierSize) and Star Feel.
    public partial class HitFx
    {
        //Flourish : a three star blow ends in its legend's element (see the top of this file)
        private void Flourish(Vector2 at, Rectangle box, float size, HitColors c)
        {
            Vector2 feet = new Vector2(box.Center.X, box.Bottom - 6f);
            Vector2 up = new Vector2(0f, -1f);
            switch (c.Element)
            {
                case SpotLook.Fire:
                case SpotLook.Red:
                    AddRing(at, size * 2f, 1f, 4f, 0.5f, c.Tint);
                    Burn(at, size * 1.3f, 1.2f, up, false, c);
                    Throw(at, up, 20, 520f, Ember, c);
                    break;

                case SpotLook.Wind:
                    float turn = (float)(random.NextDouble() * MathHelper.TwoPi);
                    AddCrescent(at, size * 1.7f, 0.55f, turn, 0f, 4.4f, 9f, 0f, c);
                    AddCrescent(at, size * 1.25f, 0.55f, turn + 3.1f, 0f, 4.4f, 6f, -0.05f, c);
                    for (int k = 0; k < 6; k++)
                    {
                        float a = turn + k * MathHelper.TwoPi / 6f;
                        Throw(at + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * size, new Vector2(-(float)Math.Sin(a), (float)Math.Cos(a)), 3, 520f, Streak, c);
                    }
                    break;

                case SpotLook.Earth:
                    AddBurst(feet, size * 1.5f, -MathHelper.PiOver2, 2.6f, 9, 7f, 0.42f, c);
                    AddRing(feet, size * 2.6f, 0.24f, 5f, 0.5f, c.Tint);
                    Throw(feet, up, 18, 560f, Shard, c);
                    Throw(feet, up, 10, 320f, Spark, c);
                    break;

                case SpotLook.Water:
                    AddRing(at, size * 1.9f, 0.35f, 3f, 0.45f, c.Hot);
                    AddRing(at, size * 1.2f, 0.35f, 2f, 0.35f, c.Tint);
                    for (int k = 0; k < 5; k++)
                    {
                        float a = -MathHelper.PiOver2 + (k - 2f) * 0.42f;
                        Throw(at, new Vector2((float)Math.Cos(a), (float)Math.Sin(a)), 5, 560f, Spark, c);   // the crown, drops falling back
                    }
                    break;

                default:                                                                       // LIGHT (and the old GOLD)
                    AddBurst(at, size * 2.3f, 0f, 1f, 24, 2f, 0.45f, c);
                    Star(at, size * 1.7f, c);
                    if (c.Flair == HitFlair.Sparkle) Sparkle(at, size * 1.2f, 1f, c);
                    break;
            }
        }
    }
}
