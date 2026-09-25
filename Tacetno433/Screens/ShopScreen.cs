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
    //   HIRE          a musician from the current era joins
    //   three motifs, rolled when the shop opens
    //Leaving asks first, because the shelves are gone once the band walks out.
    public class ShopScreen : GameScreen
    {
        //Leave Box : "are you sure?" before walking out
        private ConfirmBox confirm = new ConfirmBox();

        //Uses Escape : while the box is open, ESC answers it instead of opening the pause menu
        public override bool UsesEscape
        {
            get { return confirm.Open; }
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
        private string[] names = new string[RowCount];
        private string[] notes = new string[RowCount];
        private string[] priceLabels = new string[RowCount];
        private bool[] sold = new bool[RowCount];
        private int hoverRow = -1;
        private int lastHover = -1;
        private string line = "";
        private string lineWrapped = "";
        private float lineAge;
        private int thanksIndex;
        private float time;

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            motifs = MotifList.Roll(run.Motifs, 3, 1, run.Floor, run.Rng);

            names[SeatRow] = "EXTRA SEAT";
            notes[SeatRow] = "One more musician on stage. Two per floor at most.";
            names[TuningRow] = "TUNING";
            notes[TuningRow] = "The band gets back " + (int)(BattleRules.TuningRecover * 100) + " percent of its stamina.";
            names[HireRow] = "HIRE A PLAYER";
            notes[HireRow] = "A musician of the " + run.CurrentEra.Name + " era joins the ensemble.";

            for (int i = 0; i < 3; i++)
            {
                int row = FirstMotifRow + i;
                if (i < motifs.Length)
                {
                    names[row] = motifs[i].Name;
                    notes[row] = motifs[i].Text;
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

            //Leaving : only once the player has said yes
            if (confirm.Open)
            {
                if (confirm.Update(dt) == 1) RunFlow.NextStage(Game);
                return;
            }

            //Hover : describe whatever is being pointed at
            hoverRow = -1;
            for (int r = 0; r < RowCount; r++)
                if (Input.MouseOver(RowRect(r))) hoverRow = r;

            if (hoverRow != lastHover)
            {
                if (hoverRow >= 0 && notes[hoverRow].Length > 0) Say(notes[hoverRow]);
                if (hoverRow >= 0) SoundBank.Play(Sfx.UiMove);
                lastHover = hoverRow;
            }

            if (Input.MouseClicked() && hoverRow >= 0) Buy(hoverRow);

            if (Input.KeyPressed(Keys.Enter) || Input.ClickedOn(leaveButton))
                confirm.Show("LEAVE THE SHOP?", "Whatever stays on the shelves is gone once the band walks out.", "LEAVE", "STAY");
        }

        //Buy : check, pay, then hand the thing over
        private void Buy(int row)
        {
            RunState run = Game.CurrentRun;

            if (sold[row]) { Deny(SoldLine); return; }
            if (row == SeatRow && !run.SeatForSale) { Deny(SoldLine); return; }
            if (row == TuningRow && run.Stamina >= run.MaxStamina) { Deny(RestedLine); return; }
            if (row == HireRow && run.RosterFull) { Deny(FullLine); return; }
            if (row == HireRow && MusicianList.AvailableInEra(run.Era, run.Roster) == 0) { Deny(NobodyLine); return; }

            if (!run.SpendShards(PriceOf(row))) { Deny(PoorLine); return; }

            if (row == SeatRow)
            {
                run.AddSeat();
                if (!run.SeatForSale) sold[row] = true;
                Say(thanks[thanksIndex++ % thanks.Length]);
            }
            else if (row == TuningRow)
            {
                run.ChangeStamina((int)(run.MaxStamina * BattleRules.TuningRecover));
                sold[row] = true;
                Say(thanks[thanksIndex++ % thanks.Length]);
            }
            else if (row == HireRow)
            {
                run.JustJoined.Clear();
                Musician m = run.RecruitOne();
                if (m == null)
                {
                    run.AddShards(PriceOf(row));      // nobody came, give the money back
                    Deny(NobodyLine);
                    return;
                }
                sold[row] = true;
                bool seated = run.Formation.SeatOf(m) >= 0;
                Say(m.Name + " packs up an instrument and joins you." + (seated ? " A seat was free." : " They wait on the bench."));
            }
            else
            {
                run.AddMotif(motifs[row - FirstMotifRow]);
                sold[row] = true;
                SoundBank.Play(Sfx.MotifGet);
                Say(thanks[thanksIndex++ % thanks.Length]);
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

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;

            DrawInterior(sb);
            DrawWares(sb, run);
            DrawDialogue(sb);

            Ui.Button(sb, leaveButton, "LEAVE THE SHOP", "ENTER", true);
            Gfx.TextSpacedRight(sb, Game.Font, "CLICK A WARE TO BUY IT", leaveButton.Right, leaveButton.Y - 22, Palette.LineGrey, TextSize.Tiny, 2f);

            RunHud.DrawTop(sb, run, "SHOP");
            RunHud.DrawTips(sb, run);
            confirm.Draw(sb);
        }

        //Interior : the shop painting, or an empty slot where it goes.
        //ARTWORK : Content/Art/Places/shop.png (the shopkeeper is part of the picture), see ArtBank.
        private void DrawInterior(SpriteBatch sb)
        {
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            ArtBank.DrawOrSlot(sb, ArtBank.Shop, new Rectangle(24, 80, 672, 470), Palette.Paper, 1f);
            Ornament.Vignette(sb, 60, 0.6f);
        }

        //Wares : one row per item, with its price and a SOLD band when gone
        private void DrawWares(SpriteBatch sb, RunState run)
        {
            Ui.Panel(sb, waresBox, 1f);
            Ui.Header(sb, "", "Wares", waresBox.X + 20, waresBox.Y + 8, waresBox.Width - 40, 1f);

            for (int r = 0; r < RowCount; r++)
            {
                Rectangle row = RowRect(r);
                row.Inflate(-12, 0);
                bool over = r == hoverRow;
                bool gone = sold[r] || (r == SeatRow && !run.SeatForSale);
                float a = gone ? 0.35f : 1f;

                Gfx.Rect(sb, row, (over && !gone ? Palette.Stage : Palette.Void * 0.4f));
                if (over && !gone) Gfx.Rect(sb, row.X, row.Y, 3, row.Height, Palette.Accent);

                //Icon
                float ix = row.X + 34;
                float iy = row.Center.Y;
                DrawWareIcon(sb, r, ix, iy, a);

                //Name and Note
                Gfx.Text(sb, Game.BigFont, names[r], row.X + 70, row.Y + 4, Palette.Paper * a, TextSize.Small);
                string kind = r >= FirstMotifRow && r - FirstMotifRow < motifs.Length ? motifs[r - FirstMotifRow].RarityLabel : "SERVICE";
                Gfx.TextSpaced(sb, Game.Font, kind, row.X + 72, row.Y + 38, Palette.LineGrey * a, TextSize.Tiny, 2f);

                //Price : a diamond and the number, dimmed when you cannot afford it
                if (!gone)
                {
                    bool afford = run.Shards >= PriceOf(r);
                    Color priceColor = afford ? Palette.Highlight : Palette.LineGrey;
                    Gfx.Diamond(sb, row.Right - 70, row.Center.Y, 5, priceColor);
                    Gfx.Text(sb, Game.BigFont, priceLabels[r], row.Right - 58, row.Center.Y - 16, priceColor, TextSize.Small);
                }

                if (gone)
                {
                    Gfx.Rect(sb, row.X, row.Center.Y, row.Width, 1, Palette.LineGrey * 0.6f);
                    Ui.Tag(sb, "SOLD", row.Right - 70, row.Center.Y - 9, true, 0.8f);
                }
            }
        }

        //Ware Icons : a capsule for seats, a tuning fork, a figure, or the motif's badge
        private void DrawWareIcon(SpriteBatch sb, int row, float x, float y, float a)
        {
            Color c = Palette.Paper * a;
            if (row == SeatRow)
            {
                Gfx.CapsuleOutline(sb, new Rectangle((int)x - 9, (int)y - 22, 18, 44), c, 2f);
                Gfx.Rect(sb, x - 5, y - 1, 10, 2, c);
                Gfx.Rect(sb, x - 1, y - 5, 2, 10, c);
            }
            else if (row == TuningRow)
            {
                Gfx.Rect(sb, x - 8, y - 20, 2, 24, c);
                Gfx.Rect(sb, x + 6, y - 20, 2, 24, c);
                Gfx.Arc(sb, x, y + 4, 7, 0f, MathHelper.Pi, c, 2f);
                Gfx.Rect(sb, x - 1, y + 10, 2, 14, c);
            }
            else if (row == HireRow)
            {
                Gfx.Circle(sb, x, y - 12, 8, c);
                Gfx.Rect(sb, x - 12, y, 24, 20, c);
            }
            else if (row - FirstMotifRow < motifs.Length)
            {
                MotifArt.Badge(sb, motifs[row - FirstMotifRow], x, y, 22, a);
            }
        }

        //Dialogue : the name tag and what the shopkeeper is saying, fading in when it changes
        private void DrawDialogue(SpriteBatch sb)
        {
            Gfx.Rect(sb, dialogueBox, Palette.Void * 0.92f);
            Ornament.DoubleFrame(sb, dialogueBox, Palette.PaperDim);
            Ui.Tag(sb, KeeperName, dialogueBox.X + 24, dialogueBox.Y - 9, true, 1f);

            float a = Math.Min(1f, lineAge * 4f);
            Gfx.Text(sb, Game.StoryFont, lineWrapped, dialogueBox.X + 30, dialogueBox.Y + 26 + (1f - a) * 6f, Palette.Paper * a, TextSize.Story);

            //Next Mark : the little bouncing triangle every dialogue box has
            float bob = (float)Math.Sin(time * 5f) * 3f;
            Gfx.Triangle(sb, dialogueBox.Right - 26, dialogueBox.Bottom - 20 + bob, 6, false, Palette.Paper);
        }
    }
}
