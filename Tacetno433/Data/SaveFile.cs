using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Tacetno433.Core;

namespace Tacetno433.Data
{
    //SaveFile : the two small files the game keeps between plays.
    //   settings.txt   the options from the settings page
    //   run.txt        the run in progress, for CONTINUE on the title page
    //Both live in the player's own AppData folder (Windows keeps each user's files there),
    //in a folder called TACET433.
    //
    //WHEN THE RUN IS SAVED
    //   arriving on the route page          the stage, the band, everything so far
    //   picking a path                      the same, plus which place was picked
    //   the first page of every floor       so quitting while choosing an era is safe
    //Quitting in the middle of a place (a fight, the shop) comes back into that same place, with
    //the band as it was when it walked in, so a bad fight cannot be escaped by quitting.
    //Losing a fight, or finishing the run, deletes the run file. A roguelike has no second go.
    //
    //THE FORMAT is plain text, one  name=value  per line, so it can be opened in Notepad.
    //Lists are written with commas. Musicians, motifs, enemies and events are written as their
    //number in their list (MusicianList.All and so on), so KEEP THOSE LISTS IN THE SAME ORDER,
    //or old saves will load the wrong people.
    public static class SaveFile
    {
        public const int Version = 1;

        //Enabled : the capture tool switches this off, so its sample runs never touch real saves
        public static bool Enabled = true;

        //Pretend Run : DEVELOPER TOOL ONLY. The capture tool sets it to picture the title page
        //with CONTINUE on it, without any real file. Never set in play.
        public static bool PretendRun;

        //Loaded Place : what a loaded run was in the middle of, read by RunFlow.Continue
        public static Enemy LoadedEnemy;
        public static GameEvent LoadedEvent;

        //Folder Override : DEVELOPER TOOL ONLY. The capture tool's save check points this at its
        //own output folder, so testing never touches the player's real files.
        public static string FolderOverride = "";

        private static string Folder
        {
            get
            {
                if (FolderOverride.Length > 0) return FolderOverride;
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TACET433");
            }
        }

        private static string SettingsPath { get { return Path.Combine(Folder, "settings.txt"); } }
        private static string RunPath { get { return Path.Combine(Folder, "run.txt"); } }

        //Has Run : there is a run to CONTINUE
        public static bool HasRun
        {
            get { return PretendRun || (Enabled && File.Exists(RunPath)); }
        }

        public static void DeleteRun()
        {
            if (!Enabled) return;
            try
            {
                if (File.Exists(RunPath)) File.Delete(RunPath);
            }
            catch (Exception)
            {
                //A file that cannot be deleted is left alone, the game carries on
            }
        }

        //Settings Save : written whenever the settings page closes
        public static void SaveSettings()
        {
            List<string> lines = new List<string>();
            lines.Add("version=" + Version);
            lines.Add("master=" + Number(Settings.Master));
            lines.Add("sfx=" + Number(Settings.Sfx));
            lines.Add("music=" + Number(Settings.Music));
            lines.Add("language=" + Settings.Language);
            lines.Add("fullscreen=" + (Settings.Fullscreen ? 1 : 0));
            lines.Add("timing=" + Number(Settings.TimingOffset));
            lines.Add("maestro=" + (Settings.Maestro ? 1 : 0));
            Write(SettingsPath, lines);
        }

        //Settings Load : called once when the game starts. A missing file keeps the defaults.
        public static void LoadSettings()
        {
            Dictionary<string, string> v = Read(SettingsPath);
            if (v == null) return;

            Settings.Master = Clamp01(GetFloat(v, "master", Settings.Master));
            Settings.Sfx = Clamp01(GetFloat(v, "sfx", Settings.Sfx));
            Settings.Music = Clamp01(GetFloat(v, "music", Settings.Music));
            Settings.Language = GetInt(v, "language", 0) == 1 ? 1 : 0;
            Settings.Fullscreen = GetInt(v, "fullscreen", 0) == 1;
            Settings.SetTiming(GetFloat(v, "timing", 0f));
            Settings.Maestro = GetInt(v, "maestro", 0) == 1;
        }

        //Run Save : everything RunState needs to carry on. A fight in progress is not written,
        //only which enemy it was against, so it starts again from its first round.
        public static void SaveRun(RunState run)
        {
            if (run == null || run.RunComplete) return;

            List<string> lines = new List<string>();
            lines.Add("version=" + Version);
            lines.Add("conductor=" + Array.IndexOf(ConductorList.All, run.Conductor));
            lines.Add("band=" + run.BandName);
            lines.Add("floor=" + run.Floor);
            lines.Add("stage=" + run.Stage);
            lines.Add("stages=" + run.StagesThisFloor);
            lines.Add("era=" + run.Era);
            lines.Add("maestro=" + (run.Maestro ? 1 : 0));          // MAESTRO MODE, a save from before 29 Sep has none : NORMAL
            lines.Add("shards=" + run.Shards);
            lines.Add("seats=" + run.Seats);
            lines.Add("seatsbought=" + run.SeatsBoughtThisFloor);
            lines.Add("stamina=" + run.Stamina);
            lines.Add("maxstamina=" + run.MaxStamina);
            lines.Add("power=" + Number(run.PowerMultiplier));
            lines.Add("won=" + run.BattlesWon);
            lines.Add("perfects=" + run.PerfectsTotal);
            lines.Add("bestcombo=" + run.BestCombo);

            //Band : who is in the ensemble, how much each has rehearsed, who sits where
            List<string> roster = new List<string>();
            for (int i = 0; i < run.Roster.Count; i++) roster.Add(Array.IndexOf(MusicianList.All, run.Roster[i]).ToString());
            lines.Add("roster=" + string.Join(",", roster));

            List<string> rehearsed = new List<string>();
            for (int i = 0; i < MusicianList.All.Length; i++) rehearsed.Add(MusicianList.All[i].Rehearsed.ToString());
            lines.Add("rehearsed=" + string.Join(",", rehearsed));

            //Seated : one entry per seat. Round 12 dropped the beat plan, an old save's plan= line is ignored.
            List<string> seated = new List<string>();
            for (int s = 0; s < StageLayout.SeatCount; s++)
            {
                Musician m = run.Formation.Seated[s];
                seated.Add(m == null ? "-1" : Array.IndexOf(MusicianList.All, m).ToString());
            }
            lines.Add("seated=" + string.Join(",", seated));

            List<string> motifs = new List<string>();
            for (int i = 0; i < run.Motifs.Count; i++) motifs.Add(Array.IndexOf(MotifList.All, run.Motifs[i]).ToString());
            lines.Add("motifs=" + string.Join(",", motifs));

            //Journey : floor, kind of place, and whether the run ended there
            List<string> journey = new List<string>();
            for (int i = 0; i < run.Journey.Count; i++)
                journey.Add(run.Journey[i].Floor + ":" + (int)run.Journey[i].Type + ":" + (run.Journey[i].Lost ? 1 : 0));
            lines.Add("journey=" + string.Join(",", journey));

            //Route : the paths on offer, and the one picked if the band has walked into it
            List<string> options = new List<string>();
            for (int i = 0; i < run.Options.Length; i++) options.Add(((int)run.Options[i].Type).ToString());
            lines.Add("options=" + string.Join(",", options));
            lines.Add("chosen=" + Array.IndexOf(run.Options, run.Chosen));
            lines.Add("enemy=" + (run.Battle != null ? Array.IndexOf(EnemyList.All, run.Battle.Enemy) : -1));
            lines.Add("event=" + (run.CurrentEvent != null ? Array.IndexOf(EventList.All, run.CurrentEvent) : -1));

            Write(RunPath, lines);
        }

        //Run Load : a fresh RunState filled in from the file, or null if there is none or it
        //cannot be read. LoadedEnemy and LoadedEvent say what the band was walking into.
        public static RunState LoadRun(TacetGame game)
        {
            LoadedEnemy = null;
            LoadedEvent = null;

            Dictionary<string, string> v = Read(RunPath);
            if (v == null) return null;

            int conductor = GetInt(v, "conductor", -1);
            if (conductor < 0 || conductor >= ConductorList.All.Length) return null;

            //Start : the same start as a new run, then everything is written over from the file
            RunState run = new RunState();
            run.Start(ConductorList.All[conductor], game.StoryFont, RouteNodeInfo.CaptionWrapWidth);

            run.BandName = GetText(v, "band");
            run.Floor = GetInt(v, "floor", 1);
            run.Stage = GetInt(v, "stage", 1);
            run.StagesThisFloor = GetInt(v, "stages", 8);
            run.Era = Math.Max(0, Math.Min(EraList.All.Length - 1, GetInt(v, "era", 0)));
            run.Maestro = GetInt(v, "maestro", 0) == 1;
            run.Shards = GetInt(v, "shards", 0);
            run.Seats = GetInt(v, "seats", 3);
            run.SeatsBoughtThisFloor = GetInt(v, "seatsbought", 0);
            run.MaxStamina = GetInt(v, "maxstamina", run.MaxStamina);
            run.Stamina = GetInt(v, "stamina", run.MaxStamina);
            run.PowerMultiplier = GetFloat(v, "power", run.PowerMultiplier);
            run.BattlesWon = GetInt(v, "won", 0);
            run.PerfectsTotal = GetInt(v, "perfects", 0);
            run.BestCombo = GetInt(v, "bestcombo", 0);

            int[] roster = GetList(v, "roster");
            for (int i = 0; i < roster.Length; i++)
                if (roster[i] >= 0 && roster[i] < MusicianList.All.Length) run.Roster.Add(MusicianList.All[roster[i]]);

            int[] rehearsed = GetList(v, "rehearsed");
            for (int i = 0; i < rehearsed.Length && i < MusicianList.All.Length; i++)
                MusicianList.All[i].Rehearsed = rehearsed[i];

            int[] seated = GetList(v, "seated");
            for (int s = 0; s < StageLayout.SeatCount && s < seated.Length; s++)
                if (seated[s] >= 0 && seated[s] < MusicianList.All.Length) run.Formation.Seated[s] = MusicianList.All[seated[s]];
            run.Formation.Tidy(run.Seats);       // a save from before round 12 sat its first players down the centre

            int[] motifs = GetList(v, "motifs");
            for (int i = 0; i < motifs.Length; i++)
                if (motifs[i] >= 0 && motifs[i] < MotifList.All.Length) run.Motifs.Add(MotifList.All[motifs[i]]);

            string journey = GetText(v, "journey");
            if (journey.Length > 0)
            {
                string[] stops = journey.Split(',');
                for (int i = 0; i < stops.Length; i++)
                {
                    string[] bits = stops[i].Split(':');
                    if (bits.Length < 3) continue;
                    JourneyStop stop = new JourneyStop();
                    stop.Floor = ToInt(bits[0], 1);
                    stop.Type = (NodeType)ToInt(bits[1], 0);
                    stop.Lost = bits[2] == "1";
                    run.Journey.Add(stop);
                }
            }

            int[] options = GetList(v, "options");
            NodeType[] types = new NodeType[options.Length];
            for (int i = 0; i < options.Length; i++) types[i] = (NodeType)options[i];
            run.RestoreOptions(types);

            int chosen = GetInt(v, "chosen", -1);
            run.Chosen = chosen >= 0 && chosen < run.Options.Length ? run.Options[chosen] : null;

            int enemy = GetInt(v, "enemy", -1);
            if (enemy >= 0 && enemy < EnemyList.All.Length) LoadedEnemy = EnemyList.All[enemy];
            int ev = GetInt(v, "event", -1);
            if (ev >= 0 && ev < EventList.All.Length) LoadedEvent = EventList.All[ev];

            run.ChangeStamina(0);         // keeps stamina inside its limits
            run.RefreshLabels();
            return run;
        }

        //Run Summary : one line for the CONTINUE button, read straight from the file
        public static string RunSummary()
        {
            if (PretendRun) return "THE INFERNO  /  FLOOR 2  /  STAGE 5";
            Dictionary<string, string> v = Read(RunPath);
            if (v == null) return "";
            int conductor = GetInt(v, "conductor", -1);
            string name = conductor >= 0 && conductor < ConductorList.All.Length ? ConductorList.All[conductor].Name : "";
            return name + "  /  FLOOR " + GetInt(v, "floor", 1) + "  /  STAGE " + GetInt(v, "stage", 1);
        }

        //File Write : any trouble writing (a full disk, a locked folder) is ignored on purpose.
        //Not being able to save must never stop the game.
        private static void Write(string path, List<string> lines)
        {
            if (!Enabled) return;
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllLines(path, lines);
            }
            catch (Exception)
            {
            }
        }

        //File Read : every  name=value  line into a dictionary, or null if the file is missing,
        //unreadable, or from a different version of the game
        private static Dictionary<string, string> Read(string path)
        {
            if (!Enabled || !File.Exists(path)) return null;

            Dictionary<string, string> values = new Dictionary<string, string>();
            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    int cut = lines[i].IndexOf('=');
                    if (cut <= 0) continue;
                    values[lines[i].Substring(0, cut)] = lines[i].Substring(cut + 1);
                }
            }
            catch (Exception)
            {
                return null;
            }

            if (GetInt(values, "version", 0) != Version) return null;
            return values;
        }

        //Value Readers : each one falls back to a safe value when the line is missing or broken
        private static string GetText(Dictionary<string, string> v, string name)
        {
            string text;
            return v.TryGetValue(name, out text) ? text : "";
        }

        private static int GetInt(Dictionary<string, string> v, string name, int fallback)
        {
            return ToInt(GetText(v, name), fallback);
        }

        private static int ToInt(string text, int fallback)
        {
            int value;
            return int.TryParse(text, out value) ? value : fallback;
        }

        private static float GetFloat(Dictionary<string, string> v, string name, float fallback)
        {
            float value;
            return float.TryParse(GetText(v, name), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : fallback;
        }

        private static int[] GetList(Dictionary<string, string> v, string name)
        {
            string text = GetText(v, name);
            if (text.Length == 0) return new int[0];
            string[] parts = text.Split(',');
            int[] values = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++) values[i] = ToInt(parts[i], -1);
            return values;
        }

        //Number : a decimal written with a dot whatever the computer's language, so a file saved
        //on a Thai or a German Windows reads back the same everywhere
        private static string Number(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
