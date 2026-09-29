using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Battle;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //RunState : everything that belongs to ONE run. Made fresh when the player takes the
    //baton, thrown away when the run ends, because losing means starting over.
    //
    //Shape of a run:
    //   a run is FloorsPerRun floors. Beating the last boss wins the whole run.
    //   a FLOOR is one room of 7 to 10 STAGES
    //   STAGE 1 of every floor is locked. You pick an era, and musicians from that era
    //     join you. The very first floor also asks you to name the band.
    //   STAGES 2 upward deal route paths, and you stay in the chosen era until either a
    //     CROSSING moves you, or the floor ends
    //   the LAST stage of a floor is always the boss, and beating it opens the next floor
    //
    //Stamina belongs to the run, not to a single fight. Battles drain it, a win gives a
    //little back, and REST rooms refill it.

    //Journey Stop : one place the run went through, kept for the curtain call
    public class JourneyStop
    {
        public int Floor;
        public NodeType Type;
        public bool Lost;          // the run ended here
    }

    public class RunState
    {
        //Run Identity
        public Conductor Conductor;
        public string BandName = "";

        //Run Position
        public int Floor = 1;
        public int Stage = 1;
        public int StagesThisFloor = 8;      // the boss sits on this stage number
        public int Era = 0;                  // index into EraList.All
        public bool RunComplete;             // true once the last boss is beaten
        public bool Maestro;                 // MAESTRO MODE : hard from the first fight, NORMAL when false (29 Sep, BattleRules)

        //Run Party
        public List<Musician> Roster = new List<Musician>();       // everyone you own
        public List<Musician> JustJoined = new List<Musician>();   // shown on the recruit page
        public Formation Formation = new Formation();              // who is on stage, who is on the bench
        public List<Motif> Motifs = new List<Motif>();             // everything the band carries

        //Run Resources
        public int Shards = 0;
        public int Seats = 3;                // how many players fit on stage
        public int SeatsBoughtThisFloor = 0; // the shop will only sell two per floor
        public int Stamina = 100;
        public int MaxStamina = 100;
        public float PowerMultiplier = 1f;   // from the conductor's PUSH stat

        //Run Record : shown on the curtain call page when the run ends
        public int BattlesWon = 0;
        public int PerfectsTotal = 0;
        public int BestCombo = 0;
        public List<JourneyStop> Journey = new List<JourneyStop>();   // every place picked, in order

        //Run Route
        public RouteNode[] Options = new RouteNode[0];
        public RouteNode Chosen;
        public GameEvent CurrentEvent;

        //Run Battle : the fight in progress, null between fights
        public BattleState Battle;

        //Run Labels : rebuilt only when a value actually changes, never inside Draw
        public string StageLabel = "";       // "STAGE 5"
        public string StageNumber = "";      // "05"
        public string FloorLabel = "";       // "FLOOR 1   SIAM ERA"
        public string FloorShort = "";       // "FLOOR 1 / 3"
        public string ShardsLabel = "";
        public string SeatsLabel = "";
        public string SeatsValue = "";       // "3 / 3"
        public string StaminaLabel = "";
        public string StaminaValue = "";     // "64 / 84"
        public string RosterLabel = "";
        public string MotifsLabel = "";

        private Random random = new Random();
        private SpriteFont font;
        private float wrapWidth;

        //Random : shared by the pages that roll things (shop stock, events)
        public Random Rng
        {
            get { return random; }
        }

        //Run Start : called once when the player takes the baton
        public void Start(Conductor conductor, SpriteFont captionFont, float captionWrapWidth)
        {
            Conductor = conductor;
            font = captionFont;
            wrapWidth = captionWrapWidth;

            Floor = 1;
            Shards = 0;
            Seats = 3;
            BandName = "";
            RunComplete = false;
            BattlesWon = 0;
            PerfectsTotal = 0;
            BestCombo = 0;
            Journey.Clear();
            Roster.Clear();
            JustJoined.Clear();
            Motifs.Clear();
            Formation = new Formation();
            Battle = null;
            MusicianList.ResetRehearsals();


            //Conductor Stats : turn the 0 to 10 numbers into real values
            MaxStamina = BattleRules.StaminaBase + conductor.Stamina * BattleRules.StaminaPerPoint;
            Stamina = MaxStamina;
            PowerMultiplier = BattleRules.PowerBase + conductor.PushPower * BattleRules.PowerPerPoint;

            BeginFloor();
        }

        //Floor Begin : a new floor opens on its locked stage one, where the era is chosen
        public void BeginFloor()
        {
            Stage = 1;
            StagesThisFloor = 7 + random.Next(4);   // 7 to 10
            SeatsBoughtThisFloor = 0;
            Options = new RouteNode[0];
            RefreshLabels();
        }

        //Floor Opening : true while we are on the locked era choice stage
        public bool AtFloorOpening
        {
            get { return Stage == 1; }
        }

        //Opening Leave : the era is picked and the musicians taken, routing starts now
        public void LeaveOpening()
        {
            Stage = 2;
            RollOptions();
            RefreshLabels();
        }

        //Era Choose : used both by the floor opening and by a CROSSING
        public void ChooseEra(int era)
        {
            Era = era;
            RefreshLabels();
        }

        public Era CurrentEra
        {
            get { return EraList.All[Era]; }
        }

        //Roster Limits : everyone on stage plus a small bench
        public int RosterCapacity
        {
            get { return Seats + BattleRules.BenchSize; }
        }

        public bool RosterFull
        {
            get { return Roster.Count >= RosterCapacity; }
        }

        //Recruit Opening : musicians from the chosen era join at the start of a floor
        public void RecruitAtOpening()
        {
            JustJoined.Clear();
            int count = Floor == 1 ? BattleRules.FirstFloorRecruits : BattleRules.LaterFloorRecruits;

            for (int i = 0; i < count; i++)
                RecruitOne();

            RefreshLabels();
        }

        //Recruit One : returns the new musician, or null if nobody could join.
        //They sit down straight away if a seat is free, otherwise they wait on the bench.
        public Musician RecruitOne()
        {
            if (RosterFull) return null;
            return Recruit(MusicianList.RollFromEra(Era, Roster, random));
        }

        //Recruit : this musician joins (the shop shows who it will be before paying). Null when
        //there is no room or nobody was given.
        public Musician Recruit(Musician m)
        {
            if (m == null || RosterFull || Roster.Contains(m)) return null;

            Roster.Add(m);
            JustJoined.Add(m);
            Formation.AutoSeat(m, Seats);
            RefreshLabels();
            return m;
        }

        //Missing Section Player : someone of a section (strings, winds or percussion) nobody in the
        //ensemble plays yet, from this era if there is one, from any era if not. When every section
        //is covered, just someone new from this era. Null when nobody is left (THE EMPTY CHAIR).
        public Musician MissingSectionPlayer()
        {
            bool[] covered = new bool[StageLayout.Rows.Length];
            for (int i = 0; i < Roster.Count; i++) covered[StageLayout.SectionOf(Roster[i].Family)] = true;

            Musician best = null;
            int picks = 0;
            for (int pass = 0; pass < 2 && best == null; pass++)
                for (int i = 0; i < MusicianList.All.Length; i++)
                {
                    Musician m = MusicianList.All[i];
                    if (Roster.Contains(m) || covered[StageLayout.SectionOf(m.Family)]) continue;
                    if (pass == 0 && m.Era != Era) continue;
                    picks++;
                    if (random.Next(picks) == 0) best = m;         // every candidate gets a fair chance
                }
            if (best == null) best = MusicianList.RollFromEra(Era, Roster, random);
            return best;
        }

        //Motif Check : does the band carry this motif
        public bool Has(MotifId id)
        {
            for (int i = 0; i < Motifs.Count; i++)
                if (Motifs[i].Id == id) return true;
            return false;
        }

        public void AddMotif(Motif m)
        {
            if (m == null || Motifs.Contains(m)) return;
            Motifs.Add(m);
            RefreshLabels();
        }

        //Power Of : what one musician hits for right now, before the CUE and the beat.
        //Rehearsals and family motifs are added here, so every page shows the same number.
        public int PowerOf(Musician m)
        {
            int power = m.Power + m.Rehearsed;
            if (m.Family == Family.String && Has(MotifId.Resin)) power += BattleRules.FamilyMotifPower;
            return power;
        }

        //Rehearse : one musician gets permanently stronger for this run
        public bool CanRehearse(Musician m)
        {
            return m.Rehearsed < BattleRules.RehearseMax;
        }

        public void Rehearse(Musician m)
        {
            if (!CanRehearse(m)) return;
            m.Rehearsed++;
        }

        //Journey : called when a path is picked, and when the run ends on it
        public void RecordStop(NodeType type)
        {
            JourneyStop stop = new JourneyStop();
            stop.Floor = Floor;
            stop.Type = type;
            Journey.Add(stop);
        }

        public void MarkLastStopLost()
        {
            if (Journey.Count > 0) Journey[Journey.Count - 1].Lost = true;
        }

        //Stamina Change : always kept between zero and the maximum
        public void ChangeStamina(int amount)
        {
            Stamina += amount;
            if (Stamina < 0) Stamina = 0;
            if (Stamina > MaxStamina) Stamina = MaxStamina;
            StaminaLabel = "BREATH  " + Stamina + " / " + MaxStamina;
            StaminaValue = Stamina + " / " + MaxStamina;
        }

        public void RestoreAllStamina()
        {
            ChangeStamina(MaxStamina);
        }

        //Shards : SpendShards returns false and changes nothing when there is not enough
        public void AddShards(int amount)
        {
            Shards += amount;
            if (Shards < 0) Shards = 0;
            RefreshLabels();
        }

        public bool SpendShards(int amount)
        {
            if (Shards < amount) return false;
            Shards -= amount;
            RefreshLabels();
            return true;
        }

        //Seat Shop : each seat costs more than the last, two per floor, nine at most
        public int SeatPrice
        {
            get { return BattleRules.SeatPriceBase + BattleRules.SeatPriceStep * SeatsBoughtTotal; }
        }

        private int SeatsBoughtTotal
        {
            get
            {
                return Seats - 3;      // every run starts with 3 (round 15 : THE FOLK LEADER no longer starts with 2)
            }
        }

        public bool SeatForSale
        {
            get { return SeatsBoughtThisFloor < BattleRules.SeatsPerFloor && Seats < StageLayout.SeatCount; }
        }

        public void AddSeat()
        {
            Seats++;
            SeatsBoughtThisFloor++;
            RefreshLabels();
        }

        //Battle Begin : pick an enemy that fits the chosen path and set up the fight
        public void BeginBattle()
        {
            BeginBattle(null);
        }

        //Battle Begin Against : the same, with the enemy already known (a loaded save walks back
        //into the fight it left). Null picks one as usual.
        public void BeginBattle(Enemy known)
        {
            EnemyKind kind = EnemyKind.Normal;
            if (Chosen != null && Chosen.Type == NodeType.Elite) kind = EnemyKind.Elite;
            if (Chosen != null && Chosen.Type == NodeType.Boss) kind = EnemyKind.Boss;

            Enemy enemy = known != null ? known : EnemyList.Pick(kind, Era, random);
            Battle = new BattleState(this, enemy, random);
        }

        //Battle Finish : called once the player has seen the outcome
        public void FinishBattle()
        {
            Battle = null;
            RefreshLabels();
        }

        //Random Roll : 0 to 99, for chances written in BattleRules
        public int RollPercent()
        {
            return random.Next(100);
        }

        //Label Refresh : call after changing stage, floor, era, shards, seats or stamina
        public void RefreshLabels()
        {
            StageLabel = "STAGE " + Stage;
            StageNumber = Stage.ToString("00");
            FloorLabel = "FLOOR " + Floor + "   " + CurrentEra.Name + " ERA";
            FloorShort = "FLOOR " + Floor + " / " + BattleRules.FloorsPerRun;
            ShardsLabel = "SHARDS  " + Shards;
            SeatsLabel = "SEATS  " + Formation.SeatedCount + " / " + Seats;
            SeatsValue = Formation.SeatedCount + " / " + Seats;
            StaminaLabel = "BREATH  " + Stamina + " / " + MaxStamina;
            StaminaValue = Stamina + " / " + MaxStamina;
            RosterLabel = "ENSEMBLE  " + Roster.Count + " / " + RosterCapacity;
            MotifsLabel = "MOTIFS  " + Motifs.Count;
        }

        //Boss Next : true when the boss is the very next stage, used to warn the player
        public bool BossIsNext
        {
            get { return Stage + 1 >= StagesThisFloor; }
        }

        //Run Advance : move one stage forward. Past the boss opens the next floor,
        //which starts on its own locked era choice again. Past the last floor the run is won.
        public void Advance()
        {
            Stage++;

            if (Stage > StagesThisFloor)
            {
                Floor++;
                if (Floor > BattleRules.FloorsPerRun)
                {
                    Floor = BattleRules.FloorsPerRun;
                    RunComplete = true;
                    RefreshLabels();
                    return;
                }
                BeginFloor();
                return;
            }

            RollOptions();
            RefreshLabels();
        }

        //Route Roll : pick the paths on offer. Nothing is shown in advance, so a fresh set
        //is only rolled when the player actually arrives at the next stage.
        public void RollOptions()
        {
            //Boss Stage : no choice at all, the floor ends here
            if (Stage >= StagesThisFloor)
            {
                Options = new RouteNode[1];
                Options[0] = MakeNode(NodeType.Boss);
                return;
            }

            int count = 2 + random.Next(3);        // two to four paths
            Options = new RouteNode[count];

            for (int i = 0; i < count; i++)
                Options[i] = MakeNode(PickType());

            //Variety Guard : if every path rolled the same, change the first one,
            //otherwise the player is not really being offered a choice
            bool allSame = true;
            for (int i = 1; i < count; i++)
                if (Options[i].Type != Options[0].Type) allSame = false;

            if (allSame && count > 1)
            {
                NodeType swap = Options[0].Type == NodeType.Battle ? NodeType.Event : NodeType.Battle;
                Options[0] = MakeNode(swap);
            }
        }

        //Options Restore : the paths a save file wrote down, built again with their captions
        public void RestoreOptions(NodeType[] types)
        {
            Options = new RouteNode[types.Length];
            for (int i = 0; i < types.Length; i++)
                Options[i] = MakeNode(types[i]);
        }

        //Route Weights : how often each kind of place turns up.
        //Change these numbers to make the run feel different.
        private NodeType PickType()
        {
            int roll = random.Next(100);

            //Early Stages : no shop, rest or crossing right after the floor opens
            if (Stage <= 3)
            {
                if (roll < 65) return NodeType.Battle;
                return NodeType.Event;
            }

            if (roll < 34) return NodeType.Battle;
            if (roll < 56) return NodeType.Event;
            if (roll < 68) return NodeType.Elite;
            if (roll < 80) return NodeType.Shop;
            if (roll < 91) return NodeType.Rest;
            return NodeType.EraShift;
        }

        //Node Make : build one node and wrap its caption now, so drawing stays free of string work
        private RouteNode MakeNode(NodeType type)
        {
            RouteNode node = new RouteNode();
            node.Type = type;
            node.Title = RouteNodeInfo.TitleOf(type);
            node.Caption = RouteNodeInfo.CaptionOf(type);

            if (font != null)
                node.CaptionWrapped = Gfx.WrapText(font, node.Caption, wrapWidth, TextSize.Story, RouteNodeInfo.CaptionLines);
            else
                node.CaptionWrapped = node.Caption;

            return node;
        }
    }
}
