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
    //Beta1 screens round 1 (29 Oct, docs/SCREENS_LEVEL_UP.md 14) : UNKNOWN PATH. A cold, narrow light falls on the
    //painting and now and then it falters, like a bulb about to go, and the rest of the room is dark. The story comes in
    //piece by piece, and the choices light up like the menus when pointed at.
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

        //Wipe Word : the path's name in the wipe on the way here, the story itself waits for the page (WIPE WORD)
        public override string WipeWord
        {
            get { return "UNKNOWN PATH"; }
        }

        private int hoverChoice = -1;
        private int chosen = -1;
        private int hoverCard = -1;
        private Musician checker;
        private int chance;
        private int roll;
        private bool success;
        private bool startFight;
        private Motif gotMotif;
        private string resultWord = "";
        private string rollLabel = "";
        private string rewardLabel = "";
        private string outcomeWrapped = "";
        private string stageLine = "";
        private int[] cardChances = new int[16];
        private float phaseTimer;
        private float time;

        //Motion (Beta1 round 1) : the choices and buttons point like the menus (Core/UiMotion.cs)
        private UiMotion[] choiceMotion = { new UiMotion(), new UiMotion(), new UiMotion(), new UiMotion() };
        private UiMotion continueMotion = new UiMotion();
        private UiMotion cancelMotion = new UiMotion();

        public override void Load()
        {
            RunState run = Game.CurrentRun;
            ev = run.CurrentEvent;
            if (ev == null) ev = EventList.Pick(run.Rng);

            stageLine = "UNKNOWN PATH   /   " + run.StageLabel;
            phase = Phase.Read;
            SoundBank.Play(Sfx.PageTurn);
            SoundBank.PlayMusic(Music.Event);

            //The Lens : the darkest room of the three, the painting's light carries it (DrawLights)
            Lens.Ambient = 0.3f;
            Lens.SpotPower = 0f;
            Lens.Vignette = 0.7f;
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
            for (int i = 0; i < choiceMotion.Length; i++)
                choiceMotion[i].Update(dt, phase == Phase.Read && i < ev.Choices.Length && Input.MouseOver(ChoiceRect(i)) && CanTake(ev.Choices[i]));
            continueMotion.Update(dt, phase == Phase.Result && Input.MouseOver(continueButton));
            cancelMotion.Update(dt, phase == Phase.Pick && Input.MouseOver(cancelButton));

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
            choiceMotion[index].Press();
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
                int before = run.Stamina;                                  // step 15 : say what really changed (full life gains nothing)
                run.ChangeStamina(amount);
                return NumberText.Signed(run.Stamina - before) + " " + BattleRules.LifeWord;
            }

            if (reward == Reward.Motif)
            {
                Motif[] found = MotifList.Roll(run.Motifs, run.Roster, 1, amount, run.Rng);       // GIFTS : one that fits the band
                if (found.Length == 0)
                {
                    run.AddShards(BattleRules.SkipMotifShards);
                    return "NOTHING NEW  /  +" + BattleRules.SkipMotifShards + " SHARDS";
                }
                gotMotif = found[0];
                run.AddMotif(gotMotif);
                SoundBank.Play(Sfx.MotifGet);
                return "GIFT  /  " + gotMotif.Name;
            }

            //Rehearse : a weapon goes up one tier, the one who took the check, or without a check the band's least trained
            if (reward == Reward.Rehearse)
            {
                Musician m = checker != null ? checker : run.LeastTrained();
                if (m == null || !run.CanRehearse(m))
                {
                    run.AddShards(BattleRules.SkipMotifShards);
                    return "ALREADY AT THEIR BEST  /  +" + BattleRules.SkipMotifShards + " SHARDS";
                }
                run.Rehearse(m);
                SoundBank.Play(Sfx.Rehearse);
                return m.Name + "  /  " + m.WeaponNow;
            }

            if (reward == Reward.Fight)
            {
                startFight = true;
                return "A FIGHT BEGINS";
            }

            return "NOTHING CHANGES";
        }

        //Go On : a fight, or simply the next stage
        private void GoOn(RunState run)
        {
            if (startFight)
            {
                //The elite takes the event's place on the route (and on the floor's map) and is saved like any fight,
                //so CONTINUE goes back into it (quitting cannot undo the choice)
                run.ReplaceChosen(NodeType.Elite);
                run.BeginBattle();
                SaveFile.SaveRun(run);
                Game.Screens.Change(RunFlow.FightScreen());
                return;
            }

            RunFlow.NextStage(Game);
        }

        //======================================================================================
        //The World (THE LENS) : a dark room, one cold light on the painting
        //======================================================================================

        public override bool HasWorld
        {
            get { return true; }
        }

        public override bool ShowsGrain
        {
            get { return false; }
        }

        //Falter : the cold light's strength this moment, mostly steady, now and then it dips twice like a bulb about to go
        private float Falter()
        {
            float beat = time % 4.3f;
            float dip = beat > 3.6f && beat < 3.75f ? 0.55f : (beat > 3.85f && beat < 3.92f ? 0.7f : 1f);
            return dip * (0.97f + 0.03f * (float)Math.Sin(time * 23f));
        }

        //World : the event painting, or an empty slot for it, in a double frame, on a dark floor.
        //ARTWORK : Content/Art/Events/event_<title>.png, see ArtBank.
        public override void DrawWorld(SpriteBatch sb)
        {
            float e = UiMotion.Between(time, 0f, 0.6f);
            Gfx.Rect(sb, 0, 0, TacetGame.ScreenW, TacetGame.ScreenH, Palette.StageDeep);
            Gfx.DrawGlowBox(sb, new Rectangle(-200, -260, TacetGame.ScreenW + 400, 820), Palette.Paper * 0.04f);

            Gfx.Rect(sb, artBox, Palette.CanvasDark * e);
            Rectangle picture = artBox;
            picture.Inflate(-10, -10);
            ArtBank.DrawOrSlot(sb, ArtBank.EventOf(ev), picture, Palette.Paper, e);
            Ornament.FadeUp(sb, new Rectangle(artBox.X, artBox.Bottom - 80, artBox.Width, 80), 0.9f * e);
            Ornament.DoubleFrame(sb, artBox, Palette.PaperDim * e);
        }

        //Lights : the cold light from above on the painting (it falters, see Falter), and a dim one where the words are
        public override void DrawLights(SpriteBatch sb)
        {
            float up = UiMotion.EaseInOut(MathHelper.Clamp(time / 0.9f, 0f, 1f)) * Falter();
            Gfx.DrawGlow(sb, artBox.Center.X, artBox.Y + 170f, 330f, Color.White * (0.66f * up));
            Gfx.DrawGlowBox(sb, new Rectangle(artBox.X + 60, -120, artBox.Width - 120, artBox.Bottom + 40), Color.White * (0.36f * up));
            Gfx.DrawGlow(sb, TextX + ChoiceW / 2f, 330f, 460f, Color.White * 0.22f);
        }

        //Glow : the cold light's narrow beam
        public override void DrawGlow(SpriteBatch sb)
        {
            float a = 0.26f * UiMotion.EaseInOut(MathHelper.Clamp(time / 0.9f, 0f, 1f)) * Falter();
            Gfx.DrawGlowBox(sb, new Rectangle(artBox.X + 110, -300, artBox.Width - 220, artBox.Bottom + 200), Color.White * a);
        }

        //======================================================================================
        //The Words (crisp on top)
        //======================================================================================

        public override void Draw(SpriteBatch sb)
        {
            RunState run = Game.CurrentRun;

            //Place : the painting's caption
            float e = UiMotion.Between(time, 0.2f, BattleRules.UiEnterTime);
            Gfx.TextSpaced(sb, Game.Font, ev.Place, artBox.X + 20, artBox.Bottom - 30, Palette.Paper * e, TextSize.Label, 3f);

            DrawText(sb, run);

            if (phase == Phase.Read) DrawChoices(sb, run);
            if (phase == Phase.Pick) DrawPick(sb, run);
            if (phase == Phase.Rolling) DrawRolling(sb);
            if (phase == Phase.Result) DrawResult(sb);

            RunHud.DrawTop(sb, run, "UNKNOWN PATH");
            RunHud.DrawTips(sb, run);
        }

        //Text : where we are, the title, and the story, coming in one after another
        private void DrawText(SpriteBatch sb, RunState run)
        {
            float a = UiMotion.Between(time, 0.1f, BattleRules.UiEnterTime);
            float b = UiMotion.Between(time, 0.1f + BattleRules.UiEnterStagger, BattleRules.UiEnterTime);
            float c = UiMotion.Between(time, 0.1f + BattleRules.UiEnterStagger * 3f, BattleRules.UiEnterTime);
            float slide = BattleRules.UiEnterSlide;
            Gfx.TextSpaced(sb, Game.Font, stageLine, TextX + (1f - a) * slide, 116, Palette.LineGrey * a, TextSize.Tiny, 3f);
            Gfx.Text(sb, Game.BigFont, ev.Title, TextX + (1f - b) * slide, 134, Palette.Highlight * b, TextSize.Title);
            Ornament.Rule(sb, TextX, 186, ChoiceW * b, Palette.LineGrey * b);
            Gfx.Text(sb, Game.StoryFont, ev.TextWrapped, TextX, 204 + (1f - c) * 10f, Palette.Paper * c, TextSize.Story);
        }

        //Choices : one slanted plate per choice. Pointed at, a white ink plate shoots out under it with a grey one behind
        //(like the menus), the words turn black and it steps forward. Its hint tag on the right, its key on the left.
        private void DrawChoices(SpriteBatch sb, RunState run)
        {
            float head = UiMotion.Between(time, 0.45f, BattleRules.UiEnterTime);
            Gfx.TextSpaced(sb, Game.Font, "WHAT DO YOU DO", TextX, ChoiceY - 24, Palette.PaperDim * head, TextSize.Tiny, 4f);

            for (int i = 0; i < ev.Choices.Length; i++)
            {
                EventChoice c = ev.Choices[i];
                float enter = UiMotion.Between(time, 0.5f + i * BattleRules.UiEnterStagger, BattleRules.UiEnterTime);
                if (enter <= 0f) continue;
                Rectangle r = ChoiceRect(i);
                bool can = CanTake(c);
                float a = (can ? 1f : 0.4f) * enter;
                float e = can ? choiceMotion[i].Shown : 0f;
                float s = choiceMotion[i].Scale;
                r.X += (int)(6f * e + (1f - enter) * BattleRules.UiEnterSlide * 1.5f);

                Gfx.SlantBox(sb, r, Ui.Slant, Palette.Panel * a);
                if (e < 1f) Gfx.SlantOutline(sb, r, Ui.Slant, Palette.LineGrey * ((1f - e) * a), 1f);
                if (e > 0f)
                {
                    float h = r.Height * s;
                    float y = r.Center.Y - h / 2f;
                    Ui.InkBar(sb, r.X + BattleRules.UiShadow, y + BattleRules.UiShadow, r.Width * e, h, Ui.Slant, Palette.LineGrey * (0.55f * a), i * 5 + 2, 1f);
                    Ui.InkBar(sb, r.X, y, r.Width * e, h, Ui.Slant, Palette.Paper * a, i * 5, 1f);
                }

                Color text = Color.Lerp(Palette.Paper, Palette.Ink, e);
                Gfx.Text(sb, Game.BigFont, numerals[i], r.X + 24, r.Y + 10, Color.Lerp(Palette.PaperDim, Palette.InkSoft, e) * a, TextSize.Small);
                Gfx.Text(sb, Game.StoryFont, c.Text, r.X + 70, r.Y + 15, text * a, TextSize.Story);

                if (c.HintLabel.Length > 0)
                {
                    float tw = Ui.TagWidth(c.HintLabel);
                    Ui.Tag(sb, c.HintLabel, r.Right - Ui.Slant - tw - 14, r.Y + 18, c.Check, a);
                }

                Ui.KeyChipRight(sb, NumberText.Get(i + 1), ChoiceRect(i).X - 8, r.Center.Y, Palette.LineGrey * a);
                if (choiceMotion[i].Flash > 0f) Gfx.DrawGlow(sb, r.Center.X, r.Center.Y, r.Width * 0.6f, Color.White * (0.5f * choiceMotion[i].Flash));
            }
        }

        //Pick : who takes the check. Every card shows that musician's chance.
        private void DrawPick(SpriteBatch sb, RunState run)
        {
            float slide = UiMotion.EaseOut(Math.Min(1f, phaseTimer * 4f));
            Rectangle panel = pickPanel;
            panel.Y += (int)((1f - slide) * 60f);

            Gfx.Rect(sb, panel, Palette.Void * (0.95f * slide));
            Gfx.Rect(sb, panel.X, panel.Y, panel.Width, 1, Palette.PaperDim * slide);
            Gfx.TextCentered(sb, Game.BigFont, "Who takes it on?", 640, panel.Y + 26, Palette.Paper * slide, TextSize.Subtitle);
            Gfx.TextSpacedCentered(sb, Game.Font, ev.Choices[chosen].HintLabel, 640, panel.Y + 50, Palette.PaperDim * slide, TextSize.Tiny, 3f);

            for (int i = 0; i < run.Roster.Count && i < cardChances.Length; i++)
            {
                Rectangle card = CardRect(i);
                float k = UiMotion.Between(phaseTimer, i * 0.03f, 0.25f);
                card.Y += (int)((1f - k) * 50f);
                Musician m = run.Roster[i];
                MusicianArt.Card(sb, card, m, run, i == hoverCard, false, "");

                //Chance Band : the percent in large type along the bottom of the card
                Rectangle band = new Rectangle(card.X, card.Bottom + 4, card.Width, 26);
                bool over = i == hoverCard;
                Gfx.Rect(sb, band, (over ? Palette.Paper : Palette.Stage) * k);
                Gfx.TextCentered(sb, Game.BigFont, NumberText.Get(cardChances[i]), band.Center.X - 8, band.Center.Y - 2,
                                 (over ? Palette.Ink : Palette.Highlight) * k, TextSize.Small);
                Gfx.Text(sb, Game.Font, "%", band.Center.X + 12, band.Y + 5, (over ? Palette.InkSoft : Palette.PaperDim) * k, TextSize.Label);
            }

            Ui.Command(sb, cancelButton, "BACK", "ESC", cancelMotion, slide, false);
        }

        //Rolling : a ring with ticks turning, and a flickering number that stops on the roll
        private void DrawRolling(SpriteBatch sb)
        {
            float cx = 900;
            float cy = 480;
            float t = phaseTimer / RollTime;
            float a = Math.Min(1f, phaseTimer * 6f);

            Gfx.Rect(sb, TextX - 20, 330, ChoiceW + 40, 300, Palette.Void * (0.6f * a));
            Ornament.TickRing(sb, cx, cy, 70f, 60, time * 3f * (1f - t * 0.8f), Math.Min(1f, phaseTimer * 3f), Palette.Paper * (0.7f * a));
            Gfx.CircleOutline(sb, cx, cy, 58, Palette.Paper * a, 2f);

            int shown = t < 0.85f ? Game.CurrentRun.Rng.Next(100) : roll;
            Gfx.TextCentered(sb, Game.LogoFont, NumberText.Get(shown), cx, cy - 6, Palette.Highlight * a, TextSize.Banner * 0.7f);
            Gfx.TextSpacedCentered(sb, Game.Font, "NEEDS UNDER", cx, cy + 90, Palette.LineGrey * a, TextSize.Tiny, 3f);
            Gfx.TextCentered(sb, Game.BigFont, NumberText.Get(chance), cx, cy + 118, Palette.Paper * a, TextSize.Subtitle);
        }

        //Result : the outcome text, the change it made, and the way on
        private void DrawResult(SpriteBatch sb)
        {
            float a = UiMotion.EaseOut(Math.Min(1f, phaseTimer * 3f));
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
            float line = UiMotion.Between(phaseTimer, 0.15f, BattleRules.UiEnterTime);
            Ornament.Rule(sb, TextX, y, ChoiceW * line, Palette.LineGrey * a);
            if (gotMotif != null) MotifArt.Badge(sb, gotMotif, TextX + 22, y + 44, 20, line);
            else Gfx.Diamond(sb, TextX + 22, y + 44, 8, Palette.Paper * line);
            Gfx.Text(sb, Game.BigFont, rewardLabel, TextX + 56 + (1f - line) * 14f, y + 26, Palette.Highlight * line, TextSize.Small);
            if (gotMotif != null)
                Gfx.Text(sb, Game.StoryFont, gotMotif.TipWrapped, TextX + 56, y + 62, Palette.PaperDim * line, TextSize.StorySmall);

            string label = startFight ? "FACE IT" : "CONTINUE";
            float button = UiMotion.Between(phaseTimer, 0.3f, BattleRules.UiEnterTime);
            Ui.Command(sb, continueButton, label, "ENTER", continueMotion, button, false);
        }
    }
}
