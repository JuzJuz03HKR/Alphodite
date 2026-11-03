using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //PAGE GUIDE (3 Nov, the user 13.151 : "a NEWBIE mode, help, explanations and advice all the time on the first two floors") :
    //the GUIDE card on the pages of a NEWBIE run between fights (the map, the shop, the rehearsal ...). Each page says what it is
    //for and what would help now (GameScreen.GuideLine, one of its constants) and where the card sits (GuideBox, a place the
    //page leaves free). The fight has its own card (Screens/BattleScreen.Guide.cs).
    //The line is cut to the card's width once, the first time a page asks for it (Prepare, from Update), never while drawing.
    public static class PageGuide
    {
        public static readonly Rectangle DefaultBox = new Rectangle(330, 600, 620, 58);
        private const string Tag = "GUIDE";
        private const float Pad = 92f;                   // the line starts after the tag
        private static readonly Dictionary<string, string> cut = new Dictionary<string, string>();

        //Prepare : the line cut for its card (each line and card width once)
        public static void Prepare(string line, Rectangle box)
        {
            if (line == null || cut.ContainsKey(line)) return;
            cut[line] = Gfx.WrapText(Ui.StoryFont, line, box.Width - Pad - 14f, TextSize.StorySmall);
        }

        //Draw : the card, its tag, the line, faded in with the page (up 0..1)
        public static void Draw(SpriteBatch sb, string line, Rectangle box, float up)
        {
            string text;
            if (line == null || !cut.TryGetValue(line, out text) || up <= 0f) return;
            Ui.Panel(sb, box, 0.92f * up);
            Ui.Tag(sb, Tag, box.X + 14, box.Y + 9, true, up);
            float tall = Gfx.Measure(Ui.StoryFont, text).Y * TextSize.StorySmall;
            float y = box.Y + System.Math.Max(4f, (box.Height - tall) / 2f);
            Gfx.Text(sb, Ui.StoryFont, text, box.X + Pad, y, Palette.Paper * up, TextSize.StorySmall);
        }
    }
}
