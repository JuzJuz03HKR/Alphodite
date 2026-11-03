using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tacetno433.Data;

namespace Tacetno433.Core
{
    //Figure Tacet (Beta1 1 Nov, MATH ART) : TACET's fighters, the four looks every enemy wears (Data/Enemy.cs Looks,
    //docs/ASSET_LIST.md 1.2) and the six bosses.
    //TACET LOOK (round 6 part 2, 2 Nov, docs/BETA1_DIRECTION.md 32.5) : the four looks are the things that make music go quiet,
    //drawn from the sketch docs/art_refs/concept_v6.html with its numbers as they are (see LOOK FRAME) :
    //   hush             THE MUTE : the cone mute pushed into a horn, and a "shh". A tall leaning black cone, a slit of light
    //                    with a black bar across it like a finger on the lips, light leaking from under its hem
    //   static           THE NOISE : a radio between stations. Black bits of shell in a twitching cloud, thin white lines
    //                    between them, a blinking core with a ghost beside it. It cracks by losing its bits
    //   the_damper       THE WEIGHT : the felt that stops a piano's strings. A heavy block hung from a beam presses four
    //                    strings until they sag, its light squeezed out at the sides
    //   the_mute_choir   THE EMPTY CHOIR : six black heads in rows, mouths open on nothing, a robe of long blades down to the
    //                    floor. On its blow every mouth lights
    //   Every one : a smooth black shell with a pale rim and a soft white BACKLIGHT behind it (Screens/BattleScreen.Stage.cs),
    //   so it reads as a shape anywhere on the dark stage, and a white CORE, the brightest thing on the stage (the user 13.87 :
    //   smooth, brighter than the stage, breakable, light first). CRACKS at 75 50 25 of its life, CHIPS off its shell on every
    //   hit, and when it is beaten the shell breaks (SHARDS) and the light it held rises as a ring (RELEASE).
    //The bosses (round 6 part 3, 2 Nov, docs/BETA1_DIRECTION.md 32.6) : each one drawn from its card in the sketch the same way
    //(Draw<Name> with S and SF, its colours from SetBossInks). THE MONUMENT first, the white statue the world remembers of that
    //legend, then at THE TURN the black form inside. Each core sits where its story is :
    //   requiem          REQUIEM (Mozart) : a broken halo, a quill for a blade, tears (LACRIMOSA), the lines of a score that
    //                    stop half way (the core where they stop)
    //   the_nameless_master  THE NAMELESS MASTER (Fanny) : a blank mask, an empty frame where her name should be (the core in
    //                    it), her brother's black shadow standing behind her a moment late (ITALIEN)
    //   the_devil_s_string   THE DEVIL'S STRING (Paganini) : a violin's scroll grown into horns, one string through it from above
    //                    its head to the floor (the core on it, it shivers when it plays), needles for fingers, the bow a
    //                    scythe (ON ONE STRING)
    //   the_deaf_crown   THE DEAF CROWN (Beethoven) : the famous bust, black thorns over the ears, a stick in its teeth (the
    //                    core at its tip), a raised hand giving orders, cracks of lightning once broken (THE NINTH)
    //   the_staff        THE STAFF (Lully) : a collar of the Sun King's rays, a great staff slammed down on its own rotting
    //                    foot (the core where it lands, TE DEUM)
    //   the_blind_cantor THE BLIND CANTOR (Bach) : a blindfold leaking light, organ pipes for wings, voices going out of its
    //                    chest in rings (the core in the middle, IN THE DARK)
    //LEVEL 2 gives the four looks thin limbs under the same shapes. The bosses are statues and stay as they are.
    public partial class Figure
    {
        private const int Hush = 0;
        private const int Static = 1;
        private const int Damper = 2;
        private const int Choir = 3;
        private const int Requiem = 4;
        private const int Nameless = 5;
        private const int Devil = 6;
        private const int Crown = 7;
        private const int Staff = 8;
        private const int Cantor = 9;
        private int design;

        //Shift : where an extra copy is drawn (ECHO, the stage draws it more than once), page pixels
        public Vector2 Shift;

        //Behind : HOLE LIGHT (D7, 2 Nov). How dark it is behind this figure, 0..1, set by the battle stage each frame (0 on
        //every other page). A black shell over the hole's black would vanish, so there the hole's light catches its rim : the
        //rim grows brighter and wider instead of sinking into the dark (FigureHoleRim, FigureHoleEdge in Data/BattleRules.cs).
        public float Behind;

        private readonly Shard[] shards = new Shard[40];
        private int shardCount;

        //Life : its share of life left, 0 to 1 (the stage sets it every frame, CRACKS)
        public float Life = 1f;
        private int crackSeen;            // CRACKS : how many cracks it shows (one more at 75 50 25 of its life)
        private float crackClock;         //   seconds since the newest one began to run
        private Vector2 releaseAt;        // RELEASE : where its light was when it broke

        //THE MONUMENT (round 6 part 3, docs/BETA1_DIRECTION.md 32.6) : a boss stands first as the white monument the world
        //remembers of that legend. At half life (THE TURN, Screens/BattleScreen.Boss.cs) the white shell breaks off in a
        //flash and the black form inside is left, the same shape but dark, its core brighter.
        private bool shellOn = true;
        private float breakFlash;            // 1 just broken, fades to 0
        private float shellDue;              // seconds below half life with no THE TURN waiting (a fight begun below half)
        public bool TurnPending;             // the stage : THE TURN is waiting to play, do not break quietly
        private static readonly Color MonumentWhite = new Color(230, 229, 234);
        private static readonly Color MonumentShade = new Color(164, 163, 172);
        private static readonly Color MonumentInk = new Color(10, 10, 12);

        //Shell On : a boss still in its white shell
        public bool ShellOn
        {
            get { return Of == Sort.Boss && shellOn; }
        }

        //Break Shell : THE TURN breaks the white shell (loud : white pieces fall and the black form flashes)
        public void BreakShell(bool loud)
        {
            if (!ShellOn) return;
            shellOn = false;
            if (!loud) return;
            breakFlash = 1f;
            int count = 18;
            for (int i = 0; i < count && chipCount < chips.Length; i++)
            {
                int k = chipSerial++;
                Vector2 at = body[i % 8].At + new Vector2(Hash(k, 95) - 0.5f, Hash(k, 97) - 0.5f) * (Height * 0.3f);
                ChipAt(at, 1);
                ChipBit c = chips[chipCount - 1];
                c.White = true;
                c.Size *= 2.2f;
                c.Speed *= 1.6f;
                chips[chipCount - 1] = c;
            }
        }

        //Dim : a colour darker, still solid (a colour times a number would also make it see through)
        private static Color Dim(Color c, float k)
        {
            return new Color((byte)(c.R * k), (byte)(c.G * k), (byte)(c.B * k), c.A);
        }

        //Is Look : one of the four looks every enemy wears (not a boss)
        private bool IsLook
        {
            get { return design <= Choir; }
        }

        private static int DesignOf(string key)
        {
            switch (key)
            {
                case "static": return Static;
                case "the_damper": return Damper;
                case "the_mute_choir": return Choir;
                case "requiem": return Requiem;
                case "the_nameless_master": return Nameless;
                case "the_devil_s_string": return Devil;
                case "the_deaf_crown": return Crown;
                case "the_staff": return Staff;
                case "the_blind_cantor": return Cantor;
                default: return Hush;
            }
        }

        //Set Up Tacet : how loose each part is (SYMBOLS 13.80 : no hems or tails, shapes only)
        private void SetUpTacet()
        {
            design = DesignOf(Key);
            bodyLoose[Head] = 0.45f;
            bodyLoose[Chest] = 0.2f;
            bodyLoose[HandA] = 0.7f;
            bodyLoose[HandB] = 0.7f;
            bodyLoose[FootA] = 0.3f;
            bodyLoose[FootB] = 0.3f;
            bodyLoose[Extra] = 1f;
            chainCount = 0;
        }

        //======================================================================================
        //Pose : TACET
        //======================================================================================

        //Pose Tacet : standing as its card in the sketch has it (TacetRest), a blow joining its KEY POSES (TacetKey, ONE NOTE
        //ONE BLOW in Figure.cs). Every point is a share of its height, x toward the band.
        private void PoseTacet()
        {
            KeyPose rest = TacetRest();
            pose = down ? rest : PoseNow(rest);
            KeyPose k = pose;
            want[Head] = Local(k.Head.X, k.Head.Y);
            want[Chest] = Local(k.Chest.X, k.Chest.Y);
            want[Hip] = Local(k.Hip.X, k.Hip.Y);
            want[HandA] = Local(k.HandA.X, k.HandA.Y);
            want[HandB] = Local(k.HandB.X, k.HandB.Y);
            want[FootA] = Local(k.FootA.X, k.FootA.Y);
            want[FootB] = Local(k.FootB.X, k.FootB.Y);
            float orbit = clock * 0.9f + seed;
            want[Extra] = Local((float)Math.Cos(orbit) * 0.3f, -0.6f + (float)Math.Sin(orbit) * 0.12f);
            if (design == Nameless)                                                      // THE NAMELESS MASTER : its shadow is the master a moment late
            {
                Vector2 late = Shadow(k.Chest);
                want[Extra] = Local(late.X * 1.2f, late.Y);
            }
            for (int b = 0; b < bits.Length; b++)
            {
                float a = clock * (0.5f + 0.1f * b) + b * 2.1f + seed;
                bitWant[b] = want[Chest] + new Vector2((float)Math.Cos(a) * 0.3f * Facing, (float)Math.Sin(a) * 0.16f) * Height;
            }
            weaponWant = 0f;
            weapon2Want = 0f;
        }

        //Tacet Rest : how each one stands and breathes, as a key (the four looks the same way, a boss with its hand where its
        //card has it)
        private KeyPose TacetRest()
        {
            float bob = (float)Math.Sin(clock * 1.6f + seed) * 0.014f;
            KeyPose k;
            switch (design)
            {
                case Static:                                                             // THE NOISE : a nervous twitch
                    {
                        int step = (int)((clock + seed) * 6f);
                        k = LookRest((Hash(step, 3) - 0.5f) * 0.014f, bob + (Hash(step, 5) - 0.5f) * 0.012f, 0.4f);
                    }
                    break;
                case Damper:                                                             // THE WEIGHT : slow and heavy
                    k = LookRest(0f, bob * 0.5f, 0.6f);
                    break;
                case Choir:                                                              // THE EMPTY CHOIR : LEVEL 2's hands up, leading the silence
                    k = LookRest(0f, bob, 0.4f);
                    k.HandA = new Vector2(0.2f, -0.86f + bob * 1.2f);
                    k.HandB = new Vector2(-0.2f, -0.85f + bob * 0.8f);
                    break;
                case Requiem:                                                            // REQUIEM : tall and still, the quill held ready
                    k = LookRest(0f, bob, 0.4f);
                    k.HandA = new Vector2(-0.124f, -0.44f + bob);
                    break;
                case Nameless:
                    k = LookRest(0f, bob, 0.4f);
                    break;
                case Devil:                                                              // THE DEVIL'S STRING : the bow hand by its string
                    k = LookRest(0f, bob, 0.3f);
                    k.HandA = new Vector2(0.22f, -0.5f + bob);
                    break;
                case Crown:                                                              // THE DEAF CROWN : the bust stands still, the hand gives orders
                    k = LookRest(0f, bob * 0.5f, 0.6f);
                    k.HandA = new Vector2(-0.36f, -0.69f + bob * 1.5f);
                    break;
                case Staff:                                                              // THE STAFF : beating time on its own foot between blows
                    {
                        float beat = StaffBeat();
                        float y = bob * 0.5f - beat * 0.012f;
                        k = LookRest(0f, y, 0.5f);
                        float lift = beat * 16f / LookTall;
                        k.HandA = new Vector2(0.22f, -0.5f + y - lift);                  // the top of the staff
                        k.HandB = StaffTip + new Vector2(0f, -lift);                    // its tip over its own foot
                    }
                    break;
                case Cantor:                                                             // THE BLIND CANTOR : it sways a little, as if listening
                    k = LookRest((float)Math.Sin(clock * 0.7f + seed) * 0.008f, bob, 0.5f);
                    break;
                default:                                                                 // THE MUTE
                    k = LookRest(0f, bob, 0.2f);
                    break;
            }
            return k;
        }

        //Look Rest : the four looks stand the same way (the sketch rides on the Chest spring, see LOOK FRAME). x is the push
        //toward the band, y the bob, follow how much of the push the Hip takes (less = it leans more into it)
        private static KeyPose LookRest(float x, float y, float follow)
        {
            KeyPose k = new KeyPose();
            k.Head = new Vector2(x * 1.2f, -0.8f + y);
            k.Chest = new Vector2(x, -0.5f + y);
            k.Hip = new Vector2(x * follow, -0.2f + y * 0.5f);
            k.HandA = new Vector2(0.22f + x * 1.6f, -0.5f + y);
            k.HandB = new Vector2(-0.2f, -0.45f + y);
            k.FootA = new Vector2(-0.08f, 0f);
            k.FootB = new Vector2(0.08f, 0f);
            return k;
        }

        //Staff Tip : where THE STAFF's tip rests, just over its own rotting foot (the sketch's 150, 252)
        private static readonly Vector2 StaffTip = new Vector2(-(150f - LookMiddle) / LookTall, (252f - LookFloor) / LookTall);

        //Staff Beat : THE STAFF beats time on its own foot every 1.7 seconds. 1 = the staff lifted highest, then it comes down
        //fast to 0 : the slam, at 0.45 of each beat (TE DEUM). Not while it strikes (its blows are its own).
        private float StaffBeat()
        {
            if (Blowing) return 0f;
            float t = (clock + seed) % 1.7f;
            if (t < 0.35f) return Ease(t / 0.35f);
            if (t < 0.45f) return 1f - (t - 0.35f) / 0.1f;
            return 0f;
        }

        //======================================================================================
        //KEY POSES : TACET's blows (ONE NOTE ONE BLOW, docs/BETA1_DIRECTION.md 32.21.4)
        //======================================================================================

        //Every one winds up while the note's ring closes and strikes on the note's beat. The four looks lunge about AttackRush
        //of their height at the band, a boss keeps its monument still and moves the part that tells its story 30 to 50% of
        //its height. In shares of the height, x toward the band :
        //   THE MUTE        leans back, then jabs its finger out like a "shh!"
        //   THE NOISE       comes closer in jumps, like a picture losing its signal (Steps), its bits burst on the blow
        //   THE WEIGHT      lifted slowly on its cords (the longest wind up), then dropped to press, squashed, slowly back
        //   THE EMPTY CHOIR hands raised like a conductor's, then closed and cut down (a cut off), every mouth lit
        //   THE DEAF CROWN  the great hand raised open, then brought down as a conductor's down beat, the head never turning
        //   REQUIEM         the quill raised and turned back, then one stroke of handwriting down at the band
        //   THE NAMELESS MASTER  bows into its blow, its brother's shadow strikes again a moment late (only a look, one hit)
        //   THE STAFF       the staff lifted high (long), then slammed on the floor before the band, a wave running to them.
        //                   The last note of TE DEUM it slams on its own foot.
        //   THE DEVIL'S STRING  the bow drawn across its one string like a saw, the string shaking
        //   THE BLIND CANTOR    hardly moves : it tips its head to listen for the band, then a voice goes out of its chest
        private KeyPose TacetKey(KeyOf kind, KeyPose rest)
        {
            KeyPose k = rest;
            if (kind == KeyOf.Guard) return k;                                           // TACET does not block
            bool wound = kind == KeyOf.Wound || kind == KeyOf.Charge;
            float r = BattleRules.AttackRush;
            k.Light = wound ? 0f : 1f;
            k.Arc = 1f;                                                                  // ARCS : a hand (a boss's hand, the choir's) swings round its middle
            switch (design)
            {
                case Static:                                                             // THE NOISE : closer in jumps, then the cloud bursts
                    Push(ref k, wound ? r * 0.9f : r * 1.1f, 0f, 0.6f);
                    if (wound) k.Light = 0.3f;
                    break;
                case Damper:                                                             // THE WEIGHT : lifted, then dropped to press
                    if (wound)
                    {
                        Push(ref k, 0f, -0.12f, 1f);
                        k.Squash = -0.15f;
                    }
                    else
                    {
                        Push(ref k, r * 0.25f, 0.08f, 0.6f);
                        k.Squash = 1f;
                    }
                    break;
                case Choir:                                                              // THE EMPTY CHOIR : hands up, then a cut off
                    k.Show = 1f;
                    if (wound)
                    {
                        Push(ref k, 0f, -0.04f, 1f);
                        k.HandA = new Vector2(0.34f, -1.04f);
                        k.HandB = new Vector2(-0.12f, -1.02f);
                        k.Light = 0.2f;
                    }
                    else
                    {
                        Push(ref k, r * 0.3f, 0.02f, 0.5f);
                        k.HandA = new Vector2(0.4f, -0.6f);
                        k.HandB = new Vector2(-0.02f, -0.58f);
                        k.Aim = 1f;                                                      //   the hands closed
                    }
                    break;
                case Requiem:                                                            // REQUIEM : the quill raised and turned back, then one stroke down
                    if (wound)
                    {
                        Push(ref k, -r * 0.2f, 0f, 0.5f);
                        k.HandA = new Vector2(-0.2f, -0.7f);
                        k.Aim = -0.5f;
                    }
                    else
                    {
                        Push(ref k, r * 0.4f, 0f, 0.4f);
                        k.HandA = new Vector2(0.14f, -0.34f);
                        k.Aim = 1f;
                    }
                    break;
                case Nameless:                                                           // THE NAMELESS MASTER : leaned back, then a deep bow into it
                    if (wound) Push(ref k, -r * 0.3f, 0f, 0.3f);
                    else Push(ref k, r * 0.9f, 0.02f, 0.25f);
                    break;
                case Devil:                                                              // THE DEVIL'S STRING : the bow pushed out, then drawn across its string
                    if (wound) k.HandA = new Vector2(0.02f, -0.62f);
                    else
                    {
                        Push(ref k, r * 0.3f, 0f, 0.5f);
                        k.HandA = new Vector2(0.46f, -0.42f);
                    }
                    break;
                case Crown:                                                              // THE DEAF CROWN : the hand raised, then the down beat
                    if (wound) k.HandA = new Vector2(-0.44f, -1.04f);
                    else
                    {
                        Push(ref k, r * 0.15f, 0f, 0.6f);
                        k.HandA = new Vector2(-0.06f, -0.48f);
                    }
                    break;
                case Staff:                                                              // THE STAFF : lifted high, then slammed down
                    if (wound)
                    {
                        k.HandA = new Vector2(0.16f, -0.68f);
                        k.HandB = StaffTip + new Vector2(0.03f, -0.16f);
                    }
                    else if (blowLast)                                                   //   TE DEUM's last : on its own foot
                    {
                        k.HandA = new Vector2(0.2f, -0.46f);
                        k.HandB = StaffTip + new Vector2(0.01f, 0f);
                    }
                    else
                    {
                        Push(ref k, r * 0.15f, 0f, 0.6f);
                        k.HandA = new Vector2(0.36f, -0.43f);
                        k.HandB = new Vector2((LookMiddle - 96f) / LookTall, 0f);           //   on the floor before the band
                    }
                    break;
                case Cantor:                                                             // THE BLIND CANTOR : the head tipped to listen, then the voice
                    if (wound) k.Head += new Vector2(0.07f, 0.01f);
                    break;
                default:                                                                 // THE MUTE : leaned back, then the "shh!"
                    if (wound) Push(ref k, -r * 0.25f, 0.01f, 0f);
                    else
                    {
                        Push(ref k, r, -0.02f, 0.3f);
                        k.Aim = 1f;                                                      //   the finger jabbed out
                    }
                    break;
            }
            return k;
        }

        //Push : the whole body moved by (x, y), the head a little more and the hip only follow of it (so it leans into it)
        private static void Push(ref KeyPose k, float x, float y, float follow)
        {
            k.Head += new Vector2(x * 1.3f, y);
            k.Chest += new Vector2(x, y);
            k.Hip += new Vector2(x * follow, y * 0.7f);
            k.HandA += new Vector2(x, y);
            k.HandB += new Vector2(x, y);
        }

        //Shadow : THE NAMELESS MASTER's brother, the master's chest a moment ago (AttackShadowLate), so every move it makes,
        //blows too, the shadow makes again after it. A short memory of where the chest was, one place a frame (60 a second).
        private readonly Vector2[] trail = new Vector2[40];
        private int trailAt;
        private int trailCount;

        private Vector2 Shadow(Vector2 chest)
        {
            trailAt = (trailAt + 1) % trail.Length;
            trail[trailAt] = chest;
            if (trailCount < trail.Length) trailCount++;
            int back = Math.Min(trailCount - 1, (int)Math.Round(BattleRules.AttackShadowLate * 60f));
            return trail[(trailAt - back + trail.Length) % trail.Length];
        }

        //======================================================================================
        //Shards : a beaten enemy breaks
        //======================================================================================

        //Shatter : the parts become pieces thrown out and up, then pulled into the hole (SILENCED)
        private void Shatter()
        {
            shardCount = 0;
            if (IsLook)
            {
                SetLook();
                releaseAt = LookCore();
            }
            else releaseAt = CorePoint();
            int count = Of == Sort.Boss ? 36 : 20;
            float scale = Height / 256f;
            for (int i = 0; i < count && shardCount < shards.Length; i++)
            {
                int part = i % 8;
                float a = Hash(i, 7) * MathHelper.TwoPi;
                float power = (140f + 260f * Hash(i, 11)) * scale;
                Shard s;
                s.At = body[part].At + new Vector2(Hash(i, 3) - 0.5f, Hash(i, 5) - 0.5f) * (Height * 0.25f);
                s.Speed = new Vector2((float)Math.Cos(a) * power + 120f * scale * -Facing, (float)Math.Sin(a) * power - 220f * scale);   // thrown back, away from the band
                s.Angle = a;
                s.Spin = (Hash(i, 13) - 0.5f) * 14f;
                s.Size = (0.03f + 0.05f * Hash(i, 17)) * Height;
                s.Shape = i % 3;
                shards[shardCount++] = s;
            }
        }

        //Update Shards : gravity, the floor, a little drag, and after a moment the hole pulls them in
        private void UpdateShards(float dt)
        {
            UpdateChips(dt);
            if (shardCount == 0) return;
            float floor = Feet.Y;
            float pullIn = MathHelper.Clamp((moveTime - BattleRules.FigureShardHang) / 0.6f, 0f, 1f);
            for (int i = 0; i < shardCount; i++)
            {
                Shard s = shards[i];
                s.Speed.Y += BattleRules.FigureGravity * (1f - pullIn) * dt;
                s.Speed += (Hole - s.At) * (BattleRules.FigureShardPull * pullIn * pullIn * dt);
                s.Speed *= 1f - Math.Min(0.9f, dt * (1f + pullIn * 3f));
                s.At += s.Speed * dt;
                s.Angle += s.Spin * dt;
                if (pullIn < 0.5f && s.At.Y > floor && s.Speed.Y > 0f)            // BOUNCE on the floor, losing most of the speed
                {
                    s.At.Y = floor;
                    s.Speed.Y *= -BattleRules.FigureShardBounce;
                    s.Speed.X *= 0.6f;
                    s.Spin *= 0.5f;
                }
                shards[i] = s;
            }
        }

        //======================================================================================
        //Draw : TACET
        //======================================================================================

        private static readonly Color BodyInk = new Color(20, 20, 24);       // a little above the hole's black, so a body reads in front of it

        //Rim Share : how bright the pale rim of a black shell is, a share of the paper white. HOLE LIGHT : the darker it is
        //behind (Behind), the more the hole's light catches the rim, so a black body still reads over the black of the hole.
        private float RimShare(float light)
        {
            return MathHelper.Lerp(BattleRules.FigureTacetRim, BattleRules.FigureHoleRim, Behind) * light;
        }

        //Rim Edge : the rim's width, page pixels, wider where the hole's light catches it
        private float RimEdge()
        {
            return BattleRules.FigureTacetEdge + BattleRules.FigureHoleEdge * Behind;
        }

        //Draw Tacet : a look draws itself shape by shape (DrawLook), a boss from its card of the sketch (Draw<Name>, white as
        //THE MONUMENT until THE TURN). A beaten one is only its shards and the light it lets go.
        private void DrawTacet(float alpha, float flash, float light, float mark)
        {
            Color fill = Color.Lerp(BodyInk, Color.White, flash) * alpha;
            Color rim = Color.Lerp(Palette.Paper * RimShare(light), Color.White, mark) * alpha;
            Color edge = Color.Lerp(Palette.Paper * light, Color.White, mark) * alpha;
            if (shardCount > 0)
            {
                DrawRelease(alpha);
                DrawShards(BattleRules.FigureTacetEdge * 2f, IsLook ? rim : edge, fill);
                return;
            }
            if (down) return;
            if (IsLook)
            {
                lookEdge = RimEdge() * (1f + 1.5f * mark);
                lookRim = rim;
                lookFill = fill;
                DrawLook(alpha, light);
                DrawChips(alpha);
                return;
            }
            SetBossInks(alpha, light, flash, mark);                                  // every boss is drawn from the round 6 sketch
            BossWings(alpha);                                                        // WINGS (Worldless) : behind every boss
            switch (design)
            {
                case Requiem: DrawRequiem(alpha); break;
                case Nameless: DrawNameless(alpha); break;
                case Devil: DrawDevil(alpha); break;
                case Crown: DrawCrown(alpha); break;
                case Staff: DrawStaff(alpha); break;
                default: DrawCantor(alpha); break;
            }
            lookFill = bossLit * alpha;                                              // CHIPS : bits of its shell, white or black
            DrawChips(alpha);
        }

        private void DrawShards(float grow, Color edge, Color fill)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < shardCount; i++)
                {
                    Shard s = shards[i];
                    float size = s.Size + (pass == 0 ? grow : 0f);
                    Color c = pass == 0 ? edge : fill;
                    Vector2 at = s.At + Shift;
                    Vector2 d = new Vector2((float)Math.Cos(s.Angle), (float)Math.Sin(s.Angle));
                    if (s.Shape == 1) Shapes.Box(at, size, size * 0.7f, s.Angle, c);
                    else if (s.Shape == 2) Shapes.Kite(at - d * size, at + d * size, size * 0.45f, 0.4f, c);
                    else Shapes.Polygon(at, size * 0.7f, 4, s.Angle, c);
                }
            }
        }

        //Draw Release (RELEASE) : the light the shell held, freed, rises as a ring and fades, a flash where it was
        private void DrawRelease(float alpha)
        {
            float t = moveTime / BattleRules.FigureReleaseTime;
            if (t >= 1f) return;
            float e = Ease(t);
            float fade = (1f - t) * (1f - t);
            Vector2 at = releaseAt + Shift - new Vector2(0f, 0.35f * e * Height);
            Shapes.Ring(at, (0.03f + 0.32f * e) * Height, (0.004f + 0.018f * (1f - t)) * Height, Color.White * (alpha * fade));
            if (t < 0.3f) Shapes.Polygon(releaseAt + Shift, CoreSize * (1f + 3f * t), 4, 0f, Color.White * (alpha * (1f - t / 0.3f)));
        }

        //======================================================================================
        //The core : TACET's heart
        //======================================================================================

        //Heart (block D, 2 Nov) : where this TACET figure's white core is now, page pixels in the fighters' layer. The moves'
        //tells use it (Screens/BattleScreen.Tells.cs : CHARGE draws its shards into it, BREAK bursts out of it).
        public Vector2 Heart
        {
            get
            {
                if (Of == Sort.Musician) return body[Chest].At;
                SetLook();
                return Of == Sort.Boss ? CorePoint() + Shift : LookCore();
            }
        }

        //Core Point : a boss's heart, a small white diamond of light (the Worldless way : every creature has one small glowing
        //core, 13.86). Where it sits says something about the boss : where REQUIEM's unfinished lines stop, in the frame where
        //THE NAMELESS MASTER's name should be, on THE DEVIL'S STRING's string, at the tip of THE DEAF CROWN's stick, where THE
        //STAFF's staff comes down, in the middle of THE BLIND CANTOR's chest. The looks have their own (LookCore).
        private Vector2 CorePoint()
        {
            SetLook();
            switch (design)
            {
                case Requiem: return S(32, 160) - Shift;
                case Nameless: return S(120, 161) - Shift;
                case Devil: return DevilCore() - Shift;
                case Crown: return S(176, 109) - Shift;
                case Staff: return StaffFoot() - Shift;
                case Cantor: return S(120, 146) - Shift;
                default: return body[Chest].At;
            }
        }

        //Core Size : the bosses' hearts are bigger
        private float CoreSize
        {
            get { return (Of == Sort.Boss ? (shellOn ? 0.024f : 0.032f) : 0.034f) * Height; }   // the black form's core is brighter
        }

        //======================================================================================
        //TACET LOOK (round 6 part 2, 2 Nov, docs/BETA1_DIRECTION.md 32.5) : the four looks, from the sketch
        //======================================================================================

        //LOOK FRAME : the sketch is a card 240 x 300, the floor at 262, the tallest look 222 high, facing left (the band).
        //Its middle point (120, 151) rides on the Chest spring and the whole look leans the way the line from the Chest spring
        //to the Hip spring leans, so the bob, the lunge and every blow move every shape. Points on the floor stay put (SF).
        private const float LookMiddle = 120f;
        private const float LookFloor = 262f;
        private const float LookTall = 222f;
        private const float LookChest = 151f;

        private Vector2 lookAt;
        private float lookK;                  // page pixels for one unit of the sketch
        private float lookLean;
        private float lookCos = 1f;
        private float lookSin;
        private float lookStrike;             // 0 to 1 at the height of its own blow
        private float lookEdge;               // the pale rim's width, page pixels
        private Color lookRim;
        private Color lookFill;
        private static readonly Vector2[] grown = new Vector2[Shapes.MaxPoints];
        private static readonly Vector2[] bent = new Vector2[16];

        //Set Look : the frame for this frame (see LOOK FRAME)
        private void SetLook()
        {
            lookK = Height / LookTall;
            lookAt = body[Chest].At;
            Vector2 now = body[Hip].At - body[Chest].At;                         // at rest straight down
            lookLean = 0f;
            if (now.LengthSquared() > 0.01f) lookLean = MathHelper.WrapAngle((float)Math.Atan2(now.Y, now.X) - MathHelper.PiOver2);
            lookLean = MathHelper.Clamp(lookLean, -0.4f, 0.4f);
            lookCos = (float)Math.Cos(lookLean);
            lookSin = (float)Math.Sin(lookLean);
            lookStrike = MathHelper.Clamp(pose.Light, 0f, 1f);                  // ONE NOTE ONE BLOW : the light of its blow
        }

        //S : a point of the sketch on the body (it moves with it)
        private Vector2 S(float x, float y)
        {
            float dx = (x - LookMiddle) * lookK * -Facing;
            float dy = (y - LookChest) * lookK;
            return lookAt + new Vector2(dx * lookCos - dy * lookSin, dx * lookSin + dy * lookCos) + Shift;
        }

        //SF : a point of the sketch that stays put on the stage (the floor, THE WEIGHT's beam and the ends of its strings)
        private Vector2 SF(float x, float y)
        {
            return Feet + new Vector2((x - LookMiddle) * lookK * -Facing, (y - LookFloor) * lookK) + Shift;
        }

        //Shell : the points in line drawn as a piece of TACET's black shell, its pale rim first (the points pushed out from
        //their middle by the rim's width), then the black over it. fade lets a falling bit go.
        private void Shell(int n, float fade)
        {
            Vector2 middle = Vector2.Zero;
            for (int i = 0; i < n; i++) middle += line[i];
            middle /= n;
            for (int i = 0; i < n; i++)
            {
                Vector2 out1 = line[i] - middle;
                if (out1.LengthSquared() > 0.0001f) out1.Normalize();
                grown[i] = line[i] + out1 * lookEdge;
            }
            Shapes.Fill(grown, n, lookRim * fade);
            Shapes.Fill(line, n, lookFill * fade);
        }

        private void ShellQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            line[0] = a;
            line[1] = b;
            line[2] = c;
            line[3] = d;
            Shell(4, 1f);
        }

        private void ShellOval(Vector2 at, float rx, float ry)
        {
            Shapes.Ellipse(at, rx * lookK + lookEdge, ry * lookK + lookEdge, lookLean, lookRim, lookRim);
            Shapes.Ellipse(at, rx * lookK, ry * lookK, lookLean, lookFill, lookFill);
        }

        //Crumb : a small three cornered bit of shell (floating round it, falling off it)
        private void Crumb(Vector2 at, float size, float turn, float fade)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = turn + i * 2.1f + (i == 1 ? 0.45f : 0f);
                line[i] = at + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * size;
            }
            Shell(3, fade);
        }

        //Limb : LEVEL 2's thin legs and arms, rim then black
        private void Limb(Vector2 a, Vector2 b, float w0, float w1)
        {
            Shapes.Taper(a, b, w0 * Height + lookEdge * 2f, w1 * Height + lookEdge * 2f, lookRim);
            Shapes.Taper(a, b, w0 * Height, w1 * Height, lookFill);
        }

        private Vector2 PartAt(int part)
        {
            return body[part].At + Shift;
        }

        //Light Line : a thin white line through n points of line (a slit, a crack, a string)
        private void LightLine(int n, float width, Color c)
        {
            for (int i = 0; i < n; i++) lineW[i] = width;
            Shapes.Stroke(line, lineW, n, c, c, false);
        }

        //Diamond : the white core's shape, half as wide and half as tall as given in sketch units
        private void Diamond(Vector2 at, float w, float h, Color c)
        {
            line[0] = at + new Vector2(0f, -h * lookK);
            line[1] = at + new Vector2(w * lookK, 0f);
            line[2] = at + new Vector2(0f, h * lookK);
            line[3] = at + new Vector2(-w * lookK, 0f);
            Shapes.Fill(line, 4, c);
        }

        //Oval Line : a thin ring drawn as a line (a halo, an open mouth)
        private void OvalLine(Vector2 at, float rx, float ry, float width, Color c)
        {
            const int count = 18;
            for (int i = 0; i < count; i++)
            {
                float t = i * MathHelper.TwoPi / count;
                float x = (float)Math.Cos(t) * rx * lookK;
                float y = (float)Math.Sin(t) * ry * lookK;
                line[i] = at + new Vector2(x * lookCos - y * lookSin, x * lookSin + y * lookCos);
                lineW[i] = width * lookK;
            }
            Shapes.Stroke(line, lineW, count, c, c, true);
        }

        //Look Core : where its white core is (the brightest thing on the stage)
        private Vector2 LookCore()
        {
            switch (design)
            {
                case Static: return S(120f + NoiseShiver(9).X, 127f);
                case Damper: return S(120, 150);
                case Choir: return S(120, 70f + ChoirBob(0));
                default: return S(108, 150);
            }
        }

        private void DrawLook(float alpha, float light)
        {
            SetLook();
            if (noiseBlink > 0f) alpha *= 0.3f;                                    // THE NOISE : it blinks out as it jumps (Steps)
            float glow = alpha * (0.6f + 0.4f * light);
            switch (design)
            {
                case Static: LookNoise(alpha, glow); break;
                case Damper: LookWeight(alpha, glow); break;
                case Choir: LookChoir(alpha, glow); break;
                default: LookMute(alpha, glow); break;
            }
        }

        //THE MUTE (hush) : the cone it is, the slit of light with the core behind it, the black bar across the slit like a
        //finger on its lips (on its blow the finger lifts : the "shh" lets go), the light leaking from under its hem
        private void LookMute(float alpha, float glow)
        {
            Color white = Color.White * glow;
            line[0] = S(78, 232);                                                  // the light under its hem, spilling on the floor
            line[1] = S(164, 232);
            line[2] = SF(186, 262);
            line[3] = SF(56, 262);
            Shapes.Fill(line, 4, Color.White * (0.08f * glow));
            if (Level == 2)
            {
                Limb(S(108, 226), PartAt(FootA), 0.026f, 0.012f);
                Limb(S(134, 226), PartAt(FootB), 0.026f, 0.012f);
            }
            for (int b = 0; b < 3; b++) Crumb(bits[b].At + Shift, (5f + b * 1.5f) * lookK, clock * (0.5f + 0.2f * b) + b * 2f, 1f);
            int n = Cubic(S(128, 40), S(134, 70), S(152, 150), S(166, 230), 12, line, 0);
            n = CubicMore(S(166, 230), S(142, 238), S(100, 238), S(76, 230), 8, line, n);
            n = CubicMore(S(76, 230), S(92, 160), S(110, 90), S(128, 40), 12, line, n);
            Shell(n - 1, 1f);                                                      // the last point is the first again
            n = Cubic(S(108, 118), S(105, 140), S(105, 160), S(109, 182), 8, line, 0);
            LightLine(n, 2.4f * lookK, white);                                     // the slit
            Diamond(S(108, 150), 5f, 7f, Color.White * alpha);                     // the core
            Shapes.Box(S(108f - 26f * pose.Aim, 137.8f - 2f * pose.Aim), 24f * lookK, 3.6f * lookK, lookLean, lookFill);   // the finger, jabbed out on its blow ("shh!")
            Shapes.Ellipse(S(121, 232), 46f * lookK, 3.6f * lookK, lookLean, white * 0.85f, white * 0.85f);   // the hem's light
            DrawCracks(MuteCracks, white);
        }

        private static readonly float[] MuteCracks = { 124, 60, 131, 74, 127, 80, 146, 136, 139, 150, 144, 158, 96, 196, 104, 206, 99, 216 };

        //THE NOISE (static) : eight bits of shell (their corners from the sketch), white lines of a dead radio between them,
        //each bit jumping on its own beat, the core blinking with a ghost beside it, crumbs falling off it all the time.
        //Its CRACKS are bits it loses, in this order.
        private static readonly float[] NoiseBits =
        {
            96, 96, 118, 90, 112, 112, 94, 110,
            124, 86, 148, 94, 140, 112, 122, 104,
            84, 118, 104, 116, 100, 138, 80, 134,
            130, 116, 154, 120, 150, 140, 126, 136,
            104, 140, 124, 142, 120, 164, 100, 160,
            134, 146, 152, 150, 146, 168, 130, 162,
            62, 94, 78, 96, 76, 106, 60, 104,
            160, 100, 174, 104, 170, 116, 158, 112,
        };
        private static readonly int[] NoiseLoses = { 6, 7, 5 };
        private static readonly float[] NoiseLines = { 70, 98, 101, 116, 168, 99, 88, 126, 114, 78, 160, 128, 96, 132, 139, 110, 156, 152, 92, 122, 166 };

        //Noise Shiver : how far bit k has jumped this moment, in sketch units (now and then a big jump sideways)
        private Vector2 NoiseShiver(int k)
        {
            if (design != Static) return Vector2.Zero;
            int step = (int)((clock + k * 0.37f) * 7f);
            float x = (Hash(step, k + 40) - 0.5f) * 1.6f;
            if (Hash(step, k) > 0.74f) x += (Hash(step, k + 19) - 0.5f) * 14f;
            return new Vector2(x, (Hash(step, k + 60) - 0.5f) * 1.2f);
        }

        private bool NoiseGone(int k)
        {
            for (int i = 0; i < crackSeen && i < NoiseLoses.Length; i++)
                if (NoiseLoses[i] == k) return true;
            return false;
        }

        private Vector2 NoiseMiddle(int k)
        {
            int o = k * 8;
            return new Vector2(NoiseBits[o] + NoiseBits[o + 2] + NoiseBits[o + 4] + NoiseBits[o + 6], NoiseBits[o + 1] + NoiseBits[o + 3] + NoiseBits[o + 5] + NoiseBits[o + 7]) * 0.25f;
        }

        private void LookNoise(float alpha, float glow)
        {
            Color white = Color.White * glow;
            if (Level == 2)
            {
                Limb(S(110, 164), PartAt(FootA), 0.02f, 0.01f);
                Limb(S(134, 166), PartAt(FootB), 0.02f, 0.01f);
            }
            for (int k = 0; k < 7; k++)                                              // the dead radio's lines, blinking
            {
                if (Hash((int)(clock * 12f) + k * 5, k) < 0.25f) continue;
                float jump = (Hash((int)(clock * 7f), k + 30) - 0.5f) * 6f;
                float y = NoiseLines[k * 3 + 2];
                Shapes.Line(S(NoiseLines[k * 3] + jump, y), S(NoiseLines[k * 3 + 1] + jump, y), 0.8f * lookK, white * 0.6f);
            }
            for (int k = 0; k < 8; k++)
            {
                if (NoiseGone(k)) continue;
                Vector2 spread = (NoiseMiddle(k) - new Vector2(120f, 127f)) * (lookStrike * 0.35f) + NoiseShiver(k);   // its blow : the cloud bursts out
                int o = k * 8;
                ShellQuad(S(NoiseBits[o] + spread.X, NoiseBits[o + 1] + spread.Y), S(NoiseBits[o + 2] + spread.X, NoiseBits[o + 3] + spread.Y),
                          S(NoiseBits[o + 4] + spread.X, NoiseBits[o + 5] + spread.Y), S(NoiseBits[o + 6] + spread.X, NoiseBits[o + 7] + spread.Y));
            }
            Vector2 core = LookCore();
            if (Hash((int)(clock * 9f), 3) > 0.15f) Diamond(core, 6f, 7f, Color.White * alpha);
            Diamond(core + S(131, 127) - S(120, 127), 6f, 7f, Color.White * (0.3f * alpha));   // the ghost of a bad signal
            for (int k = 0; k < 3; k++)                                              // crumbs falling off it, over and over
            {
                float t = (clock * 0.45f + k * 0.33f + seed) % 1f;
                float x = 104f + k * 16f + (Hash(k, 71) - 0.5f) * 10f;
                Vector2 at = Vector2.Lerp(S(x, 176), SF(x - 6f, 258), t * t);
                Crumb(at, (3f + k) * lookK, t * 5f + k, 1f - t);
            }
        }

        //THE WEIGHT (the_damper) : the beam and its two cords, the light squeezed out at the sides, the heavy block, the light
        //under it with the core, and the four strings it presses down until they sag (harder on its blow)
        private void LookWeight(float alpha, float glow)
        {
            Color white = Color.White * glow;
            Shapes.Line(SF(56, 40), SF(184, 40), 3f * lookK, new Color(44, 44, 51) * alpha);              // the beam stays put
            Shapes.Line(SF(100, 40), S(100, 88), 1.4f * lookK, new Color(74, 74, 83) * alpha);           //   the cords hold the block
            Shapes.Line(SF(140, 40), S(140, 88), 1.4f * lookK, new Color(74, 74, 83) * alpha);
            line[0] = S(84, 150);                                                  // the light squeezed out at both sides
            line[1] = SF(30, 142);
            line[2] = SF(30, 156);
            Shapes.Fill(line, 3, Color.White * (0.14f * glow));
            line[0] = S(156, 150);
            line[1] = SF(210, 156);
            line[2] = SF(210, 142);
            Shapes.Fill(line, 3, Color.White * (0.14f * glow));
            if (Level == 2)
            {
                Limb(S(96, 140), PartAt(FootA), 0.04f, 0.026f);
                Limb(S(144, 140), PartAt(FootB), 0.04f, 0.026f);
            }
            float press = pose.Squash;                                             // the block, squashed as it presses (stretched as it is lifted)
            int n = RoundBox(120, 116f + 4f * press, 76f * (1f + 0.08f * press), 60f * (1f - 0.14f * press), 10);
            Shell(n, 1f);
            Shapes.Ellipse(S(120, 150), 34f * lookK, 3f * lookK, lookLean, white * 0.9f, white * 0.9f);
            Diamond(S(120, 150), 4f, 4f, Color.White * alpha);
            DrawCracks(WeightCracks, white);
            for (int k = 0; k < 4; k++)                                              // the strings
            {
                float y = 156f + k * 6f;
                Shapes.Bend(S(84, y), S(120, 178f + k * 2f + lookStrike * 16f), S(156, y), 9, bent);
                line[0] = SF(34, y);
                for (int i = 0; i < 9; i++) line[1 + i] = bent[i];
                line[10] = SF(206, y);
                LightLine(11, 1f * lookK, white * 0.8f);
            }
        }

        private static readonly float[] WeightCracks = { 96, 98, 104, 108, 100, 114, 140, 92, 134, 104, 141, 112, 118, 120, 124, 131, 119, 140 };

        //Round Box : a box with round corners round (cx, cy) of the sketch into line, returns how many points
        private int RoundBox(float cx, float cy, float w, float h, float r)
        {
            int n = 0;
            for (int c = 0; c < 4; c++)                                            // top left, top right, bottom right, bottom left
            {
                float mx = c == 1 || c == 2 ? cx + w * 0.5f - r : cx - w * 0.5f + r;
                float my = c < 2 ? cy - h * 0.5f + r : cy + h * 0.5f - r;
                float from = MathHelper.Pi + c * MathHelper.PiOver2;
                for (int i = 0; i < 5; i++)
                {
                    float a = from + i * MathHelper.PiOver2 / 4f;
                    line[n++] = S(mx + (float)Math.Cos(a) * r, my + (float)Math.Sin(a) * r);
                }
            }
            return n;
        }

        //THE EMPTY CHOIR (the_mute_choir) : the robe's blades from its body down to the floor, six heads in rows, each open
        //mouth a hollow ring (all of them light on its blow), the core in the mouth of the top one under a thin halo
        private static readonly float[] ChoirHeads = { 120, 66, 102, 96, 138, 96, 84, 126, 120, 126, 156, 126 };
        private static readonly float[] ChoirBlades = { 86, 96, 70, 54, 100, 110, 98, 84, 115, 125, 127, 113, 130, 140, 156, 142, 144, 154, 186, 170 };

        private float ChoirBob(int k)
        {
            return (float)Math.Sin(clock * 2f + k * 1.3f) * 1.4f;
        }

        private void LookChoir(float alpha, float glow)
        {
            Color white = Color.White * glow;
            if (Level == 2)
            {
                Limb(S(96, 140), PartAt(HandA), 0.018f, 0.008f);
                Limb(S(144, 140), PartAt(HandB), 0.018f, 0.008f);
            }
            for (int k = 0; k < 5; k++)                                              // the blades : on the body at the top, on the floor at the foot
            {
                int o = k * 4;
                ShellQuad(S(ChoirBlades[o], 142), S(ChoirBlades[o + 1], 142), SF(ChoirBlades[o + 2], 252), SF(ChoirBlades[o + 3], 252));
            }
            Crumb(bits[0].At + Shift, 5f * lookK, clock * 0.6f, 1f);
            for (int k = 0; k < 6; k++) ShellOval(S(ChoirHeads[k * 2], ChoirHeads[k * 2 + 1] + ChoirBob(k)), 13f, 16f);
            float sing = 0.45f + 0.55f * lookStrike;
            for (int k = 1; k < 6; k++) OvalLine(S(ChoirHeads[k * 2], ChoirHeads[k * 2 + 1] + 4f + ChoirBob(k)), 4f, 5f, 1.4f, white * sing);
            OvalLine(S(120, 44f + ChoirBob(0)), 14f, 4f, 0.9f, white * 0.6f);         // the halo
            OvalLine(LookCore(), 4f, 5f, 1.6f, white);
            Shapes.Circle(LookCore(), 1.8f * lookK + 0.6f, Color.White * alpha);
            DrawCracks(ChoirCracks, white);
            if (Level == 1 && pose.Show > 0.02f)                                     // its blow : two hands appear, raised, then closed for the cut off
            {
                ChoirHand(PartAt(HandA), pose.Show);
                ChoirHand(PartAt(HandB), pose.Show);
            }
        }

        //Choir Hand : a floating black hand of THE EMPTY CHOIR's conductor (LEVEL 1 shows it only in a blow). Open : a palm
        //and three fingers up. Closed (pose.Aim 1, the cut off) : a fist.
        private void ChoirHand(Vector2 at, float fade)
        {
            float open = 1f - MathHelper.Clamp(pose.Aim, 0f, 1f);
            Shapes.Ellipse(at, 7f * lookK + lookEdge, 6f * lookK + lookEdge, lookLean, lookRim * fade, lookRim * fade);
            Shapes.Ellipse(at, 7f * lookK, 6f * lookK, lookLean, lookFill * fade, lookFill * fade);
            if (open < 0.05f) return;
            for (int f = 0; f < 3; f++)
            {
                float a = -MathHelper.PiOver2 + (f - 1) * 0.42f;
                Vector2 way = new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
                Vector2 tip = at + way * ((7f + 9f * open) * lookK);
                Vector2 side = new Vector2(-way.Y, way.X) * (2f * lookK);
                line[0] = at + way * (4f * lookK) + side;
                line[1] = tip;
                line[2] = at + way * (4f * lookK) - side;
                Shell(3, fade);
            }
        }

        private static readonly float[] ChoirCracks = { 76, 116, 82, 124, 78, 130, 146, 88, 140, 96, 144, 104, 124, 56, 118, 63, 122, 70 };

        //======================================================================================
        //CRACKS and CHIPS : the shell breaks a little at a time
        //======================================================================================

        //Draw Cracks : one more white crack at 75 50 25 of its life (three points each), the newest running across
        private void DrawCracks(float[] cracks, Color white)
        {
            for (int k = 0; k < crackSeen && k < 3; k++)
            {
                float run = k == crackSeen - 1 ? MathHelper.Clamp(crackClock / BattleRules.FigureCrackTime, 0f, 1f) : 1f;
                int o = k * 6;
                Vector2 a = S(cracks[o], cracks[o + 1]), b = S(cracks[o + 2], cracks[o + 3]), c = S(cracks[o + 4], cracks[o + 5]);
                line[0] = a;
                line[1] = Vector2.Lerp(a, b, Math.Min(1f, run * 2f));
                int n = 2;
                if (run > 0.5f) line[n++] = Vector2.Lerp(b, c, (run - 0.5f) * 2f);
                LightLine(n, 1.1f * lookK + 0.4f, white);
            }
        }

        //Crack Spot : where crack k is (THE NOISE : the bit it lost), for the chips and the glow
        private Vector2 CrackSpot(int k)
        {
            float[] cracks;
            switch (design)
            {
                case Static:
                    Vector2 m = NoiseMiddle(NoiseLoses[Math.Min(k, NoiseLoses.Length - 1)]);
                    return S(m.X, m.Y);
                case Damper: cracks = WeightCracks; break;
                case Choir: cracks = ChoirCracks; break;
                default: cracks = MuteCracks; break;
            }
            int o = Math.Min(k, 2) * 6;
            return S(cracks[o + 2], cracks[o + 3]);
        }

        //CHIPS : small bits of shell a hit knocks off, falling and bouncing, then gone
        private struct ChipBit
        {
            public Vector2 At;
            public Vector2 Speed;
            public float Turn;
            public float Spin;
            public float Size;
            public float Age;
            public bool White;               // a piece of a boss's white shell (THE MONUMENT)
        }
        private readonly ChipBit[] chips = new ChipBit[24];
        private int chipCount;
        private int chipSerial;

        //Chip : n chips from round its core (THE NOISE breaks most easily, twice as many)
        private void Chip(int n)
        {
            Vector2 at = IsLook ? LookCoreNow() : body[Chest].At;
            if (design == Static) n *= 2;
            for (int i = 0; i < n; i++)
            {
                int k = chipSerial++;
                ChipAt(at + new Vector2(Hash(k, 81) - 0.5f, Hash(k, 83) - 0.5f) * (Height * 0.22f), 1);
            }
        }

        private Vector2 LookCoreNow()
        {
            SetLook();
            return LookCore() - Shift;
        }

        private void ChipAt(Vector2 at, int n)
        {
            float scale = Height / 256f;
            for (int i = 0; i < n; i++)
            {
                if (chipCount >= chips.Length) return;
                int k = chipSerial++;
                ChipBit c;
                c.At = at;
                c.Speed = new Vector2((-Facing * (50f + 130f * Hash(k, 85))) * scale, -(70f + 150f * Hash(k, 87)) * scale);   // away from the blow, up
                c.Turn = Hash(k, 89) * MathHelper.TwoPi;
                c.Spin = (Hash(k, 91) - 0.5f) * 16f;
                c.Size = (0.012f + 0.014f * Hash(k, 93)) * Height;
                c.Age = 0f;
                c.White = false;
                chips[chipCount++] = c;
            }
        }

        //Update Chips : gravity, the floor, age. Also CRACKS : one more crack when its life passes 75 50 25, a few chips from it.
        private void UpdateChips(float dt)
        {
            if (Of == Sort.Musician) return;
            int stage = Life < 0.25f ? 3 : (Life < 0.5f ? 2 : (Life < 0.75f ? 1 : 0));
            if (down) stage = crackSeen;
            if (stage > crackSeen)
            {
                crackSeen = stage;
                crackClock = 0f;
                if (IsLook && clock > 0.3f)
                {
                    SetLook();
                    ChipAt(CrackSpot(stage - 1) - Shift, 3);
                }
            }
            else if (stage < crackSeen) crackSeen = stage;                       // healed
            crackClock += dt;
            if (breakFlash > 0f) breakFlash = Math.Max(0f, breakFlash - dt * 1.4f);
            if (ShellOn && Life < 0.5f && !TurnPending && !down)                  // a fight begun below half : the shell goes quietly
            {
                shellDue += dt;
                if (shellDue > 1f) BreakShell(false);
            }
            else shellDue = 0f;

            float scale = Height / 512f;
            int keep = 0;
            for (int i = 0; i < chipCount; i++)
            {
                ChipBit c = chips[i];
                c.Age += dt;
                if (c.Age >= BattleRules.FigureChipLife) continue;
                c.Speed.Y += BattleRules.FigureGravity * scale * dt;
                c.At += c.Speed * dt;
                c.Turn += c.Spin * dt;
                if (c.At.Y > Feet.Y && c.Speed.Y > 0f)                           // BOUNCE
                {
                    c.At.Y = Feet.Y;
                    c.Speed.Y *= -BattleRules.FigureShardBounce;
                    c.Speed.X *= 0.6f;
                }
                chips[keep++] = c;
            }
            chipCount = keep;
        }

        private void DrawChips(float alpha)
        {
            for (int i = 0; i < chipCount; i++)
            {
                ChipBit c = chips[i];
                float fade = 1f - MathHelper.Clamp((c.Age / BattleRules.FigureChipLife - 0.6f) / 0.4f, 0f, 1f);
                if (c.White)
                {
                    Color rim = lookRim, fill = lookFill;
                    lookRim = MonumentShade * alpha;
                    lookFill = MonumentWhite * alpha;
                    Crumb(c.At + Shift, c.Size, c.Turn, fade);
                    lookRim = rim;
                    lookFill = fill;
                }
                else Crumb(c.At + Shift, c.Size, c.Turn, fade * alpha);
            }
        }

        //Glow Look : the four looks' lights into the GLOW picture (they feed the bloom) : the core strongest (and stronger on
        //its blow), the slit, the light under THE WEIGHT, the choir's mouths when it sings, the cracks (its light on the floor is
        //GlowFloor, in GlowTacet)
        private void GlowLook(SpriteBatch sb, float power)
        {
            SetLook();
            float h = Height;
            Vector2 core = LookCore();
            float flare = 1f + lookStrike * 1.5f;
            Gfx.DrawGlow(sb, core.X, core.Y, CoreSize * 5f, Color.White * (power * 1.6f * flare));
            Gfx.DrawGlow(sb, core.X, core.Y, CoreSize * 2f, Color.White * (power * 1.4f));
            Vector2 at;
            switch (design)
            {
                case Damper:
                    at = S(120, 150);
                    Gfx.DrawGlow(sb, at.X, at.Y, 0.16f * h, Color.White * (power * 0.7f * flare));
                    break;
                case Choir:
                    for (int k = 1; k < 6 && lookStrike > 0.05f; k++)
                    {
                        at = S(ChoirHeads[k * 2], ChoirHeads[k * 2 + 1] + 4f);
                        Gfx.DrawGlow(sb, at.X, at.Y, 0.06f * h, Color.White * (power * 1.2f * lookStrike));
                    }
                    break;
                case Hush:
                    at = S(108, 150);
                    Gfx.DrawGlow(sb, at.X, at.Y, 0.1f * h, Color.White * (power * 0.6f));
                    at = S(121, 232);
                    Gfx.DrawGlow(sb, at.X, at.Y, 0.14f * h, Color.White * (power * 0.5f));
                    break;
            }
            for (int k = 0; k < crackSeen && k < 3; k++)
            {
                at = CrackSpot(k);
                Gfx.DrawGlow(sb, at.X, at.Y, 0.05f * h, Color.White * (power * 0.8f));
            }
        }

        //======================================================================================
        //THE MONUMENT pieces : what every boss from the round 6 sketch is made of (round 6 part 3)
        //======================================================================================

        //Set Boss Inks : the colours of a boss drawn from the sketch. THE MONUMENT : lit white, its far side in grey shade,
        //signs cut in black, a dark grey rim. The black form : black, darker on its far side, signs in pale white, the pale rim
        //of TACET, and the white flash when it has just broken.
        private Color bossLit;
        private Color bossDark;
        private Color bossInk;

        private void SetBossInks(float alpha, float light, float flash, float mark)
        {
            SetLook();
            bool white = ShellOn;
            float lift = Math.Max(flash, breakFlash);
            bossLit = white ? Dim(MonumentWhite, 0.6f + 0.4f * light) : Color.Lerp(BodyInk, Color.White, lift);
            bossDark = white ? Dim(MonumentShade, 0.6f + 0.4f * light) : Color.Lerp(new Color(10, 10, 13), Color.White, lift);
            bossInk = white ? MonumentInk : Dim(Palette.Paper, 0.85f);
            lookEdge = (white ? BattleRules.FigureTacetEdge : RimEdge()) * (1f + 1.5f * mark);   // HOLE LIGHT : only the black form needs it
            lookRim = (white ? Dim(MonumentShade, 0.45f) : Color.Lerp(Palette.Paper * RimShare(light), Color.White, mark)) * alpha;
        }

        //Boss Swing : how far its hand (the HandA spring) has swung from where it rests, page pixels, the body's own move taken
        //out (PoseTacet moves the hand on its blow, the spring adds the follow through)
        private Vector2 BossSwing(float restX, float restY)
        {
            return body[HandA].At - Local(restX, restY) - (body[Chest].At - Local(0f, -0.5f));
        }

        //Since Blow : 0 to 1 over the half second after its blow lands, -1 the rest of the time
        private float SinceBlow()
        {
            if (hitAge < 0f || hitAge >= 0.5f) return -1f;
            return hitAge / 0.5f;
        }

        //Robe : a body of the sketch, four corners from its shoulders (top) to its hem (foot). Lit on the band's side, its far
        //half in shade (the sketch's two greys, split at its middle line). A hem on the floor stays on the floor.
        private void Robe(float top, float topLeft, float topRight, float foot, float footLeft, float footRight, float alpha)
        {
            bool floor = foot >= LookFloor;
            lookFill = bossLit * alpha;
            line[0] = S(topLeft, top);
            line[1] = S(topRight, top);
            line[2] = floor ? SF(footRight, foot) : S(footRight, foot);
            line[3] = floor ? SF(footLeft, foot) : S(footLeft, foot);
            Shell(4, 1f);
            line[0] = S(LookMiddle, top);
            line[3] = floor ? SF(LookMiddle, foot) : S(LookMiddle, foot);
            Shapes.Fill(line, 4, bossDark * alpha);
        }

        //Boss Wings (WINGS, Worldless, the user 3 Nov) : three crescent blades a side fanning up from behind the shoulders,
        //breathing slowly. White and grey on the monument, black with a thin white edge once it has broken.
        private void BossWings(float alpha)
        {
            float a = alpha * BattleRules.BossWingsLight;
            if (a <= 0f) return;
            float breathe = (float)Math.Sin(clock * 0.9f) * 4f;
            for (int side = -1; side <= 1; side += 2)
                for (int b = 2; b >= 0; b--)
                {
                    Vector2 root = S(120f + side * 12f, 118f + b * 5f);
                    Vector2 tip = S(120f + side * (74f + b * 16f + breathe * 0.5f), 34f + b * 30f - breathe);
                    Vector2 bend = S(120f + side * (40f + b * 10f), 70f + b * 22f);
                    Color body = (b == 0 ? bossLit : Color.Lerp(bossLit, bossDark, 0.45f)) * a;
                    Shapes.Kite(root, Vector2.Lerp(bend, tip, 0.15f), (9f - b * 1.5f) * lookK, 0.45f, body);
                    Shapes.Kite(Vector2.Lerp(root, bend, 0.85f), tip, (7f - b * 1.5f) * lookK, 0.3f, body);
                    Shapes.Taper(Vector2.Lerp(root, bend, 0.6f), tip, 1.4f * lookK, 0.2f * lookK, Color.White * ((ShellOn ? 0.45f : 0.6f) * a));
                }
        }

        //Boss Head : an oval of the sketch, its far half in shade. HALO (Worldless) : a thin ring round it and four ticks.
        private void BossHead(float x, float y, float rx, float ry, float alpha)
        {
            Vector2 halo = S(x, y - ry * 0.1f);
            float r = (rx + ry) * 1.45f * lookK;
            Color ring = (ShellOn ? Color.Lerp(bossLit, Color.White, 0.35f) : Palette.Paper) * (0.75f * alpha);
            Shapes.Ring(halo, r, 1.6f * lookK, ring);
            for (int t = 0; t < 4; t++)
            {
                float an = clock * 0.35f + t * MathHelper.PiOver2;
                Vector2 d = new Vector2((float)Math.Cos(an), (float)Math.Sin(an));
                Shapes.Line(halo + d * (r - 3f * lookK), halo + d * (r + 5f * lookK), 1.4f * lookK, ring);
            }
            lookFill = bossLit * alpha;
            ShellOval(S(x, y), rx, ry);
            for (int i = 0; i <= 10; i++)
            {
                float t = -MathHelper.PiOver2 + i * MathHelper.Pi / 10f;
                line[i] = S(x + (float)Math.Cos(t) * rx, y + (float)Math.Sin(t) * ry);
            }
            Shapes.Fill(line, 11, bossDark * alpha);
        }

        //Boss Core : the white diamond (w and h half its size in sketch units), bigger once the white shell is gone
        private void BossCore(Vector2 at, float w, float h, float alpha)
        {
            float grow = ShellOn ? 1f : 1.25f;
            Diamond(at, w * grow, h * grow, Color.White * alpha);
        }

        //Boss Cracks : the cracks of its card (x y pairs). In the white shell they come one by one as it is hit (CRACKS at 75 and
        //50 of its life) in black. On the black form the first two are always there in white, a third at 25.
        private void BossCracks(float[][] cracks, float alpha)
        {
            bool white = ShellOn;
            Color c = (white ? bossInk : Color.White) * alpha;
            for (int k = 0; k < cracks.Length; k++)
            {
                if (white ? k >= crackSeen : (k >= 2 && k >= crackSeen)) break;
                int n = cracks[k].Length / 2;
                for (int i = 0; i < n; i++) line[i] = S(cracks[k][i * 2], cracks[k][i * 2 + 1]);
                LightLine(n, (white ? 1.4f : 1.6f) * lookK, c);
            }
        }

        //Blade : a curved piece of the sketch between two cubic curves (a scythe, a tail, the ends of a blindfold). The first runs
        //from root to tip, the second back from tip to root. Drawn as one stroke along the middle as wide as the gap, because
        //the fan of Shapes.Fill would spill out of its hollow side. rim : a shell's pale rim first (cloth has none).
        private void Blade(Vector2 a0, Vector2 a1, Vector2 a2, Vector2 a3, Vector2 b0, Vector2 b1, Vector2 b2, Vector2 b3, Color fill, bool rim)
        {
            const int count = 12;
            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                Vector2 p = Bez(a0, a1, a2, a3, t), q = Bez(b0, b1, b2, b3, 1f - t);
                line[i] = (p + q) * 0.5f;
                lineW[i] = Vector2.Distance(p, q) + (rim ? lookEdge * 2f : 0f);
            }
            if (rim)
            {
                Shapes.Stroke(line, lineW, count, lookRim, lookRim, false);
                for (int i = 0; i < count; i++) lineW[i] -= lookEdge * 2f;
            }
            Shapes.Stroke(line, lineW, count, fill, fill, false);
        }

        //Bez : one point of a cubic BEZIER from a to d pulled by b and c (t 0 to 1)
        private static Vector2 Bez(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            float u = 1f - t;
            return a * (u * u * u) + b * (3f * u * u * t) + c * (3f * u * t * t) + d * (t * t * t);
        }

        //======================================================================================
        //THE DEAF CROWN (round 6 part 3, from the sketch docs/art_refs/concept_v6.html, docs/BETA1_DIRECTION.md 32.6)
        //======================================================================================

        //Draw Crown : the bust everyone knows of Beethoven. A V of a frown, a crown of black thorns over both ears and none in
        //front (it cannot hear), a stick held in its teeth (he heard through his jaw, a stick on the piano) whose tip holds the
        //core, a great hand raised to give orders. THE MONUMENT : white, lit from the band's side, cracking as it is hit. Broken
        //at THE TURN : the same shape in black, cracks of lightning across it (THE NINTH), the brows and the stick in white.
        private void DrawCrown(float alpha)
        {
            bool white = ShellOn;
            Color lit = bossLit, dark = bossDark, ink = bossInk;
            Vector2 hand = BossSwing(-0.36f, -0.69f);                              // the hand's own swing

            lookFill = lit * alpha;                                                // the robe of the bust, its far side in shade
            line[0] = S(64, 140); line[1] = S(176, 140); line[2] = SF(206, 262); line[3] = SF(34, 262);
            Shell(4, 1f);
            line[0] = S(120, 140); line[1] = S(176, 140); line[2] = SF(206, 262); line[3] = SF(120, 262);
            Shapes.Fill(line, 4, dark * alpha);
            int n = Cubic(S(89, 138), S(104, 154), S(136, 154), S(151, 138), 10, line, 0);   // the collar
            for (int i = 0; i < n; i++) lineW[i] = 15f * lookK;
            Shapes.Stroke(line, lineW, n, Color.Lerp(lit, dark, 0.35f) * alpha, Color.Lerp(lit, dark, 0.35f) * alpha, false);

            lookFill = ink * alpha;                                                // the thorns over the ears, none in front
            CrownThorn(100, 86, 70, 64, 96, 95); CrownThorn(97, 99, 58, 95, 97, 107); CrownThorn(100, 112, 70, 128, 101, 116);
            CrownThorn(140, 86, 170, 64, 144, 95); CrownThorn(143, 99, 182, 95, 143, 107); CrownThorn(140, 112, 170, 128, 139, 116);
            lookFill = lit * alpha;                                                // the head, its far half in shade
            ShellOval(S(120, 98), 23f, 29f);
            for (int i = 0; i <= 12; i++)
            {
                float t = -MathHelper.PiOver2 + i * MathHelper.Pi / 12f;
                line[i] = S(120f + (float)Math.Cos(t) * 23f, 98f + (float)Math.Sin(t) * 29f);
            }
            Shapes.Fill(line, 13, dark * alpha);
            line[0] = S(104, 92); line[1] = S(119, 99); line[2] = S(136, 90);      // the frown
            LightLine(3, 3.6f * lookK, ink * alpha);
            Shapes.Box(S(138.9f, 111.6f), 70f * lookK, 4f * lookK, -0.07f + lookLean, ink * alpha);   // the stick in its teeth

            lookFill = lit * alpha;                                                // the raised hand
            line[0] = S(182, 120) + hand; line[1] = S(214, 112) + hand; line[2] = S(218, 140) + hand; line[3] = S(190, 148) + hand;
            Shell(4, 1f);
            Finger(hand, 184, 120, 180, 64, 190, 118); Finger(hand, 192, 118, 195, 54, 200, 116);
            Finger(hand, 201, 116, 211, 60, 208, 116); Finger(hand, 209, 114, 225, 72, 215, 119);
            Finger(hand, 186, 133, 162, 110, 190, 127);

            Color core = Color.White * alpha;                                      // the core at the stick's tip
            Diamond(S(176, 109), white ? 6f : 7.5f, white ? 7f : 9f, core);

            if (white)                                                             // CRACKS in the white shell, light leaking out
            {
                if (crackSeen >= 1) CrownCrack(0, MonumentInk * alpha, 1.6f);
                if (crackSeen >= 2) CrownCrack(2, MonumentInk * alpha, 1.6f);
                if (crackSeen >= 1)
                {
                    line[0] = S(97, 222); line[1] = S(91, 197);
                    LightLine(2, 1.1f * lookK, Color.White * alpha);
                }
            }
            else                                                                   // THE NINTH : the black form cracked by lightning
            {
                Color bolt = Color.White * alpha;
                CrownCrack(0, bolt, 1.8f);
                CrownCrack(1, bolt, 1.8f);
                if (crackSeen >= 3) CrownCrack(2, bolt, 1.6f);
            }
        }

        private void CrownThorn(float ax, float ay, float bx, float by, float cx, float cy)
        {
            line[0] = S(ax, ay); line[1] = S(bx, by); line[2] = S(cx, cy);
            Shell(3, 1f);
        }

        //Finger : three corners of the sketch moved by a hand's swing (THE DEAF CROWN's fingers, THE DEVIL'S STRING's needles)
        private void Finger(Vector2 hand, float ax, float ay, float bx, float by, float cx, float cy)
        {
            line[0] = S(ax, ay) + hand; line[1] = S(bx, by) + hand; line[2] = S(cx, cy) + hand;
            Shell(3, 1f);
        }

        //Crown Crack : the cracks of the sketch (down its left side, down its right, over its brow)
        private static readonly float[] CrownCracks =
        {
            84, 168, 96, 186, 90, 196, 102, 222, 96, 240,
            156, 162, 146, 182, 154, 194, 142, 224, 142, 224,
            108, 72, 114, 84, 110, 92, 110, 92, 110, 92,
        };

        private void CrownCrack(int k, Color c, float width)
        {
            for (int i = 0; i < 5; i++) line[i] = S(CrownCracks[k * 10 + i * 2], CrownCracks[k * 10 + i * 2 + 1]);
            LightLine(5, width * lookK, c);
        }

        //======================================================================================
        //REQUIEM (round 6 part 3, from the sketch) : the piece Mozart never finished
        //======================================================================================

        //Draw Requiem : a tall slim robe, a small head under a broken halo, a quill held as a blade, tears running down
        //(LACRIMOSA), and from its other hand the five lines of a score that stop half way across : the core is where they
        //stop. THE MONUMENT white, then the black form with the same lines and tears in white.
        private void DrawRequiem(float alpha)
        {
            bool white = ShellOn;
            Color lit = bossLit, dark = bossDark, ink = bossInk;
            Color light = Dim(MonumentWhite, 1f) * (0.7f * alpha);
            Vector2 hand = BossSwing(-0.124f, -0.44f);                             // the quill's own swing

            for (int k = 0; k < 5; k++)                                            // the unfinished lines, their ends never still
            {
                float y = 150f + k * 5f;
                float end = RequiemEnds[k] - (float)Math.Sin(clock * 1.3f + k * 1.7f) * 3f;
                Shapes.Line(S(end, y), S(88, y), 0.9f * lookK, light);
            }
            lookFill = lit * alpha;                                                // the robe, its far side in shade
            int n = Cubic(S(120, 98), S(138, 140), S(152, 208), SF(166, 262), 10, line, 0);
            line[n++] = SF(74, 262);
            n = CubicMore(SF(74, 262), S(88, 208), S(102, 140), S(120, 98), 10, line, n);
            Shell(n - 1, 1f);
            n = Cubic(S(120, 98), S(138, 140), S(152, 208), SF(166, 262), 10, line, 0);
            line[n++] = SF(120, 262);
            Shapes.Fill(line, n, dark * alpha);
            ShellOval(S(120, 80), 13f, 16f);                                       // the head, its far half in shade
            for (int i = 0; i <= 10; i++)
            {
                float t = -MathHelper.PiOver2 + i * MathHelper.Pi / 10f;
                line[i] = S(120f + (float)Math.Cos(t) * 13f, 80f + (float)Math.Sin(t) * 16f);
            }
            Shapes.Fill(line, 11, dark * alpha);
            for (int i = 0; i < 16; i++)                                           // the broken halo, open at the front
            {
                float t = MathHelper.ToRadians(190f + i * 12f);
                line[i] = S(118f + (float)Math.Cos(t) * 24f, 60f + (float)Math.Sin(t) * 7f);
            }
            LightLine(16, 2.4f * lookK, (white ? MonumentWhite : Dim(Palette.Paper, 0.9f)) * alpha);
            lookFill = lit * alpha;
            line[0] = S(140, 72); line[1] = S(146, 75); line[2] = S(141, 78);      //   a piece of it, fallen
            Shell(3, 1f);

            //The quill, a blade. ONE NOTE ONE BLOW : it turns round the hand holding it (cocked back as it winds up, its tip
            //swept down at the band on the blow, one stroke of handwriting)
            quillGrip = S(147.5f, 164.5f) + hand;
            quillTurn = -pose.Aim * 1.9f;
            n = Cubic(Quill(146, 162, hand), Quill(172, 128, hand), Quill(196, 96, hand), Quill(218, 60, hand), 10, line, 0);
            n = CubicMore(Quill(218, 60, hand), Quill(206, 100, hand), Quill(186, 134, hand), Quill(152, 168, hand), 10, line, n);
            Shell(n, 1f);
            Shapes.Line(Quill(150, 166, hand), Quill(216, 62, hand), 1f * lookK, dark * alpha);
            Shapes.Polygon(quillGrip, 6.4f * lookK, 4, lookLean, lit * alpha);                   // the hand on it
            Shapes.Polygon(S(90, 159), 5.7f * lookK, 4, lookLean, lit * alpha);                 // the hand the lines leave
            float after = SinceBlow();
            if (after >= 0f)                                                       // LACRIMOSA : after its stroke, tears fall from the blade's edge
                for (int k = 0; k < 3; k++)
                {
                    Vector2 at = Vector2.Lerp(Quill(152, 168, hand), Quill(214, 64, hand), 0.3f + k * 0.25f) + new Vector2(0f, after * after * 46f * lookK);
                    float size = (3.2f - k * 0.4f) * lookK;
                    Color tear = Color.White * (alpha * (1f - after));
                    Shapes.Circle(at, size * 0.6f, tear);
                    Shapes.Kite(at, at - new Vector2(0f, size * 1.8f), size * 1.1f, 0.15f, tear);
                }

            for (int k = 0; k < 3; k++)                                            // LACRIMOSA : tears running down
            {
                float t = (clock * 0.3f + k / 3f) % 1f;
                Vector2 at = S(114f - k * 1.5f, 98f + t * 52f);
                float size = (3.6f - k * 0.5f) * lookK;
                Color tear = Color.White * (alpha * (1f - t * t));
                Shapes.Circle(at, size * 0.6f, tear);
                Shapes.Kite(at, at - new Vector2(0f, size * 1.8f), size * 1.1f, 0.15f, tear);
            }
            Diamond(S(32, 160), white ? 6f : 7.5f, white ? 7f : 9f, Color.White * alpha);   // the core where the lines stop

            if (white)                                                             // CRACKS in the white shell
            {
                if (crackSeen >= 1) RequiemCrack(0, ink * alpha, 1.4f);
                if (crackSeen >= 2) RequiemCrack(1, ink * alpha, 1.4f);
            }
            else
            {
                RequiemCrack(0, Color.White * alpha, 1.6f);
                RequiemCrack(1, Color.White * alpha, 1.6f);
            }
        }

        //Quill : a point of REQUIEM's quill (its sketch place moved by the hand's swing), turned round the hand by quillTurn
        private Vector2 quillGrip;
        private float quillTurn;

        private Vector2 Quill(float x, float y, Vector2 hand)
        {
            Vector2 d = S(x, y) + hand - quillGrip;
            float cos = (float)Math.Cos(quillTurn), sin = (float)Math.Sin(quillTurn);
            return quillGrip + new Vector2(d.X * cos - d.Y * sin, d.X * sin + d.Y * cos);
        }

        private static readonly float[] RequiemEnds = { 34, 30, 38, 33, 40 };
        private static readonly float[] RequiemCracks = { 104, 190, 110, 206, 106, 214, 114, 236, 140, 200, 134, 214, 139, 224, 139, 224 };

        private void RequiemCrack(int k, Color c, float width)
        {
            for (int i = 0; i < 4; i++) line[i] = S(RequiemCracks[k * 8 + i * 2], RequiemCracks[k * 8 + i * 2 + 1]);
            LightLine(4, width * lookK, c);
        }

        //======================================================================================
        //THE NAMELESS MASTER (round 6 part 3, from the sketch) : Fanny Mendelssohn, whose songs came out under her brother's name
        //======================================================================================

        //Draw Nameless : a blank mask with no face, an empty frame floating on her chest where a name should be (the core is
        //in it), and behind her the black shadow of her brother, a moment late in every move (PoseTacet), only its eyes and a
        //tear showing. THE MONUMENT white with the frame cut in black, then the black form with the frame in white.
        private void DrawNameless(float alpha)
        {
            bool white = ShellOn;
            Vector2 late = body[Extra].At - body[Chest].At;                       // the shadow, a moment behind
            Vector2 lateFloor = new Vector2(late.X, 0f);                          //   its hem stays on the floor
            Color rim = lookRim;
            float edge = lookEdge;
            lookRim = ShadowRim * alpha;                                          //   black, a pale edge so it reads on the dark
            lookFill = ShadowInk * alpha;
            lookEdge = edge * 1.4f;
            line[0] = S(126, 102) + late; line[1] = S(154, 102) + late; line[2] = SF(182, 262) + lateFloor; line[3] = SF(98, 262) + lateFloor;
            Shell(4, 1f);
            ShellOval(S(140, 82) + late, 14f, 18f);
            lookRim = rim;
            lookEdge = edge;
            Shapes.Circle(S(134, 80) + late, 2f * lookK, Color.White * alpha);    //   its eyes
            Shapes.Circle(S(147, 80) + late, 2f * lookK, Color.White * alpha);
            float drop = (clock * 0.3f + seed) % 1f;                              //   a tear from the eye her mask does not hide
            Vector2 tear = S(147f, 85f + drop * 26f) + late;
            float size = 2.4f * lookK;
            Color wet = Color.White * (alpha * (1f - drop * drop));
            Shapes.Circle(tear, size * 0.6f, wet);
            Shapes.Kite(tear, tear - new Vector2(0f, size * 1.8f), size * 1.1f, 0.15f, wet);

            Robe(110, 104, 136, 262, 80, 160, alpha);                             // the master
            lookFill = (white ? Color.Lerp(bossLit, Color.White, 0.3f) : bossLit) * alpha;   // the blank mask
            ShellOval(S(120, 90), 15f, 19f);
            if (white) OvalLine(S(120, 90), 15f, 19f, 1f, bossDark * alpha);
            Color frame = (white ? bossInk : Color.White) * alpha;                // the empty frame where her name should be
            line[0] = S(98, 146); line[1] = S(142, 146); line[2] = S(142, 176); line[3] = S(98, 176);
            for (int i = 0; i < 4; i++) lineW[i] = 1.6f * lookK;
            Shapes.Stroke(line, lineW, 4, frame, frame, true);
            BossCore(S(120, 161), 6f, 7f, alpha);
            BossCracks(NamelessCracks, alpha);
        }

        private static readonly Color ShadowInk = new Color(6, 6, 8);
        private static readonly Color ShadowRim = new Color(214, 214, 220) * 0.7f;
        private static readonly float[][] NamelessCracks =
        {
            new float[] { 92, 196, 100, 212, 95, 220, 102, 240 },
            new float[] { 146, 206, 140, 218, 145, 228 },
        };

        //======================================================================================
        //THE STAFF (round 6 part 3, from the sketch) : Lully, who beat time with a great staff and struck his own foot
        //======================================================================================

        //Draw Staff : a man of the Sun King's court, its collar a ring of the sun's rays, beating time with a great staff on
        //its own foot. That foot is rotting and falls away in bits (Lully died of the wound he struck himself conducting a
        //TE DEUM). Every 1.7 seconds it lifts the staff and slams it down (StaffBeat) : a ring goes out along the floor and
        //bits of the foot fall off. The core is the staff's tip, where it lands. On its blow the staff swings at the band.
        private void DrawStaff(float alpha)
        {
            bool white = ShellOn;
            Color lit = bossLit * alpha, dark = bossDark * alpha;
            lookFill = Dim(bossDark, 0.94f) * alpha;                              // the sun's rays round its head, turning slowly
            float turn = clock * 0.06f;
            for (int j = 0; j < 16; j++)
            {
                float b = j / 16f * MathHelper.TwoPi + turn, r = j % 2 == 1 ? 38f : 48f;
                line[0] = S(120f + (float)Math.Cos(b - 0.08f) * 22f, 94f + (float)Math.Sin(b - 0.08f) * 22f);
                line[1] = S(120f + (float)Math.Cos(b) * r, 94f + (float)Math.Sin(b) * r);
                line[2] = S(120f + (float)Math.Cos(b + 0.08f) * 22f, 94f + (float)Math.Sin(b + 0.08f) * 22f);
                Shell(3, 1f);
            }
            Robe(118, 90, 150, 250, 62, 178, alpha);
            BossHead(120, 94, 15f, 19f, alpha);

            lookFill = lit;                                                       // the foot it stands on
            int n = Cubic(SF(96, 262), SF(92, 256), SF(88, 248), SF(91, 243), 6, line, 0);
            n = CubicMore(SF(91, 243), SF(93, 239), SF(99, 239), SF(101, 243), 6, line, n);
            n = CubicMore(SF(101, 243), SF(104, 248), SF(100, 256), SF(96, 262), 6, line, n);
            Shell(n - 1, 1f);
            lookFill = dark;                                                      // the foot it strikes, rotting, a bite out of it
            n = Cubic(SF(146, 262), SF(142, 257), SF(139, 250), SF(141, 246), 6, line, 0);
            line[n++] = SF(148, 249);
            line[n++] = SF(151, 244);
            n = CubicMore(SF(151, 244), SF(153, 249), SF(151, 256), SF(146, 262), 6, line, n);
            Shell(n - 1, 1f);
            line[0] = SF(144, 250); line[1] = SF(148, 256);                       //   cracked through
            LightLine(2, 1.3f * lookK, (white ? bossInk : Color.White) * alpha);
            bool onFoot = phase == Phase.None || blowLast;                        //   its own beat between blows, and TE DEUM's last blow, land on the foot
            float since = phase == Phase.None ? (clock + seed) % 1.7f - 0.45f : (hitAge >= 0f ? hitAge : -1f);
            if (onFoot)                                                           //   after each slam on it bits of it fall
                for (int k = 0; k < 3; k++)
                {
                    float t = (since - k * 0.07f) / 0.6f;
                    if (t < 0f || t > 1f) continue;
                    Vector2 from = SF(149f + k * 3f, 246f + k), to = SF(157f + k * 5f, 261f);
                    Vector2 at = new Vector2(MathHelper.Lerp(from.X, to.X, t), MathHelper.Lerp(from.Y, to.Y, t * t));
                    Crumb(at, (2.6f - k * 0.4f) * lookK, t * 5f + k, 1f - t * t * t);
                }
            Crumb(SF(160, 260), 1.6f * lookK, 0.7f, 1f);                          //   one already on the floor
            BossCracks(StaffCracks, alpha);

            Vector2 top = S(162, 48) + BossSwing(0.22f, -0.5f), tip = StaffFoot();   // the staff, lifted between beats, lifted and slammed in a blow
            float ring = since / 0.8f;                                            // TE DEUM : each slam goes out along the floor
            if (ring > 0f && ring < 1f)
            {
                float wide = 1.6f * lookK * (1f - ring);
                Color wave = Color.White * (0.55f * alpha * (1f - ring));
                if (onFoot) Shapes.Arc(SF(150, 262), (14f + ring * 70f) * lookK, MathHelper.Pi, MathHelper.TwoPi, wide, wide, wave);
                else                                                              //   a blow's slam : the wave runs along the floor to the band
                {
                    Vector2 at = new Vector2(tip.X + Facing * ring * 230f * lookK, SF(150, 262).Y);
                    Shapes.Arc(at, (12f + ring * 16f) * lookK, MathHelper.Pi, MathHelper.TwoPi, wide * 1.4f, wide * 1.4f, wave);
                }
            }
            Shapes.Line(top, tip, 4f * lookK + lookEdge * 2f, lookRim);
            Shapes.Line(top, tip, 4f * lookK, lit);
            Vector2 knob = top + (S(162.5f, 44f) - S(162f, 48f));
            Shapes.Circle(knob, 8f * lookK + lookEdge, lookRim);
            Shapes.Circle(knob, 8f * lookK, lit);
            Vector2 grip = Vector2.Lerp(top, tip, (125f - 48f) / (252f - 48f));    //   the hand on it
            Shapes.Polygon(grip, 7.1f * lookK + lookEdge, 4, lookLean, lookRim);
            Shapes.Polygon(grip, 7.1f * lookK, 4, lookLean, lit);
            BossCore(tip, 5f, 6f, alpha);
        }

        //Staff Foot : the staff's tip (its core), where the HandB spring has it : over its rotting foot between blows, lifted
        //and slammed on the floor before the band in a blow (TacetKey)
        private Vector2 StaffFoot()
        {
            return PartAt(HandB);
        }

        private static readonly float[][] StaffCracks =
        {
            new float[] { 160, 214, 156, 228, 162, 236, 158, 250 },
            new float[] { 170, 226, 166, 240 },
        };

        //======================================================================================
        //THE DEVIL'S STRING (round 6 part 3, from the sketch) : Paganini, the violinist people said had sold his soul
        //======================================================================================

        //Draw Devil : tall, thin and bent, the scroll of a violin grown into two great horns, one string pulled from above its
        //head to the floor straight through it (he played whole pieces on one string, ON ONE STRING), long needles for
        //fingers, the bow a scythe, a tail. The core sits on the string at its chest and the string shivers, wide while it
        //plays : its blow draws the scythe across the string. The big form of the small horns on PAGANINI's head in the band.
        private void DrawDevil(float alpha)
        {
            Color lit = bossLit * alpha, dark = bossDark * alpha;
            Vector2 swing = BossSwing(0.22f, -0.5f);                              // the bow hand's own swing
            float flick = (float)Math.Sin(clock * 1.3f + seed) * 3f;              // its tail
            Blade(S(110, 248), S(90, 256), S(76, 250 + flick * 0.5f), S(70, 238 + flick),
                  S(70, 238 + flick), S(80, 246 + flick * 0.5f), S(94, 248), S(108, 242), dark, true);
            Robe(96, 114, 126, 250, 106, 134, alpha);
            lookFill = lit;                                                       // the shoulders, a bar
            ShellQuad(S(92, 104), S(148, 104), S(144, 110), S(96, 110));
            BossHead(120, 80, 10f, 14f, alpha);
            Horn(DevilHornLit, lit);                                              // the scroll grown into horns
            Horn(DevilHornShade, dark);
            lookFill = lit;                                                       // the bow hand's needles, then the bow : a scythe
            Finger(swing, 150, 138, 196, 124, 152, 142);
            Finger(swing, 151, 141, 198, 140, 152, 145);
            Finger(swing, 151, 144, 194, 156, 151, 147);
            lookFill = dark;                                                      // the other hand's needles
            Finger(Vector2.Zero, 90, 146, 52, 136, 89, 150);
            Finger(Vector2.Zero, 90, 150, 50, 152, 90, 153);
            Blade(S(188, 126) + swing, S(230, 140) + swing, S(236, 196) + swing, S(206, 228) + swing,
                  S(206, 228) + swing, S(220, 192) + swing, S(214, 152) + swing, S(188, 132) + swing, lit, true);
            for (int i = 0; i < 16; i++)                                          // ON ONE STRING : from above its head to the floor
            {
                float t = i / 15f;
                line[i] = Bez(S(120, 30), S(121.5f, 100), S(118.5f, 180), SF(120, 262), t) + new Vector2(StringShake(t) * lookK, 0f);
                lineW[i] = 1.4f * lookK;
            }
            Shapes.Stroke(line, lineW, 16, Color.White * alpha, Color.White * alpha, false);
            BossCore(DevilCore(), 6f, 7f, alpha);
            BossCracks(DevilCracks, alpha);
        }

        //Horn : one of THE DEVIL'S STRING's horns, the curl of a violin's scroll (three curves of the sketch), thick at the root
        //and thin at the curl, its rim first
        private void Horn(float[] p, Color fill)
        {
            int n = Cubic(S(p[0], p[1]), S(p[2], p[3]), S(p[4], p[5]), S(p[6], p[7]), 8, line, 0);
            n = CubicMore(S(p[6], p[7]), S(p[8], p[9]), S(p[10], p[11]), S(p[12], p[13]), 6, line, n);
            n = CubicMore(S(p[12], p[13]), S(p[14], p[15]), S(p[16], p[17]), S(p[18], p[19]), 5, line, n);
            for (int i = 0; i < n; i++) lineW[i] = MathHelper.Lerp(6f, 2.5f, i / (n - 1f)) * lookK + lookEdge * 2f;
            Shapes.Stroke(line, lineW, n, lookRim, lookRim, false);
            for (int i = 0; i < n; i++) lineW[i] -= lookEdge * 2f;
            Shapes.Stroke(line, lineW, n, fill, fill, false);
        }

        //String Shake : how far the string is off its line, sketch units (t 0 at its top, 1 on the floor). A fine shiver all
        //the time, five times wider at its blow
        private float StringShake(float t)
        {
            return (float)Math.Sin(t * MathHelper.Pi) * (float)Math.Sin(clock * 31f) * (0.8f + 3.2f * lookStrike);
        }

        //Devil Core : the core on the string at its chest, shaking with it
        private Vector2 DevilCore()
        {
            return S(120, 150) + new Vector2(StringShake(0.52f) * lookK, 0f);
        }

        private static readonly float[] DevilHornLit = { 112, 70, 96, 60, 84, 40, 96, 30, 104, 24, 112, 32, 106, 38, 102, 42, 98, 38, 100, 35 };
        private static readonly float[] DevilHornShade = { 128, 70, 144, 60, 156, 40, 144, 30, 136, 24, 128, 32, 134, 38, 138, 42, 142, 38, 140, 35 };
        private static readonly float[][] DevilCracks =
        {
            new float[] { 112, 176, 117, 190, 113, 198 },
            new float[] { 128, 206, 124, 220 },
        };

        //======================================================================================
        //THE BLIND CANTOR (round 6 part 3, from the sketch) : Bach, who went blind at the end of his life
        //======================================================================================

        //Draw Cantor : a blindfold with light leaking at its edges, wings of organ pipes rising one higher than the next, and
        //voices going out of its chest in rings, one after another (a fugue). The core is the middle of its chest, the heart
        //of the organ. On its blow the pipes sound (their mouths light) and a voice is thrown at the band (IN THE DARK).
        private void DrawCantor(float alpha)
        {
            Color lit = bossLit * alpha, dark = bossDark * alpha, ink = bossInk * alpha;
            float after = SinceBlow();
            Color mouth = Color.Lerp(bossInk, Color.White, after >= 0f ? (1f - after) * (1f - after) : 0f) * alpha;
            for (int k = 0; k < 5; k++)                                           // the organ pipes behind it, the outer ones taller
            {
                float top = 120f - k * 15f - Math.Max(0f, (float)Math.Sin(clock * 1.1f - k * 0.7f)) * 2.5f;   // rising one by one
                for (int side = 0; side < 2; side++)
                {
                    float x = side == 0 ? 90f - k * 10f : 143f + k * 10f;
                    lookFill = side == 0 ? lit : dark;
                    ShellQuad(S(x, top), S(x + 7f, top), S(x + 7f, 206f), S(x, 206f));
                    Shapes.Box(S(x + 3.5f, 192.5f), 4f * lookK, 5f * lookK, lookLean, mouth);
                }
            }
            Robe(112, 104, 136, 262, 74, 166, alpha);
            Vector2 keep = Shift;                                                 // the head and its blindfold tip to listen (the Head spring's own move)
            Shift += body[Head].At - Local(0f, -0.8f) - (body[Chest].At - Local(0f, -0.5f));
            BossHead(120, 88, 16f, 20f, alpha);
            float flap = (float)Math.Sin(clock * 2.2f + seed) * 2.5f;            // the blindfold's two ends in the air
            Blade(S(102, 82), S(86, 86), S(76, 96 + flap * 0.5f), S(66, 112 + flap),
                  S(71, 114 + flap), S(80, 100 + flap * 0.5f), S(90, 92), S(102, 89), ink, false);
            Blade(S(102, 86), S(90, 94), S(84, 104 + flap * 0.5f), S(80, 118 + flap * 0.8f),
                  S(85, 119 + flap * 0.8f), S(88, 106 + flap * 0.5f), S(94, 98), S(103, 91), ink, false);
            Shapes.Box(S(120, 84.5f), 36f * lookK, 9f * lookK, lookLean, ink);   // the blindfold
            Shapes.Line(S(104, 79), S(136, 79), 1.2f * lookK, Color.White * alpha);   //   light leaking at its edges
            Shapes.Line(S(104, 90.5f), S(136, 90.5f), 1f * lookK, Color.White * alpha);
            Shift = keep;
            Vector2 heart = S(120, 146);
            for (int k = 0; k < 3; k++)                                           // voices going out, one after another
            {
                float t = (clock * 0.4f + k / 3f) % 1f;
                Shapes.Ring(heart, (11f + t * 30f) * lookK, 1.1f * lookK, Color.White * (alpha * 0.6f * (1f - t)));
            }
            if (after >= 0f)                                                      // IN THE DARK : a voice thrown at the band
                Shapes.Ring(heart, (13f + after * 100f) * lookK, (0.6f + 2f * (1f - after)) * lookK, Color.White * (alpha * (1f - after)));
            BossCore(heart, 6f, 7f, alpha);
            BossCracks(CantorCracks, alpha);
        }

        //The sketch draws no cracks on THE BLIND CANTOR : these are like the others', down the robe on both sides
        private static readonly float[][] CantorCracks =
        {
            new float[] { 96, 198, 104, 214, 99, 224, 106, 246 },
            new float[] { 146, 206, 139, 220, 144, 232 },
        };

        //======================================================================================
        //Glow : TACET
        //======================================================================================

        //Boss Floor Wide : how wide the light each boss gives the floor is, a share of its height (the sketch's haze under each
        //card : REQUIEM, THE NAMELESS MASTER, THE DEVIL'S STRING, THE DEAF CROWN, THE STAFF, THE BLIND CANTOR)
        private static readonly float[] BossFloorWide = { 0.63f, 0.68f, 0.58f, 0.86f, 0.72f, 0.81f };

        private void GlowTacet(SpriteBatch sb, float alpha)
        {
            if (down && shardCount == 0) return;
            float h = Height;
            float power = BattleRules.FigureGlowTacet * alpha;
            Vector2 at = body[Head].At + Shift;
            if (shardCount > 0)
            {
                for (int i = 0; i < shardCount; i += 4) Gfx.DrawGlow(sb, shards[i].At.X, shards[i].At.Y, shards[i].Size * 2f, Color.White * (power * 0.5f));
                float t = moveTime / BattleRules.FigureReleaseTime;                  // RELEASE : the freed light glows as it rises
                if (t < 1f)
                {
                    float e = Ease(t);
                    Vector2 ring = releaseAt + Shift - new Vector2(0f, 0.35f * e * Height);
                    Gfx.DrawGlow(sb, ring.X, ring.Y, (0.1f + 0.35f * e) * h, Color.White * (power * 2.2f * (1f - t)));
                }
                return;
            }
            GlowFloor(sb, IsLook ? 0.56f : BossFloorWide[design - Requiem], Color.White * (BattleRules.FigureFloorLight * alpha));   // FLOOR LIGHT
            if (IsLook)
            {
                GlowLook(sb, power);
                return;
            }
            Gfx.DrawGlow(sb, at.X, at.Y, 0.18f * h, Color.White * (power * (ShellOn ? 0.9f : 0.6f)));
            Vector2 core = CorePoint() + Shift;                                      // the heart glows (it feeds the bloom)
            float bright = ShellOn ? 1f : 1.5f;                                      // brighter once the shell is gone
            Gfx.DrawGlow(sb, core.X, core.Y, CoreSize * 5f, Color.White * (power * 1.6f * bright));
            Gfx.DrawGlow(sb, core.X, core.Y, CoreSize * 2f, Color.White * (power * 1.4f * bright));
            if (breakFlash > 0f)                                                     // THE MONUMENT breaking : a white blast
            {
                Vector2 middle = body[Chest].At + Shift;
                Gfx.DrawGlow(sb, middle.X, middle.Y, 0.75f * h, Color.White * (power * 4f * breakFlash));
            }
            SetLook();                                                               // each boss's own lights (S already holds Shift)
            switch (design)
            {
                case Crown:                                                          // THE NINTH : the lightning cracks glow
                    if (!ShellOn)
                    {
                        Vector2 c1 = S(96, 200), c2 = S(150, 190);
                        Gfx.DrawGlow(sb, c1.X, c1.Y, 0.12f * h, Color.White * (power * 0.9f));
                        Gfx.DrawGlow(sb, c2.X, c2.Y, 0.1f * h, Color.White * (power * 0.9f));
                    }
                    break;
                case Nameless:                                                       // the empty frame, and the shadow's eyes
                    {
                        Vector2 frame = S(120, 161);
                        Gfx.DrawGlow(sb, frame.X, frame.Y, 0.09f * h, Color.White * (power * (ShellOn ? 0.5f : 0.9f)));
                        Vector2 eyes = S(140.5f, 80f) + body[Extra].At - body[Chest].At;
                        Gfx.DrawGlow(sb, eyes.X, eyes.Y, 0.03f * h, Color.White * (power * 0.8f));
                    }
                    break;
                case Devil:                                                          // the string glows, more while it plays
                    for (int i = 1; i < 6; i++)
                    {
                        Vector2 p = Bez(S(120, 30), S(121.5f, 100), S(118.5f, 180), SF(120, 262), i / 6f);
                        Gfx.DrawGlow(sb, p.X, p.Y, 0.05f * h, Color.White * (power * (0.3f + 0.9f * lookStrike)));
                    }
                    break;
                case Staff:                                                          // TE DEUM : the slam lights the floor where it lands
                    {
                        bool idle = phase == Phase.None;
                        float t = idle ? ((clock + seed) % 1.7f - 0.45f) / 0.4f : (hitAge >= 0f ? hitAge / 0.4f : -1f);
                        if (t > 0f && t < 1f)
                        {
                            Vector2 floor = SF(150, 262);
                            if (!idle && !blowLast) floor.X = StaffFoot().X;
                            Gfx.DrawGlow(sb, floor.X, floor.Y, 0.14f * h, Color.White * (power * 1.6f * (1f - t)));
                        }
                    }
                    break;
                case Cantor:                                                         // the light leaking round the blindfold
                    {
                        Vector2 band = S(120, 84.5f);
                        int w = (int)(44f * lookK), tall = (int)(16f * lookK);
                        Gfx.DrawGlowBox(sb, new Rectangle((int)band.X - w / 2, (int)band.Y - tall / 2, w, tall), Color.White * (power * 0.7f));
                    }
                    break;
            }
        }
    }
}
