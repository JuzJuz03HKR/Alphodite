using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //ChapterScreen : the card that opens every floor, staged like a Limbus Company canto card.
    //A floor is a MOVEMENT of the piece. It plays by itself for a few seconds, and a click or a key skips it.
    //Next comes the era choice that opens the floor.
    //
    //Beta1 screens round 2 (29 Oct, docs/SCREENS_LEVEL_UP.md 19) : TACET's real hole hangs in the middle (the shader of the
    //battle), bigger with every movement, and opens with a shock wave. A ring of ticks draws itself round it, a line of
    //light runs out from it both ways, and the movement's name rises letter by letter out of the hole's rim.
    //No dust or specks : the user does not like white dots on a page with words.
    public class ChapterScreen : GameScreen
    {
        private const float ShowTime = 3.6f;

        //Movement Text : one per floor. THE PLACE TO EDIT the words on the card.
        private static string[] numerals = { "I", "II", "III" };
        private static string[] names = { "PRELUDE", "NOCTURNE", "CODA" };
        private static string[] lines =
        {
            "The hall is full. Nobody makes a sound.",
            "Somewhere, a note was left unfinished.",
            "Four minutes and thirty-three seconds. Then nothing."
        };

        //Card Layout : TACET's hole, bigger with every movement
        private static readonly Vector2 Hole = new Vector2(640f, 236f);
        private static readonly float[] holeSizes = { 74f, 88f, 104f };

        private string movementLabel = "";
        private string nameLabel = "";
        private string lineLabel = "";
        private string floorLabel = "";
        private string[] nameLetters = new string[0];      // the name one letter at a time, made in Load
        private float[] letterX = new float[0];
        private float nameW;
        private float holeSize;
        private float time;
        private bool opened;

        //Wipe Word : the movement's number runs through the wipe, and the card itself shows its name (WIPE WORD)
        private static readonly string[] wipeWords = { "MOVEMENT I", "MOVEMENT II", "MOVEMENT III" };

        public override string WipeWord
        {
            get { return Game.CurrentRun == null ? null : wipeWords[Math.Max(1, Math.Min(Game.CurrentRun.Floor, wipeWords.Length)) - 1]; }
        }

        public override void Load()
        {
            int floor = Math.Max(1, Math.Min(Game.CurrentRun.Floor, numerals.Length));
            movementLabel = "MOVEMENT  " + numerals[floor - 1];
            nameLabel = names[floor - 1];
            lineLabel = lines[floor - 1];
            floorLabel = Game.CurrentRun.FloorShort;
            holeSize = holeSizes[floor - 1];

            //Name Letters : where each letter starts inside the name, so the letters can rise one by one
            float scale = TextSize.Banner * 0.9f;
            nameLetters = new string[nameLabel.Length];
            letterX = new float[nameLabel.Length];
            for (int i = 0; i < nameLabel.Length; i++)
            {
                nameLetters[i] = nameLabel.Substring(i, 1);
                letterX[i] = Game.LogoFont.MeasureString(nameLabel.Substring(0, i)).X * scale;
            }
            nameW = Game.LogoFont.MeasureString(nameLabel).X * scale;

            //TACET and the page's light : a dark hall, the rim of the hole glowing
            Dark = new TacetVoid();
            Lens.Ambient = 0.4f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.7f;
            Lens.BloomThreshold = 0.62f;
            Lens.BloomStrength = 0.6f;

            SoundBank.Play(Sfx.PageTurn);

            //Save : a new floor opens here, so CONTINUE comes back to its choice of era
            SaveFile.SaveRun(Game.CurrentRun);
        }

        public override void Update(float dt)
        {
            time += dt;

            //TACET : opens a little past its size and settles, with a shock wave, then breathes
            float open = UiMotion.EaseBack(MathHelper.Clamp(time / 0.6f, 0f, 1f));
            float breath = 1f + 0.035f * (float)Math.Sin(time * 0.9f);
            Dark.Update(dt, 0.35f);
            Dark.PlaceOnPage(Hole, holeSize * open * breath, 0f, 0f, 0f);
            if (!opened && time > 0.15f)
            {
                opened = true;
                Dark.Hit(Hole + new Vector2(-1f, 0f), true, 0.9f);
            }

            bool skip = time > 0.6f && (Input.MouseClicked() || Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space));
            if (time >= ShowTime || skip)
                Game.Screens.Change(new EraChoiceScreen(true));
        }

        //======================================================================================
        //The World (THE LENS) : the dark hall and TACET's hole
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //Back : what the hole bends. A dark hall, a pale floor of light under the words, and the line of light.
        public override void DrawBack(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.Void);
            Gfx.DrawGlowBox(sb, new Rectangle(-200, 300, 1680, 500), Palette.Paper * 0.05f);

            //Line Of Light : runs out of the hole both ways, under the movement's number
            float run = UiMotion.Between(time, 0.35f, 0.9f);
            float half = 560f * run;
            Gfx.Rect(sb, Hole.X - half, 432f, half * 2f, 1f, Palette.Paper * (0.5f * run));
            Gfx.Rect(sb, Hole.X - half * 0.4f, 431f, half * 0.8f, 3f, Palette.Paper * (0.8f * run));
        }

        //World : the dark bits the hole pulls in, and the ring of ticks round it
        public override void DrawWorld(SpriteBatch sb)
        {
            Dark.DrawDebris(sb, false);
            float ring = UiMotion.Between(time, 0.25f, 0.9f);
            Ornament.TickRing(sb, Hole.X, Hole.Y, holeSize + 34f, 60, time * 0.05f, ring, Palette.Paper * 0.55f);
            Gfx.Ring(sb, Hole.X, Hole.Y, holeSize + 70f, Palette.Paper * (0.12f * ring), 1f);
        }

        //Glow : the line of light glows
        public override void DrawGlow(SpriteBatch sb)
        {
            float run = UiMotion.Between(time, 0.35f, 0.9f);
            Gfx.DrawGlowBox(sb, new Rectangle((int)(Hole.X - 420f * run), 412, (int)(840f * run), 40), Palette.Paper * (0.25f * run));
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            float cx = TacetGame.ScreenW / 2f;

            //Movement : wide spaced letters on the line of light
            float m = UiMotion.Between(time, 0.45f, BattleRules.UiEnterTime);
            Gfx.TextSpacedCentered(sb, Game.Font, movementLabel, cx, 400 - (1f - m) * 12f, Palette.Highlight * m, TextSize.Heading, 16f);

            //Name : the letters rise out of the rim of the hole one after another and settle on their line
            float left = cx - nameW / 2f;
            for (int i = 0; i < nameLetters.Length; i++)
            {
                float k = UiMotion.Between(time, 0.6f + i * 0.06f, 0.45f);
                if (k <= 0f) continue;
                float y = MathHelper.Lerp(Hole.Y + holeSize * 0.6f, 462f, UiMotion.EaseOut(k));
                Gfx.Text(sb, Game.LogoFont, nameLetters[i], left + letterX[i], y, Palette.Paper * k, TextSize.Banner * 0.9f);
            }

            float l = UiMotion.Between(time, 1.1f, 0.5f);
            Gfx.TextCentered(sb, Game.StoryFont, lineLabel, cx, 566, Palette.PaperDim * l, TextSize.Story);

            //Corners : where we are, and how to move on
            Gfx.TextSpaced(sb, Game.Font, "TACET 4'33", 40, 680, Palette.LineGrey * l, TextSize.Tiny, 3f);
            Gfx.TextSpacedRight(sb, Game.Font, floorLabel, TacetGame.ScreenW - 40, 680, Palette.LineGrey * l, TextSize.Tiny, 3f);
            if (time > 1.2f)
                Gfx.TextSpacedCentered(sb, Game.Font, "CLICK TO CONTINUE", cx, 680, Palette.LineGrey * l, TextSize.Tiny, 3f);
        }
    }
}
