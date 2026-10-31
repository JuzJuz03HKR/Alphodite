using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //RehearsalScreen : REHEARSAL (Beta1 31 Oct, the user picked it, docs/BETA1_DIRECTION.md 16.14 point 1 and 17). After a
    //fight won the band rehearses : one of the nine buttons it presses in a fight gets a star (Data/Rehearsal.cs).
    //   Three columns, one for each legend : the face, the name, the power, then their three buttons as the battle shows them
    //   (ATTACK and the two skills, a band in the colour of their kind) with their stars. Under each button what its next star
    //   changes, before and after an arrow ("4 CUTS > 5 CUTS"). The third star is the legend : the button's new name and what
    //   it does in one line.
    //   A click (or the arrows and ENTER) gives the star, it fills in the musician's colour and the page moves on by itself.
    //Opened by the result page after a win (an ELITE's or a BOSS's motif comes after it) and by the rest stop (REHEARSE).
    //Its look is a first one : the VFX of a new star and of a legend wait for the user's references (16.14 point 5).
    public class RehearsalScreen : GameScreen
    {
        public enum From { Fight, Rest }

        //Rehearsal Layout
        public const float LineWidth = 340f;            // the legends' lines are wrapped to this (RehearsalList.PrepareText)
        private const int ColumnW = 376;
        private const int ColumnGap = 22;
        private const int ColumnsTop = 180;
        private const int FaceSize = 96;
        private const int CardH = 106;
        private const int CardGap = 8;
        private const int CardsTop = ColumnsTop + FaceSize + 16;
        private const float LeaveAfter = 1.2f;          // seconds the new star is shown before the page moves on

        //Rehearsal State : the band in the band page's order, and one card per button, column by column (card 3c + b is
        //button b of the legend in column c). Every word is made in Load (never in Draw).
        private readonly From from;
        private List<Musician> band = new List<Musician>();
        private Rectangle[] faces = new Rectangle[0];
        private string[] powerText = new string[0];
        private Rectangle[] cards = new Rectangle[0];
        private string[] names = new string[0];
        private string[] fromText = new string[0];
        private string[] toText = new string[0];
        private string[] legendText = new string[0];    // the third star's line (wrapped), "" for the first two
        private int[] stars = new int[0];
        private bool[] can = new bool[0];
        private Color[] kind = new Color[0];
        private float[] lift = new float[0];
        private int selected = -1;
        private int picked = -1;
        private float pickTime;
        private bool anyLeft;
        private bool gone;
        private float time;
        private Vector2 lastMouse;
        private string over = "";
        private string doneLine = "";

        public RehearsalScreen(From from)
        {
            this.from = from;
        }

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD)
        public override string WipeWord
        {
            get { return "THE REHEARSAL"; }
        }

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            if (from == From.Fight) run.RehearsalPending = false;                         // the win's star is picked here
            band = run.Lineup();
            over = from == From.Fight ? "AFTER THE CONCERT" : "A QUIET ROOM";

            int count = band.Count;
            faces = new Rectangle[count];
            powerText = new string[count];
            int n = count * RehearsalList.Buttons;
            cards = new Rectangle[n];
            names = new string[n];
            fromText = new string[n];
            toText = new string[n];
            legendText = new string[n];
            stars = new int[n];
            can = new bool[n];
            kind = new Color[n];
            lift = new float[n];

            int total = count * ColumnW + Math.Max(0, count - 1) * ColumnGap;
            int left = (TacetGame.ScreenW - total) / 2;
            for (int c = 0; c < count; c++)
            {
                int x = left + c * (ColumnW + ColumnGap);
                faces[c] = new Rectangle(x, ColumnsTop, FaceSize, FaceSize);
                for (int b = 0; b < RehearsalList.Buttons; b++)
                    cards[c * RehearsalList.Buttons + b] = new Rectangle(x, CardsTop + b * (CardH + CardGap), ColumnW, CardH);
                Words(run, c);
            }

            anyLeft = run.AnyStarLeft;
            for (int i = 0; i < n && selected < 0; i++)
                if (can[i]) selected = i;
            lastMouse = Input.MousePos;
            SoundBank.PlayMusic(from == From.Fight ? Music.Victory : Music.Rest);

            //The Lens : a dark rehearsal room, a light over each legend
            Lens.Ambient = 0.38f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.55f;
            Lens.BloomThreshold = 0.72f;
            Lens.BloomStrength = 0.4f;
        }

        //Words : column c's power and its three cards, as the stars are now
        private void Words(RunState run, int c)
        {
            Musician m = band[c];
            int power = run.PowerOf(m);
            powerText[c] = "POWER " + power;
            for (int b = 0; b < RehearsalList.Buttons; b++)
            {
                int i = c * RehearsalList.Buttons + b;
                Star next = RehearsalList.Next(m, b);
                Skill s = RehearsalList.SkillOf(m, b);
                names[i] = RehearsalList.ButtonName(m, b);
                stars[i] = RehearsalList.StarsOf(m, b);
                can[i] = next != null;
                kind[i] = Palette.Kind(s != null ? s.Kind : SkillKind.Attack);
                fromText[i] = RehearsalList.FromText(m, b, power);
                toText[i] = RehearsalList.ToText(m, b, power);
                legendText[i] = next != null ? next.LineWrapped : "";
            }
        }

        public override void Update(float dt)
        {
            time += dt;
            for (int i = 0; i < lift.Length; i++)
            {
                bool up = picked < 0 ? i == selected && can[i] : i == picked;
                lift[i] = MathHelper.Clamp(lift[i] + (up ? dt : -dt) / BattleRules.UiHoverTime, 0f, 1f);
            }

            //Leaving : the new star has a moment on its own first
            if (picked >= 0)
            {
                pickTime += dt;
                if (pickTime > LeaveAfter && !gone)
                {
                    gone = true;
                    GoOn();
                }
                return;
            }

            //Nothing To Rehearse : every button has its three stars, a click or ENTER moves on
            if (!anyLeft)
            {
                if (time > 0.5f && !gone && (Input.MouseClicked() || Input.KeyPressed(Keys.Enter)))
                {
                    gone = true;
                    GoOn();
                }
                return;
            }

            //Pointer and Keys : the arrows go across the legends (left, right) and down their buttons (up, down)
            int before = selected;
            if (Input.MousePos != lastMouse)
                for (int i = 0; i < cards.Length; i++)
                    if (Input.MouseOver(cards[i])) selected = i;
            lastMouse = Input.MousePos;
            int buttons = RehearsalList.Buttons;
            if (selected >= 0)
            {
                if ((Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A)) && selected - buttons >= 0) selected -= buttons;
                if ((Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D)) && selected + buttons < cards.Length) selected += buttons;
                if ((Input.KeyPressed(Keys.Up) || Input.KeyPressed(Keys.W)) && selected % buttons > 0) selected--;
                if ((Input.KeyPressed(Keys.Down) || Input.KeyPressed(Keys.S)) && selected % buttons < buttons - 1) selected++;
            }
            if (selected != before) SoundBank.Play(Sfx.UiMove);

            //Pick : once the columns are in
            if (time < 0.5f) return;
            int choice = -1;
            if (selected >= 0 && (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space))) choice = selected;
            for (int i = 0; i < cards.Length; i++)
                if (Input.ClickedOn(cards[i])) choice = i;
            if (choice >= 0) Pick(choice);
        }

        //Pick : card i's button gets its star (RunState.Raise), its words stay as they were so the player sees what was earned
        private void Pick(int i)
        {
            if (!can[i])
            {
                SoundBank.Play(Sfx.UiDenied);
                return;
            }
            RunState run = Game.CurrentRun;
            int c = i / RehearsalList.Buttons;
            Musician m = band[c];
            if (!run.Raise(m, i % RehearsalList.Buttons)) return;
            selected = i;
            picked = i;
            pickTime = 0f;
            stars[i]++;
            powerText[c] = "POWER " + run.PowerOf(m);
            doneLine = m.Name + "   " + RehearsalList.ButtonName(m, i % RehearsalList.Buttons) + "   " + StarWords[Math.Min(stars[i], StarWords.Length - 1)];
            run.RefreshLabels();
            SoundBank.Play(Sfx.Rehearse);
        }

        private static readonly string[] StarWords = { "", "1 STAR", "2 STARS", "3 STARS" };

        //Go On : an ELITE's or a BOSS's motif after the fight's star, else the next stage
        private void GoOn()
        {
            RunState run = Game.CurrentRun;
            if (from == From.Fight && run.MotifAfterRehearsal)
            {
                run.MotifAfterRehearsal = false;
                Game.Screens.Change(new MotifRewardScreen());
                return;
            }
            RunFlow.NextStage(Game);
        }

        //Column In : the columns come in one after another
        private float ColumnIn(int c)
        {
            return UiMotion.Between(time, 0.15f + c * 0.08f, BattleRules.UiEnterTime);
        }

        //Shown : card i this frame, stepped to the right when pointed (like the battle's buttons)
        private Rectangle Shown(int i)
        {
            Rectangle r = cards[i];
            r.X += (int)Math.Round(6f * UiMotion.EaseOut(lift[i]));
            r.Y += (int)Math.Round((1f - ColumnIn(i / RehearsalList.Buttons)) * 30f);
            return r;
        }

        //======================================================================================
        //The World (THE LENS) : a dark room, a light over each legend
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        public override void DrawWorld(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(-120, 560, 1520, 220), Palette.Paper * 0.05f);
        }

        //Lights : a soft light over each column, the pointed card lit, the picked one lit in the musician's colour
        public override void DrawLights(SpriteBatch sb)
        {
            for (int c = 0; c < faces.Length; c++)
            {
                float a = ColumnIn(c);
                Gfx.DrawGlowBox(sb, new Rectangle(faces[c].X - 20, 40, ColumnW + 40, 600), Color.White * (0.16f * a));
                Gfx.DrawGlow(sb, faces[c].X + ColumnW / 2f, faces[c].Center.Y, 300f, Color.White * (0.3f * a));
            }
            for (int i = 0; i < cards.Length; i++)
            {
                float k = UiMotion.EaseOut(lift[i]);
                if (k <= 0f) continue;
                Rectangle r = Shown(i);
                Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, 230f, Color.White * (0.3f * k));
            }
        }

        //Glow : the picked card flashes in the musician's colour (not with LESS FLASHING)
        public override void DrawGlow(SpriteBatch sb)
        {
            if (picked < 0 || Settings.ReduceFlashing || pickTime > 0.7f) return;
            Rectangle r = Shown(picked);
            float k = pickTime / 0.7f;
            Color spot = BandColumn.SpotOf(band[picked / RehearsalList.Buttons]);
            Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, 200f + 120f * k, spot * (0.6f * (1f - k)));
        }

        //======================================================================================
        //The Words and the Cards (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;
            float cx = TacetGame.ScreenW / 2f;

            //Heading
            float head = UiMotion.Between(time, 0.05f, BattleRules.UiEnterTime);
            float rise = (1f - head) * BattleRules.UiEnterSlide;
            Gfx.TextSpacedCentered(sb, Game.Font, over, cx, 74 - rise, Palette.LineGrey * head, TextSize.Tiny, 5f);
            Gfx.TextCentered(sb, Game.BigFont, anyLeft ? "Rehearse one move" : "Every move has its three stars", cx, 112 - rise, Palette.Paper * head, TextSize.Title);
            Gfx.TextCentered(sb, Game.StoryFont, "It gets a star. Three stars make it a legend.", cx, 146 - rise, Palette.PaperDim * head, TextSize.Story);

            for (int c = 0; c < faces.Length; c++) DrawLegend(sb, c);
            for (int i = 0; i < cards.Length; i++) DrawCard(sb, i);

            //Foot : what was earned, or how to choose
            float foot = UiMotion.Between(time, 0.5f, BattleRules.UiEnterTime);
            if (picked >= 0) Gfx.TextSpacedCentered(sb, Game.Font, doneLine, cx, 652, BandColumn.SpotOf(band[picked / RehearsalList.Buttons]), TextSize.Label, 3f);
            else if (anyLeft) Gfx.TextSpacedCentered(sb, Game.Font, "CLICK A MOVE   /   IT COUNTS FROM THE NEXT FIGHT", cx, 652, Palette.LineGrey * foot, TextSize.Tiny, 2f);
            else Gfx.TextSpacedCentered(sb, Game.Font, "CLICK TO GO ON", cx, 652, Palette.LineGrey * foot, TextSize.Tiny, 2f);

            RunHud.DrawTop(sb, run, "REHEARSAL");
            RunHud.DrawTips(sb, run);
        }

        //Draw Legend : the head of column c, the face (the art at the size it was drawn, the empty frame until it comes), the
        //name with a stroke of their colour, the name the legends give them and their power
        private void DrawLegend(SpriteBatch sb, int c)
        {
            Musician m = band[c];
            float a = ColumnIn(c);
            Rectangle face = faces[c];
            face.Y += (int)Math.Round((1f - a) * 30f);
            Color spot = BandColumn.SpotOf(m);
            Gfx.Rect(sb, face, Palette.CanvasDark * a);
            ArtBank.DrawOrSlot(sb, UiArt.FaceOf(m), face, Palette.Paper, a);

            float x = face.Right + 14;
            Gfx.TextSpaced(sb, Game.BigFont, m.Name, x, face.Y + 8, Palette.Highlight * a, TextSize.Small, 3f);
            Gfx.Rect(sb, x, face.Y + 44, Math.Min(ColumnW - FaceSize - 20, Gfx.SpacedWidth(Game.BigFont, m.Name, TextSize.Small, 3f)), 2f, spot * a);
            Gfx.TextSpaced(sb, Game.Font, m.Epithet, x, face.Y + 54, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, powerText[c], x, face.Y + 74, Palette.PaperDim * a, TextSize.Tiny, 2f);
        }

        //Draw Card : one button, its kind's band, its name and stars, then what the next star changes (before, an arrow, after)
        //and the legend's line on a third star. A button with its three stars is dimmed. The picked one keeps its words and
        //its new star pops in.
        private void DrawCard(SpriteBatch sb, int i)
        {
            int c = i / RehearsalList.Buttons;
            Color spot = BandColumn.SpotOf(band[c]);
            Rectangle r = Shown(i);
            bool pointed = picked < 0 && i == selected && can[i];
            float fade = picked >= 0 && i != picked ? 1f - MathHelper.Clamp(pickTime / 0.4f, 0f, 0.55f) : 1f;
            float a = ColumnIn(c) * fade * (can[i] || i == picked ? 1f : 0.45f);

            Gfx.Rect(sb, new Rectangle(r.X + 5, r.Y + 5, r.Width, r.Height), Color.Black * (0.45f * a));
            Gfx.Rect(sb, r, Color.Lerp(Palette.Void, Palette.Panel, 0.5f * UiMotion.EaseOut(lift[i])) * (0.95f * a));
            Gfx.Rect(sb, r.X, r.Y, 5, r.Height, kind[i] * a);                                            // SKILL KIND
            Gfx.RectOutline(sb, r, (pointed || i == picked ? spot : Palette.LineGrey * 0.6f) * a, pointed || i == picked ? 2 : 1);

            //Name and Stars : the new star pops in bigger first, a ring going out from it
            Gfx.TextSpaced(sb, Game.Font, names[i], r.X + 18, r.Y + 10, Palette.Highlight * a, TextSize.Body, 2f);
            float sx = r.Right - 58;
            float sy = r.Y + 21;
            Ui.Stars(sb, sx, sy, i == picked ? stars[i] - 1 : stars[i], RehearsalList.Most, 6f, 18f, spot, a);
            if (i == picked)
            {
                float k = MathHelper.Clamp(pickTime / 0.35f, 0f, 1f);
                float px = sx + (stars[i] - 1) * 18f;
                Gfx.Star(sb, px, sy, 6f + 8f * (1f - UiMotion.EaseOut(k)), spot * a);
                if (k < 1f) Gfx.CircleOutline(sb, px, sy, 8f + 22f * k, spot * (a * (1f - k)), 1.5f);
            }

            //The Next Star : before, the arrow, after (in the musician's colour), and a legend's line
            if (!can[i] && i != picked)
            {
                Gfx.TextSpaced(sb, Game.Font, "ALL THREE STARS", r.X + 18, r.Y + 44, Palette.LineGrey * a, TextSize.Tiny, 2f);
                return;
            }
            float y = r.Y + 42;
            Gfx.Text(sb, Game.Font, fromText[i], r.X + 18, y, Palette.PaperDim * a, TextSize.Label);
            float ax = r.X + 18 + Gfx.TextWidth(Game.Font, fromText[i], TextSize.Label) + 10f;
            float ay = y + Game.Font.LineSpacing * TextSize.Label / 2f;
            Arrow(sb, ax, ay, spot * a);
            Gfx.Text(sb, Game.Font, toText[i], ax + 24f, y, Color.Lerp(Palette.Highlight, spot, 0.5f) * a, TextSize.Label);
            if (legendText[i].Length > 0) Gfx.Text(sb, Game.StoryFont, legendText[i], r.X + 18, r.Y + 66, Palette.Paper * a, TextSize.StorySmall);
        }

        //Arrow : a small arrow pointing right, from x
        private static void Arrow(SpriteBatch sb, float x, float y, Color color)
        {
            Gfx.Line(sb, x, y, x + 14f, y, color, 2f);
            Gfx.Line(sb, x + 8f, y - 5f, x + 14f, y, color, 2f);
            Gfx.Line(sb, x + 8f, y + 5f, x + 14f, y, color, 2f);
        }
    }
}
