using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Audio;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //EventScreen : the ??? path. A short scene with two or three choices.
    //
    //   READ     the story and the choices
    //   PICK     a choice with a check asks which musician tries it, each card shows the chance
    //   ROLLING  a short moment of suspense while the number spins
    //   RESULT   what happened, what was gained or lost, and a button to carry on
    //
    //The events themselves are written in Data/GameEvent.cs.
    public class EventScreen : GameScreen
    {
        private enum Phase { Read, Pick, Rolling, Result }

        //Event Layout
        private Rectangle artBox = new Rectangle(40, 110, 540, 450);
        private const int TextX = 640;
        private const int ChoiceW = 600;
        private const int ChoiceH = 54;
        private const int ChoiceY = 356;
        private Rectangle pickPanel = new Rectangle(0, 424, TacetGame.ScreenW, 296);
        private Rectangle continueButton = new Rectangle(930, 630, 310, 54);
        private Rectangle cancelButton = new Rectangle(40, 654, 170, 44);
        private const float RollTime = 1.1f;

        private static string[] numerals = { "I", "II", "III", "IV" };

        //Event State
        private GameEvent ev;
        private Phase phase;

        //Uses Escape : while picking who takes the chance, ESC puts the choice back
        //instead of opening the pause menu
        public override bool UsesEscape
        {
            get { return phase == Phase.Pick; }
        }
        private int hoverChoice = -1;
        private int chosen = -1;
        private int hoverCard = -1;
        private Musician checker;
        private int chance;
        private int roll;
        private bool success;
        private bool startFight;
        private bool recruited;
        private Motif gotMotif;
        private string resultWord = "";
        private string rollLabel = "";
        private string rewardLabel = "";
        private string outcomeWrapped = "";
        private string stageLine = "";
        private int[] cardChances = new int[16];
        private float phaseTimer;
        private float time;

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            ev = run.CurrentEvent;
            if (ev == null) ev = EventList.Pick(run.Rng);

            stageLine = "UNKNOWN PATH   /   " + run.StageLabel;
            phase = Phase.Read;
            SoundBank.Play(Sfx.PageTurn);
            SoundBank.PlayMusic(Music.Event);
        }

        private Rectangle ChoiceRect(int i)
        {
            return new Rectangle(TextX, ChoiceY + i * (ChoiceH + 10), ChoiceW, ChoiceH);
        }

        //Card Rect : the musician cards on the pick panel, centred in one row
        private Rectangle CardRect(int i)
        {
            int count = Game.CurrentRun.Roster.Count;
            int gap = 10;
            int w = Math.Min(110, (TacetGame.ScreenW - 40 - (count - 1) * gap) / Math.Max(1, count));
            int total = count * w + (count - 1) * gap;
            int x = (TacetGame.ScreenW - total) / 2 + i * (w + gap);
            return new Rectangle(x, pickPanel.Y + 74, w, 170);
        }

        private bool CanTake(EventChoice c)
        {
            return Game.CurrentRun.Shards >= c.ShardCost;
        }

        public override void Update(float dt)
        {
            time += dt;
            phaseTimer += dt;
            RunState run = Game.CurrentRun;

            if (phase == Phase.Read)
            {
                hoverChoice = -1;
                for (int i = 0; i < ev.Choices.Length; i++)
                    if (Input.MouseOver(ChoiceRect(i))) hoverChoice = i;

                int pick = -1;
                if (Input.MouseClicked() && hoverChoice >= 0) pick = hoverChoice;
                if (Input.KeyPressed(Keys.D1)) pick = 0;
                if (Input.KeyPressed(Keys.D2)) pick = 1;
                if (Input.KeyPressed(Keys.D3)) pick = 2;

                if (pick >= 0 && pick < ev.Choices.Length) Take(pick);
            }
            else if (phase == Phase.Pick)
            {
                hoverCard = -1;
                for (int i = 0; i < run.Roster.Count; i++)
                    if (Input.MouseOver(CardRect(i))) hoverCard = i;

                if (Input.MouseClicked() && hoverCard >= 0)
                {
                    checker = run.Roster[hoverCard];
                    chance = EventList.ChanceFor(ev.Choices[chosen], run, checker);
                    roll = run.RollPercent();
                    success = roll < chance;
                    SetPhase(Phase.Rolling);
                    SoundBank.Play(Sfx.UiConfirm);
                }

                //Cancel : put the choice back, nothing was paid yet for checks
                if (Input.KeyPressed(Keys.Escape) || Input.MouseRightClicked() || Input.ClickedOn(cancelButton))
                {
                    SetPhase(Phase.Read);
                    SoundBank.Play(Sfx.UiBack);
                }
            }
            else if (phase == Phase.Rolling)
            {
                if (phaseTimer >= RollTime)
                {
                    EventChoice c = ev.Choices[chosen];
                    SoundBank.Play(success ? Sfx.CheckPass : Sfx.CheckFail);
                    resultWord = success ? "SUCCESS" : "FAILURE";
                    rollLabel = checker.Name + "   /   CHANCE " + chance + "%   /   ROLLED " + roll;
                    if (success) Finish(c.Win, c.WinAmount, c.WinWrapped);
                    else Finish(c.Lose, c.LoseAmount, c.LoseWrapped);
                }
            }
            else if (phase == Phase.Result)
            {
                if (phaseTimer > 0.4f && (Input.KeyPressed(Keys.Enter) || Input.KeyPressed(Keys.Space) || Input.ClickedOn(continueButton)))
                {
                    SoundBank.Play(Sfx.UiConfirm);
                    GoOn(run);
                }
            }
        }

        private void SetPhase(Phase next)
        {
            phase = next;
            phaseTimer = 0f;
        }

        //Take : a choice was clicked. Checks go to the pick panel, the rest happen now.
        private void Take(int index)
        {
            EventChoice c = ev.Choices[index];
            RunState run = Game.CurrentRun;

            if (!CanTake(c))
            {
                SoundBank.Play(Sfx.UiDenied);
                return;
            }

            chosen = index;
            SoundBank.Play(Sfx.UiConfirm);

            if (c.Check)
            {
                //Chances : worked out once for every card, so drawing only reads them
                for (int i = 0; i < run.Roster.Count && i < cardChances.Length; i++)
                    cardChances[i] = EventList.ChanceFor(c, run, run.Roster[i]);
                SetPhase(Phase.Pick);
                return;
            }

            run.SpendShards(c.ShardCost);
            resultWord = "";
            rollLabel = "";
            Finish(c.Win, c.WinAmount, c.WinWrapped);
        }

        //Finish : apply the outcome and show it
        private void Finish(Reward reward, int amount, string text)
        {
            outcomeWrapped = text;
            rewardLabel = Apply(reward, amount);
            SetPhase(Phase.Result);
        }

        //Apply : change the run, and return the short line that says what changed
        private string Apply(Reward reward, int amount)
        {
            RunState run = Game.CurrentRun;

            if (reward == Reward.Shards)
            {
                run.AddShards(amount);
                return NumberText.Signed(amount) + " SHARDS";
            }

            if (reward == Reward.Stamina)
            {
                run.ChangeStamina(amount);
                return NumberText.Signed(amount) + " STAMINA";
            }

            if (reward == Reward.Motif)
            {
                Motif[] found = MotifList.Roll(run.Motifs, 1, amount, run.Floor, run.Rng);
                if (found.Length == 0)
                {
                    run.AddShards(BattleRules.SkipMotifShards);
                    return "NOTHING NEW  /  +" + BattleRules.SkipMotifShards + " SHARDS";
                }
                gotMotif = found[0];
                run.AddMotif(gotMotif);
                SoundBank.Play(Sfx.MotifGet);
                return "MOTIF  /  " + gotMotif.Name;
            }

            if (reward == Reward.Recruit || reward == Reward.RecruitPart)
            {
                run.JustJoined.Clear();
                Musician m = reward == Reward.RecruitPart ? run.Recruit(run.MissingSectionPlayer()) : run.RecruitOne();
                if (m == null)
                {
                    run.AddShards(ev.Choices[chosen].ShardCost);       // money back, nobody could come
                    return "NO ROOM IN THE ENSEMBLE";
                }
                recruited = true;
                return m.Name + " JOINS THE ENSEMBLE";
            }

            if (reward == Reward.Rehearse)
            {
                Musician m = checker;
                if (m == null && run.Roster.Count > 0) m = run.Roster[0];
                if (m == null || !run.CanRehearse(m))
                {
                    run.AddShards(BattleRules.SkipMotifShards);
                    return "ALREADY AT THEIR BEST  /  +" + BattleRules.SkipMotifShards + " SHARDS";
                }
                run.Rehearse(m);
                SoundBank.Play(Sfx.Rehearse);
                return m.Name + "  +1 POWER";
            }

            if (reward == Reward.Fight)
            {
                startFight = true;
                return "A FIGHT BEGINS";
            }

            return "NOTHING CHANGES";
        }

        //Go On : a fight, a new face, or simply the next stage
        private void GoOn(RunState run)
        {
            if (startFight)
            {
                run.Chosen = new RouteNode();
                run.Chosen.Type = NodeType.Elite;
                run.Chosen.Title = RouteNodeInfo.TitleOf(NodeType.Elite);
                run.BeginBattle();
                Game.Screens.Change(new DuelScreen());
                return;
            }

            if (recruited)
            {
                Game.Screens.Change(new RecruitScreen(true));
                return;
            }

            RunFlow.NextStage(Game);
        }

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;

            SceneBackdrop.Draw(sb, time);
            DrawIllustration(sb, run);
            DrawText(sb, run);

            if (phase == Phase.Read) DrawChoices(sb, run);
            if (phase == Phase.Pick) DrawPick(sb, run);
            if (phase == Phase.Rolling) DrawRolling(sb);
            if (phase == Phase.Result) DrawResult(sb);

            RunHud.DrawTop(sb, run, "UNKNOWN PATH");
            RunHud.DrawTips(sb, run);
        }

        //Illustration : the event painting, or an empty slot for it, in a double frame.
        //ARTWORK : Content/Art/Events/event_<title>.png, see ArtBank.
        private void DrawIllustration(SpriteBatch sb, RunState run)
        {
            Gfx.Rect(sb, artBox, Palette.CanvasDark);
            Rectangle picture = artBox;
            picture.Inflate(-10, -10);
            ArtBank.DrawOrSlot(sb, ArtBank.EventOf(ev), picture, Palette.Paper, 1f);

            Ornament.FadeUp(sb, new Rectangle(artBox.X, artBox.Bottom - 80, artBox.Width, 80), 0.9f);
            Ornament.DoubleFrame(sb, artBox, Palette.PaperDim);
            Gfx.TextSpaced(sb, Game.Font, ev.Place, artBox.X + 20, artBox.Bottom - 30, Palette.Paper, TextSize.Label, 3f);
        }

        //Text : where we are, the title, and the story
        private void DrawText(SpriteBatch sb, RunState run)
        {
            Gfx.TextSpaced(sb, Game.Font, stageLine, TextX, 116, Palette.LineGrey, TextSize.Tiny, 3f);
            Gfx.Text(sb, Game.BigFont, ev.Title, TextX, 134, Palette.Highlight, TextSize.Title);
            Ornament.Rule(sb, TextX, 186, ChoiceW, Palette.LineGrey);
            Gfx.Text(sb, Game.StoryFont, ev.TextWrapped, TextX, 204, Palette.Paper, TextSize.Story);
        }

        //Choices : one blade per choice, with its hint tag on the right
        private void DrawChoices(SpriteBatch sb, RunState run)
        {
            Gfx.TextSpaced(sb, Game.Font, "WHAT DO YOU DO", TextX, ChoiceY - 24, Palette.PaperDim, TextSize.Tiny, 4f);

            for (int i = 0; i < ev.Choices.Length; i++)
            {
                EventChoice c = ev.Choices[i];
                Rectangle r = ChoiceRect(i);
                bool can = CanTake(c);
                bool over = i == hoverChoice && can;
                float a = can ? 1f : 0.4f;

                if (over) r.X += 6;
                Gfx.SlantBox(sb, r, Ui.Slant, (over ? Palette.Paper : Palette.Panel) * a);
                if (!over) Gfx.SlantOutline(sb, r, Ui.Slant, Palette.LineGrey * a, 1f);

                Color text = over ? Palette.Ink : Palette.Paper;
                Gfx.Text(sb, Game.BigFont, numerals[i], r.X + 24, r.Y + 10, (over ? Palette.InkSoft : Palette.PaperDim) * a, TextSize.Small);
                Gfx.Text(sb, Game.StoryFont, c.Text, r.X + 70, r.Y + 15, text * a, TextSize.Story);

                if (c.HintLabel.Length > 0)
                {
                    float tw = Ui.TagWidth(c.HintLabel);
                    Ui.Tag(sb, c.HintLabel, r.Right - Ui.Slant - tw - 14, r.Y + 18, c.Check, a);
                }

                Ui.KeyChipRight(sb, NumberText.Get(i + 1), r.X - 8, r.Center.Y, Palette.LineGrey * a);
            }
        }

        //Pick : who takes the check. Every card shows that musician's chance.
        private void DrawPick(SpriteBatch sb, RunState run)
        {
            float slide = Math.Min(1f, phaseTimer * 5f);
            Rectangle panel = pickPanel;
            panel.Y += (int)((1f - slide) * 60f);

            Gfx.Rect(sb, panel, Palette.Void * 0.95f);
            Gfx.Rect(sb, panel.X, panel.Y, panel.Width, 1, Palette.PaperDim);
            Gfx.TextCentered(sb, Game.BigFont, "Who takes it on?", 640, panel.Y + 26, Palette.Paper, TextSize.Subtitle);
            Gfx.TextSpacedCentered(sb, Game.Font, ev.Choices[chosen].HintLabel, 640, panel.Y + 50, Palette.PaperDim, TextSize.Tiny, 3f);

            for (int i = 0; i < run.Roster.Count && i < cardChances.Length; i++)
            {
                Rectangle card = CardRect(i);
                card.Y += (int)((1f - slide) * 60f);
                Musician m = run.Roster[i];
                MusicianArt.Card(sb, card, m, run, i == hoverCard, false, "");

                //Chance Band : the percent in large type along the bottom of the card
                Rectangle band = new Rectangle(card.X, card.Bottom + 4, card.Width, 26);
                bool over = i == hoverCard;
                Gfx.Rect(sb, band, over ? Palette.Paper : Palette.Stage);
                Gfx.TextCentered(sb, Game.BigFont, NumberText.Get(cardChances[i]), band.Center.X - 8, band.Center.Y - 2,
                                 over ? Palette.Ink : Palette.Highlight, TextSize.Small);
                Gfx.Text(sb, Game.Font, "%", band.Center.X + 12, band.Y + 5, over ? Palette.InkSoft : Palette.PaperDim, TextSize.Label);
            }

            Ui.Button(sb, cancelButton, "BACK", "ESC", false);
        }

        //Rolling : a spinning ring and a flickering number
        private void DrawRolling(SpriteBatch sb)
        {
            float cx = 900;
            float cy = 480;
            float t = phaseTimer / RollTime;

            Gfx.Rect(sb, TextX - 20, 330, ChoiceW + 40, 300, Palette.Void * 0.6f);
            Ornament.Rays(sb, cx, cy, 70, 110, 12, time * 6f, Palette.Paper * 0.4f);
            Gfx.CircleOutline(sb, cx, cy, 64, Palette.Paper, 2f);

            int shown = t < 0.85f ? Game.CurrentRun.Rng.Next(100) : roll;
            Gfx.TextCentered(sb, Game.LogoFont, NumberText.Get(shown), cx, cy - 6, Palette.Highlight, TextSize.Banner * 0.7f);
            Gfx.TextSpacedCentered(sb, Game.Font, "NEEDS UNDER", cx, cy + 90, Palette.LineGrey, TextSize.Tiny, 3f);
            Gfx.TextCentered(sb, Game.BigFont, NumberText.Get(chance), cx, cy + 118, Palette.Paper, TextSize.Subtitle);
        }

        //Result : the outcome text, the change it made, and the way on
        private void DrawResult(SpriteBatch sb)
        {
            float a = Math.Min(1f, phaseTimer * 3f);
            int y = 340;

            Gfx.Rect(sb, TextX - 20, y - 10, ChoiceW + 40, 270, Palette.Void * (0.6f * a));

            if (resultWord.Length > 0)
            {
                Ui.Tag(sb, resultWord, TextX, y, success, a);
                Gfx.TextSpaced(sb, Game.Font, rollLabel, TextX + Ui.TagWidth(resultWord) + 14, y + 3, Palette.PaperDim * a, TextSize.Tiny, 2f);
                y += 34;
            }

            Gfx.Text(sb, Game.StoryFont, outcomeWrapped, TextX, y, Palette.Paper * a, TextSize.Story);
            y += 70;

            //Reward Line : a badge for a motif, a diamond for everything else
            Ornament.Rule(sb, TextX, y, ChoiceW, Palette.LineGrey * a);
            if (gotMotif != null) MotifArt.Badge(sb, gotMotif, TextX + 22, y + 44, 20, a);
            else Gfx.Diamond(sb, TextX + 22, y + 44, 8, Palette.Paper * a);
            Gfx.Text(sb, Game.BigFont, rewardLabel, TextX + 56, y + 26, Palette.Highlight * a, TextSize.Small);
            if (gotMotif != null)
                Gfx.Text(sb, Game.StoryFont, gotMotif.TipWrapped, TextX + 56, y + 62, Palette.PaperDim * a, TextSize.StorySmall);

            string label = startFight ? "FACE IT" : "CONTINUE";
            Ui.Button(sb, continueButton, label, "ENTER", true, phaseTimer > 0.4f, a);
        }
    }
}
