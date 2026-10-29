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
    //   a FLOOR is one room of 7 to 10 STAGES, and its whole MAP is made when it opens (Beta1, Data/FloorMap.cs)
    //   STAGE 1 of every floor is its opening, the movement's card (Data/Movement.cs). Before the very first one the run
    //     picks its band, 3 of the 5 legends (THE BAND OF THREE, TeamPickScreen), and names it.
    //   STAGES 2 upward offer the places of the map the band can walk to next
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
        public bool RunComplete;             // true once the last boss is beaten
        public bool Maestro;                 // MAESTRO MODE : hard from the first fight, NORMAL when false (29 Sep, BattleRules)

        //Run Party : THE BAND OF THREE (Beta1, 29 Oct, docs/BETA1_DIRECTION.md 8), picked at the start and together all run
        public List<Musician> Roster = new List<Musician>();       // the band, BattleRules.BandSize musicians
        public Formation Formation = new Formation();              // everybody is on stage (the old duel still reads their seats)
        public List<Musician> Order = new List<Musician>();        // the band page's order (Beta1), the first one in front, see Lineup
        public List<Motif> Motifs = new List<Motif>();             // everything the band carries

        //Run Resources
        public int Shards = 0;
        public int PlaceSeed;                // the dice of the place the band walked into (the shop's shelves, the treasure), saved with the run,
                                             // so leaving the game and coming back finds the same shelves (RunFlow.Enter)
        public int LastRounds;               // Alpha 7 : rounds the last turn battle took, for the result page
        public bool EchoesPaid;              // ARCHIVE : this run's ECHOES are already given (Archive.Pay)
        public int EchoesEarned;
        public int Stamina = 100;
        public int MaxStamina = 100;
        public float PowerMultiplier = 1f;   // from the conductor's PUSH stat

        //Run Record : shown on the curtain call page when the run ends
        public int BattlesWon = 0;
        public int PerfectsTotal = 0;
        public int BestCombo = 0;
        public List<JourneyStop> Journey = new List<JourneyStop>();   // every place picked, in order

        //Run Route
        public RouteNode[] Options = new RouteNode[0];     // the places the band can walk to now (from the map)
        public RouteNode Chosen;
        public GameEvent CurrentEvent;

        //Run Map (Beta1, 29 Oct) : the whole floor (Data/FloorMap.cs), where the band stands on it, and the way it came
        public FloorMap Map = new FloorMap();
        public int MapAt = -1;                               // the column of the last place walked through, -1 at the opening
        public List<int> MapPath = new List<int>();          // the columns walked this floor, row 2 first

        //Run Battle : the fight in progress, null between fights
        public BattleState Battle;

        //Run Labels : rebuilt only when a value actually changes, never inside Draw
        public string StageLabel = "";       // "STAGE 5"
        public string StageNumber = "";      // "05"
        public string FloorLabel = "";       // "FLOOR 1   SIAM ERA"
        public string FloorShort = "";       // "FLOOR 1 / 3"
        public string ShardsLabel = "";
        public string StaminaLabel = "";
        public string StaminaValue = "";     // "64 / 84"
        public string MotifsLabel = "";

        private Random random = DebugShots.Steady ? new Random(433) : new Random();     // the capture tool's --steady fixes the dice
        private Random mapDice = DebugShots.Steady ? new Random(4331) : new Random();   // the maps have their own dice, so the rest
                                                                                         // of a run rolls the same as before the map
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
            BandName = "";
            RunComplete = false;
            BattlesWon = 0;
            PerfectsTotal = 0;
            BestCombo = 0;
            Journey.Clear();
            Roster.Clear();
            Order.Clear();
            Motifs.Clear();
            Formation = new Formation();
            Battle = null;
            MusicianList.ResetRehearsals();


            //ARCHIVE : what earlier runs bought (step 7)
            Shards = Archive.Level(Archive.Savings) * BattleRules.ArchiveShardsPerLevel;                 // SAVINGS
            if (Archive.Level(Archive.Keepsake) > 0)                                                     // KEEPSAKE
            {
                //One common motif from floor 1 (MotifList.Roll would let a rarer one in)
                List<Motif> commons = new List<Motif>();
                for (int i = 0; i < MotifList.All.Length; i++)
                    if (MotifList.All[i].Rarity == 1 && MotifList.All[i].FromFloor <= 1) commons.Add(MotifList.All[i]);
                if (commons.Count > 0) Motifs.Add(commons[random.Next(commons.Count)]);
            }

            //Conductor Stats : turn the 0 to 10 numbers into real values
            MaxStamina = BattleRules.StaminaBase + conductor.Stamina * BattleRules.StaminaPerPoint;
            Stamina = MaxStamina;
            PowerMultiplier = BattleRules.PowerBase + conductor.PushPower * BattleRules.PowerPerPoint;

            BeginFloor();
        }

        //Floor Begin : a new floor opens on its locked stage one, where its movement's card shows, and its whole map is made
        public void BeginFloor()
        {
            Stage = 1;
            StagesThisFloor = 7 + random.Next(4);   // 7 to 10
            Options = new RouteNode[0];
            BuildMap();
            RefreshLabels();
        }

        //Floor Opening : true while we are on the locked stage one (the movement's card)
        public bool AtFloorOpening
        {
            get { return Stage == 1; }
        }

        //Opening Leave : the card has been shown, routing starts now
        public void LeaveOpening()
        {
            Stage = 2;
            OfferPlaces();
            RefreshLabels();
        }

        //Movement : NO MORE ERAS (Beta1, 29 Oct). Each floor is a movement of the piece (Data/Movement.cs), which picks its
        //bosses and its battle backdrop. It follows the floor, so it is never saved.
        public int Movement
        {
            get { return MovementList.Of(Floor); }
        }

        //THE BAND OF THREE (Beta1, 29 Oct, docs/BETA1_DIRECTION.md 8) : the run picks its band once, at its start (TeamPickScreen),
        //BattleRules.BandSize of the five legends, and they stay together all run, every one of them in every fight. Until 29 Oct
        //new faces joined at each floor, the shop hired more, a seat on stage was bought and the BENCH was CALLED in.
        //Pick Band : the ones picked, in the order they were picked (the first one stands in front)
        public void PickBand(List<Musician> picked)
        {
            SeatBand(picked);

            //FIRST CHAIR (ARCHIVE) : the first one picked starts with a better weapon
            if (Roster.Count > 0 && Archive.Level(Archive.FirstChair) > 0)
                Roster[0].Rehearsed = Math.Max(Roster[0].Rehearsed, Math.Min(BattleRules.RehearseMax, BattleRules.ArchiveFirstChairTier));
            RefreshLabels();
        }

        //Band Ready : the run has its whole band. A save made on the first floor's opening before the band of three has nobody yet.
        public bool BandReady
        {
            get { return Roster.Count >= BattleRules.BandSize; }
        }

        //Keep Band : a save from before the band of three can hold up to seven musicians, some on the BENCH. The first ones of the
        //fight's lineup (on stage first, in the player's order) stay, the others leave, and everybody left is on stage. The roster
        //and the order keep their own order. Called once when a save is loaded.
        public void KeepBand()
        {
            List<Musician> lineup = Lineup();
            List<Musician> keep = new List<Musician>();
            for (int i = 0; i < lineup.Count && keep.Count < BattleRules.BandSize; i++) keep.Add(lineup[i]);
            Roster.RemoveAll(m => !keep.Contains(m));
            Order.RemoveAll(m => !keep.Contains(m));
            Formation = new Formation();
            for (int i = 0; i < Roster.Count; i++) Formation.AutoSeat(Roster[i], BattleRules.BandSize);
        }

        //Seat Band : these musicians are the band, all on stage, in this order
        private void SeatBand(List<Musician> band)
        {
            Roster.Clear();
            Order.Clear();
            Formation = new Formation();
            for (int i = 0; i < band.Count && Roster.Count < BattleRules.BandSize; i++)
            {
                Musician m = band[i];
                if (m == null || Roster.Contains(m)) continue;
                Roster.Add(m);
                Order.Add(m);
                Formation.AutoSeat(m, BattleRules.BandSize);
            }
        }

        //Order (Beta1, 29 Oct, the band page's columns) : the order the player put the band in, everyone in the roster once.
        //A fight takes them in this order (Lineup), so the first one stands in front. Anyone missing from it (a save from
        //before the band page) joins at the end, among themselves in the order of their home seats (strings first).
        public void SyncOrder()
        {
            for (int i = Order.Count - 1; i >= 0; i--)
                if (!Roster.Contains(Order[i])) Order.RemoveAt(i);
            int from = Order.Count;
            for (int i = 0; i < Roster.Count; i++)
                if (!Order.Contains(Roster[i])) Order.Add(Roster[i]);
            for (int i = from + 1; i < Order.Count; i++)                 // a few new faces : sorted by home seat
                for (int k = i; k > from && StageLayout.HomeSeat(Order[k]) < StageLayout.HomeSeat(Order[k - 1]); k--)
                {
                    Musician swap = Order[k];
                    Order[k] = Order[k - 1];
                    Order[k - 1] = swap;
                }
        }

        //Lineup : the band the way a fight takes it, in the player's order. Everyone is on stage since the band of three, the
        //ones off stage only come from an old save (KeepBand reads them last).
        public List<Musician> Lineup()
        {
            SyncOrder();
            List<Musician> band = new List<Musician>();
            for (int i = 0; i < Order.Count; i++)
                if (Formation.OnStage(Order[i])) band.Add(Order[i]);
            for (int i = 0; i < Order.Count; i++)
                if (!Formation.OnStage(Order[i])) band.Add(Order[i]);
            return band;
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
        //Their weapon's tier and family motifs are added here, so every page shows the same number.
        public int PowerOf(Musician m)
        {
            int power = m.Power + m.Rehearsed * BattleRules.WeaponTierPower;                // WEAPON TIERS
            if (m.Family == Family.String && Has(MotifId.Resin)) power += BattleRules.FamilyMotifPower;
            return power;
        }

        //Rehearse : WEAPON TIERS (29 Oct), one musician's weapon goes up a tier for the rest of the run (it was 1 power,
        //and still is : BattleRules.WeaponTierPower). The rest stop, the luthier, the shop, events and the bosses all use it.
        public bool CanRehearse(Musician m)
        {
            return m != null && m.Rehearsed < BattleRules.RehearseMax;
        }

        public void Rehearse(Musician m)
        {
            if (!CanRehearse(m)) return;
            m.Rehearsed++;
        }

        //Least Trained : the musician of the band with the lowest weapon tier (the one further forward on a tie), null when
        //every weapon is at the top. For a lesson nobody picked (an event).
        public Musician LeastTrained()
        {
            Musician best = null;
            List<Musician> band = Lineup();
            for (int i = 0; i < band.Count; i++)
                if (CanRehearse(band[i]) && (best == null || band[i].Rehearsed < best.Rehearsed)) best = band[i];
            return best;
        }

        //Any To Train : at least one weapon of the band can still go up a tier
        public bool AnyToTrain
        {
            get { return LeastTrained() != null; }
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

        //Stamina Change : always kept between zero and the maximum.
        //BAND LIFE (Alpha 7) : in the turn battle the change goes to every musician's own life instead,
        //as the same share of each one's maximum (an event's -12 out of a band of 92 is -13 percent each).
        //Nobody drops below 1 outside a fight.
        public void ChangeStamina(int amount)
        {
            if (BattleRules.TurnBattleOn)
            {
                SyncBandLife();
                float share = MaxStamina > 0 ? amount / (float)MaxStamina : 0f;
                for (int i = 0; i < Roster.Count; i++)
                {
                    Musician m = Roster[i];
                    SetLife(m, LifeOf(m) + (int)Math.Round(share * MaxLifeOf(m)));
                }
                RefreshLabels();
                return;
            }

            Stamina += amount;
            if (Stamina < 0) Stamina = 0;
            if (Stamina > MaxStamina) Stamina = MaxStamina;
            RefreshLabels();
        }

        public void RestoreAllStamina()
        {
            ChangeStamina(MaxStamina);
        }

        //BAND LIFE (Alpha 7) : each musician's life between fights, -1 on the musician means full
        public int MaxLifeOf(Musician m)
        {
            return BattleRules.TurnHp[(int)m.Family] + Archive.Level(Archive.Life) * BattleRules.ArchiveLifePerLevel;   // STRONGER BAND
        }

        public int LifeOf(Musician m)
        {
            int max = MaxLifeOf(m);
            return m.Life < 0 ? max : Math.Min(m.Life, max);
        }

        public void SetLife(Musician m, int life)
        {
            int max = MaxLifeOf(m);
            m.Life = Math.Max(1, Math.Min(max, life));
        }

        //Sync Band Life : the run's breath numbers hold the whole band's life added up, so the run bar,
        //the shop and the rest room read it like they read breath
        private void SyncBandLife()
        {
            if (!BattleRules.TurnBattleOn) return;
            Stamina = 0;
            MaxStamina = 0;
            for (int i = 0; i < Roster.Count; i++)
            {
                Stamina += LifeOf(Roster[i]);
                MaxStamina += MaxLifeOf(Roster[i]);
            }
            if (MaxStamina == 0) MaxStamina = 1;          // before anybody has joined
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

        //Battle Begin : pick an enemy that fits the chosen path and set up the fight
        public void BeginBattle()
        {
            BeginBattle(null);
        }

        //Battle Begin Against : the same, with the enemy already known (a loaded save walks back
        //into the fight it left). Null picks one as usual. knownPack is the whole saved PACK, or null.
        public void BeginBattle(Enemy known, List<Enemy> knownPack = null)
        {
            EnemyKind kind = EnemyKind.Normal;
            if (Chosen != null && Chosen.Type == NodeType.Elite) kind = EnemyKind.Elite;
            if (Chosen != null && Chosen.Type == NodeType.Boss) kind = EnemyKind.Boss;

            Enemy enemy = known != null ? known : EnemyList.Pick(kind, Movement, random);
            Battle = new BattleState(this, enemy, random);

            //PACKS (Alpha 7 step 12) : the turn battle brings ordinary enemies along, more on deeper floors
            //and more in the second half of a floor (BattleRules ONE BAR)
            if (!BattleRules.TurnBattleOn) return;
            if (knownPack != null && knownPack.Count > 0 && knownPack[0] == enemy)
            {
                Battle.Pack = new List<Enemy>(knownPack);
                return;
            }
            for (int i = 0; i < PackEscorts(kind); i++) Battle.Pack.Add(EnemyList.PickOther(EnemyKind.Normal, Movement, Battle.Pack, random));
        }

        //Pack Escorts : how many ordinary enemies join the one picked for this fight
        public int PackEscorts(EnemyKind kind)
        {
            int f = Math.Max(1, Math.Min(3, Floor)) - 1;
            if (kind == EnemyKind.Elite) return BattleRules.TurnEliteEscort[f];
            if (kind == EnemyKind.Boss) return BattleRules.TurnBossEscort[f];
            bool late = Stage * 2 > StagesThisFloor;
            return (late ? BattleRules.TurnPackLate[f] : BattleRules.TurnPackEarly[f]) - 1;
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

        //Label Refresh : call after changing stage, floor, shards or stamina
        public void RefreshLabels()
        {
            StageLabel = "STAGE " + Stage;
            StageNumber = Stage.ToString("00");
            FloorLabel = "FLOOR " + Floor + "   " + MovementList.Names[Movement];
            FloorShort = "FLOOR " + Floor + " / " + BattleRules.FloorsPerRun;
            ShardsLabel = "SHARDS  " + Shards;
            SyncBandLife();
            StaminaLabel = BattleRules.LifeWord + "  " + Stamina + " / " + MaxStamina;
            StaminaValue = Stamina + " / " + MaxStamina;
            MotifsLabel = "MOTIFS  " + Motifs.Count;
        }

        //Boss Next : true when the boss is the very next stage, used to warn the player
        public bool BossIsNext
        {
            get { return Stage + 1 >= StagesThisFloor; }
        }

        //Run Advance : move one stage forward. Past the boss opens the next floor, which starts on its own opening (the
        //movement's card) again. Past the last floor the run is won.
        public void Advance()
        {
            //MAP : the place just finished is where the band stands now
            if (Chosen != null && Chosen.Row == Stage && Stage >= 2)
            {
                MapAt = Chosen.Column;
                MapPath.Add(Chosen.Column);
            }
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

            OfferPlaces();
            RefreshLabels();
        }

        //Offer Places (Beta1 map) : the places the band can walk to now, read from the floor's map. From the opening every
        //place of row 2, after that the places the last one leads to. (Before the map a fresh set was rolled here, and
        //nothing further on could be seen.)
        public void OfferPlaces()
        {
            RouteNode from = Map.At(Stage - 1, MapAt);
            if (from == null || Stage <= 2)
            {
                Options = Map.Count(Stage) > 0 ? (RouteNode[])Map.Rows[Stage].Clone() : new RouteNode[0];
                return;
            }
            List<RouteNode> ways = new List<RouteNode>();
            for (int k = 0; k < from.Next.Count; k++)
            {
                RouteNode next = Map.At(Stage, from.Next[k]);
                if (next != null) ways.Add(next);
            }
            if (ways.Count == 0 && Map.Count(Stage) > 0) ways.AddRange(Map.Rows[Stage]);       // a broken map still goes on
            Options = ways.ToArray();
        }

        //Map Build : the whole floor at once (Data/FloorMap.cs). Each row's kinds come from the same weights the paths
        //were rolled with before the map, the last row is the boss, then the rows are joined by the STAIRCASE.
        private void BuildMap()
        {
            int stages = StagesThisFloor;
            Map = new FloorMap();
            Map.Rows = new RouteNode[stages + 1][];
            Map.Rows[0] = new RouteNode[0];
            Map.Rows[1] = new RouteNode[0];                                  // the opening holds no places
            for (int s = 2; s <= stages; s++)
            {
                int count = s == stages ? 1 : (s == 2 ? 3 + mapDice.Next(2) : 2 + mapDice.Next(3));   // the first row is wide, 3 or 4
                RouteNode[] row = new RouteNode[count];
                for (int c = 0; c < count; c++) row[c] = MakeNode(s == stages ? NodeType.Boss : PickType(s), s, c);

                //Variety Guard : if every place of a row rolled the same, change the first one,
                //otherwise the player is not really being offered a choice
                bool allSame = true;
                for (int c = 1; c < count; c++)
                    if (row[c].Type != row[0].Type) allSame = false;
                if (allSame && count > 1) row[0] = MakeNode(row[0].Type == NodeType.Battle ? NodeType.Event : NodeType.Battle, s, 0);

                Map.Rows[s] = row;
            }
            for (int s = 2; s < stages; s++) FloorMap.Join(Map.Rows[s], Map.Rows[s + 1], mapDice);
            MapAt = -1;
            MapPath.Clear();
        }

        //Map Restore : the map a save file wrote down (FloorMap.Write), and the way the band came. A save from before the
        //map, or a broken line, gets a new map for its floor and the band at its opening.
        public void RestoreMap(string line, int[] path)
        {
            List<List<KeyValuePair<NodeType, List<int>>>> rows = FloorMap.Read(line);
            if (rows == null || rows.Count != StagesThisFloor - 1)
            {
                BuildMap();
                OfferPlaces();
                return;
            }
            Map = new FloorMap();
            Map.Rows = new RouteNode[StagesThisFloor + 1][];
            Map.Rows[0] = new RouteNode[0];
            Map.Rows[1] = new RouteNode[0];
            for (int r = 0; r < rows.Count; r++)
            {
                int s = r + 2;
                Map.Rows[s] = new RouteNode[rows[r].Count];
                for (int c = 0; c < rows[r].Count; c++)
                {
                    NodeType type = rows[r][c].Key == NodeType.EraShift ? NodeType.Event : rows[r][c].Key;   // NO MORE ERAS : an old save's CROSSING is a ???
                    Map.Rows[s][c] = MakeNode(type, s, c);
                    Map.Rows[s][c].Next.AddRange(rows[r][c].Value);
                }
            }
            MapPath.Clear();
            for (int i = 0; i < path.Length; i++) MapPath.Add(path[i]);
            MapAt = MapPath.Count > 0 ? MapPath[MapPath.Count - 1] : -1;
            OfferPlaces();
        }

        //Replace Chosen : the place the band walked into turns into another kind (an event that starts an elite fight).
        //The new place takes the old one's spot on the map and its ways, so the floor carries on from there.
        public void ReplaceChosen(NodeType type)
        {
            RouteNode old = Chosen;
            RouteNode node = old != null ? MakeNode(type, old.Row, old.Column) : MakeNode(type);
            if (old != null)
            {
                node.Next.AddRange(old.Next);
                if (Map.At(old.Row, old.Column) == old) Map.Rows[old.Row][old.Column] = node;
                int slot = Array.IndexOf(Options, old);
                if (slot >= 0) Options[slot] = node;
            }
            Chosen = node;
        }

        //Route Weights : how often each kind of place turns up on a stage of the floor.
        //Change these numbers to make the run feel different.
        private NodeType PickType(int stage)
        {
            int roll = mapDice.Next(100);

            //Early Stages : no shop or rest right after the floor opens
            if (stage <= 3)
            {
                if (roll < 65) return NodeType.Battle;
                return NodeType.Event;
            }

            //NO MORE ERAS (29 Oct) : the CROSSING's 9 went to the ??? (22 -> 31)
            if (roll < 34) return NodeType.Battle;
            if (roll < 65) return NodeType.Event;
            if (roll < 77) return NodeType.Elite;
            if (roll < 89) return NodeType.Shop;
            return NodeType.Rest;
        }

        //Node Make : build one node and wrap its caption now, so drawing stays free of string work
        private RouteNode MakeNode(NodeType type, int row, int column)
        {
            RouteNode node = MakeNode(type);
            node.Row = row;
            node.Column = column;
            return node;
        }

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
