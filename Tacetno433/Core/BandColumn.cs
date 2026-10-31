using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //BandColumn (Beta1, 29 Oct, docs/BETA1_DIRECTION.md 3.2) : one member of the band as a tall slanted column (menu8).
    //The picture cut by the column (UiArt.Busts : bust_<name>.png, 160 x 256, drawn at the size it was drawn), the name with
    //its slash, the instrument they fight with, and their two skills as small cards. Pointed at, the column steps up and
    //their own colour rises from its foot (menu6). The band page, the audition (TeamPickScreen), the name page and the
    //luthier stand them in a row, and Detail explains the one pointed at in the box under the row.
    //The cards under a column are a first try : the user has an idea for the band's cards that is still to come
    //(BETA1_DIRECTION 4), so they only show a skill's name and its BREATH for now.
    public static class BandColumn
    {
        public const int PictureH = 256;
        public const float Slant = 18f;
        public const int Lift = 8;                   // a pointed column steps up this far (pixels)
        public const int WordsH = 108;               // the room the words take under a column

        //Spot Of : a musician's own colour (Musician.Spot)
        public static Color SpotOf(Musician m)
        {
            return m.Spot == SpotLook.Red ? Palette.SpotRed : Palette.SpotGold;
        }

        //Picture : the slanted window, the picture in it (or the empty slot), their colour rising when pointed, the edge
        public static void Picture(SpriteBatch sb, Rectangle r, Texture2D art, Color spot, float lit, float alpha)
        {
            Ui.SlantCut(sb, null, r, Slant, Palette.Panel * alpha);
            if (art != null) Ui.SlantCut(sb, art, r, Slant, Color.White * (MathHelper.Lerp(0.8f, 1f, lit) * alpha));
            else Ui.SlantSlot(sb, r, Slant, alpha);
            if (lit > 0f) Ui.ColourRise(sb, r, Slant, spot, 0.48f * lit, alpha);
            Ui.SlantEdge(sb, r, Slant, Color.Lerp(Palette.LineGrey * 0.55f, Palette.Paper, lit) * alpha);
        }

        //Draw : a musician's whole column, the picture and the words under it. dim : waiting on the bench.
        public static void Draw(SpriteBatch sb, Rectangle r, Musician m, float lit, float alpha, bool dim)
        {
            float a = alpha * (dim ? 0.55f : 1f);
            Picture(sb, r, UiArt.BustOf(m), SpotOf(m), lit, a);
            Words(sb, r, m.NameTag, m.Instrument, SkillList.First(m).Name, SkillList.First(m).Cost, SkillList.Second(m).Name, SkillList.Second(m).Cost, SpotOf(m), lit, a);
        }

        //Words : the name with its slash, the instrument, and two small cards (a name and a cost, cost 0 for none)
        public static void Words(SpriteBatch sb, Rectangle r, string name, string kind, string first, int firstCost, string second, int secondCost, Color spot, float lit, float alpha)
        {
            float x = r.X + 2f;
            float y = r.Bottom + 10f;
            float room = r.Width - 4f;

            //Name : serif, smaller when it would not fit, a stroke of their colour under it when pointed
            float scale = TextSize.Subtitle;
            float width = Gfx.TextWidth(Ui.BigFont, name, scale);
            if (width > room) scale *= room / width;
            Gfx.Text(sb, Ui.BigFont, name, x, y, Color.Lerp(Palette.Paper, Palette.Highlight, lit) * alpha, scale);
            if (lit > 0f) Ui.InkBar(sb, x - 2f, y + 24f, (Math.Min(width, room) + 4f) * lit, 3f, 3f, spot * alpha, name.Length * 7, 0.6f);

            //Instrument
            Gfx.TextSpaced(sb, Ui.Font, kind, x, y + 32f, Palette.LineGrey * alpha, TextSize.Tiny, 2f);

            //Cards : the two skills
            Chip(sb, x, y + 52f, room, first, firstCost, lit, alpha);
            Chip(sb, x, y + 76f, room, second, secondCost, lit, alpha);
        }

        //Chip : one small card, the name on the left and its BREATH on the right
        private static void Chip(SpriteBatch sb, float x, float y, float width, string name, int cost, float lit, float alpha)
        {
            Rectangle box = new Rectangle((int)x, (int)y, (int)width, 20);
            Gfx.Rect(sb, box, Palette.Panel * (0.9f * alpha));
            Gfx.RectOutline(sb, box, Color.Lerp(Palette.LineGrey * 0.45f, Palette.PaperDim, lit) * alpha, 1);
            float costW = cost > 0 ? 18f : 0f;
            float scale = TextSize.Tiny;
            float wide = Gfx.SpacedWidth(Ui.Font, name, scale, 1f);
            float room = width - 12f - costW;
            if (wide > room) scale *= room / wide;
            Gfx.TextSpaced(sb, Ui.Font, name, x + 6f, y + 4f, Palette.Paper * alpha, scale, 1f);
            if (cost > 0)
            {
                Gfx.Diamond(sb, box.Right - 15f, box.Center.Y, 3f, Palette.PaperDim * alpha);
                Gfx.Text(sb, Ui.Font, NumberText.Get(cost), box.Right - 10f, y + 2f, Palette.Paper * alpha, TextSize.Tiny);
            }
        }

        //Skill Text : each musician's two skills explained, by MusicianList index, wrapped once for Detail (a page's Load)
        public static string[] SkillText(SpriteFont font)
        {
            string[] text = new string[MusicianList.All.Length];
            for (int i = 0; i < MusicianList.All.Length; i++)
            {
                Musician m = MusicianList.All[i];
                Skill first = SkillList.First(m);
                Skill second = SkillList.Second(m);
                text[i] = Gfx.WrapText(font, first.Name + " : " + first.Text, 470, TextSize.StorySmall, 2) + "\n"
                        + Gfx.WrapText(font, second.Name + " : " + second.Text, 470, TextSize.StorySmall, 2);
            }
            return text;
        }

        //Detail : one musician explained in the box under the row. The name, the name the legends give them (or the family
        //for a hidden one from an old save), life and power, the weapon their instrument becomes at its tier (WEAPON TIERS),
        //then their trait and both skills (skills from SkillText).
        public static void Detail(SpriteBatch sb, Rectangle box, Musician m, RunState run, string skills, float a)
        {
            int x = box.X + 18;
            int y = box.Y + 12;
            Gfx.Text(sb, Ui.BigFont, m.NameTag, x, y - 2, SpotOf(m) * a, TextSize.Subtitle);
            if (m.Key != GiftKey.Band)                                                       // GIFTS : their mark, the icon of the gifts for them
                MotifArt.Keyword(sb, m.Key, x + Gfx.TextWidth(Ui.BigFont, m.NameTag, TextSize.Subtitle) + 18f, y + 12, 20, a);
            Gfx.TextSpaced(sb, Ui.Font, m.Epithet.Length > 0 ? m.Epithet : m.FamilyLabel, x, y + 30, Palette.PaperDim * a, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Ui.Font, "LIFE", x, y + 56, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Gfx.Text(sb, Ui.Font, NumberText.Get(run.LifeOf(m)), x + 44, y + 52, Palette.Paper * a, TextSize.Body);
            Gfx.Text(sb, Ui.Font, NumberText.Get(run.MaxLifeOf(m)), x + 84, y + 56, Palette.LineGrey * a, TextSize.Tiny);
            Gfx.TextSpaced(sb, Ui.Font, "POWER", x + 130, y + 56, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Gfx.Text(sb, Ui.Font, NumberText.Get(run.PowerOf(m)), x + 186, y + 52, Palette.Paper * a, TextSize.Body);
            Gfx.TextSpaced(sb, Ui.Font, "WEAPON", x, y + 80, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Ui.Font, m.WeaponNow, x + 74, y + 80, Palette.Paper * a, TextSize.Tiny, 2f);

            //Trait and Skills
            Gfx.Rect(sb, x + 250, y, 1, 86, Palette.LineGrey * (0.5f * a));
            Ui.Tag(sb, m.TraitName, x + 266, y, true, a);
            Gfx.Text(sb, Ui.StoryFont, m.TraitWrapped, x + 266, y + 26, Palette.Paper * a, TextSize.StorySmall);
            Gfx.Text(sb, Ui.StoryFont, skills, x + 600, y, Palette.Paper * a, TextSize.StorySmall);
        }

        //Tier Pips : WEAPON TIERS, three small diamonds, the tiers the weapon has filled
        public static void TierPips(SpriteBatch sb, float x, float y, Musician m, Color spot, float alpha)
        {
            for (int t = 0; t < BattleRules.RehearseMax; t++)
            {
                float px = x + t * 12f;
                if (t < m.Rehearsed) Gfx.Diamond(sb, px, y, 4f, spot * alpha);
                else Gfx.DiamondOutline(sb, px, y, 4f, Palette.LineGrey * (0.8f * alpha), 1f);
            }
        }

        //Empty : a place nobody fills yet, a plus in a faint slanted window (menu8)
        public static void Empty(SpriteBatch sb, Rectangle r, float alpha)
        {
            Ui.SlantCut(sb, null, r, Slant, Palette.Paper * (0.025f * alpha));
            Ui.SlantEdge(sb, r, Slant, Palette.LineGrey * (0.3f * alpha));
            float cx = r.Center.X;
            float cy = r.Center.Y;
            Gfx.Rect(sb, cx - 12f, cy, 25f, 1f, Palette.LineGrey * (0.7f * alpha));
            Gfx.Rect(sb, cx, cy - 12f, 1f, 25f, Palette.LineGrey * (0.7f * alpha));
        }
    }
}
