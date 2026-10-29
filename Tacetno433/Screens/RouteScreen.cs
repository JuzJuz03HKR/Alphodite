using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //RouteScreen : THE ROAD, the floor's map (Beta1, 29 Oct, docs/BETA1_DIRECTION.md 3.1, docs/SCREENS_LEVEL_UP.md 17 and 22).
    //
    //FAR VIEW (the user's ask, 29 Oct) : the whole floor (Data/FloorMap.cs) seen from where the band stands, looking down
    //the road to TACET's hole. Every row of the floor lies across the road a step further on : the band stands on the
    //nearest one and the boss waits in front of the hole at the far end. Near rows are big and clear, far ones small and
    //faint, and the hole grows as the band comes closer. Rings on the floor flow slowly into the hole (RIPPLES, drawn in
    //DrawBack, the layer TACET's dark bends), and the screen's edges darken the closer the boss is (GLOOM).
    //   PLACES   a circle with the kind's letter (RouteNodeInfo.Marks). The ones the band can walk to now are bright and a
    //            light runs along their ways toward them, the places further on are grey, the ones out of reach fade, the
    //            places walked through are filled and joined by a solid line.
    //   HERE     where the band stands : a ring of ticks turning round it, or the way in before the first place
    //   BOSS     a big black disc standing in front of the hole, thorns turning round it and a dark beating behind it
    //   WALK     back from a room the camera walks from the last row to the new one, so the way behind slides off the screen
    //   POINT    a place the band can walk to steps up and fills. The card beside the map shows its picture, its kind and
    //            its line. Any other place shows its card too, so the way through the floor can be planned.
    //   GO       a click or ENTER : the camera walks and dives toward the place, then the page changes.
    //TAB (or the button) opens the band page. ESC opens the pause menu, like every page of a run. Arriving here saves the run.
    public class RouteScreen : GameScreen
    {
        //Road Layout : where the floor falls on the screen (see PERSPECTIVE below)
        private const float Horizon = 150f;           // everything far away closes in on this line, TACET's hole sits on it
        private const float NearY = 572f;             // the row the band stands on lies this far down the screen ...
        private const float NextY = 446f;             // ... and the next row, the places it can walk to now, this far
        private const float NextGap = 210f;           // the next row's places sit this far apart (pixels)
        private const float Spread = 1.4f;            // a row's places sit this many rows apart, across the road
        private const float Funnel = 0.8f;            // the rows narrow toward the boss, the last one before it this much
        private const float Follow = 0.5f;            // the camera leans this much toward the side the band's place is on
        private const float PlaceR = 22f;             // a place of the next row is this big (radius), nearer ones bigger
        private const float BossGap = 0.6f;           // the boss stands this many rows past the last row before it
        private const float HoleGap = 1f;             // TACET's hole lies this many rows past the boss
        private const float HoleSize = 1.05f;         // the hole's size, half across (rows)
        private const float HoleRise = 1f;            // its middle stands this many times the camera's height off the floor
        private const float FadeRows = 5f;            // past the next row things fade over this many rows (to UiRouteFar)
        private const float NearestZ = 0.15f;         // nothing closer to the camera than this many rows is drawn
        private const int Ripples = 7;                // RIPPLES : rings on the floor flowing into the hole
        private const float RippleTime = 7f;          //   one ring takes this long to go from the furthest out into the hole (seconds)

        //PERSPECTIVE, worked out once from the layout above
        //   NEAR    how many rows the camera stands behind the band's row
        //   LIFT    a point on the floor d rows from the camera shows LIFT / d pixels below the horizon
        //   FOCAL   a point d rows from the camera and x rows to the side of it shows FOCAL x x / d pixels from the middle
        //ADVANCED PART : a camera looking along a flat floor sees everything twice as far away half as big and half as far
        //from the point it looks at (the vanishing point, here TACET's hole). Dividing by the distance is all of it : the
        //rows bunch up toward the horizon and the places shrink, like a real road. NEAR and LIFT come from asking the band's
        //row to show at NearY and the next row at NextY : LIFT / NEAR = NearY - Horizon and LIFT / (NEAR + 1) = NextY - Horizon.
        private static readonly float Near = (NextY - Horizon) / (NearY - NextY);
        private static readonly float Lift = (NearY - Horizon) * Near;
        private static readonly float Focal = NextGap * (Near + 1f) / Spread;

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
        private string[] cardLines = new string[0];   // each kind's line wrapped for the card, made in Load
        private float cardShown;                      // the card eases in and out
        private RouteNode cardNode;                   // the place the card shows
        private bool[][] ahead = new bool[0][];       // the places the band can still get to from where it stands, made in Load

        //Camera : where it stands on the road (in rows : the band's row, or between two rows while it walks), and how far
        //it leans to the side (rows, toward the band's place, FOLLOW)
        private float camRow;
        private float camSide;
        private float walkFrom;
        private float walkTo;
        private float sideFrom;
        private float sideTo;

        //Projected : every place on the screen this frame (Project, made fresh after the camera moves, no garbage)
        private Vector2[][] spots = new Vector2[0][];  // where it lies on the floor
        private float[][] depths = new float[0][];     // how many rows from the camera
        private Vector2 entrance;                      // the way in (row 1)
        private float entranceDepth;
        private Vector2 holeAt;                        // TACET's hole : its middle and its size
        private float holeR;

        //Walk Memory : the run, the floor and the row the camera last stood on, so coming back from the band page does not
        //walk again. Kept by the page, not the run, so it is never saved : a run loaded from the save walks its last row once more.
        private static RunState walkedRun;
        private static int walkedFloor;
        private static float walkedRow;

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

            //Projected : room for every place's spot on the screen, filled every frame by Project
            spots = new Vector2[run.Map.Rows.Length][];
            depths = new float[run.Map.Rows.Length][];
            for (int s = 0; s < spots.Length; s++)
            {
                spots[s] = new Vector2[run.Map.Count(s)];
                depths[s] = new float[run.Map.Count(s)];
            }

            //WALK : from the row the camera last stood on to the band's row now. Back from the band page it stays put, a new
            //floor walks in to the way in, anything else (a run loaded from the save) walks the last row once more.
            float here = HereRow(run);
            if (walkedRun == run && walkedFloor == run.Floor && walkedRow <= here) walkFrom = walkedRow;
            else walkFrom = here > 1f ? here - 1f : here - BattleRules.UiRouteStartBack;
            walkTo = here;
            walkedRun = run;
            walkedFloor = run.Floor;
            walkedRow = here;
            sideFrom = Follow * SideOfPath(run, (int)Math.Round(walkFrom));
            sideTo = Follow * SideOfPath(run, (int)Math.Round(walkTo));
            camRow = walkFrom;
            camSide = sideFrom;
            Project(run);

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

        //Here Row : the row the band stands on, the last place walked through or the way in (row 1)
        private static float HereRow(RunState run)
        {
            return run.MapAt >= 0 && run.Stage > 2 ? run.Stage - 1 : 1;
        }

        //======================================================================================
        //PERSPECTIVE : the floor seen from the camera
        //======================================================================================

        //To Screen : a spot on the floor, along rows from the way in and side rows to the right of the road's middle, as the
        //camera sees it now. rise lifts it off the floor (a share of the camera's height). Returns how many rows away it is,
        //which is below NearestZ when it is too close or behind the camera (then at is only a far away guess).
        private float ToScreen(float along, float side, float rise, out Vector2 at)
        {
            float depth = along - (camRow - Near);
            float d = Math.Max(depth, NearestZ);
            at = new Vector2(TacetGame.ScreenW / 2f + Focal * (side - camSide) / d, Horizon + Lift * (1f - rise) / d);
            return depth;
        }

        //On Floor : where a place lies. Row s lies across the road s rows from the way in, its places Spread apart and
        //centred on the road, the rows a little narrower toward the boss (FUNNEL). The first place is on the left, so the
        //ways between two rows never cross (FloorMap's STAIRCASE). The boss stands a little further on (BossGap).
        private static void OnFloor(FloorMap map, int stage, int column, out float along, out float side)
        {
            int last = map.LastRow;
            float t = last > 3 ? MathHelper.Clamp((stage - 2f) / (last - 3f), 0f, 1f) : 0f;
            side = (column - (map.Count(stage) - 1) / 2f) * Spread * MathHelper.Lerp(1f, Funnel, t);
            along = stage == last ? stage + BossGap : stage;
        }

        //Side Of Path : how far to the side the place the band walked through on row stage lies (0 for the way in)
        private static float SideOfPath(RunState run, int stage)
        {
            int k = stage - 2;
            if (k < 0 || k >= run.MapPath.Count || run.Map.At(stage, run.MapPath[k]) == null) return 0f;
            float along, side;
            OnFloor(run.Map, stage, run.MapPath[k], out along, out side);
            return side;
        }

        //Project : every place, the way in and the hole, seen from where the camera stands now. Called once a frame.
        private void Project(RunState run)
        {
            FloorMap map = run.Map;
            for (int s = 2; s < spots.Length; s++)
                for (int c = 0; c < spots[s].Length; c++)
                {
                    float along, side;
                    OnFloor(map, s, c, out along, out side);
                    depths[s][c] = ToScreen(along, side, 0f, out spots[s][c]);
                }
            entranceDepth = ToScreen(1f, 0f, 0f, out entrance);

            //The Hole : past the boss, its middle lifted off the floor, as big as its distance allows
            float depth = Math.Max(NearestZ, ToScreen(HoleRow(map), 0f, HoleRise, out holeAt));
            holeR = Focal * HoleSize / depth;
        }

        //Hole Row : how far along the road TACET's hole lies
        private static float HoleRow(FloorMap map)
        {
            return map.LastRow + BossGap + HoleGap;
        }

        //Size : how big something this many rows from the camera is drawn, 1 at the next row
        private static float Size(float depth)
        {
            return (Near + 1f) / Math.Max(depth, NearestZ);
        }

        //Far : how bright something this many rows from the camera is drawn. The next row and nearer are as bright as they
        //come, past it things fade over FadeRows rows to UiRouteFar.
        private static float Far(float depth)
        {
            float t = MathHelper.Clamp((depth - Near - 1f) / FadeRows, 0f, 1f);
            return MathHelper.Lerp(1f, BattleRules.UiRouteFar, t);
        }

        //Boss Lift : the boss stands on the floor instead of lying on it, its middle this far above its spot
        private float BossLift(float depth)
        {
            return PlaceR * BattleRules.UiRouteBoss * Size(depth) * 0.85f;
        }

        //Center : where a place's circle is drawn (the boss stands up off its spot)
        private Vector2 Center(FloorMap map, int stage, int column)
        {
            Vector2 p = spots[stage][column];
            if (stage == map.LastRow) p.Y -= BossLift(depths[stage][column]);
            return p;
        }

        //Here : where the band stands on the screen
        private Vector2 Here(RunState run)
        {
            if (StandsOnPlace(run)) return spots[run.Stage - 1][run.MapAt];
            return entrance;
        }

        private float HereDepth(RunState run)
        {
            if (StandsOnPlace(run)) return depths[run.Stage - 1][run.MapAt];
            return entranceDepth;
        }

        //Stands On Place : whether the band stands on a place of the map (not the way in)
        private bool StandsOnPlace(RunState run)
        {
            return run.MapAt >= 0 && run.Stage > 2 && run.Stage - 1 < spots.Length && run.MapAt < spots[run.Stage - 1].Length;
        }

        //Row In : ENTER, how far row s has come in. The rows come in from the band's own row toward the hole.
        private float RowIn(RunState run, int stage)
        {
            return UiMotion.Between(time, 0.12f + (stage - walkTo) * 0.05f, 0.4f);
        }

        public override void Update(float dt)
        {
            time += dt;
            if (Game.CurrentRun == null) { Game.Screens.Change(new TitleScreen()); return; }
            RunState run = Game.CurrentRun;
            RouteNode[] options = run.Options;

            //WALK : the camera walks to the band's row, slowing to a stop, and on toward the chosen place while it dives
            float walk = UiMotion.EaseOut(MathHelper.Clamp(time / BattleRules.UiRouteWalk, 0f, 1f));
            float dive = diveTime >= 0f ? UiMotion.EaseInOut(MathHelper.Clamp(diveTime / BattleRules.UiRouteDive, 0f, 1f)) : 0f;
            camRow = MathHelper.Lerp(walkFrom, walkTo, walk) + BattleRules.UiRouteDiveWalk * dive;
            camSide = MathHelper.Lerp(sideFrom, sideTo, walk);
            if (dive > 0f && selected < options.Length)
            {
                float along, side;
                OnFloor(run.Map, options[selected].Row, options[selected].Column, out along, out side);
                camSide = MathHelper.Lerp(camSide, Follow * side, BattleRules.UiRouteDiveWalk * dive);
            }
            Project(run);

            //TACET : the hole breathes, a little wilder the closer the boss, and opens when the page does. The edges of the
            //screen close in the closer the band is to the boss (GLOOM).
            float open = UiMotion.EaseBack(MathHelper.Clamp(time / 0.5f, 0f, 1f));
            float breath = 1f + 0.04f * (float)Math.Sin(time * 0.8f);
            Dark.Update(dt, 0.3f + 0.35f * Progress(run));
            Dark.PlaceOnPage(holeAt, holeR * open * breath, 0f, 0f, 0f);
            Lens.Vignette = 0.55f + BattleRules.UiRouteGloom * Progress(run) * UiMotion.Between(time, 0.2f, 1.2f);

            //Card : eases toward the place pointed at, or the chosen one
            RouteNode want = pointed != null ? pointed : (selected < options.Length ? options[selected] : null);
            if (want != cardNode)
            {
                cardShown = Math.Max(0f, cardShown - dt * 10f);
                if (cardShown <= 0f) cardNode = want;
            }
            else cardShown = Math.Min(1f, cardShown + dt * 6f);

            //GO : the camera walks and dives toward the chosen place, then the place opens
            if (diveTime >= 0f)
            {
                diveTime += dt;
                float t = MathHelper.Clamp(diveTime / BattleRules.UiRouteDive, 0f, 1f);
                RouteNode node = options[selected];
                Lens.ZoomAt = Center(run.Map, node.Row, node.Column);
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

        //Place Under : the place within reach of the mouse, or null. The nearest rows are asked first, they are drawn on top.
        private RouteNode PlaceUnder(RunState run, Vector2 mouse)
        {
            FloorMap map = run.Map;
            for (int s = 2; s <= map.LastRow && s < spots.Length; s++)
                for (int c = 0; c < spots[s].Length; c++)
                {
                    float depth = depths[s][c];
                    if (depth <= NearestZ) continue;
                    float r = PlaceR * Size(depth) * (s == map.LastRow ? BattleRules.UiRouteBoss : 1f);
                    float reach = Math.Max(r, 8f) + 6f;
                    if (Vector2.DistanceSquared(mouse, Center(map, s, c)) <= reach * reach) return map.Rows[s][c];
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
            Lens.ZoomAt = Center(run.Map, node.Row, node.Column);
            diveTime = 0f;
        }

        //======================================================================================
        //The World (THE LENS) : the floor and its rings, the ways, the places
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //Back : what TACET's hole bends. The dark floor lit round the band, a faint glow where it meets the dark far away,
        //a faint line across the road for every row still ahead (the next one brighter), and the RIPPLES.
        public override void DrawBack(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(-200, 380, 1680, 640), Palette.Paper * 0.06f);              // the floor lit round the band
            Gfx.DrawGlowBox(sb, new Rectangle(-100, (int)Horizon - 30, 1480, 60), Palette.Paper * 0.035f);  // the far end
            if (Game.CurrentRun == null) return;
            RunState run = Game.CurrentRun;
            FloorMap map = run.Map;

            //Rows : a line across the road under every row still ahead, a little wider than its places
            for (int s = Math.Max(2, run.Stage); s < map.LastRow && s < spots.Length; s++)
            {
                int n = spots[s].Length;
                if (n == 0 || depths[s][0] <= NearestZ) continue;
                float a = RowIn(run, s);
                float far = Far(depths[s][0]);
                float reach = Spread * 0.6f;
                float along, left, right;
                OnFloor(map, s, 0, out along, out left);
                OnFloor(map, s, n - 1, out along, out right);
                Vector2 p, q;
                ToScreen(along, left - reach, 0f, out p);
                ToScreen(along, right + reach, 0f, out q);
                Color line = s == run.Stage ? Palette.Paper * (0.16f * a) : Palette.Paper * (0.06f * far * a);
                Gfx.Line(sb, p, q, line, 1f);
            }

            //RIPPLES : rings on the floor round the hole, slowly shrinking into it, so the whole floor seems to flow into
            //TACET. Each ring is born far behind the camera, fades in, and fades out as the hole swallows it.
            float holeRow = HoleRow(map);
            float widest = holeRow + 2f;
            float up = UiMotion.Between(time, 0.2f, 0.8f);
            for (int k = 0; k < Ripples; k++)
            {
                float left = 1f - ((k / (float)Ripples + time / RippleTime) % 1f);   // 1 just born, 0 swallowed
                float ring = MathHelper.Lerp(0.35f, widest, left);
                float seen = Math.Min(1f, left * 3f) * Math.Min(1f, (1f - left) * 4f);
                float far = Far(holeRow - ring - (camRow - Near));
                FloorRing(sb, holeRow, ring, Palette.Paper * (0.085f * seen * far * up));
            }
        }

        //Floor Ring : a ring lying on the floor round the hole, through the PERSPECTIVE. Pieces too close to the camera
        //are left out.
        private void FloorRing(SpriteBatch sb, float holeRow, float ring, Color color)
        {
            const int pieces = 96;
            Vector2 last;
            float lastDepth = ToScreen(holeRow - ring, 0f, 0f, out last);
            for (int p = 1; p <= pieces; p++)
            {
                float angle = MathHelper.TwoPi * p / pieces;
                Vector2 now;
                float depth = ToScreen(holeRow - ring * (float)Math.Cos(angle), ring * (float)Math.Sin(angle), 0f, out now);
                if (depth > NearestZ && lastDepth > NearestZ) Gfx.Line(sb, last, now, color, 1f);
                last = now;
                lastDepth = depth;
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
        //A way fades with its far end and is left out once its near end is behind the camera.
        private void DrawWays(SpriteBatch sb, RunState run)
        {
            FloorMap map = run.Map;
            int from = run.Stage - 1;
            for (int s = 2; s < map.LastRow && s + 1 < spots.Length; s++)
            {
                float a = RowIn(run, s + 1);
                if (a <= 0f) continue;
                for (int c = 0; c < spots[s].Length; c++)
                {
                    if (depths[s][c] <= NearestZ) continue;
                    RouteNode node = map.Rows[s][c];
                    Vector2 p = spots[s][c];
                    for (int k = 0; k < node.Next.Count; k++)
                    {
                        int d = node.Next[k];
                        if (d >= spots[s + 1].Length) continue;
                        Vector2 q = spots[s + 1][d];
                        float far = Far(depths[s + 1][d]) * a;
                        bool walked = Walked(run, s, c) && Walked(run, s + 1, d);
                        bool open = s == from && c == run.MapAt;
                        bool later = Ahead(s, c);
                        if (walked) Gfx.Line(sb, p, q, Palette.PaperDim * (0.9f * far), 2f);
                        else if (open) OpenWay(sb, p, q, Array.IndexOf(run.Options, map.Rows[s + 1][d]), far);
                        else Gfx.Line(sb, p, q, Palette.LineGrey * ((later ? 0.32f : 0.1f) * far), 1f);
                    }
                }
            }

            //The Way In : before the first place, the ways run from the entrance to every place of row 2
            if (spots.Length <= 2 || entranceDepth <= NearestZ) return;
            if (run.Stage == 2 || run.MapAt < 0)
            {
                float a = RowIn(run, 2);
                for (int c = 0; c < spots[2].Length && run.Stage == 2; c++)
                    OpenWay(sb, entrance, spots[2][c], c, a);
            }
            else if (run.MapPath.Count > 0 && run.MapPath[0] < spots[2].Length)
                Gfx.Line(sb, entrance, spots[2][run.MapPath[0]], Palette.PaperDim * 0.9f, 2f);
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

        //Places : every place of the floor, the furthest first so the near ones lie over them. The ones within reach now
        //are bright, the chosen one steps up and fills. A place's circle and letter shrink and fade with its distance.
        private void DrawPlaces(SpriteBatch sb, RunState run)
        {
            FloorMap map = run.Map;
            for (int s = map.LastRow; s >= 2; s--)
            {
                if (s >= spots.Length) continue;
                float a = RowIn(run, s);
                if (a <= 0f) continue;
                for (int c = 0; c < spots[s].Length; c++)
                {
                    float depth = depths[s][c];
                    if (depth <= NearestZ) continue;
                    RouteNode node = map.Rows[s][c];
                    float size = Size(depth);
                    float far = Far(depth) * a;
                    int option = Array.IndexOf(run.Options, node);

                    if (s == map.LastRow)
                    {
                        DrawBoss(sb, node, spots[s][c], depth, Math.Max(far, 0.85f * a), option);
                        continue;
                    }

                    Vector2 p = spots[s][c];
                    float r = PlaceR * size;
                    string mark = RouteNodeInfo.Marks[(int)node.Type];
                    float letter = TextSize.Label * MathHelper.Clamp(size, 0.5f, 1.5f);
                    bool words = r >= 7f;                                                    // too small to read : the circle alone

                    if (Walked(run, s, c))
                    {
                        //the place the band stands on keeps only HERE's mark, its letter would sit under the dot
                        Gfx.Circle(sb, p.X, p.Y, r * 0.8f, Palette.PaperDim * far);
                        if (words && s != run.Stage - 1) Gfx.TextCentered(sb, Game.Font, mark, p.X, p.Y - 1f, Palette.Ink * far, letter);
                        continue;
                    }

                    if (option >= 0)
                    {
                        //Within Reach : bright, and the chosen one steps up, fills and sends out a ring
                        UiMotion m = optionMotion[Math.Min(option, optionMotion.Length - 1)];
                        float e = m.Shown;
                        float stepped = r * (1f + 0.3f * e) * m.Scale;
                        float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 3f + option);
                        Gfx.Circle(sb, p.X, p.Y, stepped, Color.Lerp(Palette.Void, Palette.Paper, e) * a);
                        Gfx.CircleOutline(sb, p.X, p.Y, stepped, Palette.Paper * a, 1.5f);
                        Gfx.CircleOutline(sb, p.X, p.Y, stepped + 4f + 2f * pulse, Palette.Paper * ((0.25f + 0.2f * pulse) * (1f - e) * a), 1f);
                        if (m.Ring < 1f) Gfx.CircleOutline(sb, p.X, p.Y, stepped + 22f * m.Ring, Palette.Paper * ((1f - m.Ring) * 0.8f * a), 1.5f);
                        Gfx.TextCentered(sb, Game.Font, mark, p.X, p.Y - 1f, Color.Lerp(Palette.Paper, Palette.Ink, e) * a, letter);
                        continue;
                    }

                    //Further On, or out of reach. The rest of the band's own row is behind it already : barely there.
                    bool passed = s < run.Stage;
                    float k = (Ahead(s, c) ? 1f : (passed ? 0.12f : 0.35f)) * far;
                    Gfx.Circle(sb, p.X, p.Y, r, Palette.Void * (passed ? 0.5f * a : a));
                    Gfx.CircleOutline(sb, p.X, p.Y, r, Palette.LineGrey * k, 1f);
                    if (words && !passed) Gfx.TextCentered(sb, Game.Font, mark, p.X, p.Y - 1f, Palette.PaperDim * k, letter);
                }
            }
        }

        //Boss : the floor's last room, big and frightening (the user's ask). A black disc standing on the floor in front of
        //TACET's hole, three times a usual place (UiRouteBoss) so it reads as the end of the road even far away. Thorns
        //turn slowly round it and a dark beats behind it like a slow heart. Within reach it steps up like any other place,
        //but stays black : the boss never turns into paper.
        private void DrawBoss(SpriteBatch sb, RouteNode node, Vector2 foot, float depth, float a, int option)
        {
            float r = PlaceR * BattleRules.UiRouteBoss * Size(depth);
            Vector2 p = new Vector2(foot.X, foot.Y - BossLift(depth));
            float step = 1f;
            float e = 0f;
            if (option >= 0)
            {
                UiMotion m = optionMotion[Math.Min(option, optionMotion.Length - 1)];
                e = m.Shown;
                step = (1f + 0.08f * e) * m.Scale;
            }
            r *= step;

            //THE DARK THAT BEATS : a soft black halo, two quick beats then a rest, like a heart
            float beat = (float)Math.Pow(Math.Max(0.0, Math.Sin(time * 4.2f)), 6.0) * (Math.Sin(time * 2.1f) > 0f ? 1f : 0.45f);
            Gfx.DrawGlow(sb, p.X, p.Y, r * (2.5f + 0.35f * beat), Color.Black * ((0.5f + 0.3f * beat) * a));

            Thorns(sb, p, r, time * 0.18f, Palette.Paper * ((0.45f + 0.35f * e) * a));
            Gfx.Circle(sb, p.X, p.Y, r, Palette.Void * a);
            Gfx.CircleOutline(sb, p.X, p.Y, r, Palette.Paper * a, 2f);
            Gfx.CircleOutline(sb, p.X, p.Y, r + 5f + 3f * beat, Palette.Paper * ((0.3f + 0.25f * beat) * a), 1f);
            if (option >= 0)
            {
                UiMotion m = optionMotion[Math.Min(option, optionMotion.Length - 1)];
                if (m.Ring < 1f) Gfx.CircleOutline(sb, p.X, p.Y, r + 30f * m.Ring, Palette.Paper * ((1f - m.Ring) * 0.8f * a), 2f);
            }
            float letter = TextSize.Body * MathHelper.Clamp(r / 40f, 0.6f, 2.2f);
            Gfx.TextCentered(sb, Game.BigFont, RouteNodeInfo.Marks[(int)node.Type], p.X, p.Y - 2f, Palette.Paper * a, letter);
        }

        //Thorns : sharp spikes round a circle, long and short in turn, turning slowly (the boss's crown)
        private static void Thorns(SpriteBatch sb, Vector2 at, float r, float turn, Color color)
        {
            const int count = 14;
            for (int i = 0; i < count; i++)
            {
                float angle = turn + MathHelper.TwoPi * i / count;
                Vector2 out1 = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Vector2 across = new Vector2(-out1.Y, out1.X) * (r * 0.08f);
                Vector2 tip = at + out1 * (r * (i % 2 == 0 ? 1.42f : 1.24f));
                Vector2 root = at + out1 * (r - 1f);
                Gfx.Line(sb, root - across, tip, color, 1.5f);
                Gfx.Line(sb, root + across, tip, color, 1.5f);
            }
        }

        //Here : where the band stands, a ring of ticks turning round it (menu4)
        private void DrawHere(SpriteBatch sb, RunState run)
        {
            float a = UiMotion.Between(time, 0.5f, 0.4f);
            if (a <= 0f) return;
            float depth = HereDepth(run);
            if (depth <= NearestZ) return;
            Vector2 here = Here(run);
            float size = MathHelper.Clamp(Size(depth), 0.6f, 1.6f);
            Ornament.TickRing(sb, here.X, here.Y, 24f * size, 30, time * 0.6f, a, Palette.Paper * 0.8f);
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
                Vector2 p = Center(run.Map, node.Row, node.Column);
                float size = MathHelper.Clamp(Size(depths[node.Row][node.Column]), 0.5f, 1.4f);
                Gfx.DrawGlow(sb, p.X, p.Y, 220f * size, Color.White * (0.5f * up));
            }
        }

        //Glow : a halo on the chosen place
        public override void DrawGlow(SpriteBatch sb)
        {
            if (Game.CurrentRun == null) return;
            RunState run = Game.CurrentRun;
            if (selected >= run.Options.Length) return;
            RouteNode node = run.Options[selected];
            Vector2 p = Center(run.Map, node.Row, node.Column);
            float size = MathHelper.Clamp(Size(depths[node.Row][node.Column]), 0.5f, 1.4f);
            if (node.Row == run.Map.LastRow) size *= BattleRules.UiRouteBoss * 0.7f;
            Gfx.DrawGlow(sb, p.X, p.Y, 46f * size, Palette.Paper * (0.35f * UiMotion.Between(time, 0.4f, 0.4f)));
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
            Vector2 p = Center(run.Map, cardNode.Row, cardNode.Column);
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

            Gfx.TextSpacedRight(sb, Game.Font, run.FloorShort, 1244, 640, Palette.PaperDim, TextSize.Label, 3f);
            Gfx.TextSpacedRight(sb, Game.Font, "ARROWS  CHOOSE   /   ENTER  GO", 1244, 670, Palette.LineGrey, TextSize.Tiny, 2f);
        }
    }
}
