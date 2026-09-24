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
    //A floor is a MOVEMENT of the piece. The black sun hangs over the title, holes of silence
    //drift up through the dark, and a streak of light runs through the movement's name.
    //It plays by itself for a few seconds, and a click or a key skips it.
    //Next comes the era choice that opens the floor.
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

        private string movementLabel = "";
        private string nameLabel = "";
        private string lineLabel = "";
        private string floorLabel = "";
        private float time;

        public override void Load()
        {
            int floor = Math.Max(1, Math.Min(Game.CurrentRun.Floor, numerals.Length));
            movementLabel = "MOVEMENT  " + numerals[floor - 1];
            nameLabel = names[floor - 1];
            lineLabel = lines[floor - 1];
            floorLabel = Game.CurrentRun.FloorShort;
            SoundBank.Play(Sfx.PageTurn);

            //Save : a new floor opens here, so CONTINUE comes back to its choice of era
            SaveFile.SaveRun(Game.CurrentRun);
        }

        public override void Update(float dt)
        {
            time += dt;
            bool skip = time > 0.6f && (Input.MouseClicked() || Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space));
            if (time >= ShowTime || skip)
                Game.Screens.Change(new EraChoiceScreen(true));
        }

        public override void Draw(SpriteBatch sb)
        {
            float cx = TacetGame.ScreenW / 2f;
            float rise = MathHelper.Clamp(time / 0.9f, 0f, 1f);
            rise = rise * rise * (3f - 2f * rise);
            float words = MathHelper.Clamp((time - 0.5f) / 0.7f, 0f, 1f);

            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.Void);

            //Holes : small glowing rings drifting upward, the same every time
            for (int i = 0; i < 26; i++)
            {
                float x = (i * 173) % TacetGame.ScreenW + (float)Math.Sin(time * 0.6f + i) * 12f;
                float y = (720f + 40f - ((i * 97) % 760 + time * (18f + (i % 5) * 9f)) % 800f);
                float r = 3f + (i % 4) * 3f;
                if (i % 3 == 0) Hollow.Ring(sb, x, y, r * 1.6f, 1.5f, 0.5f * rise);
                else Gfx.DrawGlow(sb, x, y, r * 2f, Palette.Paper * (0.3f * rise));
            }

            //Black Sun : grows into place over the title
            Hollow.Eclipse(sb, cx, 250f, 108f * (0.85f + 0.15f * rise), time * 0.35f, rise);

            //Movement : wide spaced letters with the light running straight through them
            Gfx.TextSpacedCentered(sb, Game.Font, movementLabel, cx, 404, Palette.Highlight * words, TextSize.Heading, 16f);
            Hollow.Streak(sb, cx, 444f, 1100f * words, words * 0.9f);
            Gfx.TextCentered(sb, Game.LogoFont, nameLabel, cx, 500, Palette.Paper * words, TextSize.Banner * 0.9f);
            Gfx.TextCentered(sb, Game.StoryFont, lineLabel, cx, 566, Palette.PaperDim * words, TextSize.Story);

            //Corners : where we are, and how to move on
            Gfx.TextSpaced(sb, Game.Font, "TACET 4'33", 40, 680, Palette.LineGrey * words, TextSize.Tiny, 3f);
            Gfx.TextSpacedRight(sb, Game.Font, floorLabel, TacetGame.ScreenW - 40, 680, Palette.LineGrey * words, TextSize.Tiny, 3f);
            if (time > 1.2f)
                Gfx.TextSpacedCentered(sb, Game.Font, "CLICK TO CONTINUE", cx, 680, Palette.LineGrey * words, TextSize.Tiny, 3f);

            Ornament.Vignette(sb, 90, 0.8f);
        }
    }
}
