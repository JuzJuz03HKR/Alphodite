using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //DuelScreen.Panels : the three panels along the bottom of the screen, like the party panel
    //of a storybook battle. The signature recipe on the left, the band in the middle (who plays
    //now, and each player's part), and the story box on the right.
    public partial class DuelScreen
    {
        //Signature Panel : the conductor's face and the recipe. Each column is one instrument
        //family, with a box for every note the recipe asks for. A family nobody on stage plays
        //shows a dash and is skipped. When every column is full the panel lights up and SPACE
        //lets the signature loose.
        private void DrawSignaturePanel(SpriteBatch sb)
        {
            Rectangle p = signPanel;
            bool ready = battle.SignatureReady;
            bool armed = battle.SignatureArmed;
            bool glowing = ready || armed;
            float pulse = glowing ? (float)Math.Sin(time * 6f) * 0.5f + 0.5f : 0f;
            Color ink = glowing ? Palette.Highlight : Palette.PaperDim;

            if (glowing) Gfx.DrawGlowBox(sb, new Rectangle(p.X - 10, p.Y - 10, p.Width + 20, p.Height + 20), Palette.Highlight * (0.2f + 0.25f * pulse));
            Gfx.Rect(sb, p, Palette.Void * 0.92f);
            Ornament.DoubleFrame(sb, p, glowing ? Palette.Highlight : Palette.PaperDim);

            //Face, and the key that lets the signature loose. The glow round the panel says it is ready.
            Rectangle face = new Rectangle(p.X + 12, p.Y + 8, 48, 48);
            PortraitBox.DrawFaceIcon(sb, face, Game.CurrentRun.Conductor, glowing ? 1f : 0.55f);
            Ui.KeyChipRight(sb, "SPACE", face.Center.X + 22, p.Y + 88, ink);

            //Recipe Columns : strings, winds, percussion
            for (int f = 0; f < 3; f++)
            {
                int need = battle.NeedFor(f);
                float cx = p.X + 98 + f * 28;
                MusicianArt.FamilyGlyph(sb, (Family)f, cx, p.Y + 86, 0.4f, need > 0 ? Palette.Paper : Palette.LineGrey * 0.5f);

                if (need == 0)
                {
                    Gfx.Rect(sb, cx - 6, p.Y + 60, 12, 2, Palette.LineGrey * 0.5f);
                    continue;
                }

                for (int i = 0; i < need; i++)
                {
                    Rectangle cell = new Rectangle((int)cx - 8, p.Y + 52 - i * 20, 16, 16);
                    bool have = battle.Notes[f] > i;
                    Gfx.Rect(sb, cell, have ? Palette.Highlight : Palette.Stage);
                    Gfx.RectOutline(sb, cell, have ? Palette.Highlight : Palette.PaperDim, 1);
                }
            }
        }

        //Band Panel : one box per musician on stage, like a party panel. A box turns white when
        //its player plays the beat being answered, and glows while their sound is out. The eight
        //marks along the bottom are that player's part for this round, the current beat ringed.
        private void DrawBandPanel(SpriteBatch sb)
        {
            Formation f = Game.CurrentRun.Formation;
            int count = 0;
            for (int s = 0; s < StageLayout.SeatCount; s++)
                if (f.Seated[s] != null) count++;
            if (count == 0) return;

            Gfx.Rect(sb, bandPanel, Palette.Void * 0.92f);
            Ornament.DoubleFrame(sb, bandPanel, Palette.PaperDim);

            float gap = 6f;
            float w = Math.Min(150f, (bandPanel.Width - 16 - gap * (count - 1)) / count);
            float x = bandPanel.X + 8;
            int current = AnswerProgress() >= 0f ? bar * 4 + pending : -1;
            bool roomForFace = w >= 110f;

            for (int i = 0; i < panelOrder.Length; i++)
            {
                int s = panelOrder[i];
                Musician m = f.Seated[s];
                if (m == null) continue;

                Rectangle box = new Rectangle((int)x, bandPanel.Y + 8, (int)w, bandPanel.Height - 16);
                bool now = current >= 0 && f.Plays(s, current);
                Color ink = now ? Palette.Ink : Palette.Paper;
                Color soft = now ? Palette.InkSoft : Palette.LineGrey;

                if (lit[s] > 0.05f) Gfx.DrawGlowBox(sb, box, Palette.Highlight * (0.45f * lit[s]));
                Gfx.Rect(sb, box, now ? Palette.Paper : Palette.Stage);
                Gfx.RectOutline(sb, box, now ? Palette.Highlight : Palette.LineGrey * 0.6f, 1);

                //Face And Name
                float textX = box.X + 8;
                if (roomForFace)
                {
                    MusicianArt.Tile(sb, new Rectangle(box.X + 6, box.Y + 6, 38, 38), m, 1f);
                    textX = box.X + 52;
                }
                Gfx.Text(sb, Game.Font, m.Name, textX, box.Y + 6, ink, TextSize.Body);
                Gfx.TextSpaced(sb, Game.Font, StageLayout.RowOf(s).Name, textX, box.Y + 28, soft, TextSize.Tiny, 1.5f);
                MusicianArt.FamilyGlyph(sb, m.Family, box.Right - 14, box.Y + 16, 0.35f, soft);

                //Part : eight marks, filled where this player plays
                float step = (box.Width - 16) / 7f;
                for (int n = 0; n < BattleRules.BeatsPerRound; n++)
                {
                    float mx = box.X + 8 + n * step;
                    float my = box.Bottom - 12;
                    if (f.Plan[s, n]) Gfx.Diamond(sb, mx, my, 3, ink);
                    else Gfx.Rect(sb, mx - 2, my, 4, 1, soft);
                    if (n == current) Gfx.DiamondOutline(sb, mx, my, 6, ink, 1f);
                }

                x += w + gap;
            }
        }

        //Story Box : the line at the bottom right, typed out a little at a time
        private void DrawStory(SpriteBatch sb)
        {
            Gfx.Rect(sb, textBox, Palette.Void);
            Ornament.DoubleFrame(sb, textBox, Palette.Paper);
            if (saying < 0) return;

            float x = textBox.X + 18;
            float y1 = textBox.Y + 20;
            float lineH = Game.StoryFont.LineSpacing * TextSize.Story;
            float y2 = y1 + lineH + 6;

            Gfx.Text(sb, Game.StoryFont, sayLineA[saying], x, y1, Palette.Paper, TextSize.Story);
            Gfx.Text(sb, Game.StoryFont, sayLineB[saying], x, y2, Palette.Paper, TextSize.Story);

            //Typewriter : the whole line is drawn, then the part not reached yet is covered
            //over. Covering is much cheaper than cutting a shorter string every frame.
            float shown = sayTimer * SaySpeed;
            float widthA = sayWidthA[saying];
            float widthB = sayWidthB[saying];
            if (shown < widthA)
                Gfx.Rect(sb, x + shown, y1 - 2, widthA - shown + 6, lineH + 4, Palette.Void);
            float shownB = Math.Max(0f, shown - widthA);
            if (shownB < widthB)
                Gfx.Rect(sb, x + shownB, y2 - 2, widthB - shownB + 6, lineH + 4, Palette.Void);
        }
    }
}
