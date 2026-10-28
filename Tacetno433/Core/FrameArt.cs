using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //FrameArt (Beta1) : the animated pixel art of the battle page, read straight from PNG files in
    //Content/Art/Battle. The csproj copies that folder next to the game, so a new picture needs no
    //Content.mgcb entry : drop the files in, Rebuild, done. Nothing here is needed for the game to run,
    //a character with no files is drawn as an empty frame (ArtSlot).
    //Two ways to hand over a character (docs/BATTLE_ART_PLAN.md 8) :
    //   A  Aseprite : <name>.png (a sprite sheet) + <name>.json (Array or Hash, with a Tag for each animation).
    //      The frame times come from Aseprite.
    //   B  one file per frame : <name>_<animation>_<number>.png, numbers from 0, ten frames a second.
    //Optimize : every file is read once per game and kept (Cache), never while a frame is drawn.
    public class FrameSet
    {
        public Point Size;                                                 // one frame, in pixels of the art
        public Dictionary<string, FrameAnim> Anims = new Dictionary<string, FrameAnim>();

        public FrameAnim Find(string name)
        {
            FrameAnim anim;
            return Anims.TryGetValue(name, out anim) ? anim : null;
        }
    }

    public class FrameAnim
    {
        public string Name = "";
        public Texture2D[] Pictures = new Texture2D[0];   // the picture each frame comes from (the same sheet for A)
        public Rectangle[] Sources = new Rectangle[0];    // where in that picture
        public Point[] Offsets = new Point[0];            // where the frame sits in the canvas (Aseprite trims empty edges)
        public float[] Times = new float[0];              // seconds each frame stays
        public bool Loop;
        public int HitFrame = -1;                         // the frame where a weapon lands (-1 : none)
        public float Length;
    }

    //FramePlayer : one character's animation clock. A struct with no lists, so playing never makes garbage.
    public struct FramePlayer
    {
        public FrameAnim Anim;
        public float Time;
        public int Frame;
        private bool hitDone;

        //Play : start an animation (again, or only if it is not already on)
        public void Play(FrameAnim anim, bool restart)
        {
            if (anim == null) return;
            if (!restart && Anim == anim) return;
            Anim = anim;
            Time = 0f;
            Frame = 0;
            hitDone = false;
        }

        public bool Done
        {
            get { return Anim == null || (!Anim.Loop && Time >= Anim.Length); }
        }

        //Update : true on the update the HIT FRAME is reached
        public bool Update(float dt)
        {
            if (Anim == null || Anim.Times.Length == 0) return false;
            Time += dt;
            if (Anim.Loop && Anim.Length > 0f) Time %= Anim.Length;

            float t = Time;
            int f = 0;
            while (f < Anim.Times.Length - 1 && t >= Anim.Times[f])
            {
                t -= Anim.Times[f];
                f++;
            }
            Frame = f;

            if (!hitDone && Anim.HitFrame >= 0 && Frame >= Anim.HitFrame)
            {
                hitDone = true;
                return true;
            }
            return false;
        }

        //Draw : feet (the middle of the bottom row of the canvas) at feet, PIXEL x scale. Draw in a batch with PointClamp.
        public void Draw(SpriteBatch sb, FrameSet set, Vector2 feet, int scale, Color tint)
        {
            if (Anim == null || set == null || Anim.Pictures.Length == 0) return;
            int f = Math.Min(Frame, Anim.Pictures.Length - 1);
            Rectangle src = Anim.Sources[f];
            Point off = Anim.Offsets[f];
            int x = (int)feet.X - set.Size.X * scale / 2 + off.X * scale;
            int y = (int)feet.Y - set.Size.Y * scale + off.Y * scale;
            sb.Draw(Anim.Pictures[f], new Rectangle(x, y, src.Width * scale, src.Height * scale), src, tint);
        }
    }

    public static class FrameArt
    {
        public const float DefaultFrameTime = 0.1f;        // B : ten frames a second until a table says otherwise
        public static int Loaded;                          // F3 : how many sets were found

        //Loops : these animations repeat, every other one plays once and holds its last frame
        private static readonly string[] loops = { "idle", "victory" };

        //Strikes : these animations land a blow. Until the artist says which frame (an Aseprite tag's User Data
        //"hit 5", or a line in HitFrames below), the blow lands on the middle frame.
        private static readonly string[] strikes = { "attack", "skill", "parry" };

        //Hit Frames : "<kind>/<key>/<animation>" = the frame the blow lands on, for art handed over as files (way B)
        public static readonly Dictionary<string, int> HitFrames = new Dictionary<string, int>();

        private static void SetHit(FrameAnim anim, string id, int fromTag)
        {
            int frame;
            if (fromTag >= 0) anim.HitFrame = fromTag;
            else if (HitFrames.TryGetValue(id + "/" + anim.Name, out frame)) anim.HitFrame = frame;
            else if (Array.IndexOf(strikes, anim.Name) >= 0) anim.HitFrame = anim.Times.Length / 2;
            anim.HitFrame = Math.Min(anim.HitFrame, anim.Times.Length - 1);
        }

        private static Dictionary<string, FrameSet> cache = new Dictionary<string, FrameSet>();

        //Folder : where the battle art sits next to the game
        public static string Folder(string kind)
        {
            return Path.Combine(AppContext.BaseDirectory, "Content", "Art", "Battle", kind);
        }

        //Load : the set for one character, or null when it has no files. animations lists the names to look for
        //with way B. Asked again, it answers from the cache.
        public static FrameSet Load(GraphicsDevice device, string kind, string key, string[] animations)
        {
            string id = kind + "/" + key;
            FrameSet set;
            if (cache.TryGetValue(id, out set)) return set;

            set = null;
            try
            {
                string folder = Folder(kind);
                if (Directory.Exists(folder))
                {
                    string json = Path.Combine(folder, key + ".json");
                    set = File.Exists(json) ? LoadAseprite(device, folder, key, json, id) : LoadFrames(device, folder, key, animations, id);
                }
            }
            catch (Exception)
            {
                set = null;                                // a broken file must never stop the fight
            }

            if (set != null) Loaded++;
            cache[id] = set;
            return set;
        }

        //Picture : one plain PNG of the battle art (a backdrop layer), or null
        public static Texture2D Picture(GraphicsDevice device, string kind, string name)
        {
            string id = kind + "/" + name + ".png";
            FrameSet held;
            if (cache.TryGetValue(id, out held)) return held != null && held.Anims.Count > 0 ? held.Find("still").Pictures[0] : null;

            Texture2D picture = null;
            try
            {
                string path = Path.Combine(Folder(kind), name + ".png");
                if (File.Exists(path)) picture = Texture2D.FromFile(device, path, DefaultColorProcessors.PremultiplyAlpha);
            }
            catch (Exception)
            {
                picture = null;
            }

            FrameSet one = null;
            if (picture != null)
            {
                one = new FrameSet { Size = new Point(picture.Width, picture.Height) };
                one.Anims["still"] = Single(picture, "still");
                Loaded++;
            }
            cache[id] = one;
            return picture;
        }

        private static FrameAnim Single(Texture2D picture, string name)
        {
            return new FrameAnim
            {
                Name = name,
                Pictures = new[] { picture },
                Sources = new[] { new Rectangle(0, 0, picture.Width, picture.Height) },
                Offsets = new[] { Point.Zero },
                Times = new[] { DefaultFrameTime },
                Length = DefaultFrameTime,
            };
        }

        //Way B : <key>_<animation>_<n>.png
        private static FrameSet LoadFrames(GraphicsDevice device, string folder, string key, string[] animations, string id)
        {
            FrameSet set = new FrameSet();
            for (int a = 0; a < animations.Length; a++)
            {
                List<Texture2D> pictures = new List<Texture2D>();
                for (int n = 0; n < 64; n++)
                {
                    string path = Path.Combine(folder, key + "_" + animations[a] + "_" + n + ".png");
                    if (!File.Exists(path)) break;
                    pictures.Add(Texture2D.FromFile(device, path, DefaultColorProcessors.PremultiplyAlpha));
                }
                if (pictures.Count == 0) continue;

                FrameAnim anim = new FrameAnim { Name = animations[a], Loop = Array.IndexOf(loops, animations[a]) >= 0 };
                anim.Pictures = pictures.ToArray();
                anim.Sources = new Rectangle[pictures.Count];
                anim.Offsets = new Point[pictures.Count];
                anim.Times = new float[pictures.Count];
                for (int i = 0; i < pictures.Count; i++)
                {
                    anim.Sources[i] = new Rectangle(0, 0, pictures[i].Width, pictures[i].Height);
                    anim.Times[i] = DefaultFrameTime;
                    set.Size = new Point(Math.Max(set.Size.X, pictures[i].Width), Math.Max(set.Size.Y, pictures[i].Height));
                }
                anim.Length = pictures.Count * DefaultFrameTime;
                SetHit(anim, id, -1);
                set.Anims[anim.Name] = anim;
            }
            return set.Anims.Count > 0 ? set : null;
        }

        //Way A : an Aseprite sheet. Frames in "frames" (an Array, or a Hash in order), animations in meta.frameTags.
        //ADVANCED PART : JsonDocument reads the file as a tree; each frame says where it is in the sheet
        //("frame"), where it sits in the full canvas ("spriteSourceSize", when Aseprite trimmed it), the canvas
        //size ("sourceSize") and how long it stays ("duration", milliseconds).
        private static FrameSet LoadAseprite(GraphicsDevice device, string folder, string key, string jsonPath, string id)
        {
            string sheetPath = Path.Combine(folder, key + ".png");
            if (!File.Exists(sheetPath)) return null;
            Texture2D sheet = Texture2D.FromFile(device, sheetPath, DefaultColorProcessors.PremultiplyAlpha);

            using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(jsonPath)))
            {
                JsonElement root = doc.RootElement;
                List<JsonElement> frames = new List<JsonElement>();
                JsonElement list = root.GetProperty("frames");
                if (list.ValueKind == JsonValueKind.Array)
                    foreach (JsonElement f in list.EnumerateArray()) frames.Add(f);
                else
                    foreach (JsonProperty f in list.EnumerateObject()) frames.Add(f.Value);
                if (frames.Count == 0) return null;

                Rectangle[] sources = new Rectangle[frames.Count];
                Point[] offsets = new Point[frames.Count];
                float[] times = new float[frames.Count];
                FrameSet set = new FrameSet();
                for (int i = 0; i < frames.Count; i++)
                {
                    JsonElement r = frames[i].GetProperty("frame");
                    sources[i] = new Rectangle(r.GetProperty("x").GetInt32(), r.GetProperty("y").GetInt32(), r.GetProperty("w").GetInt32(), r.GetProperty("h").GetInt32());
                    JsonElement trim;
                    if (frames[i].TryGetProperty("spriteSourceSize", out trim))
                        offsets[i] = new Point(trim.GetProperty("x").GetInt32(), trim.GetProperty("y").GetInt32());
                    JsonElement canvas;
                    if (frames[i].TryGetProperty("sourceSize", out canvas))
                        set.Size = new Point(Math.Max(set.Size.X, canvas.GetProperty("w").GetInt32()), Math.Max(set.Size.Y, canvas.GetProperty("h").GetInt32()));
                    JsonElement duration;
                    times[i] = frames[i].TryGetProperty("duration", out duration) ? duration.GetInt32() / 1000f : DefaultFrameTime;
                }

                //Tags : one animation each. No tags : the whole sheet is "idle".
                JsonElement meta, tags;
                bool tagged = root.TryGetProperty("meta", out meta) && meta.TryGetProperty("frameTags", out tags) && tags.GetArrayLength() > 0;
                if (!tagged)
                {
                    FrameAnim whole = Slice("idle", sheet, sources, offsets, times, 0, frames.Count - 1);
                    whole.Loop = true;
                    set.Anims["idle"] = whole;
                    return set;
                }
                foreach (JsonElement tag in meta.GetProperty("frameTags").EnumerateArray())
                {
                    string name = tag.GetProperty("name").GetString().Trim().ToLowerInvariant();
                    int from = Math.Max(0, tag.GetProperty("from").GetInt32());
                    int to = Math.Min(frames.Count - 1, tag.GetProperty("to").GetInt32());
                    if (to < from) continue;
                    FrameAnim anim = Slice(name, sheet, sources, offsets, times, from, to);
                    SetHit(anim, id, HitFromTag(tag));
                    set.Anims[name] = anim;
                }
                return set.Anims.Count > 0 ? set : null;
            }
        }

        //Hit From Tag : an Aseprite tag's User Data such as "hit 5" or "hit=5" (the frame counted inside the tag, from 0)
        private static int HitFromTag(JsonElement tag)
        {
            JsonElement data;
            if (!tag.TryGetProperty("data", out data) || data.ValueKind != JsonValueKind.String) return -1;
            string text = data.GetString().ToLowerInvariant().Replace("=", " ");
            int at = text.IndexOf("hit");
            if (at < 0) return -1;
            string[] words = text.Substring(at + 3).Trim().Split(' ');
            int frame;
            return words.Length > 0 && int.TryParse(words[0], out frame) ? frame : -1;
        }

        private static FrameAnim Slice(string name, Texture2D sheet, Rectangle[] sources, Point[] offsets, float[] times, int from, int to)
        {
            int count = to - from + 1;
            FrameAnim anim = new FrameAnim { Name = name, Loop = Array.IndexOf(loops, name) >= 0 };
            anim.Pictures = new Texture2D[count];
            anim.Sources = new Rectangle[count];
            anim.Offsets = new Point[count];
            anim.Times = new float[count];
            for (int i = 0; i < count; i++)
            {
                anim.Pictures[i] = sheet;
                anim.Sources[i] = sources[from + i];
                anim.Offsets[i] = offsets[from + i];
                anim.Times[i] = times[from + i];
                anim.Length += times[from + i];
            }
            return anim;
        }
    }
}
