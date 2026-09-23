using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //HandPose : one drawing of the conductor's hand. The duel switches pose the moment the
    //player flicks the baton, so the hand follows what the player just did.
    public enum HandPose { Ready, Down, Left, Right, Up, Signature }

    //HandArt : THE PLACE WHERE THE HAND PICTURES GO.
    //
    //HOW TO ADD THE ARTWORK
    //  1. put the pictures in  Content/Art/Hand  and name them like this
    //         hand_ready_0.png
    //         hand_down_0.png   hand_down_1.png   hand_down_2.png   ...
    //         hand_left_0.png   hand_right_0.png   hand_up_0.png   hand_signature_0.png
    //     one picture is enough for a pose. Several numbered ones play as an animation,
    //     in order, at FrameTime seconds each.
    //  2. add them in the MGCB editor so they get built
    //  3. that is all. Nothing else in the game has to change.
    //
    //Until the pictures exist an empty ArtSlot box marks where the hand goes.
    public static class HandArt
    {
        public const int PoseCount = 6;
        public const int MaxFrames = 8;

        //Pose Files : the middle part of the file name for each pose
        private static string[] poseFile = { "ready", "down", "left", "right", "up", "signature" };

        //Hand Frames : Frames[pose] is the list of pictures for that pose, null until loaded
        public static Texture2D[][] Frames = new Texture2D[PoseCount][];

        //Hand Timing : how long one frame of an animation is shown, and how long a pose is
        //held before the hand goes back to READY
        public static float FrameTime = 0.07f;
        public static float HoldTime = 0.32f;

        public static int LoadedPoses;

        //Hand Load : called once from LoadContent. Missing pictures are skipped quietly,
        //exactly like the sounds, so the game runs today with no artwork at all.
        public static void Load(ContentManager content)
        {
            LoadedPoses = 0;

            for (int p = 0; p < PoseCount; p++)
            {
                Texture2D[] found = new Texture2D[MaxFrames];
                int have = 0;

                for (int f = 0; f < MaxFrames; f++)
                {
                    string name = "Art/Hand/hand_" + poseFile[p] + "_" + f;
                    if (!Exists(content, name)) break;

                    try
                    {
                        found[f] = content.Load<Texture2D>(name);
                        have++;
                    }
                    catch (Exception) { break; }
                }

                if (have == 0) continue;

                Frames[p] = new Texture2D[have];
                for (int f = 0; f < have; f++) Frames[p][f] = found[f];
                LoadedPoses++;
            }
        }

        private static bool Exists(ContentManager content, string name)
        {
            string path = Path.Combine(AppContext.BaseDirectory, content.RootDirectory, name + ".xnb");
            return File.Exists(path);
        }

        //Hand Frame Count : 1 when there is no artwork yet, so the rest of the code is the same
        public static int FrameCount(HandPose pose)
        {
            Texture2D[] list = Frames[(int)pose];
            return list == null ? 1 : list.Length;
        }

        //Hand Draw : the picture if it is there, the empty slot if it is not
        public static void Draw(SpriteBatch sb, HandPose pose, int frame, Rectangle box, Color ink, float alpha)
        {
            Texture2D[] list = Frames[(int)pose];

            if (list != null)
            {
                if (frame < 0) frame = 0;
                if (frame >= list.Length) frame = list.Length - 1;
                sb.Draw(list[frame], box, Color.White * alpha);
                return;
            }

            ArtSlot.Draw(sb, box, ink, alpha);
        }
    }

    //HandAnim : keeps track of which pose is showing and which frame of it.
    //A pose plays once and then falls back to READY by itself.
    public class HandAnim
    {
        public HandPose Pose = HandPose.Ready;
        public int Frame;

        private float timer;

        //Hand Play : called the moment the player flicks
        public void Play(HandPose pose)
        {
            Pose = pose;
            Frame = 0;
            timer = 0f;
        }

        public void Update(float dt)
        {
            if (Pose == HandPose.Ready) return;

            timer += dt;

            int frames = HandArt.FrameCount(Pose);
            if (frames > 1)
            {
                Frame = (int)(timer / HandArt.FrameTime);
                if (Frame >= frames) Frame = frames - 1;
            }

            //Pose Over : a picture animation ends when its last frame has been shown,
            //a single picture is simply held for a moment
            float length = frames > 1 ? frames * HandArt.FrameTime : HandArt.HoldTime;
            if (timer >= length) Play(HandPose.Ready);
        }
    }
}
