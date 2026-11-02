# Patch a COPY of the game so a test page plays a skill by itself, PERFECT on every note, and the same page can be
# captured frame after frame without reopening (BETA1_DIRECTION 32.22 PROBE). Never applied to the repo.
import sys
root = sys.argv[1]
def patch(rel, old, new, count=1):
    p = root + '/' + rel
    s = open(p, encoding='utf-8-sig').read()
    bom = open(p, 'rb').read(3) == b'\xef\xbb\xbf'
    assert s.count(old) == count, (rel, old[:60], s.count(old))
    s = s.replace(old, new)
    open(p, 'w', encoding='utf-8-sig' if bom else 'utf-8').write(s)

# 1 continuous frames of one page
patch('Tacetno433/Core/DebugShots.cs',
'''            index++;
            frames = 0;
            if (index >= names.Length)
            {
                game.Exit();
                return;
            }
            Open(game, names[index]);''',
'''            index++;
            frames = 0;
            if (index >= names.Length)
            {
                game.Exit();
                return;
            }
            if (names[index] == names[index - 1] && waits[index] > waits[index - 1]) { frames = waits[index - 1]; return; }   // PROBE
            Open(game, names[index]);''')

# 2 the front musician plays a button : TACET_PROBE=skill1|skill2
patch('Tacetno433/Screens/BattleScreen.cs',
'''        public void ShowForPicture(string name)
        {
            NextStep();                                                                 // the first bar is being written''',
'''        public void ShowForPicture(string name)
        {
            NextStep();                                                                 // the first bar is being written
            string probeWhat = System.Environment.GetEnvironmentVariable("TACET_PROBE");
            if (probeWhat == "finale")                                                  // a FINALE from its cut in : TACET_KIT=fanny|bach|beethoven TACET_LEVEL=0..2
            {
                string kit = System.Environment.GetEnvironmentVariable("TACET_KIT") ?? "fanny";
                battle.Kit = kit == "bach" ? TurnKit.Bach : kit == "beethoven" ? TurnKit.Beethoven : TurnKit.Fanny;
                battle.Floor = 1 + int.Parse(System.Environment.GetEnvironmentVariable("TACET_LEVEL") ?? "0");
                for (int i = 0; i < ConductorList.All.Length; i++)
                    if (ConductorList.All[i].Turn == battle.Kit) signatureName = ConductorList.All[i].SignatureNameAt(battle.SignatureLevel);
                battle.Signature = battle.SignatureNeed;
                MoodBegins();                                                           // THE STAGE TURNS : this conductor's weather
                StartSignature();
                return;
            }
            if (probeWhat == "skill1" || probeWhat == "skill2")
            {
                battle.Breath = 10;
                Fighter front = battle.Party[0];
                Skill ps = probeWhat == "skill1" ? SkillList.First(front.Musician) : SkillList.Second(front.Musician);
                Fighter pat = ps.Target == SkillTarget.Enemy ? FirstStanding() : (ps.Target == SkillTarget.Ally ? AllyFor(front, ps) : null);
                string setup = System.Environment.GetEnvironmentVariable("TACET_SETUP") ?? "";
                Fighter aimed = FirstStanding();
                if (setup.Contains("charge")) { aimed.Charge = 2; aimed.ChargeHit = 24; }
                if (setup.Contains("armor")) aimed.Block = 22;
                if (setup.Contains("stun")) aimed.Staggered = true;
                if (setup.Contains("echo")) { front.Echo = 1f; front.EchoLeft = 1; }   // FUGUE : the skill plays again in gold
                battle.PlanSkill(front, ps, pat);
                battle.PlayBar();
                NextStep();
                return;
            }''')

# 3 PERFECT on every note by itself : TACET_AUTO=1
patch('Tacetno433/Screens/BattleScreen.cs',
'''            TellBlows(n, now);                                         // ONE NOTE ONE BLOW : the wind up as the ring closes (BattleScreen.Stage.cs)
            if (n.Kind == NoteKind.Roll)''',
'''            TellBlows(n, now);                                         // ONE NOTE ONE BLOW : the wind up as the ring closes (BattleScreen.Stage.cs)
            if (System.Environment.GetEnvironmentVariable("TACET_AUTO") == "1" && n.Kind != NoteKind.Roll && now >= n.Due)
            {
                lastWay = n.Kind == NoteKind.Spark ? Flick.Right : n.Way;
                baton.Whip(lastWay);
                System.Console.WriteLine("PROBE note " + next + " " + n.Kind + " at clock " + clock.ToString("0.000"));
                if (n.Kind == NoteKind.Hold)
                {
                    holding = true; holdTime = 0f; holdDown = 0f; holdTick = 0f; holdGrade = Grade.Perfect;
                    HoldBlow(Grade.Perfect);
                    return;
                }
                Land(Grade.Perfect, 1f);
                return;
            }
            if (n.Kind == NoteKind.Roll)''')
patch('Tacetno433/Screens/BattleScreen.cs',
'''            bool down = Input.MouseDown();''',
'''            bool down = Input.MouseDown() || System.Environment.GetEnvironmentVariable("TACET_AUTO") == "1";''')
patch('Tacetno433/Screens/BattleScreen.cs',
'''            bool stroke = gesture.Read();
            if (now < n.Due) return;''',
'''            bool stroke = gesture.Read();
            if (now < n.Due) return;
            if (System.Environment.GetEnvironmentVariable("TACET_AUTO") == "1") { stroke = now - probeShake >= 0.11f; if (stroke) probeShake = now; }''')
patch('Tacetno433/Screens/BattleScreen.cs',
'''        private bool lastSkillNote;''',
'''        private bool lastSkillNote;
        private float probeShake = -9f;''')
print('patched')
