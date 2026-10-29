using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //RouteScreen : THE ROAD, the floor's map (Beta1, 29 Oct, docs/BETA1_DIRECTION.md 3.1, docs/SCREENS_LEVEL_UP.md 17).
    //
    //The whole floor (Data/FloorMap.cs) lies round TACET's hole, one ring for every stage, seen lying on the floor at a
    //slant (menu7). The way in is the outer ring at the front, every ring after it is closer to the hole, and the boss
    //waits on the hole's rim. The hole grows as the band comes closer, and bends the rings near it (they are drawn in
    //DrawBack, the layer TACET's dark bends).
    //   PLACES   a circle with the kind's letter (RouteNodeInfo.Marks). The ones the band can walk to now are bright and a
    //            light runs along their ways toward them, the places further on are grey, the ones out of reach fade, the
    //            places walked through are filled and joined by a solid line.
    //   HERE     where the band stands : a ring of ticks turning round it, or the way in before the first place
    //   POINT    a place the band can walk to steps up and fills. The card beside the map shows its picture, its kind and
    //            its line. Any other place shows its card too, so the way through the floor can be planned.
    //   GO       a click or ENTER : the camera dives along the way into it, then the page changes.
    //TAB (or the button) opens the band page. ESC opens the pause menu, like every page of a run. Arriving here saves the run.
    public class RouteScreen : GameScreen
    {
        //Map Layout : TACET's hole, and the rings of the floor round it
        private static readonly Vector2 Hole = new Vector2(640f, 214f);
        private const float Flat = 0.58f;             // the rings lie on the floor : this much as tall as they are wide
        private const float OuterRing = 560f;         // row 2, the way in (pixels across, half)
        private const float InnerRing = 132f;         // the boss's ring, at the hole's rim
        private const float OuterStep = 20f;          // the places of the outer ring sit this many degrees apart round the hole,
        private const float InnerStep = 32f;          // the inner rings' a little more, so the rows close in like a funnel
        private const float NodeR = 14f;
        private const float BossR = 22f;
        private static readonly Vector2 Entrance = new Vector2(640f, 584f);   // where the band stands before the first place

        //Footer and Card
        private const int FooterY = 612;
        private Rectangle bandButton = new Rectangle(36, 648, 230, 44);
        private const int CardW = 290;
        private const int CardArtW = 230;             // the kind's picture, as drawn (docs/UI_ART_PLAN.md 3.3)
        private const int CardArtH = 142;

        //Route State
        private int selected;                         // which of run.Options is chosen
        private RouteNode pointed;                    // the place under the mouse (any place), or null
        private float time;
        private float diveTime = -1f;                 // GO : seconds into the dive, -1 before
        private bool entered;
        private Vector2 lastMouse;
        private UiMotion[] optionMotion = new UiMotion[0];
        private UiMotion bandMotion = new UiMotion();
        private string benchLabel = "";               // "2 ON THE BENCH" over the band button, made in Load
        private string[] cardLines = new string[0];   // each kind's line wrapped for the card, made in Load
        private float cardShown;                      // the card eases in and out
        private RouteNode cardNode;                   // the place the card shows
        private bool[][] ahead = new bool[0][];       // the places the band can still get to from where it stands, made in Load

        //Wipe Word : the page's name in the wipe on the way here (WIPE WORD, the user's pick)
        public override string WipeWord
        {
            get { return "THE ROAD"; }
        }

        public override void Load()
        {
            if (Game.CurrentRun == null) return;
            RunState run = Game.CurrentRun;
            selected = 0;

            //Bench Note : players waiting off stage are easy to forget, so the band button says so
            int benched = run.Roster.Count - run.Formation.SeatedCount;
            benchLabel = benched > 0 ? benched + " ON THE BENCH" : "";
            optionMotion = new UiMotion[run.Options.Length];
            for (int i = 0; i < optionMotion.Length; i++) optionMotion[i] = new UiMotion();
            lastMouse = Input.MousePos;

            //Ahead : every place still on the band's way, worked out once (drawing them reads it every frame)
            ahead = new bool[run.Map.Rows.Length][];
            for (int s = 0; s < ahead.Length; s++)
            {
                ahead[s] = new bool[run.Map.Count(s)];
                for (int c = 0; c < ahead[s].Length; c++) ahead[s][c] = run.Map.OnTheWay(run.Stage - 1, run.MapAt, s, c);
            }

            //Card Lines : every kind's line wrapped to the card once, never while drawing
            cardLines = new string[RouteNodeInfo.Captions.Length];
            for (int t = 0; t < cardLines.Length; t++)
                cardLines[t] = Gfx.WrapText(Game.StoryFont, RouteNodeInfo.Captions[t], CardW - 28, TextSize.StorySmall, 4);

            //TACET : the hole the whole floor closes in on, and the page's light
            Dark = new TacetVoid();
            Lens.Ambient = 0.42f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.55f;
            Lens.BloomThreshold = 0.7f;
            Lens.BloomStrength = 0.45f;

            //Save : every arrival on the route is a point CONTINUE can come back to
            SaveFile.SaveRun(run);
            SoundBank.PlayMusic(Music.Route);
        }

        //Progress : 0 at the way in, 1 at the boss
        private float Progress(RunState run)
        {
            int last = run.Map.LastRow;
            return last > 2 ? MathHelper.Clamp((run.Stage - 2f) / (last - 2f), 0f, 1f) : 0f;
        }

        //Hole Radius : TACET's hole grows with every place the band comes closer
        private float HoleRadius(RunState run)
        {
            float open = UiMotion.EaseBack(MathHelper.Clamp(time / 0.5f, 0f, 1f));
            float breath = 1f + 0.04f * (float)Math.Sin(time * 0.8f);
            return MathHelper.Lerp(34f, 66f, Progress(run)) * open * breath;
        }

        //Ring Of : the size of a row's ring (half its width)
        private static float RingOf(FloorMap map, int stage)
        {
            int last = map.LastRow;
            float t = last > 2 ? (stage - 2f) / (last - 2f) : 0f;
            return MathHelper.Lerp(OuterRing, InnerRing, t);
        }

        //Place At : where a place sits on the screen. A row's places spread round the front of its ring by the same angle
        //seen from the hole, so the outer rows are wide and the rows close in toward the hole like a funnel. The first
        //place is on the left, so the ways between two rows never cross (FloorMap's STAIRCASE).
        private static Vector2 PlaceAt(FloorMap map, int stage, int column)
        {
            int n = map.Count(stage);
            float r = RingOf(map, stage);
            int last = map.LastRow;
            float depth = last > 2 ? (stage - 2f) / (last - 2f) : 0f;
            float step = MathHelper.ToRadians(MathHelper.Lerp(OuterStep, InnerStep, depth));
            float angle = MathHelper.PiOver2 - (column - (n - 1) / 2f) * step;
            return new Vector2(Hole.X + r * (float)Math.Cos(angle), Hole.Y + Flat * r * (float)Math.Sin(angle));
        }

        //Here : where the band stands, the last place walked through or the way in
        private static Vector2 Here(RunState run)
        {
            if (run.MapAt >= 0 && run.Stage > 2) return PlaceAt(run.Map, run.Stage - 1, run.MapAt);
            return Entrance;
        }

        //Row In : ENTER, how far row s has come in. The rings and places come in from the outside toward the hole.
        private float RowIn(RunState run, int stage)
        {
            return UiMotion.Between(time, 0.12f + (stage - 2) * 0.05f, 0.4f);
        }

        public override void Update(float dt)
        {
            time += dt;
            if (Game.CurrentRun == null) { Game.Screens.Change(new TitleScreen()); return; }
            RunState run = Game.CurrentRun;
            RouteNode[] options = run.Options;

            //TACET : the hole breathes, a little wilder the closer the boss
            Dark.Update(dt, 0.3f + 0.35f * Progress(run));
            Dark.PlaceOnPage(Hole, HoleRadius(run), 0f, 0f, 0f);

            //Card : eases toward the place pointed at, or the chosen one
            RouteNode want = pointed != null ? pointed : (selected < options.Length ? options[selected] : null);
            if (want != cardNode)
            {
                cardShown = Math.Max(0f, cardShown - dt * 10f);
                if (cardShown <= 0f) cardNode = want;
            }
            else cardShown = Math.Min(1f, cardShown + dt * 6f);

            //GO : the camera dives along the way into the chosen place, then the place opens
            if (diveTime >= 0f)
            {
                diveTime += dt;
                float t = MathHelper.Clamp(diveTime / BattleRules.UiRouteDive, 0f, 1f);
                Lens.Zoom = BattleRules.UiRouteDiveZoom * UiMotion.EaseInOut(t);
                if (t >= 1f && !entered)
                {
                    entered = true;
                    RunFlow.Enter(Game, options[selected], null, null);      // this also saves the run, and starts the page change
                }
                return;
            }

            int before = selected;

            //Route Keyboard : the places the band can walk to, left to right
            if (options.Length > 0)
            {
                if (Input.KeyPressed(Keys.Right) || Input.KeyPressed(Keys.D)) selected = (selected + 1) % options.Length;
                if (Input.KeyPressed(Keys.Left) || Input.KeyPressed(Keys.A)) selected = (selected - 1 + options.Length) % options.Length;
            }

            //Route Mouse : any place under the mouse shows its card, a place the band can walk to is chosen too
            //(only once the mouse actually moves)
            pointed = PlaceUnder(run, Input.MousePos);
            if (Input.MousePos != lastMouse)
            {
                lastMouse = Input.MousePos;
                int over = Array.IndexOf(options, pointed);
                if (over >= 0) selected = over;
            }
            if (selected != before) SoundBank.Play(Sfx.UiMove);

            for (int i = 0; i < optionMotion.Length; i++) optionMotion[i].Update(dt, i == selected && RowIn(run, run.Stage) > 0.6f);
            bandMotion.Update(dt, Input.MouseOver(bandButton));

            //Band : look at the ensemble between fights, and choose who plays
            if (Input.KeyPressed(Keys.Tab) || Input.ClickedOn(bandButton))
            {
                bandMotion.Press();
                SoundBank.Play(Sfx.UiConfirm);
                Game.Screens.Change(new BandScreen());
                return;
            }

            //Route Confirm
            bool confirm = Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space);
            if (Input.MouseClicked() && pointed != null)
            {
                int at = Array.IndexOf(options, pointed);
                if (at >= 0) { selected = at; confirm = true; }
                else SoundBank.Play(Sfx.UiDenied);                        // a place further on, or out of reach
            }
            if (confirm && options.Length > 0) Choose(selected);
        }

        //Place Under : the place within reach of the mouse, or null
        private RouteNode PlaceUnder(RunState run, Vector2 mouse)
        {
            FloorMap map = run.Map;
            for (int s = 2; s <= map.LastRow; s++)
                for (int c = 0; c < map.Count(s); c++)
                {
                    float reach = (s == map.LastRow ? BossR : NodeR) + 10f;
                    if (Vector2.DistanceSquared(mouse, PlaceAt(map, s, c)) <= reach * reach) return map.Rows[s][c];
                }
            return null;
        }

        //Route Choose : the stop is written down, then the camera dives into the place (Update)
        private void Choose(int index)
        {
            RunState run = Game.CurrentRun;
            RouteNode node = run.Options[index];
            run.Chosen = node;
            run.RecordStop(node.Type);
            optionMotion[index].Press();
            SoundBank.Play(Sfx.PathChosen);
            Lens.ZoomAt = PlaceAt(run.Map, node.Row, node.Column);
            diveTime = 0f;
        }

        //======================================================================================
        //The World (THE LENS) : the rings on the floor, the ways, the places
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //Back : what TACET's hole bends. The dark floor and its rings, one for every stage, the band's ring brighter.
        public override void DrawBack(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(-200, 260, 1680, 700), Palette.Paper * 0.05f);
            if (Game.CurrentRun == null) return;
            RunState run = Game.CurrentRun;
            FloorMap map = run.Map;
            for (int s = 2; s <= map.LastRow; s++)
            {
                float a = RowIn(run, s);
                if (a <= 0f) continue;
                float r = RingOf(map, s);
                float near = 1f - 0.5f * (s - 2f) / Math.Max(1f, map.LastRow - 2f);       // the rings dim toward the hole
                Color ring = s == run.Stage ? Palette.Paper * (0.22f * a) : Palette.Paper * (0.08f * near * a);
                Ellipse(sb, Hole.X, Hole.Y, r, r * Flat, ring, a);
            }
        }

        //Ellipse : a ring lying on the floor. drawn 0..1 is how much of it is there yet, going round from the front.
        private static void Ellipse(SpriteBatch sb, float cx, float cy, float rx, float ry, Color color, float drawn)
        {
            const int pieces = 72;
            int shown = (int)(pieces * drawn);
            float start = MathHelper.PiOver2;
            Vector2 last = new Vector2(cx + rx * (float)Math.Cos(start), cy + ry * (float)Math.Sin(start));
            for (int p = 1; p <= shown; p++)
            {
                float angle = start + MathHelper.TwoPi * p / pieces;
                Vector2 now = new Vector2(cx + rx * (float)Math.Cos(angle), cy + ry * (float)Math.Sin(angle));
                Gfx.Line(sb, last, now, color, 1f);
                last = now;
            }
        }

        public override void DrawWorld(SpriteBatch sb)
        {
            if (Game.CurrentRun == null) return;
            RunState run = Game.CurrentRun;
            Dark.DrawDebris(sb, false);
            DrawWays(sb, run);
            DrawPlaces(sb, run);
            DrawHere(sb, run);
        }

        //Ways : thin lines between the places, like a map of the stars (menu4). The way already walked is solid, the
        //ways the band can take now are bright with a light running toward the place, the rest are faint.
        private void DrawWays(SpriteBatch sb, RunState run)
        {
            FloorMap map = run.Map;
            int from = run.Stage - 1;
            for (int s = 2; s < map.LastRow; s++)
            {
                float a = RowIn(run, s + 1);
                if (a <= 0f) continue;
                for (int c = 0; c < map.Count(s); c++)
                {
                    RouteNode node = map.Rows[s][c];
                    Vector2 p = PlaceAt(map, s, c);
                    for (int k = 0; k < node.Next.Count; k++)
                    {
                        int d = node.Next[k];
                        Vector2 q = PlaceAt(map, s + 1, d);
                        bool walked = Walked(run, s, c) && Walked(run, s + 1, d);
                        bool open = s == from && c == run.MapAt;
                        bool later = Ahead(s, c);
                        if (walked) Gfx.Line(sb, p, q, Palette.PaperDim * (0.9f * a), 2f);
                        else if (open) OpenWay(sb, p, q, Array.IndexOf(run.Options, map.Rows[s + 1][d]), a);
                        else Gfx.Line(sb, p, q, Palette.LineGrey * ((later ? 0.32f : 0.1f) * a), 1f);
                    }
                }
            }

            //The Way In : before the first place, the ways run from the entrance to every place of row 2
            if (run.Stage == 2 || run.MapAt < 0)
            {
                float a = RowIn(run, 2);
                for (int c = 0; c < map.Count(2) && run.Stage == 2; c++)
                    OpenWay(sb, Entrance, PlaceAt(map, 2, c), c, a);
            }
            else if (run.MapPath.Count > 0)
                Gfx.Line(sb, Entrance, PlaceAt(map, 2, run.MapPath[0]), Palette.PaperDim * 0.9f, 2f);
        }

        //Open Way : a way the band can take now. A bright line, and a light running along it toward the place.
        private void OpenWay(SpriteBatch sb, Vector2 p, Vector2 q, int option, float a)
        {
            bool chosen = option == selected;
            Gfx.Line(sb, p, q, (chosen ? Palette.Paper : Palette.PaperDim * 0.6f) * a, chosen ? 2f : 1f);
            float run = (time * 0.9f + option * 0.37f) % 1f;
            Vector2 at = Vector2.Lerp(p, q, run);
            Gfx.Circle(sb, at.X, at.Y, chosen ? 2.5f : 1.5f, Palette.Paper * (a * (1f - run) * (chosen ? 1f : 0.6f)));
        }

        //Ahead : whether a place is still on the band's way (the table made in Load)
        private bool Ahead(int stage, int column)
        {
            return stage < ahead.Length && column < ahead[stage].Length && ahead[stage][column];
        }

        //Walked : whether the band went through this place on this floor
        private static bool Walked(RunState run, int stage, int column)
        {
            int k = stage - 2;
            return k >= 0 && k < run.MapPath.Count && run.MapPath[k] == column;
        }

        //Places : every place of the floor. The ones within reach now are bright, the chosen one steps up and fills.
        private void DrawPlaces(SpriteBatch sb, RunState run)
        {
            FloorMap map = run.Map;
            int from = run.Stage - 1;
            for (int s = map.LastRow; s >= 2; s--)
            {
                float a = RowIn(run, s);
                if (a <= 0f) continue;
                float near = 1f - 0.35f * (s - 2f) / Math.Max(1f, map.LastRow - 2f);
                for (int c = 0; c < map.Count(s); c++)
                {
                    RouteNode node = map.Rows[s][c];
                    Vector2 p = PlaceAt(map, s, c);
                    bool boss = s == map.LastRow;
                    float r = boss ? BossR : NodeR;
                    int option = Array.IndexOf(run.Options, node);
                    string mark = RouteNodeInfo.Marks[(int)node.Type];

                    if (Walked(run, s, c))
                    {
                        Gfx.Circle(sb, p.X, p.Y, r * 0.8f, Palette.PaperDim * a);
                        Gfx.TextCentered(sb, Game.Font, mark, p.X, p.Y - 1f, Palette.Ink * a, TextSize.Label);
                        continue;
                    }

                    if (option >= 0)
                    {
                        //Within Reach : bright, and the chosen one steps up, fills and sends out a ring
                        UiMotion m = optionMotion[Math.Min(option, optionMotion.Length - 1)];
                        float e = m.Shown;
                        float size = r * (1f + 0.3f * e) * m.Scale;
                        float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 3f + option);
                        Gfx.Circle(sb, p.X, p.Y, size, Color.Lerp(Palette.Void, Palette.Paper, e) * a);
                        Gfx.CircleOutline(sb, p.X, p.Y, size, Palette.Paper * a, 1.5f);
                        Gfx.CircleOutline(sb, p.X, p.Y, size + 4f + 2f * pulse, Palette.Paper * ((0.25f + 0.2f * pulse) * (1f - e) * a), 1f);
                        if (m.Ring < 1f) Gfx.CircleOutline(sb, p.X, p.Y, size + 22f * m.Ring, Palette.Paper * ((1f - m.Ring) * 0.8f * a), 1.5f);
                        Gfx.TextCentered(sb, Game.Font, mark, p.X, p.Y - 1f, Color.Lerp(Palette.Paper, Palette.Ink, e) * a, boss ? TextSize.Body : TextSize.Label);
                        continue;
                    }

                    //Further On, or out of reach
                    float k = (Ahead(s, c) ? 1f : 0.35f) * near * a;
                    Gfx.Circle(sb, p.X, p.Y, r, Palette.Void * a);
                    Gfx.CircleOutline(sb, p.X, p.Y, r, Palette.LineGrey * k, 1f);
                    if (boss) Gfx.CircleOutline(sb, p.X, p.Y, r + 5f, Palette.LineGrey * (0.6f * k), 1f);
                    Gfx.TextCentered(sb, Game.Font, mark, p.X, p.Y - 1f, Palette.PaperDim * k, boss ? TextSize.Body : TextSize.Label);
                }
            }
        }

        //Here : where the band stands, a ring of ticks turning round it (menu4)
        private void DrawHere(SpriteBatch sb, RunState run)
        {
            float a = UiMotion.Between(time, 0.5f, 0.4f);
            if (a <= 0f) return;
            Vector2 here = Here(run);
            Ornament.TickRing(sb, here.X, here.Y, 24f, 30, time * 0.6f, a, Palette.Paper * 0.8f);
            Gfx.Circle(sb, here.X, here.Y, 4f, Palette.Paper * a);
        }

        //Lights : a soft light on where the band stands and on the chosen place (TACET adds its own)
        public override void DrawLights(SpriteBatch sb)
        {
            if (Game.CurrentRun == null) return;
            RunState run = Game.CurrentRun;
            float up = UiMotion.Between(time, 0f, 0.6f);
            Vector2 here = Here(run);
            Gfx.DrawGlow(sb, here.X, here.Y, 260f, Color.White * (0.42f * up));
            if (selected < run.Options.Length)
            {
                RouteNode node = run.Options[selected];
                Vector2 p = PlaceAt(run.Map, node.Row, node.Column);
                Gfx.DrawGlow(sb, p.X, p.Y, 220f, Color.White * (0.5f * up));
            }
        }

        //Glow : a halo on the chosen place
        public override void DrawGlow(SpriteBatch sb)
        {
            if (Game.CurrentRun == null) return;
            RunState run = Game.CurrentRun;
            if (selected >= run.Options.Length) return;
            RouteNode node = run.Options[selected];
            Vector2 p = PlaceAt(run.Map, node.Row, node.Column);
            Gfx.DrawGlow(sb, p.X, p.Y, 46f, Palette.Paper * (0.35f * UiMotion.Between(time, 0.4f, 0.4f)));
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            if (Game.CurrentRun == null) return;
            RunState run = Game.CurrentRun;

            DrawCard(sb, run);

            //Top Bar
            bool warn = run.BossIsNext && run.Stage < run.StagesThisFloor;
            RunHud.DrawTop(sb, run, warn ? "THE BOSS IS THE NEXT ROOM" : "CHOOSE A PATH");
            Gfx.TextSpaced(sb, Game.Font, run.FloorLabel, 36, RunHud.TopH + 12, Palette.LineGrey, TextSize.Tiny, 3f);

            DrawFooter(sb, run);
            RunHud.DrawTips(sb, run);
        }

        //Card : the place pointed at (or chosen), beside the map on the side away from it. Its picture as drawn
        //(ARTWORK : Content/Art/Route/route_<kind>.png, see ArtBank), its kind, its name and its line.
        private void DrawCard(SpriteBatch sb, RunState run)
        {
            if (cardNode == null || cardShown <= 0f) return;
            float a = UiMotion.EaseOut(cardShown) * UiMotion.Between(time, 0.35f, 0.3f);
            Vector2 p = PlaceAt(run.Map, cardNode.Row, cardNode.Column);
            bool right = p.X < TacetGame.ScreenW / 2f;
            int x = right ? TacetGame.ScreenW - 36 - CardW : 36;
            int y = 150;
            x += (int)Math.Round((1f - a) * (right ? 24f : -24f));
            Rectangle card = new Rectangle(x, y, CardW, 340);
            int type = (int)cardNode.Type;

            Gfx.Rect(sb, card, Palette.Panel * (0.94f * a));
            Ornament.DoubleFrame(sb, card, Palette.PaperDim * a);

            Rectangle art = new Rectangle(card.Center.X - CardArtW / 2, card.Y + 16, CardArtW, CardArtH);
            Texture2D picture = ArtBank.RouteOf(cardNode.Type);
            if (picture != null) sb.Draw(picture, art, Color.White * a);
            else
            {
                Gfx.Rect(sb, art, Palette.Stage * a);
                ArtSlot.Draw(sb, art, Palette.Paper, 0.8f * a);
            }

            int tx = card.X + 14;
            Ui.Tag(sb, RouteNodeInfo.Tags[type], tx, art.Bottom + 12, Array.IndexOf(run.Options, cardNode) >= 0, a);
            for (int d = 0; d < 3; d++)
            {
                float px = card.Right - 54 + d * 13;
                if (d < RouteNodeInfo.Danger[type]) Gfx.Diamond(sb, px, art.Bottom + 21, 4, Palette.Paper * a);
                else Gfx.DiamondOutline(sb, px, art.Bottom + 21, 4, Palette.LineGrey * a, 1f);
            }
            Gfx.Text(sb, Game.BigFont, cardNode.Title, tx, art.Bottom + 38, Palette.Highlight * a, TextSize.Subtitle);
            Gfx.Text(sb, Game.StoryFont, cardLines[type], tx, art.Bottom + 70, Palette.PaperDim * a, TextSize.StorySmall);

            //Reach : what the band can do about it now
            string reach = Array.IndexOf(run.Options, cardNode) >= 0 ? "CLICK OR ENTER TO GO" : (cardNode.Row > run.Stage ? "FURTHER ON" : "OUT OF REACH");
            Gfx.TextSpaced(sb, Game.Font, reach, tx, card.Bottom - 26, Palette.LineGrey * a, TextSize.Tiny, 2f);
        }

        //Footer : the stage number, the band button and the keys
        private void DrawFooter(SpriteBatch sb, RunState run)
        {
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, TacetGame.ScreenH - FooterY, Palette.Void * 0.9f);
            Gfx.Rect(sb, 0, FooterY, TacetGame.ScreenW, 1, Palette.LineGrey * 0.5f);

            float cx = TacetGame.ScreenW / 2f;
            Gfx.TextSpacedCentered(sb, Game.BigFont, run.StageLabel, cx, 640, Palette.Paper, TextSize.Subtitle, 6f);

            Ui.Command(sb, bandButton, "THE BAND", "TAB", bandMotion, 1f, false);
            if (benchLabel.Length > 0)
                Gfx.TextSpaced(sb, Game.Font, benchLabel, bandButton.X + 4, bandButton.Y - 20, Palette.Highlight, TextSize.Tiny, 2f);

            Gfx.TextSpacedRight(sb, Game.Font, run.FloorShort, 1244, 640, Palette.PaperDim, TextSize.Label, 3f);
            Gfx.TextSpacedRight(sb, Game.Font, "ARROWS  CHOOSE   /   ENTER  GO", 1244, 670, Palette.LineGrey, TextSize.Tiny, 2f);
        }
    }
}
