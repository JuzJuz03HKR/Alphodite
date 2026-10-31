using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //ShopScreen : trade shards for what makes the whole band stronger.
    //
    //Left  : the shop itself, shelves and a counter with the shopkeeper behind it (placeholder)
    //Right : the wares and REROLL. Click a ware to buy it.
    //Bottom: the shopkeeper talks in a dialogue box, and reacts to what you point at.
    //
    //THE SHOP OF THE BAND OF THREE (29 Oct, docs/BETA1_DIRECTION.md 8.5, the user : "the important place that makes the whole
    //team stronger, the wares are random and can be rolled again for shards, so early shards matter a lot") :
    //   MOTIF x2    rolled from the motifs the band does not carry
    //   REHEARSAL   a star for one button of the band (REHEARSAL, 31 Oct, Data/Rehearsal.cs), rolled among the buttons that can
    //               still get one, dearer for the second and third star (until 31 Oct : WEAPON, one weapon up a tier)
    //   THE BAND    TUNING (life back for everyone) or, now and then, SECTIONAL (every weapon up a tier : a star on every ATTACK)
    //   REROLL      a new shelf, dearer each time in one visit (BattleRules.ShopRerollBase / ShopRerollStep, like Balatro)
    //The shelves come from the place's own dice (RunState.PlaceSeed) and the number of rolls, so leaving the game and coming
    //back finds the same first shelf (the run was saved when the band walked in, the shards too). The seats on stage and
    //HIRE (a new musician) were sold here until the band of three.
    //Leaving asks first, because the shelves are gone once the band walks out.
    //Beta1 screens round 1 (29 Oct, docs/SCREENS_LEVEL_UP.md 14) : THE ARCHIVIST stands in a stage light, the rest of the
    //room is dark. The wares come in one after another and light up like the menus when pointed at. What the band buys
    //flies up into the top bar (a motif to its badge, tuning to LIFE, a weapon to the band's name), and the keeper's words
    //are written out line by line. A new shelf comes in the same way.
    public class ShopScreen : GameScreen
    {
        //Leave Box : "are you sure?" before walking out
        private ConfirmBox confirm = new ConfirmBox();

        //Uses Escape : while the box is open, ESC answers it instead of opening the pause menu
        public override bool UsesEscape
        {
            get { return confirm.Open; }
        }

        //Wipe Word : the keeper's name runs through the wipe on the way here (WIPE WORD, the user's pick)
        public override string WipeWord
        {
            get { return KeeperName; }
        }

        //Shop Text : THE PLACE TO EDIT WHAT THE SHOPKEEPER SAYS
        private const string KeeperName = "THE ARCHIVIST";
        private static string[] greetings =
        {
            "Ah, musicians. Everything on these shelves once belonged to a song that stopped.",
            "Come in, come in. Mind the silence by the door, it bites.",
        };
        private static string[] thanks =
        {
            "A fine choice. It still remembers how to sing.",
            "Sold. The silence will hate that.",
            "Take good care of it. I will not be buying it back.",
        };
        private const string PoorLine = "Not enough shards, I am afraid. Come back a little richer.";
        private const string SoldLine = "That is all I have of those for now.";
        private const string RestedLine = "Your band is already in perfect tune.";
        private const string RerollLine = "Something else from the back, then. It costs a little more every time.";
        private const string RerolledLine = "There. A fresh shelf. Mind the dust.";

        //Wares : what one place on the shelf holds
        private enum WareKind { Motif, Rehearsal, Tuning, Sectional, Empty }

        private class Ware
        {
            public WareKind Kind;
            public Motif Motif;              // MOTIF
            public Musician Who;             // REHEARSAL : whose button
            public int Button;               //             which one (0 the ATTACK, 1 and 2 the skills)
            public int Price;
            public bool Sold;
            public string Name = "";
            public string KindLabel = "";    // COMMON / RARE / TREASURED, REHEARSAL, FOR THE BAND
            public string Note = "";         // what the keeper says when it is pointed at
            public string PriceLabel = "";
            public string IconLabel = "";    // the word or numeral where a badge would be
        }

        private const int WareCount = 4;

        //Shop Layout
        private Rectangle waresBox = new Rectangle(730, 120, 510, 450);
        private Rectangle dialogueBox = new Rectangle(36, 566, 660, 130);
        private Rectangle leaveButton = new Rectangle(930, 630, 310, 54);
        private Rectangle rerollButton = new Rectangle(750, 500, 470, 50);
        private const int RowH = 82;

        //Shop State
        private Ware[] wares = new Ware[WareCount];
        private int rerolls;                    // new shelves bought in this visit
        private string rerollLabel = "";
        private int hoverRow = -1;              // a ware, WareCount for REROLL, -1 for nothing
        private int lastHover = -1;
        private string line = "";
        private string lineWrapped = "";
        private string[] lineRows = new string[0];  // the keeper's line, one string per row, made by Say for the writing out
        private float lineAge;
        private int thanksIndex;
        private float time;
        private float shelfTime;                // since this shelf came in, the wares come in one after another from it

        //Motion (Beta1 round 1) : the rows and the buttons point like the menus (Core/UiMotion.cs)
        private UiMotion[] rowMotion = new UiMotion[WareCount];
        private UiMotion rerollMotion = new UiMotion();
        private UiMotion leaveMotion = new UiMotion();

        //FLY UP : what was just bought, on its way from the shelf to the top bar
        private const float FlyTime = 0.6f;
        private int flyRow = -1;                // the ware flying, -1 for none
        private Ware flyWare;                   // the ware it is, for its badge
        private Vector2 flyFrom;
        private Vector2 flyTo;
        private float flyAge;
        private float landAge = 99f;            // seconds since it landed, for the ring where it lands
        private Vector2 landAt;

        //Light : the keeper's stage light (KEEPER) and the softer one over the wares
        private static readonly Vector2 KeeperLight = new Vector2(360f, 250f);
        private const float WriteSpeed = 900f;  // the keeper's words are written out this many pixels a second

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            Stock();
            Say(greetings[run.Rng.Next(greetings.Length)]);
            SoundBank.PlayMusic(Music.Shop);

            for (int r = 0; r < WareCount; r++) rowMotion[r] = new UiMotion();
            RunHud.Arriving = 0;

            //The Lens : a dark room, the light is the keeper's (DrawLights)
            Lens.Ambient = 0.34f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.6f;
        }

        //Leave : nothing left flying into the top bar
        public override void Leave()
        {
            RunHud.Arriving = 0;
        }

        //======================================================================================
        //The Shelf
        //======================================================================================

        //Stock : a shelf from the place's dice and the number of rolls, so the same visit always shows the same shelves
        private void Stock()
        {
            RunState run = Game.CurrentRun;
            Random dice = new Random(run.PlaceSeed + rerolls * 7919);

            Motif[] motifs = MotifList.Roll(run.Motifs, 2, 1, run.Floor, dice);
            for (int i = 0; i < 2; i++)
                wares[i] = i < motifs.Length ? MotifWare(motifs[i]) : EmptyWare();

            //Rehearsal : one button of the band that can still get a star, rolled
            Musician who = null;
            int button = 0;
            int candidates = 0;
            for (int i = 0; i < run.Roster.Count; i++)
                for (int b = 0; b < RehearsalList.Buttons; b++)
                    if (RehearsalList.CanRaise(run.Roster[i], b) && dice.Next(++candidates) == 0)
                    {
                        who = run.Roster[i];
                        button = b;
                    }
            wares[2] = who != null ? RehearsalWare(run, who, button) : EmptyWare();

            //The Band : TUNING, or now and then SECTIONAL while a weapon can still go up
            bool sectional = run.AnyToTrain && dice.Next(100) < BattleRules.ShopSectionalChance;
            wares[3] = sectional ? SectionalWare() : TuningWare();

            rerollLabel = "REROLL   " + RerollPrice;
            shelfTime = 0f;
        }

        private int RerollPrice
        {
            get { return BattleRules.ShopRerollBase + BattleRules.ShopRerollStep * rerolls; }
        }

        private static Ware MotifWare(Motif m)
        {
            Ware w = new Ware();
            w.Kind = WareKind.Motif;
            w.Motif = m;
            w.Price = MotifList.Price(m);
            w.Name = m.Name;
            w.KindLabel = m.RarityLabel;
            w.Note = BattleRules.TurnBattleOn && m.TurnText.Length > 0 ? m.TurnText : m.Text;   // step 15 : the turn battle's words, like the cards
            w.PriceLabel = w.Price.ToString();
            return w;
        }

        //Rehearsal Ware : a star for button b of musician m, what it changes in the keeper's words (the rehearsal page's numbers)
        private static Ware RehearsalWare(RunState run, Musician m, int button)
        {
            int next = RehearsalList.StarsOf(m, button) + 1;
            int power = run.PowerOf(m);
            Star star = RehearsalList.Next(m, button);
            string name = RehearsalList.ButtonName(m, button);
            Ware w = new Ware();
            w.Kind = WareKind.Rehearsal;
            w.Who = m;
            w.Button = button;
            w.Price = BattleRules.ShopStarPrice[Math.Min(next, BattleRules.ShopStarPrice.Length - 1)];
            w.Name = name;
            w.KindLabel = "REHEARSAL  /  " + m.Name + "  /  STAR " + next;
            if (star != null && star.Name.Length > 0)
                w.Note = "A rehearsal for " + m.Name + ". " + name + " becomes " + star.Name + ". " + star.Line;
            else
                w.Note = "A rehearsal for " + m.Name + ". " + (button == 0 ? "Their ATTACK" : name) + " gets a star : "
                       + RehearsalList.FromText(m, button, power) + " to " + RehearsalList.ToText(m, button, power) + ".";
            w.PriceLabel = w.Price.ToString();
            return w;
        }

        private static Ware TuningWare()
        {
            Ware w = new Ware();
            w.Kind = WareKind.Tuning;
            w.Price = BattleRules.TuningPrice;
            w.Name = "TUNING";
            w.KindLabel = "FOR THE BAND";
            w.Note = "The whole band gets back " + (int)(BattleRules.TuningRecover * 100) + " percent of its " + BattleRules.LifeWordLower + ".";
            w.PriceLabel = w.Price.ToString();
            w.IconLabel = "TUNE";
            return w;
        }

        private static Ware SectionalWare()
        {
            Ware w = new Ware();
            w.Kind = WareKind.Sectional;
            w.Price = BattleRules.ShopSectionalPrice;
            w.Name = "SECTIONAL";
            w.KindLabel = "FOR THE BAND";
            w.Note = "A long night of practice. Every ATTACK in the band gets a star : more power for everyone.";
            w.PriceLabel = w.Price.ToString();
            w.IconLabel = "ALL";
            return w;
        }

        private static Ware EmptyWare()
        {
            Ware w = new Ware();
            w.Kind = WareKind.Empty;
            w.Sold = true;
            w.Name = "EMPTY SHELF";
            w.KindLabel = "";
            return w;
        }

        //Say : the shopkeeper's line, wrapped once when it changes
        private void Say(string text)
        {
            if (text == line) return;
            line = text;
            lineWrapped = Gfx.WrapText(Game.StoryFont, text, dialogueBox.Width - 60, TextSize.Story);
            lineRows = lineWrapped.Split('\n');
            lineAge = 0f;
        }

        private Rectangle RowRect(int row)
        {
            return new Rectangle(waresBox.X, waresBox.Y + 44 + row * RowH, waresBox.Width, RowH - 8);
        }

        public override void Update(float dt)
        {
            time += dt;
            shelfTime += dt;
            lineAge += dt;
            UpdateFly(dt);

            //Leaving : only once the player has said yes
            if (confirm.Open)
            {
                for (int r = 0; r < WareCount; r++) rowMotion[r].Update(dt, false);
                rerollMotion.Update(dt, false);
                leaveMotion.Update(dt, false);
                if (confirm.Update(dt) == 1) RunFlow.NextStage(Game);
                return;
            }

            //Hover : describe whatever is being pointed at
            hoverRow = -1;
            for (int r = 0; r < WareCount; r++)
                if (Input.MouseOver(RowRect(r))) hoverRow = r;
            if (Input.MouseOver(rerollButton)) hoverRow = WareCount;
            for (int r = 0; r < WareCount; r++) rowMotion[r].Update(dt, r == hoverRow && !wares[r].Sold);
            rerollMotion.Update(dt, hoverRow == WareCount);
            leaveMotion.Update(dt, Input.MouseOver(leaveButton));

            if (hoverRow != lastHover)
            {
                if (hoverRow >= 0 && hoverRow < WareCount && wares[hoverRow].Note.Length > 0) Say(wares[hoverRow].Note);
                if (hoverRow == WareCount) Say(RerollLine);
                if (hoverRow >= 0) SoundBank.Play(Sfx.UiMove);
                lastHover = hoverRow;
            }

            if (Input.MouseClicked() && hoverRow >= 0 && hoverRow < WareCount) Buy(hoverRow);
            if (Input.ClickedOn(rerollButton) || Input.KeyPressed(Keys.R)) Reroll();

            if (Input.KeyPressed(Keys.Enter) || Input.ClickedOn(leaveButton))
            {
                leaveMotion.Press();
                confirm.Show("LEAVE THE SHOP?", "Whatever stays on the shelves is gone once the band walks out.", "LEAVE", "STAY");
            }
        }

        //Reroll : pay, and a new shelf comes in (everything on the old one goes, sold or not)
        private void Reroll()
        {
            RunState run = Game.CurrentRun;
            if (!run.SpendShards(RerollPrice)) { Deny(PoorLine); return; }
            rerollMotion.Press();
            rerolls++;
            Stock();
            for (int r = 0; r < WareCount; r++) rowMotion[r] = new UiMotion();
            Say(RerolledLine);
            SoundBank.Play(Sfx.PageTurn);
        }

        //Fly : what was bought leaves its shelf for the top bar. A motif's badge is not drawn up there until it lands.
        private void Fly(int row, Vector2 to)
        {
            Rectangle r = RowRect(row);
            flyRow = row;
            flyWare = wares[row];
            flyFrom = new Vector2(r.X + 46, r.Center.Y);
            flyTo = to;
            flyAge = 0f;
            RunHud.Arriving = flyWare.Kind == WareKind.Motif ? 1 : 0;
        }

        private void UpdateFly(float dt)
        {
            landAge += dt;
            if (flyRow < 0) return;
            flyAge += dt;
            if (flyAge < FlyTime) return;
            landAt = flyTo;
            landAge = 0f;
            flyRow = -1;
            RunHud.Arriving = 0;
        }

        //Fly Point : where the flying ware is at 0..1 of its way, along a curve that rises over the shelf first
        private Vector2 FlyPoint(float t)
        {
            Vector2 bend = new Vector2((flyFrom.X + flyTo.X) / 2f, Math.Min(flyFrom.Y, flyTo.Y) - 120f);
            float u = 1f - t;
            return u * u * flyFrom + 2f * u * t * bend + t * t * flyTo;
        }

        //Buy : check, pay, then hand the thing over
        private void Buy(int row)
        {
            RunState run = Game.CurrentRun;
            Ware w = wares[row];

            if (w.Sold) { Deny(SoldLine); return; }
            if (w.Kind == WareKind.Tuning && run.Stamina >= run.MaxStamina) { Deny(RestedLine); return; }
            if (w.Kind == WareKind.Rehearsal && !RehearsalList.CanRaise(w.Who, w.Button)) { Deny(SoldLine); return; }
            if (!run.SpendShards(w.Price)) { Deny(PoorLine); return; }

            w.Sold = true;
            if (w.Kind == WareKind.Motif)
            {
                run.AddMotif(w.Motif);
                SoundBank.Play(Sfx.MotifGet);
                Fly(row, RunHud.BadgeCentre(run.Motifs.Count - 1));
            }
            else if (w.Kind == WareKind.Rehearsal)
            {
                run.Raise(w.Who, w.Button);
                SoundBank.Play(Sfx.Rehearse);
                Fly(row, RunHud.BandAt);
            }
            else if (w.Kind == WareKind.Tuning)
            {
                run.ChangeStamina((int)(run.MaxStamina * BattleRules.TuningRecover));
                Fly(row, RunHud.LifeAt);
            }
            else if (w.Kind == WareKind.Sectional)
            {
                for (int i = 0; i < run.Roster.Count; i++) run.Rehearse(run.Roster[i]);
                SoundBank.Play(Sfx.Rehearse);
                Fly(row, RunHud.BandAt);
            }
            Say(thanks[thanksIndex++ % thanks.Length]);
            run.RefreshLabels();
            SoundBank.Play(Sfx.Buy);

            //A rehearsal still on the shelf says what its star gives now (SECTIONAL may have given that ATTACK a star already)
            for (int r = 0; r < WareCount; r++)
                if (wares[r].Kind == WareKind.Rehearsal && !wares[r].Sold)
                    wares[r] = RehearsalList.CanRaise(wares[r].Who, wares[r].Button) ? RehearsalWare(run, wares[r].Who, wares[r].Button) : EmptyWare();
        }

        private void Deny(string text)
        {
            Say(text);
            SoundBank.Play(Sfx.UiDenied);
        }

        //======================================================================================
        //The World (THE LENS) : the keeper's room, lit from above
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //World : the shop painting with the keeper in it, or the empty slot where it goes, on a dark floor.
        //ARTWORK : Content/Art/Places/shop.png (the shopkeeper is part of the picture), see ArtBank.
        public override void DrawWorld(SpriteBatch sb)
        {
            float e = UiMotion.Between(time, 0f, 0.6f);
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(-120, 500, 960, 300), Palette.Paper * 0.05f);
            ArtBank.DrawOrSlot(sb, ArtBank.Shop, new Rectangle(24, 80, 672, 470), Palette.Paper, e);
        }

        //Lights : KEEPER, a stage light from above on the keeper that comes up as the page opens and breathes a little,
        //a softer one on the wares, and the pointed ware lit
        public override void DrawLights(SpriteBatch sb)
        {
            float up = UiMotion.EaseInOut(MathHelper.Clamp(time / 0.8f, 0f, 1f));
            float breathe = 1f + 0.04f * (float)Math.Sin(time * 1.3f);
            Gfx.DrawGlowBox(sb, new Rectangle(140, -80, 440, 720), Color.White * (0.4f * up));
            Gfx.DrawGlow(sb, KeeperLight.X, KeeperLight.Y, 380f, Color.White * (0.62f * up * breathe));
            Gfx.DrawGlow(sb, waresBox.Center.X, waresBox.Center.Y, 440f, Color.White * 0.3f);
            if (hoverRow >= 0 && hoverRow < WareCount && !wares[hoverRow].Sold)
            {
                Rectangle r = RowRect(hoverRow);
                Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, 240f, Color.White * (0.3f * rowMotion[hoverRow].Shown));
            }
        }

        //Glow : the beam of the keeper's light, a wide dim shaft with a brighter core (like the battle's, DrawMood)
        public override void DrawGlow(SpriteBatch sb)
        {
            float a = 0.3f * UiMotion.EaseInOut(MathHelper.Clamp(time / 0.8f, 0f, 1f));
            Gfx.DrawGlowBox(sb, new Rectangle(140, -300, 440, 860), Color.White * (a * 0.6f));
            Gfx.DrawGlowBox(sb, new Rectangle(250, -240, 220, 740), Color.White * a);
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;

            DrawWares(sb, run);
            DrawDialogue(sb);

            float button = UiMotion.Between(time, 0.55f, BattleRules.UiEnterTime);
            Ui.Command(sb, leaveButton, "LEAVE THE SHOP", "ENTER", leaveMotion, button, false);
            Gfx.TextSpacedRight(sb, Game.Font, "CLICK A WARE TO BUY IT", leaveButton.Right, leaveButton.Y - 22, Palette.LineGrey * button, TextSize.Tiny, 2f);

            RunHud.DrawTop(sb, run, "SHOP");
            DrawFly(sb);
            RunHud.DrawTips(sb, run);
            confirm.Draw(sb);
        }

        //Wares : one row per ware, coming in one after another (again with every new shelf). Pointed at, an ink plate shoots
        //out under it like the menus (Ui.InkBar) and its words turn black. Its price, and a SOLD band when it is gone.
        //REROLL sits under them with its price.
        private void DrawWares(SpriteBatch sb, RunState run)
        {
            float panel = UiMotion.Between(time, 0.1f, BattleRules.UiEnterTime);
            Rectangle box = waresBox;
            box.X += (int)((1f - panel) * BattleRules.UiEnterSlide);
            Ui.Panel(sb, box, panel);
            Ui.Header(sb, "", "Wares", box.X + 20, box.Y + 8, box.Width - 40, panel);

            float start = time < 1f ? 0.18f : 0f;                  // the first shelf waits for the panel, a new one comes in at once
            for (int r = 0; r < WareCount; r++)
            {
                float a = UiMotion.Between(shelfTime, start + r * BattleRules.UiEnterStagger, BattleRules.UiEnterTime);
                if (a <= 0f) continue;
                Ware w = wares[r];
                Rectangle row = RowRect(r);
                row.Inflate(-12, 0);
                row.X += (int)((1f - a) * BattleRules.UiEnterSlide * 1.5f);
                float dim = (w.Sold ? 0.35f : 1f) * a;
                float e = w.Sold ? 0f : rowMotion[r].Shown;

                Gfx.Rect(sb, row, Palette.Void * (0.4f * a));
                if (e > 0f)
                {
                    Ui.InkBar(sb, row.X + BattleRules.UiShadow, row.Y + BattleRules.UiShadow, row.Width * e, row.Height, 10f, Palette.LineGrey * (0.5f * a), r * 7 + 3, 1f);
                    Ui.InkBar(sb, row.X, row.Y, row.Width * e, row.Height, 10f, Palette.Paper * a, r * 7, 1f);
                }
                Color ink = Color.Lerp(Palette.Paper, Palette.Ink, e);
                Color soft = Color.Lerp(Palette.LineGrey, Palette.InkSoft, e);

                //Icon : not on the shelf while it is flying up
                if (r != flyRow) DrawWareIcon(sb, w, row.X + 34, row.Center.Y, dim, ink);

                //Name and Kind
                Gfx.Text(sb, Game.BigFont, w.Name, row.X + 70, row.Y + 10, ink * dim, TextSize.Small);
                if (w.KindLabel.Length > 0) Gfx.TextSpaced(sb, Game.Font, w.KindLabel, row.X + 72, row.Y + 44, soft * dim, TextSize.Tiny, 2f);

                //Price : a diamond and the number, dimmed when you cannot afford it
                if (!w.Sold)
                {
                    bool afford = run.Shards >= w.Price;
                    Color priceColor = Color.Lerp(afford ? Palette.Highlight : Palette.LineGrey, Palette.Ink, e);
                    Gfx.Diamond(sb, row.Right - 70, row.Center.Y, 5, priceColor * a);
                    Gfx.Text(sb, Game.BigFont, w.PriceLabel, row.Right - 58, row.Center.Y - 16, priceColor * a, TextSize.Small);
                }
                else if (w.Kind != WareKind.Empty)
                {
                    Gfx.Rect(sb, row.X, row.Center.Y, row.Width, 1, Palette.LineGrey * (0.6f * a));
                    Ui.Tag(sb, "SOLD", row.Right - 70, row.Center.Y - 9, true, 0.8f * a);
                }
            }

            //REROLL : its price on the button, dimmed when there is not enough
            float b = UiMotion.Between(time, 0.45f, BattleRules.UiEnterTime);
            bool can = run.Shards >= RerollPrice;
            Ui.Command(sb, rerollButton, rerollLabel, "R", rerollMotion, can ? b : b * 0.5f, false);
        }

        //Ware Icons : the motif's badge, a rehearsal's star in the musician's colour, or a word for the band's wares
        //(round 15 : plain words, the drawn chair and tuning fork were hard to read)
        private void DrawWareIcon(SpriteBatch sb, Ware w, float x, float y, float a, Color ink)
        {
            Color c = ink * a;
            if (w.Kind == WareKind.Motif) MotifArt.Badge(sb, w.Motif, x, y, 22, a);
            else if (w.Kind == WareKind.Rehearsal) Gfx.Star(sb, x, y, 22f, BandColumn.SpotOf(w.Who) * a);
            else if (w.IconLabel.Length > 0) Gfx.TextSpacedCentered(sb, Game.Font, w.IconLabel, x, y - 7, c, TextSize.Tiny, 2f);
        }

        //Fly Draw : FLY UP, the ware rising off its shelf, growing on the way up and shrinking into its place, with a
        //short trail behind it. Where it lands, a ring goes out. Drawn over the top bar.
        private void DrawFly(SpriteBatch sb)
        {
            if (flyRow >= 0)
            {
                float t = MathHelper.Clamp(flyAge / FlyTime, 0f, 1f);
                for (int k = 4; k >= 0; k--)
                {
                    float behind = MathHelper.Clamp(t - k * 0.045f, 0f, 1f);
                    Vector2 at = FlyPoint(UiMotion.EaseInOut(behind));
                    float size = (1f + 0.45f * (float)Math.Sin(behind * MathHelper.Pi)) * (1f - 0.25f * behind);
                    float alpha = k == 0 ? 1f : 0.3f * (1f - k / 5f);
                    if (k == 0) Gfx.DrawGlow(sb, at.X, at.Y, 46f * size, Palette.Paper * 0.35f);
                    if (flyWare.Kind == WareKind.Motif) MotifArt.Badge(sb, flyWare.Motif, at.X, at.Y, 22f * size, alpha);
                    else DrawWareIcon(sb, flyWare, at.X, at.Y, alpha, Palette.Paper);
                }
            }
            if (landAge < 0.45f)
            {
                float k = landAge / 0.45f;
                Gfx.CircleOutline(sb, landAt.X, landAt.Y, 10f + 36f * UiMotion.EaseOut(k), Palette.Paper * (0.9f * (1f - k)), 1.5f);
                Gfx.DrawGlow(sb, landAt.X, landAt.Y, 30f + 30f * k, Palette.Paper * (0.35f * (1f - k)));
            }
        }

        //Dialogue : the name tag and what the keeper is saying, written out row by row. Each row being written is shown
        //through a box that widens (a scissor box, in real pixels), with a small bright pen mark at its end.
        private static readonly RasterizerState clip = new RasterizerState { ScissorTestEnable = true };

        private void DrawDialogue(SpriteBatch sb)
        {
            float a = UiMotion.Between(time, 0.3f, BattleRules.UiEnterTime);
            Rectangle box = dialogueBox;
            box.Y += (int)((1f - a) * BattleRules.UiEnterSlide);
            Gfx.Rect(sb, box, Palette.Void * (0.92f * a));
            Ornament.DoubleFrame(sb, box, Palette.PaperDim * a);
            Ui.Tag(sb, KeeperName, box.X + 24, box.Y - 9, true, a);

            float written = (lineAge - 0.05f) * WriteSpeed;
            float rowH = Game.StoryFont.LineSpacing * TextSize.Story;
            float x = box.X + 30;
            float y = box.Y + 26;
            GraphicsDevice device = sb.GraphicsDevice;
            float s = Sharp.Scale;
            for (int k = 0; k < lineRows.Length; k++)
            {
                float rowW = Gfx.TextWidth(Game.StoryFont, lineRows[k], TextSize.Story) + 12f;
                float show = Math.Min(written, rowW);
                written -= rowW;
                if (show <= 0f) break;
                float ry = y + k * rowH;
                if (show >= rowW)
                {
                    Gfx.Text(sb, Game.StoryFont, lineRows[k], x, ry, Palette.Paper * a, TextSize.Story);
                    continue;
                }

                Rectangle before = device.ScissorRectangle;
                sb.End();
                device.ScissorRectangle = new Rectangle((int)(x * s), (int)((ry - 6f) * s), (int)(show * s), (int)((rowH + 12f) * s));
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, clip, null, Sharp.Transform);
                Gfx.Text(sb, Game.StoryFont, lineRows[k], x, ry, Palette.Paper * a, TextSize.Story);
                sb.End();
                device.ScissorRectangle = before;
                Sharp.Begin(sb);
                Gfx.Rect(sb, x + show, ry + 3f, 2f, rowH - 4f, Palette.Paper * (0.7f * a));
            }

            //Next Mark : the little bouncing triangle every dialogue box has, once every word is out
            if (written >= 0f)
            {
                float bob = (float)Math.Sin(time * 5f) * 3f;
                Gfx.Triangle(sb, box.Right - 26, box.Bottom - 20 + bob, 6, false, Palette.Paper * a);
            }
        }
    }
}
