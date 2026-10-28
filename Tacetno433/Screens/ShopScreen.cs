using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //ShopScreen : trade shards for help.
    //
    //Left  : the shop itself, shelves and a counter with the shopkeeper behind it (placeholder)
    //Right : the wares. Click one to buy it.
    //Bottom: the shopkeeper talks in a dialogue box, and reacts to what you point at.
    //
    //Wares, top to bottom:
    //   EXTRA SEAT    one more seat on stage, at most two per floor, dearer each time
    //   TUNING        stamina back
    //   HIRE          a musician from the current era joins. Round 14 : the shop shows who, with
    //                 their part, before paying, so the band can be built on purpose
    //   three motifs, rolled when the shop opens
    //Leaving asks first, because the shelves are gone once the band walks out.
    //Beta1 screens round 1 (29 Oct, docs/SCREENS_LEVEL_UP.md 14) : THE ARCHIVIST stands in a stage light, the rest of the
    //room is dark. The wares come in one after another and light up like the menus when pointed at. What the band buys
    //flies up into the top bar (a motif to its badge, a seat to SEATS, tuning to LIFE, a player to the band's name), and
    //the keeper's words are written out line by line.
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
        private const string FullLine = "Your ensemble has no room for anyone else.";
        private const string NobodyLine = "Nobody from this era is looking for a band right now.";
        private const string RestedLine = "Your band is already in perfect tune.";

        //Ware Rows
        private const int SeatRow = 0;
        private static readonly string[] TurnWays = { "LEFT", "DOWN", "RIGHT" };   // a musician's CUE as the way their skills start
        private const int TuningRow = 1;
        private const int HireRow = 2;
        private const int FirstMotifRow = 3;
        private const int RowCount = 6;

        //Shop Layout
        private Rectangle waresBox = new Rectangle(730, 120, 510, 450);
        private Rectangle dialogueBox = new Rectangle(36, 566, 660, 130);
        private Rectangle leaveButton = new Rectangle(930, 630, 310, 54);
        private const int RowH = 68;

        //Shop State
        private Motif[] motifs = new Motif[0];
        private Musician hire;                 // the musician on offer, rolled when the shop opens, or null
        private string[] names = new string[RowCount];
        private string[] notes = new string[RowCount];
        private string[] priceLabels = new string[RowCount];
        private bool[] sold = new bool[RowCount];
        private int hoverRow = -1;
        private int lastHover = -1;
        private string line = "";
        private string lineWrapped = "";
        private string[] lineRows = new string[0];  // the keeper's line, one string per row, made by Say for the writing out
        private float lineAge;
        private int thanksIndex;
        private float time;

        //Motion (Beta1 round 1) : the rows and the button point like the menus (Core/UiMotion.cs)
        private UiMotion[] rowMotion = new UiMotion[RowCount];
        private UiMotion leaveMotion = new UiMotion();

        //FLY UP : what was just bought, on its way from the shelf to the top bar
        private const float FlyTime = 0.6f;
        private int flyRow = -1;                // the ware flying, -1 for none
        private Motif flyMotif;                 // the motif it is, for its badge
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
            motifs = MotifList.Roll(run.Motifs, 3, 1, run.Floor, run.Rng);

            names[SeatRow] = "EXTRA SEAT";
            notes[SeatRow] = "One more musician on stage. Two per floor at most.";
            names[TuningRow] = "TUNING";
            notes[TuningRow] = "The band gets back " + (int)(BattleRules.TuningRecover * 100) + " percent of its " + BattleRules.LifeWordLower + ".";
            if (BattleRules.TurnBattleOn) notes[SeatRow] = "One more musician on stage when a fight starts. Two per floor at most.";   // TURN BATTLE (step 15)
            //Hire : one player from this era, known before buying, with their seat
            hire = MusicianList.RollFromEra(run.Era, run.Roster, run.Rng);
            names[HireRow] = hire != null ? "HIRE " + hire.Name : "HIRE A PLAYER";
            notes[HireRow] = hire != null
                ? hire.Name + ", " + hire.Instrument + ". Sits " + StageLayout.Rows[StageLayout.SectionOf(hire.Family)].Name
                  + ", " + StageLayout.SideNames[hire.Cue] + ". " + hire.TraitName + " : " + hire.TraitText
                : NobodyLine;
            if (hire != null && BattleRules.TurnBattleOn)                                   // TURN BATTLE : the side is the way their skills start
                notes[HireRow] = hire.Name + ", " + hire.Instrument + ". Skills " + TurnWays[hire.Cue] + ". " + hire.TraitName + " : " + hire.TraitText;
            if (hire == null) sold[HireRow] = true;

            for (int i = 0; i < 3; i++)
            {
                int row = FirstMotifRow + i;
                if (i < motifs.Length)
                {
                    names[row] = motifs[i].Name;
                    notes[row] = BattleRules.TurnBattleOn && motifs[i].TurnText.Length > 0 ? motifs[i].TurnText : motifs[i].Text;   // step 15 : the turn battle's words, like the cards
                }
                else
                {
                    names[row] = "EMPTY SHELF";
                    notes[row] = "";
                    sold[row] = true;
                }
            }

            RefreshPrices();
            Say(greetings[run.Rng.Next(greetings.Length)]);
            SoundBank.PlayMusic(Music.Shop);

            for (int r = 0; r < RowCount; r++) rowMotion[r] = new UiMotion();
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

        //Prices : the seat gets dearer after each purchase, so the labels are rebuilt then
        private void RefreshPrices()
        {
            priceLabels[SeatRow] = Game.CurrentRun.SeatPrice.ToString();
            priceLabels[TuningRow] = BattleRules.TuningPrice.ToString();
            priceLabels[HireRow] = BattleRules.HirePrice.ToString();
            for (int i = 0; i < motifs.Length; i++)
                priceLabels[FirstMotifRow + i] = MotifList.Price(motifs[i]).ToString();
        }

        private int PriceOf(int row)
        {
            if (row == SeatRow) return Game.CurrentRun.SeatPrice;
            if (row == TuningRow) return BattleRules.TuningPrice;
            if (row == HireRow) return BattleRules.HirePrice;
            return MotifList.Price(motifs[row - FirstMotifRow]);
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
            return new Rectangle(waresBox.X, waresBox.Y + 44 + row * RowH, waresBox.Width, RowH - 6);
        }

        public override void Update(float dt)
        {
            time += dt;
            lineAge += dt;
            UpdateFly(dt);

            //Leaving : only once the player has said yes
            if (confirm.Open)
            {
                for (int r = 0; r < RowCount; r++) rowMotion[r].Update(dt, false);
                leaveMotion.Update(dt, false);
                if (confirm.Update(dt) == 1) RunFlow.NextStage(Game);
                return;
            }

            //Hover : describe whatever is being pointed at
            hoverRow = -1;
            for (int r = 0; r < RowCount; r++)
                if (Input.MouseOver(RowRect(r))) hoverRow = r;
            for (int r = 0; r < RowCount; r++) rowMotion[r].Update(dt, r == hoverRow && !Gone(r));
            leaveMotion.Update(dt, Input.MouseOver(leaveButton));

            if (hoverRow != lastHover)
            {
                if (hoverRow >= 0 && notes[hoverRow].Length > 0) Say(notes[hoverRow]);
                if (hoverRow >= 0) SoundBank.Play(Sfx.UiMove);
                lastHover = hoverRow;
            }

            if (Input.MouseClicked() && hoverRow >= 0) Buy(hoverRow);

            if (Input.KeyPressed(Keys.Enter) || Input.ClickedOn(leaveButton))
            {
                leaveMotion.Press();
                confirm.Show("LEAVE THE SHOP?", "Whatever stays on the shelves is gone once the band walks out.", "LEAVE", "STAY");
            }
        }

        //Gone : sold out, or a seat the band cannot buy any more of on this floor
        private bool Gone(int row)
        {
            return sold[row] || (row == SeatRow && !Game.CurrentRun.SeatForSale);
        }

        //Fly : what was bought leaves its shelf for the top bar. A motif's badge is not drawn up there until it lands.
        private void Fly(int row, Vector2 to)
        {
            Rectangle r = RowRect(row);
            flyRow = row;
            flyMotif = row >= FirstMotifRow ? motifs[row - FirstMotifRow] : null;
            flyFrom = new Vector2(r.X + 46, r.Center.Y);
            flyTo = to;
            flyAge = 0f;
            RunHud.Arriving = flyMotif != null ? 1 : 0;
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

            if (sold[row]) { Deny(SoldLine); return; }
            if (row == SeatRow && !run.SeatForSale) { Deny(SoldLine); return; }
            if (row == TuningRow && run.Stamina >= run.MaxStamina) { Deny(RestedLine); return; }
            if (row == HireRow && run.RosterFull) { Deny(FullLine); return; }
            if (row == HireRow && hire == null) { Deny(NobodyLine); return; }

            if (!run.SpendShards(PriceOf(row))) { Deny(PoorLine); return; }

            if (row == SeatRow)
            {
                run.AddSeat();
                if (!run.SeatForSale) sold[row] = true;
                Say(thanks[thanksIndex++ % thanks.Length]);
                Fly(row, RunHud.SeatsAt);
            }
            else if (row == TuningRow)
            {
                run.ChangeStamina((int)(run.MaxStamina * BattleRules.TuningRecover));
                sold[row] = true;
                Say(thanks[thanksIndex++ % thanks.Length]);
                Fly(row, RunHud.LifeAt);
            }
            else if (row == HireRow)
            {
                run.JustJoined.Clear();
                Musician m = run.Recruit(hire);
                if (m == null)
                {
                    run.AddShards(PriceOf(row));      // nobody came, give the money back
                    Deny(NobodyLine);
                    return;
                }
                sold[row] = true;
                bool seated = run.Formation.SeatOf(m) >= 0;
                Say(m.Name + " packs up an instrument and joins you." + (seated ? " A seat was free." : " They wait on the bench."));
                Fly(row, RunHud.BandAt);
            }
            else
            {
                run.AddMotif(motifs[row - FirstMotifRow]);
                sold[row] = true;
                SoundBank.Play(Sfx.MotifGet);
                Say(thanks[thanksIndex++ % thanks.Length]);
                Fly(row, RunHud.BadgeCentre(run.Motifs.Count - 1));
            }

            run.RefreshLabels();
            RefreshPrices();
            SoundBank.Play(Sfx.Buy);
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
            if (hoverRow >= 0 && !Gone(hoverRow))
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

        //Wares : one row per item, coming in one after another. Pointed at, an ink plate shoots out under it like the
        //menus (Ui.InkBar) and its words turn black. Its price, and a SOLD band when it is gone.
        private void DrawWares(SpriteBatch sb, RunState run)
        {
            float panel = UiMotion.Between(time, 0.1f, BattleRules.UiEnterTime);
            Rectangle box = waresBox;
            box.X += (int)((1f - panel) * BattleRules.UiEnterSlide);
            Ui.Panel(sb, box, panel);
            Ui.Header(sb, "", "Wares", box.X + 20, box.Y + 8, box.Width - 40, panel);

            for (int r = 0; r < RowCount; r++)
            {
                float a = UiMotion.Between(time, 0.18f + r * BattleRules.UiEnterStagger, BattleRules.UiEnterTime);
                if (a <= 0f) continue;
                Rectangle row = RowRect(r);
                row.Inflate(-12, 0);
                row.X += (int)((1f - a) * BattleRules.UiEnterSlide * 1.5f);
                bool gone = Gone(r);
                float dim = (gone ? 0.35f : 1f) * a;
                float e = gone ? 0f : rowMotion[r].Shown;

                Gfx.Rect(sb, row, Palette.Void * (0.4f * a));
                if (e > 0f)
                {
                    Ui.InkBar(sb, row.X + BattleRules.UiShadow, row.Y + BattleRules.UiShadow, row.Width * e, row.Height, 10f, Palette.LineGrey * (0.5f * a), r * 7 + 3, 1f);
                    Ui.InkBar(sb, row.X, row.Y, row.Width * e, row.Height, 10f, Palette.Paper * a, r * 7, 1f);
                }
                Color ink = Color.Lerp(Palette.Paper, Palette.Ink, e);
                Color soft = Color.Lerp(Palette.LineGrey, Palette.InkSoft, e);

                //Icon : not on the shelf while it is flying up
                if (r != flyRow) DrawWareIcon(sb, r, row.X + 34, row.Center.Y, dim, ink);

                //Name and Note
                Gfx.Text(sb, Game.BigFont, names[r], row.X + 70, row.Y + 4, ink * dim, TextSize.Small);
                string kind = r >= FirstMotifRow && r - FirstMotifRow < motifs.Length ? motifs[r - FirstMotifRow].RarityLabel : "SERVICE";
                Gfx.TextSpaced(sb, Game.Font, kind, row.X + 72, row.Y + 38, soft * dim, TextSize.Tiny, 2f);

                //Price : a diamond and the number, dimmed when you cannot afford it
                if (!gone)
                {
                    bool afford = run.Shards >= PriceOf(r);
                    Color priceColor = Color.Lerp(afford ? Palette.Highlight : Palette.LineGrey, Palette.Ink, e);
                    Gfx.Diamond(sb, row.Right - 70, row.Center.Y, 5, priceColor * a);
                    Gfx.Text(sb, Game.BigFont, priceLabels[r], row.Right - 58, row.Center.Y - 16, priceColor * a, TextSize.Small);
                }
                else
                {
                    Gfx.Rect(sb, row.X, row.Center.Y, row.Width, 1, Palette.LineGrey * (0.6f * a));
                    Ui.Tag(sb, "SOLD", row.Right - 70, row.Center.Y - 9, true, 0.8f * a);
                }
            }
        }

        //Ware Icons : a word for a seat and for tuning, the new player's side arrow, or the motif's
        //badge (round 15 : plain words, the drawn chair and tuning fork were hard to read)
        private void DrawWareIcon(SpriteBatch sb, int row, float x, float y, float a, Color ink)
        {
            Color c = ink * a;
            if (row == SeatRow) Gfx.TextSpacedCentered(sb, Game.Font, "SEAT", x, y - 7, c, TextSize.Tiny, 2f);
            else if (row == TuningRow) Gfx.TextSpacedCentered(sb, Game.Font, "TUNE", x, y - 7, c, TextSize.Tiny, 2f);
            else if (row == HireRow)
            {
                if (hire != null) MusicianArt.SideBadge(sb, hire.Cue, x, y, 14f, Palette.Ink, Palette.Paper, a);
                else Gfx.TextSpacedCentered(sb, Game.Font, "HIRE", x, y - 7, c, TextSize.Tiny, 2f);
            }
            else if (row - FirstMotifRow < motifs.Length)
            {
                MotifArt.Badge(sb, motifs[row - FirstMotifRow], x, y, 22, a);
            }
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
                    if (flyMotif != null) MotifArt.Badge(sb, flyMotif, at.X, at.Y, 22f * size, alpha);
                    else DrawWareIcon(sb, flyRow, at.X, at.Y, alpha, Palette.Paper);
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
