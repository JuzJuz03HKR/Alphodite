using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //======================================================================================
    //MARKS AND AIM (Beta1 30 Oct, block 1 item 4, docs/BETA1_DIRECTION.md 11.7 point 4). The battle says who a move is for and
    //what it does with the marks games use. The user (USER_BRIEFS 13.28) : at a friend a GUI on the friend, at an enemy the GUI
    //standard games use, "not lines". The lines that showed targets are gone.
    //   RETICLE      a ring on the enemy a move is aimed at : 4 ticks round it, a diamond in the middle, a ring on the floor under
    //                its feet, in the colour of the move's kind. Every enemy for a skill at all of them, 1 2 3 along a CHAKRAM's
    //                line. While a target is still to be picked, the ones that could be wait faint.
    //   FRIEND MARK  a double ring on a friend a skill is for (their card in THE BAND row, the chest of the one in front) with
    //                what they get : +6, +2, or the icon of SHIELD, THE LESSON, GUARD
    //   TARGET MARK  on the card of the musician an enemy's next attack lands on, kept while it is parried. The pointer on an
    //                enemy brings its own target forward.
    //   AIM ARC      while an enemy is picked, dots fly from the command to its reticle (CZN's aiming arrow made of the game's
    //                diamonds, ending on the ring). BattleRules.AimArc turns it off.
    //   SKILL CARD   over the command under the pointer : its name and cost, its kind, who it is for, what it does in numbers
    //   LIFE PREVIEW the part of a life bar the move would take off (or give back) blinks, the number by the reticle
    //   ICONS        statuses and intents as icons with their count, art slots for the team (docs/UI_ART_PLAN.md 2.5) with a
    //                short code until the art comes. The pointer on one says what it is (X = Y).
    //The numbers come from the rules (TurnBattle.Preview), the words are made in Update when the move under the pointer
    //changes, Draw only draws.
    //======================================================================================
    public partial class BattleScreen
    {
        //Icons : the statuses and the intents over TACET's heads. Code shows until the art exists (Content/Art/Ui/Icons/<File>.png),
        //Name and Line are what the pointer on it shows.
        private enum Ico { Dissonance, Crescendo, Mute, Guard, Echo, Shield, Lesson, Block, OffBeat, CutIn, Hit, Charge, Raise, Rest, Broken, Court }
        private static readonly string[] IcoCode = { "DIS", "CRE", "MUT", "GRD", "ECH", "SHD", "LES", "GRD", "OFF", "CUT", "HIT", "CHG", "GRD", "RST", "BRK", "ALL" };
        private static readonly string[] IcoFile =
        {
            "status_dissonance", "status_crescendo", "status_mute", "status_guard", "status_echo", "status_shield", "status_lesson",
            "status_block", "status_off_beat", "status_cut_in", "intent_attack", "intent_charge", "intent_guard", "intent_rest",
            "intent_broken", "intent_everyone",
        };
        private static readonly string[] IcoName = { "DISSONANCE", "CRESCENDO", "MUTE", "GUARD", "ECHO", "SHIELD", "LESSON", "GUARD", "OFF-BEAT", "CUT IN", "ATTACK", "CHARGE", "GUARD", "REST", "BROKEN", "EVERYONE" };
        private static readonly string[] IcoLine =
        {
            "= loses this much life at the end of its turn, then 1 less",
            "= every stack hits harder. A MISS loses them all",
            "= no skills for this many turns",
            "= takes half of every hit until their next turn",
            "= their next move plays again",
            "= DUPORT stands in front and takes their hits",
            "= their next skill : wider PERFECT, harder hits",
            "= takes the band's damage before its life. IL CANNONE goes through",
            "= it loses its next turn and takes more damage",
            "= it plays in the middle of the band's part of the bar",
            "= its notes hit the marked musician. Swipe back to block them",
            "= next bar one big note. OFF-BEAT or TIMPANI ROLL breaks it",
            "= it raises this much GUARD and plays no note",
            "= it hums this turn and heals instead of attacking",
            "= its CHARGE was broken, the blow will not come",
            "= its notes hit every musician standing",
        };
        private static Texture2D[] icoArt = new Texture2D[0];

        private const int IconSize = 22;           // a status icon
        private const int IconStep = 25;
        private const int IntentIcon = 24;         // the icon of an enemy's intent
        private const float IntentNumberRoom = 42f;
        private const float IntentNoteStep = 28f;

        //Mark : one icon on screen, laid out in Update (Build Marks) and drawn as it is
        private struct IconMark
        {
            public Fighter Who;
            public Ico Id;
            public int Count;            // the number on its corner (GUARD : the BLOCK in the middle), 0 for none
            public Rectangle Box;
            public bool Filled;          // it hurts the one who has it
            public bool OnStage;         // on TACET, drawn through the battle's camera, else with THE BAND row
            public Color Frame;
            public float Alpha;
        }
        private IconMark[] marks = new IconMark[64];
        private int markCount;
        private int markHover = -1;      // the icon under the pointer (its line shows), -1 for none
        private Rectangle tipBox;

        //Aim : the move under the pointer and what it would do. Worked out again only when it changes.
        private readonly MovePreview aim = new MovePreview();       // the marks' move : its targets, the life they lose or get back
        private readonly MovePreview cardAim = new MovePreview();   // the card's numbers (with a stand in while no target is picked)
        private readonly List<Fighter> aimCanPick = new List<Fighter>();
        private bool aimOn;             // a move is aimed, its marks show
        private bool aimFull;           //   its targets are known : full marks and numbers. Else the ones that could be picked, faint
        private bool aimPlaying;        //   it is being swiped now (the marks stay, the numbers go : the life drops for real)
        private bool aimAttack;
        private Skill aimSkill;
        private Fighter aimWho;
        private Fighter aimAt;
        private int aimCommand = -1;    //   the command it is (0 ATTACK, 1 or 2 a skill), -1 when none shows its card
        private int aimRound = -1;
        private float aimTime;          //   seconds since it changed : the reticle closes in
        private Color aimColor;
        private int hoverCommand = -1;  // the command under the pointer in the plan

        //Skill Card : the words, made when the move changes
        private string cardName = "";
        private string cardKind = "";
        private string cardFor = "";
        private string cardLine = "";
        private int cardCost;
        private Rectangle cardBox;

        //Command Icons : each command's icon and the word for who it is for, made in Select
        private readonly Texture2D[] commandArt = new Texture2D[3];
        private readonly string[] commandCode = { "AT", "", "" };
        private readonly string[] commandFor = { "", "", "" };
        private const float CommandIconRoom = 30f;

        //Load Marks : the icons the team has drawn so far (UiArt reads the folder when the game starts)
        private void LoadMarks()
        {
            if (icoArt.Length == IcoFile.Length) return;
            icoArt = new Texture2D[IcoFile.Length];
            for (int i = 0; i < IcoFile.Length; i++) icoArt[i] = UiArt.IconOf(IcoFile[i]);
        }

        //Prepare Commands : the icon, the code and the target word of the selected musician's three commands (Select)
        private void PrepareCommands()
        {
            commandArt[0] = UiArt.IconOf("skill_attack");
            commandFor[0] = "ONE ENEMY";
            for (int k = 1; k < 3; k++)
            {
                Skill s = commandSkill[k];
                commandArt[k] = s != null ? UiArt.IconOf("skill_" + ArtBank.Key(s.Name)) : null;
                commandCode[k] = s != null ? Initials(s.Name) : "";
                commandFor[k] = s != null ? TargetWord(s) : "";
            }
        }

        //Initials : a skill's code until its icon is drawn, the first letters of its first two words ("THE" left out) or
        //the first two letters of a one word name : CAPRICE CA, IL CANNONE IC, THE LESSON LE
        private static string Initials(string name)
        {
            string[] words = name.Replace("THE ", "").Split(' ');
            if (words.Length >= 2 && words[1].Length > 0) return words[0].Substring(0, 1) + words[1].Substring(0, 1);
            return words[0].Length >= 2 ? words[0].Substring(0, 2) : words[0];
        }

        //Target Word : who a skill is for, the label over its command
        private static string TargetWord(Skill s)
        {
            switch (s.Target)
            {
                case SkillTarget.AllEnemies: return "ALL ENEMIES";
                case SkillTarget.Self: return "SELF";
                case SkillTarget.Ally: return "ONE ALLY";
                case SkillTarget.AllAllies: return "WHOLE BAND";
                default: return s.Bounce > 1 ? "UP TO " + s.Bounce + " ENEMIES" : "ONE ENEMY";
            }
        }

        private static string KindWord(SkillKind kind)
        {
            switch (kind)
            {
                case SkillKind.Defend: return "DEFEND";
                case SkillKind.Heal: return "HEAL";
                case SkillKind.Boost: return "BOOST";
                default: return "ATTACK";
            }
        }

        //======================================================================================
        //Update Marks : after the page's own update, every frame
        //======================================================================================

        private void UpdateMarks(float dt)
        {
            UpdateAim(dt);
            UpdateFriendRow(dt);                                                         // FRIEND PICK (BattleScreen.Friends.cs)
            BuildMarks();

            //The pointer on an icon : its line, only while the bar is written (nothing to read while it plays)
            int over = -1;
            if ((view == View.Plan || view == View.Target) && stepFor == null)
                for (int i = 0; i < markCount; i++)
                    if (marks[i].Alpha > 0.3f && Input.MouseOver(marks[i].Box)) over = i;
            if (over != markHover && over >= 0)
            {
                IconMark m = marks[over];
                float w = Gfx.SpacedWidth(Game.Font, IcoName[(int)m.Id], TextSize.Label, 2f) + 10f + Gfx.TextWidth(Game.Font, IcoLine[(int)m.Id], TextSize.Label);
                int x = Math.Max(8, Math.Min(TacetGame.ScreenW - 8 - (int)w - 20, m.Box.X - 6));
                int y = m.Box.Bottom + 6;
                if (y + 26 > PanelTop - 4) y = m.Box.Y - 32;
                tipBox = new Rectangle(x, y, (int)w + 20, 26);
            }
            markHover = over;
        }

        //Update Aim : which move is under the pointer. In the plan, a command or an enemy (a click there is the ATTACK), while
        //picking, the move waiting and the target under the pointer, while a skill is swiped, that skill.
        private void UpdateAim(float dt)
        {
            bool on = false;
            bool attack = false;
            Skill skill = null;
            Fighter who = selected;
            Fighter at = null;
            int command = -1;
            bool playing = false;

            if (view == View.Plan && stepFor == null && who != null && battle.CanPlan(who))
            {
                if (hoverCommand >= 0)
                {
                    on = true;
                    command = hoverCommand;
                    attack = command == 0;
                    skill = attack ? null : commandSkill[command];
                    at = OnlyTarget(skill);
                }
                else if (hover != null && hover.IsEnemy && battle.CanTarget(hover))
                {
                    on = true;
                    attack = true;
                    at = hover;
                }
            }
            else if (view == View.Target && who != null)
            {
                on = true;
                attack = pending == null;
                skill = pending;
                command = attack ? 0 : (pending == commandSkill[1] ? 1 : 2);
                at = hover ?? OnlyTarget(skill);
            }
            else if (view == View.Swipe && swipeFor == SwipeFor.Skill && battle.CurrentMove != null && battle.CurrentMove.Skill != null)
            {
                on = true;
                playing = true;
                who = battle.Current;
                skill = battle.CurrentMove.Skill;
                at = battle.CurrentMove.Target;
            }
            if (on && skill == null && !attack) on = false;

            bool full = on && (at != null || (skill != null && skill.Target != SkillTarget.Enemy && skill.Target != SkillTarget.Ally));
            bool changed = on != aimOn || full != aimFull || playing != aimPlaying || attack != aimAttack || skill != aimSkill || who != aimWho
                           || at != aimAt || command != aimCommand || battle.Round != aimRound;
            aimTime += dt;
            if (!changed) return;

            if (at != aimAt || skill != aimSkill || on != aimOn) aimTime = 0f;           // a new target : the reticle closes in again
            aimOn = on;
            aimFull = full;
            aimPlaying = playing;
            aimAttack = attack;
            aimSkill = skill;
            aimWho = who;
            aimAt = at;
            aimCommand = command;
            aimRound = battle.Round;
            aimColor = Palette.Kind(skill == null ? SkillKind.Attack : skill.Kind);
            aimCanPick.Clear();
            if (!on) return;

            if (full) battle.Preview(who, skill, at, aim);
            else
            {
                //Still to pick : every enemy (or friend) it could go to waits faint
                bool forAlly = skill != null && skill.Target == SkillTarget.Ally;
                if (forAlly)
                {
                    for (int i = 0; i < battle.Party.Count; i++)
                        if (battle.CanAim(who, skill, battle.Party[i])) aimCanPick.Add(battle.Party[i]);
                }
                else
                {
                    for (int i = 0; i < battle.Enemies.Count; i++)
                        if (battle.CanTarget(battle.Enemies[i])) aimCanPick.Add(battle.Enemies[i]);
                }
            }
            if (command >= 0) PrepareCard(who, skill, full ? at : StandIn(skill), command);
        }

        //Only Target : the one target a move goes to without being picked (the only enemy standing), null when it must be picked
        private Fighter OnlyTarget(Skill skill)
        {
            bool atEnemy = skill == null || skill.Target == SkillTarget.Enemy;
            if (atEnemy && StandingEnemies() == 1) return FirstStanding();
            return null;
        }

        //Stand In : for the card's numbers while no target is picked, the first one it could go to
        private Fighter StandIn(Skill skill)
        {
            if (skill != null && skill.Target == SkillTarget.Ally)
                for (int i = 0; i < battle.Party.Count; i++)
                    if (battle.CanAim(selected, skill, battle.Party[i])) return battle.Party[i];
            return FirstStanding();
        }

        //Prepare Card : the skill card's words. Damage is the first target's, every note GOOD and every hold held (PREVIEW).
        private void PrepareCard(Fighter who, Skill s, Fighter at, int command)
        {
            battle.Preview(who, s, at, cardAim);
            cardName = s == null ? WordAttack : s.Name;
            cardKind = KindWord(s == null ? SkillKind.Attack : s.Kind);
            cardFor = s == null ? "ONE ENEMY" : TargetWord(s);
            cardCost = s == null ? 0 : s.Cost;

            string line = "";
            if (s == null)
            {
                line = Join(line, cardAim.PerNote + " DAMAGE");
                line = Join(line, "+" + cardAim.Breath + " BREATH");
            }
            else
            {
                if (s.Damage > 0f)
                {
                    int notes = Math.Max(1, s.Notes.Length);
                    if (s.Bounce > 0) line = Join(line, notes + " THROWS  " + cardAim.PerNote + " EACH");
                    else if (notes > 1) line = Join(line, cardAim.PerNote * notes + " DAMAGE  (" + notes + " x " + cardAim.PerNote + ")");
                    else line = Join(line, cardAim.PerNote + " DAMAGE" + (s.Notes[0].Kind == NoteKind.Hold ? " HELD" : ""));
                    if (s.Target == SkillTarget.AllEnemies) line += " EACH";
                }
                if (cardAim.Heal > 0) line = Join(line, "+" + cardAim.Heal + " LIFE" + (s.Target == SkillTarget.AllAllies ? " EACH" : ""));
                if (cardAim.Crescendo > 0) line = Join(line, "+" + cardAim.Crescendo + " CRESCENDO" + (s.Target == SkillTarget.AllAllies ? " EACH" : ""));
                if (cardAim.Dissonance > 0) line = Join(line, "+" + cardAim.Dissonance + " DISSONANCE");
                if (cardAim.OffBeat > 0) line = Join(line, "+" + cardAim.OffBeat + " OFF-BEAT");
                if (cardAim.Breath > 0) line = Join(line, "+" + cardAim.Breath + " BREATH");
                if (s.Pierce) line = Join(line, "THROUGH GUARD");
                if (s.Breaks) line = Join(line, "BREAKS CHARGE");
                if (s.Revenge > 0f) line = Join(line, (who != null && who.Wounded ? "x" : "AFTER A HIT x") + Share(s.Revenge) + (who != null && who.Wounded ? " NOW" : ""));
                if (s.Taunt) line = Join(line, "ENEMIES AIM HERE");
                if (s.Guard) line = Join(line, "HITS x" + Share(cardAim.Share));
                if (s.Shield) line = Join(line, "THEIR HITS ON " + (who != null ? who.Name : "DUPORT") + " x" + Share(cardAim.Share));
                if (s.Lesson) line = Join(line, "NEXT SKILL x" + Share(cardAim.Share) + "  WIDER PERFECT");

                //Paid in life : THE DEVIL'S BARGAIN short of BREATH (as it stands while the bar is written), THE PACT every time
                int life = 0;
                if (who != null && battle.PaysInLife(who) && battle.BreathFree < s.Cost)
                    life += (s.Cost - Math.Max(0, battle.BreathFree)) * BattleRules.TurnPaganiniLife;
                if (who != null && who.Musician.Has(PathRule.PaidInBlood)) life += Math.Max(0, Math.Min(BattleRules.TurnPactLife, who.Hp - 1));
                if (life > 0) line = Join(line, "PAYS " + life + " LIFE");
            }
            cardLine = line;

            float w = Math.Max(Gfx.TextWidth(Game.BigFont, cardName, TextSize.Small) + 90f, Gfx.TextWidth(Game.Font, cardLine, TextSize.Label));
            w = Math.Max(w, Gfx.SpacedWidth(Game.Font, cardKind, TextSize.Tiny, 2f) + Gfx.SpacedWidth(Game.Font, cardFor, TextSize.Tiny, 2f) + 24f);
            Rectangle box = commandBox[command];
            int width = (int)Math.Max(230f, w + 30f);
            int x = Math.Min(box.X, TacetGame.ScreenW - 8 - width);
            cardBox = new Rectangle(x, box.Y - 18 - 72, width, 72);
        }

        private static string Join(string line, string part)
        {
            return line.Length == 0 ? part : line + "   " + part;
        }

        private static string Share(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        //Build Marks : every icon's place this frame. THE BAND's under their cards, TACET's under its plate (GUARD's BLOCK left
        //of it) and its intent over it.
        private void BuildMarks()
        {
            markCount = 0;
            if (battle == null) return;
            for (int i = 0; i < battle.Party.Count; i++)
            {
                Fighter f = battle.Party[i];
                Rectangle r = ChipBox(i);
                float a = f.Down ? 0.4f : 1f;
                float x = r.X;
                float y = r.Bottom + 5;
                if (f.Dissonance > 0) x = AddMark(f, Ico.Dissonance, f.Dissonance, true, Palette.Paper, false, x, y, a);
                if (f.Crescendo > 0) x = AddMark(f, Ico.Crescendo, f.Crescendo, false, Palette.KindBoost, false, x, y, a);
                if (f.Mute > 0) x = AddMark(f, Ico.Mute, f.Mute, true, Palette.Paper, false, x, y, a);
                if (f.Guard) x = AddMark(f, Ico.Guard, 0, false, Palette.KindDefend, false, x, y, a);
                if (f.Echo > 0f) x = AddMark(f, Ico.Echo, f.EchoLeft > 1 ? f.EchoLeft : 0, false, Palette.KindBoost, false, x, y, a);   // FUGUE
                if (f.CoveredBy != null) x = AddMark(f, Ico.Shield, 0, false, Palette.KindDefend, false, x, y, a);   // SHIELD
                if (f.Lesson) x = AddMark(f, Ico.Lesson, 0, false, Palette.KindBoost, false, x, y, a);               // THE LESSON
            }

            float ia = 1f - FocusEased;                                       // the intent fades out as a ring comes up (round 4)
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                Fighter f = battle.Enemies[i];
                if (f.Down) continue;
                Rectangle plate = EnemyPlate(BoxOf(f));
                float a = PlateAlpha(f);
                if (f.Block > 0) Add(f, Ico.Block, f.Block, BlockBox(plate), false, Palette.Paper, true, a);   // GUARD : the BLOCK left
                float x = plate.X;
                float y = plate.Bottom + 3;
                if (TurnBattle.CutsIn(f)) x = AddMark(f, Ico.CutIn, 0, false, Palette.PaperDim, true, x, y, a);
                if (f.Staggered) x = AddMark(f, Ico.OffBeat, 0, true, Palette.Paper, true, x, y, a);
                if (f.Dissonance > 0) x = AddMark(f, Ico.Dissonance, f.Dissonance, true, Palette.Paper, true, x, y, a);

                Ico intent;
                if (view != View.Over && ia > 0.01f && IntentOf(f, out intent))
                {
                    float left, cy;
                    IntentRow(f, out left, out cy);
                    Add(f, intent, 0, new Rectangle((int)left, (int)(cy - IntentIcon / 2f), IntentIcon, IntentIcon), false, Palette.Paper, true, ia);
                }
            }
        }

        private float AddMark(Fighter f, Ico id, int count, bool filled, Color frame, bool onStage, float x, float y, float alpha)
        {
            Add(f, id, count, new Rectangle((int)x, (int)y, IconSize, IconSize), filled, frame, onStage, alpha);
            return x + IconStep;
        }

        private void Add(Fighter f, Ico id, int count, Rectangle box, bool filled, Color frame, bool onStage, float alpha)
        {
            if (markCount >= marks.Length) Array.Resize(ref marks, marks.Length * 2);
            marks[markCount].Who = f;
            marks[markCount].Id = id;
            marks[markCount].Count = count;
            marks[markCount].Box = box;
            marks[markCount].Filled = filled;
            marks[markCount].Frame = frame;
            marks[markCount].OnStage = onStage;
            marks[markCount].Alpha = alpha;
            markCount++;
        }

        //Block Box : GUARD's BLOCK, an icon left of the plate with the number in it (CZN keeps it by the life bar)
        private static Rectangle BlockBox(Rectangle plate)
        {
            return new Rectangle(plate.X - 30, plate.Y + plate.Height / 2 - 13, 26, 26);
        }

        //Plate Alpha : how bright an enemy's plate is. SILENCED : it goes with the enemy. QTE FOCUS : only the one attacking
        //(or being hit) stays bright.
        private float PlateAlpha(Fighter f)
        {
            float a = f.Down ? 0.35f * (1f - GoneOf(f)) : 1f;
            bool lit = battle.Current == f || (swipeFor == SwipeFor.Skill && SkillAims(f));   // ACTION : the band's target stays lit
            if (!lit) a *= MathHelper.Lerp(1f, 0.35f, FocusEased);
            return a;
        }

        //Intent Of : the icon of an enemy's next move (false : it has none to show)
        private static bool IntentOf(Fighter f, out Ico id)
        {
            id = Ico.Hit;
            if (f.IntentRest) id = Ico.Rest;                                    // LULLABY
            else if (f.ChargeBroken) id = Ico.Broken;                           // BREAK
            else if (f.IntentCharge) id = Ico.Charge;                           // CHARGE
            else if (f.IntentGuard > 0) id = Ico.Raise;                         // GUARD
            else if (f.IntentNotes.Length > 0) id = f.IntentAll ? Ico.Court : Ico.Hit;   // THE COURT : everyone
            else return false;
            return true;
        }

        //Plays Notes : the enemy's next move is an attack whose notes show after its number
        private static bool PlaysNotes(Fighter f)
        {
            return !f.IntentRest && !f.ChargeBroken && !f.IntentCharge && f.IntentGuard <= 0 && f.IntentNotes.Length > 0;
        }

        //Intent Row : where an enemy's intent sits over its plate, the icon at left, then the number, then the notes.
        //cy is the middle of the row. CLEAR FIGHT : the whole attack a little higher when a note's arrow points down.
        private void IntentRow(Fighter f, out float left, out float cy)
        {
            Rectangle r = EnemyPlate(BoxOf(f));
            cy = r.Y - 22;
            int notes = PlaysNotes(f) ? f.IntentNotes.Length : 0;
            bool number = notes > 0 || f.IntentCharge || f.IntentGuard > 0;
            float width = IntentIcon + (number ? 6f + IntentNumberRoom : 0f) + notes * IntentNoteStep;
            left = r.Center.X - width / 2f;
            if (!Settings.ClearFight || notes == 0) return;
            for (int n = 0; n < f.IntentNotes.Length; n++)
                if (f.IntentWays[n] == Flick.Down) { cy -= 14f; return; }
        }

        //======================================================================================
        //Draw Marks
        //======================================================================================

        //Draw Icons : the icons of one side, onStage for TACET's (inside the camera batch), else THE BAND's
        private void DrawIcons(SpriteBatch sb, bool onStage)
        {
            for (int i = 0; i < markCount; i++)
            {
                IconMark m = marks[i];
                if (m.OnStage != onStage || m.Alpha <= 0.01f) continue;
                bool over = i == markHover;
                Rectangle r = m.Box;
                if (m.Id == Ico.Block)
                {
                    //GUARD : the BLOCK is the number itself, big in a doubled frame (on the art when it comes)
                    Texture2D art = icoArt.Length > (int)m.Id ? icoArt[(int)m.Id] : null;
                    if (art != null) sb.Draw(art, new Vector2(r.Center.X - art.Width / 2, r.Center.Y - art.Height / 2), Color.White * m.Alpha);
                    else
                    {
                        Gfx.Rect(sb, r, Palette.Void * (0.85f * m.Alpha));
                        Gfx.RectOutline(sb, r, Palette.Paper * m.Alpha, 1);
                        Gfx.RectOutline(sb, new Rectangle(r.X + 3, r.Y + 3, r.Width - 6, r.Height - 6), Palette.LineGrey * m.Alpha, 1);
                    }
                    Gfx.TextCentered(sb, Game.Font, NumberText.Get(m.Count), r.Center.X, r.Center.Y - 1, Palette.Highlight * m.Alpha, TextSize.Body);
                }
                else Ui.Icon(sb, r, icoArt.Length > (int)m.Id ? icoArt[(int)m.Id] : null, IcoCode[(int)m.Id], m.Frame, m.Filled, m.Count, m.Alpha);
                if (over) Gfx.RectOutline(sb, new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), Palette.Highlight, 1);
            }
        }

        //Draw Tip : the icon under the pointer, its name and what it does on one line
        private void DrawTip(SpriteBatch sb)
        {
            if (markHover < 0 || markHover >= markCount) return;
            IconMark m = marks[markHover];
            Gfx.Rect(sb, new Rectangle(tipBox.X + 3, tipBox.Y + 3, tipBox.Width, tipBox.Height), Color.Black * 0.5f);
            Gfx.Rect(sb, tipBox, Palette.Void * 0.95f);
            Gfx.RectOutline(sb, tipBox, Palette.LineGrey, 1);
            string name = IcoName[(int)m.Id];
            Gfx.TextSpaced(sb, Game.Font, name, tipBox.X + 10, tipBox.Y + 5, Palette.Highlight, TextSize.Label, 2f);
            Gfx.Text(sb, Game.Font, IcoLine[(int)m.Id], tipBox.X + 20 + Gfx.SpacedWidth(Game.Font, name, TextSize.Label, 2f), tipBox.Y + 5, Palette.PaperDim, TextSize.Label);
        }

        //Draw Reticles : on TACET (inside the camera batch). The move's targets full, the ones that could be picked faint.
        private void DrawReticles(SpriteBatch sb)
        {
            if (!aimOn || stepFor != null) return;
            float lockIn = Smooth(Math.Min(1f, aimTime / BattleRules.AimLockTime));
            float a = aimPlaying ? 0.7f : 1f;
            if (aimFull)
            {
                bool numbered = aimSkill != null && aimSkill.Bounce > 0 && aim.Targets.Count > 1;   // CHAKRAM : the order it throws in
                for (int i = 0; i < aim.Targets.Count; i++)
                {
                    Fighter f = aim.Targets[i];
                    if (!f.IsEnemy || f.Down) continue;
                    int loss = aimPlaying || f.Slot >= aim.Loss.Length ? 0 : aim.Loss[f.Slot];
                    DrawReticle(sb, f, a, lockIn, numbered ? i + 1 : 0, loss);
                }
                return;
            }
            for (int i = 0; i < aimCanPick.Count; i++)
                if (aimCanPick[i].IsEnemy && !aimCanPick[i].Down) DrawReticle(sb, aimCanPick[i], BattleRules.AimFaint, 1f, 0, 0);
        }

        //Reticle Of : where an enemy's ring goes and how big it is (the middle of its body, a fifth of its frame)
        private Vector2 ReticleOf(Fighter f, out float radius)
        {
            Rectangle r = DrawBox(f);
            radius = Math.Min(r.Width, r.Height) * 0.2f;
            return new Vector2(r.Center.X, r.Y + r.Height * 0.45f);
        }

        //Reticle : the ring, the ticks and the diamond, closing in from wider and turned as lockIn goes 0 to 1, the ring on the
        //floor under its feet, its place in a CHAKRAM's line, the life it would lose
        private void DrawReticle(SpriteBatch sb, Fighter f, float alpha, float lockIn, int number, int loss)
        {
            float radius;
            Vector2 c = ReticleOf(f, out radius);
            Rectangle box = DrawBox(f);
            Color ink = aimColor * alpha;
            float ring = radius * (1f + 0.4f * (1f - lockIn));
            float turn = (1f - lockIn) * MathHelper.PiOver4 + (float)Math.Sin(time * 2f) * 0.05f;

            Gfx.EllipseOutline(sb, box.Center.X, box.Bottom - 6, box.Width * 0.3f, box.Width * 0.075f, ink * 0.7f, 1.5f);
            Gfx.Ring(sb, c.X, c.Y, ring, ink, 2f);
            for (int k = 0; k < 4; k++)
            {
                float angle = turn + k * MathHelper.PiOver2;
                Vector2 way = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Gfx.Line(sb, c + way * (ring + 4f), c + way * (ring + 13f), ink, 3f);
            }
            Gfx.Diamond(sb, c.X, c.Y, 4f, ink);

            if (number > 0)
            {
                float nx = c.X + ring * 0.78f + 8f;
                float ny = c.Y - ring * 0.78f - 8f;
                Gfx.Diamond(sb, nx, ny, 11f, ink);
                Gfx.TextCentered(sb, Game.Font, NumberText.Get(number), nx, ny - 1f, Palette.Ink * alpha, TextSize.Label);
            }
            if (loss > 0)
            {
                string text = NumberText.Signed(-loss);
                float lx = c.X + ring + 18f;
                Gfx.Text(sb, Game.Font, text, lx + 1f, c.Y - 9f, Color.Black * (0.7f * alpha), TextSize.Heading);
                Gfx.Text(sb, Game.Font, text, lx, c.Y - 10f, Color.Lerp(aimColor, Color.White, 0.35f) * alpha, TextSize.Heading);
            }
        }

        //Life Preview : on a life bar, the part the move would take off (loss) or give back (gain) blinks in its kind's colour
        private void DrawLifePreview(SpriteBatch sb, Rectangle bar, Fighter f, int loss, int gain, float alpha)
        {
            if (f.MaxHp <= 0 || (loss <= 0 && gain <= 0)) return;
            float pulse = 0.55f + 0.45f * (float)Math.Sin(time * 7f);
            float now = Math.Max(0, f.Hp) / (float)f.MaxHp;
            if (loss > 0)
            {
                //The part that goes : the bar's dark under it, the kind's colour blinking on top
                float after = Math.Max(0, f.Hp - loss) / (float)f.MaxHp;
                Rectangle part = new Rectangle((int)(bar.X + bar.Width * after), bar.Y, Math.Max(1, (int)Math.Round(bar.Width * (now - after))), bar.Height);
                Gfx.Rect(sb, part, Palette.Stage * alpha);
                Gfx.Rect(sb, part, aimColor * ((0.45f + 0.55f * pulse) * alpha));
            }
            if (gain > 0)
            {
                float after = Math.Min(f.MaxHp, f.Hp + gain) / (float)f.MaxHp;
                Gfx.Rect(sb, bar.X + bar.Width * now, bar.Y, bar.Width * (after - now), bar.Height, aimColor * ((0.45f + 0.55f * pulse) * alpha));
            }
        }

        //Loss Of / Gain Of : what the aimed move would do to this fighter's life (0 when it is not aimed at them)
        private int LossOf(Fighter f)
        {
            if (!aimOn || !aimFull || aimPlaying || !f.IsEnemy || f.Slot >= aim.Loss.Length) return 0;
            return aim.Loss[f.Slot];
        }

        private int GainOf(Fighter f)
        {
            if (!aimOn || !aimFull || aimPlaying || f.IsEnemy || f.Slot >= aim.Gain.Length) return 0;
            return aim.Gain[f.Slot];
        }

        //Friend Aimed : a friend is a target of the aimed move (full), or could be picked (faint)
        private bool FriendAimed(Fighter f, out bool full)
        {
            full = false;
            if (!aimOn || f.IsEnemy || f.Down || stepFor != null) return false;
            if (aimFull)
            {
                full = aim.Targets.Contains(f);
                return full;
            }
            return aimCanPick.Contains(f);
        }

        //Friend Mark : a double ring with a diamond in the middle, in the move's kind colour, and what the friend gets beside it
        //(the life back, CRESCENDO) or the icon of what it does (SHIELD, THE LESSON, GUARD)
        private void DrawFriendMark(SpriteBatch sb, float cx, float cy, float radius, bool full, bool big)
        {
            float lockIn = full ? Smooth(Math.Min(1f, aimTime / BattleRules.AimLockTime)) : 1f;
            float a = full ? (aimPlaying ? 0.7f : 1f) : BattleRules.AimFaint;
            Color ink = aimColor * a;
            float ring = radius * (1f + 0.4f * (1f - lockIn));
            Gfx.Ring(sb, cx, cy, ring, ink, 2f);
            Gfx.Ring(sb, cx, cy, ring * 0.62f, ink * 0.7f, 1.5f);
            Gfx.Diamond(sb, cx, cy, big ? 5f : 3.5f, ink);
            if (!full || aimPlaying || aimSkill == null) return;

            //What they get, beside the ring
            float tx = cx + ring + 6f;
            float size = big ? TextSize.Heading : TextSize.Label;
            float ty = cy - Game.Font.LineSpacing * size / 2f;
            string value = null;
            if (aim.Heal > 0) value = NumberText.Signed(aim.Heal);
            else if (aim.Crescendo > 0) value = NumberText.Signed(aim.Crescendo);
            if (value != null)
            {
                Gfx.Text(sb, Game.Font, value, tx + 1f, ty + 1f, Color.Black * 0.7f, size);
                Gfx.Text(sb, Game.Font, value, tx, ty, Color.Lerp(aimColor, Color.White, 0.3f), size);
                return;
            }
            Ico id = aimSkill.Shield ? Ico.Shield : (aimSkill.Lesson ? Ico.Lesson : Ico.Guard);
            if (!aimSkill.Shield && !aimSkill.Lesson && !aimSkill.Guard) return;
            int s = big ? 24 : 18;
            Ui.Icon(sb, new Rectangle((int)tx, (int)(cy - s / 2f), s, s), icoArt.Length > (int)id ? icoArt[(int)id] : null, IcoCode[(int)id], aimColor, false, 0, 1f);
        }

        //Fore Marks : on the musician in front. A friend mark on their chest (HSR) when a skill is for them, and the target
        //mark over their head while an enemy attacks them (the parry) or the pointer is on an enemy that aims at them.
        private void DrawForeMarks(SpriteBatch sb)
        {
            if (fore == null || fore.Down || view == View.Over) return;
            Rectangle r = DrawBox(fore);
            bool full;
            if (FriendAimed(fore, out full)) DrawFriendMark(sb, r.Center.X + 40f, r.Y + 130f, 24f, full, true);

            Fighter enemy = null;
            if (view == View.Swipe && swipeFor == SwipeFor.Parry && battle.Current != null && battle.Current.IsEnemy) enemy = battle.Current;
            else if ((view == View.Plan || view == View.Target) && hover != null && hover.IsEnemy) enemy = hover;
            if (enemy == null || !TurnBattle.Aims(enemy) || (battle.HitTarget(enemy) != fore && !enemy.IntentAll)) return;
            float pulse = 0.65f + 0.35f * (float)Math.Sin(time * 5f);
            DrawTargetSign(sb, r.Center.X, r.Y - 18f + (float)Math.Sin(time * 3f) * 2f, 9f, Palette.Accent * pulse);
        }

        //Target Sign : TACET's mark on the one it will hit, a small ring with four ticks and a dot (a reticle of its own,
        //white like everything of TACET's)
        private static void DrawTargetSign(SpriteBatch sb, float cx, float cy, float radius, Color ink)
        {
            Gfx.Ring(sb, cx, cy, radius, ink, 2f);
            Gfx.Rect(sb, cx - 1f, cy - radius - 4f, 2f, 5f, ink);
            Gfx.Rect(sb, cx - 1f, cy + radius - 1f, 2f, 5f, ink);
            Gfx.Rect(sb, cx - radius - 4f, cy - 1f, 5f, 2f, ink);
            Gfx.Rect(sb, cx + radius - 1f, cy - 1f, 5f, 2f, ink);
            Gfx.Diamond(sb, cx, cy, 2f, ink);
        }

        //Aim Arc : while an enemy is being picked, dots fly from the command to its reticle, faint at the command, brighter on the
        //way, ending on the ring. A quadratic curve (B(t) = (1-t)^2 P0 + 2(1-t)t P1 + t^2 P2) bowed up over the stage.
        //ADVANCED PART : the reticle is drawn through the battle's camera, so its point is pushed through the same matrix here
        //(the arc itself is drawn with the panel, which never moves).
        private void DrawAimArc(SpriteBatch sb)
        {
            if (!BattleRules.AimArc || !aimOn || !aimFull || aimPlaying || view != View.Target || aimCommand < 0 || aimAt == null || !aimAt.IsEnemy) return;
            float radius;
            Vector2 target = Vector2.Transform(ReticleOf(aimAt, out radius), CameraPush());
            Rectangle box = commandBox[aimCommand];
            Vector2 p0 = new Vector2(box.Center.X, box.Y - 4f);
            Vector2 p1 = new Vector2(MathHelper.Lerp(p0.X, target.X, 0.35f), Math.Min(p0.Y, target.Y) - 110f);
            const int Dots = 16;
            float flow = time * 0.9f % 1f;
            for (int k = 0; k < Dots; k++)
            {
                float t = (k + flow) / Dots;
                float u = 1f - t;
                Vector2 p = u * u * p0 + 2f * u * t * p1 + t * t * target;
                if (Vector2.Distance(p, target) < radius * camZoom + 14f) continue;          // stop at the ring
                Gfx.Diamond(sb, p.X, p.Y, 2f + 1.5f * t, aimColor * (0.25f + 0.6f * t));
            }
        }

        //Camera Push : the matrix BeginCameraBatch draws TACET through, without SHARP SCREEN (the panel's own batch adds it)
        private Matrix CameraPush()
        {
            return Matrix.CreateTranslation(-camAt.X, -camAt.Y, 0f) * Matrix.CreateScale(camZoom, camZoom, 1f)
                 * Matrix.CreateRotationZ(camTurn) * Matrix.CreateTranslation(camAt.X + camShift.X, camAt.Y + camShift.Y, 0f);
        }

        //Skill Card : over the command under the pointer (or the one waiting for its target). The kind's colour down its left,
        //the name and the BREATH it costs, the kind and who it is for, what it does in numbers.
        private void DrawSkillCard(SpriteBatch sb)
        {
            if (!aimOn || aimPlaying || aimCommand < 0 || stepFor != null) return;
            Rectangle r = cardShown;                                                     // FRIEND PICK : over the row while it is open
            float rise = Smooth(Math.Min(1f, aimTime / 0.12f));
            r.Y += (int)((1f - rise) * 8f);
            float a = rise;
            Gfx.Rect(sb, new Rectangle(r.X + 4, r.Y + 4, r.Width, r.Height), Color.Black * (0.5f * a));
            Gfx.Rect(sb, r, Palette.Void * (0.95f * a));
            Gfx.RectOutline(sb, r, Palette.LineGrey * a, 1);
            Gfx.Rect(sb, r.X, r.Y, 5, r.Height, aimColor * a);
            Gfx.Text(sb, Game.BigFont, cardName, r.X + 16, r.Y + 6, Palette.Highlight * a, TextSize.Small);
            if (cardCost > 0) Ui.Pips(sb, r.Right - 14 - (cardCost - 1) * 14f, r.Y + 17, cardCost, cardCost, 4.5f, 14f, a);
            Gfx.TextSpaced(sb, Game.Font, cardKind, r.X + 16, r.Y + 31, aimColor * a, TextSize.Tiny, 2f);
            Gfx.TextSpaced(sb, Game.Font, cardFor, r.X + 24 + Gfx.SpacedWidth(Game.Font, cardKind, TextSize.Tiny, 2f), r.Y + 31, Palette.LineGrey * a, TextSize.Tiny, 2f);
            Gfx.Text(sb, Game.Font, cardLine, r.X + 16, r.Y + 47, Palette.Paper * a, TextSize.Label);
        }
    }
}
