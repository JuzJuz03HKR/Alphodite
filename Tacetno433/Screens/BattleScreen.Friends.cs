using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tacetno433.Battle;
using Tacetno433.Core;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //======================================================================================
    //FRIEND PICK (Beta1 31 Oct, docs/BETA1_DIRECTION.md 14.5 choice A). The user tried the new battle screen : a skill for one
    //friend (SHIELD, THE LESSON) sent the eyes up to THE BAND row at the top left to find who to give it to, and nothing led them
    //there. Now the friends it can go to come up as big cards right over the commands, where the eyes already are.
    //   FRIEND ROW   one card per friend the skill can go to, in THE BAND's order : the face, the name, the life, what they get,
    //                the attack TACET aims at them (SHIELD goes where the blow lands) and the move they have written (THE LESSON
    //                goes to the one who plays a skill)
    //   PICK         click a card or press its number. The row closes by itself once a friend is picked, right click goes back.
    //                THE BAND row at the top left and the musician in front still take a click as before.
    //The skill card moves up over the row, so the skill still says what it does. The row's words are made when it opens, the
    //numbers come from the rules (TurnBattle.Preview), Draw only draws.
    //======================================================================================
    public partial class BattleScreen
    {
        private const int FriendW = 288;          // a friend's card : the face (112 x 112, drawn at the size it was drawn) and the words
        private const int FriendH = 144;          //   tall enough for the two lines of the foot (The Foot), the face in the middle
        private const int FriendGap = 12;
        private const int FriendFace = 112;
        private const int FriendLine = 16;        // the foot's two lines of small words, this far apart
        private const int FriendBottom = 584;     // the row's foot, over the words above the commands
        private const float FriendRise = 0.16f;   // seconds for the row to slide up (and down again)
        private const float FriendLift = 18f;     //   from this many pixels lower
        private static readonly string[] friendKey = { "1", "2", "3" };

        private readonly List<Fighter> friendPick = new List<Fighter>();   // the friends the waiting skill can go to
        private MovePreview[] friendAim = new MovePreview[0];               // what each would get
        private Rectangle[] friendBox = new Rectangle[0];
        private string[] friendGets = new string[0];                        // what they get, in the kind's colour
        private string[] friendMore = new string[0];                        //   the line under it ("" for none)
        private string[] friendMove = new string[0];                        // the move they have written ("" for none)
        private string[] friendIncoming = new string[0];                    // the attack aimed at them ("" for none)
        private Ico friendIcon;
        private bool friendIconOn;                // SHIELD and THE LESSON show their icon by what they get, a heal shows its number only
        private Skill friendFor;                  // the skill the row was made for, null while it is shut
        private Fighter friendBy;                 //   and the one who plays it
        private float friendOpen;                 // 0 shut, 1 all the way up
        private int friendHover = -1;             // the card under the pointer, -1 for none
        private Rectangle cardShown;              // where the skill card is drawn this frame (over the row while it is open)

        //Friend Row Wanted : a skill for one friend is waiting for its friend
        private bool FriendRowWanted
        {
            get { return view == View.Target && pending != null && pending.Target == SkillTarget.Ally && selected != null; }
        }

        //Friend Row Ready : the row was made for the skill waiting now, so its cards can be clicked (while it slides up too)
        private bool FriendRowReady
        {
            get { return FriendRowWanted && friendFor == pending && friendBy == selected; }
        }

        //======================================================================================
        //Update
        //======================================================================================

        //Update Friend Row : after Update Aim, every frame. Makes the row when a skill for one friend starts waiting, slides it
        //up and down, and puts the skill card over it.
        private void UpdateFriendRow(float dt)
        {
            bool want = FriendRowWanted;
            if (want && !FriendRowReady) BuildFriendRow();
            if (!want) friendFor = null;
            friendOpen = MathHelper.Clamp(friendOpen + (want ? dt : -dt) / FriendRise, 0f, 1f);

            friendHover = -1;
            if (want)
                for (int k = 0; k < friendBox.Length; k++)
                    if (Input.MouseOver(friendBox[k])) friendHover = k;

            //The Skill Card : over the command as before, or centred over the row while it is open
            cardShown = cardBox;
            if (friendOpen > 0f && friendBox.Length > 0)
            {
                float e = Smooth(friendOpen);
                int x = RowCentre - cardBox.Width / 2;
                int y = friendBox[0].Y - 12 - cardBox.Height;
                cardShown.X = (int)MathHelper.Lerp(cardBox.X, x, e);
                cardShown.Y = (int)MathHelper.Lerp(cardBox.Y, y, e);
            }
        }

        //Row Centre : the middle of the three commands, the row and the card above it line up on it
        private static int RowCentre
        {
            get { return (commandBox[0].X + commandBox[commandBox.Length - 1].Right) / 2; }
        }

        //Build Friend Row : who the skill can go to, and each card's words (made here, never in Draw)
        private void BuildFriendRow()
        {
            friendFor = pending;
            friendBy = selected;
            friendPick.Clear();
            for (int i = 0; i < battle.Party.Count; i++)
                if (battle.CanAim(selected, pending, battle.Party[i])) friendPick.Add(battle.Party[i]);

            int n = friendPick.Count;
            if (friendAim.Length != n)
            {
                friendAim = new MovePreview[n];
                for (int k = 0; k < n; k++) friendAim[k] = new MovePreview();
                friendBox = new Rectangle[n];
                friendGets = new string[n];
                friendMore = new string[n];
                friendMove = new string[n];
                friendIncoming = new string[n];
            }

            friendIconOn = pending.Shield || pending.Lesson || pending.Guard;
            friendIcon = pending.Shield ? Ico.Shield : (pending.Lesson ? Ico.Lesson : Ico.Guard);

            int width = n * FriendW + Math.Max(0, n - 1) * FriendGap;
            int left = RowCentre - width / 2;
            for (int k = 0; k < n; k++)
            {
                Fighter f = friendPick[k];
                friendBox[k] = new Rectangle(left + k * (FriendW + FriendGap), FriendBottom - FriendH, FriendW, FriendH);
                battle.Preview(selected, pending, f, friendAim[k]);
                string more;
                friendGets[k] = GetsLine(pending, friendAim[k], selected, out more);
                friendMore[k] = more;

                BarMove move = battle.MoveOf(f);
                if (move != null) friendMove[k] = move.Skill == null ? WordAttack : move.Skill.Name;
                else friendMove[k] = battle.CanPlan(f) ? WordAttack : "";                  // nothing written : it ATTACKs

                int incoming = IncomingTo(f);
                friendIncoming[k] = incoming > 0 ? "INCOMING " + incoming : "";
            }
        }

        //Gets Line : what a friend gets from the skill, in a few words, and the line under it
        private static string GetsLine(Skill s, MovePreview p, Fighter by, out string more)
        {
            more = "";
            if (p.Heal > 0) return "+" + p.Heal + " LIFE";
            if (p.Crescendo > 0) return "+" + p.Crescendo + " " + Words.Volume;
            if (s.Shield)
            {
                more = "x" + Share(p.Share) + " DAMAGE";                                  // SHIELD : the share DUPORT takes
                return "HITS GO TO " + (by != null ? by.Name : "DUPORT");
            }
            if (s.Lesson)
            {
                more = "NEXT SKILL x" + Share(p.Share);                                  // THE LESSON leaves FOCUS (the card says the rest)
                return "+" + Words.Focus;
            }
            if (s.Guard) return Words.Guard + " : HITS x" + Share(p.Share);
            return s.Name;
        }

        //Incoming To : the life TACET's next attacks would take off this musician if none of their notes were blocked (the numbers
        //over the enemies, added up). THE COURT hits everyone. SHIELD : the band's target mark already moves to the one in front.
        private int IncomingTo(Fighter f)
        {
            int total = 0;
            for (int e = 0; e < battle.Enemies.Count; e++)
            {
                Fighter enemy = battle.Enemies[e];
                if (enemy.Down || !TurnBattle.Aims(enemy)) continue;
                if (battle.HitTarget(enemy) != f && !enemy.IntentAll) continue;
                for (int n = 0; n < enemy.IntentNotes.Length; n++) total += enemy.IntentNotes[n];
            }
            return total;
        }

        //Friend Under Pointer : the friend whose card is under the pointer, null for none (or while the row is not this skill's)
        private Fighter FriendUnderPointer()
        {
            if (!FriendRowReady) return null;
            for (int k = 0; k < friendBox.Length && k < friendPick.Count; k++)
                if (Input.MouseOver(friendBox[k])) return friendPick[k];
            return null;
        }

        //Friend By Key : the friend whose number was pressed (1 2 3 on the row or the number pad), null for none
        private Fighter FriendByKey()
        {
            if (!FriendRowReady) return null;
            for (int k = 0; k < friendPick.Count && k < friendKey.Length; k++)
                if (Input.KeyPressed(Keys.D1 + k) || Input.KeyPressed(Keys.NumPad1 + k)) return friendPick[k];
            return null;
        }

        //======================================================================================
        //Draw
        //======================================================================================

        //Draw Friend Row : the cards over the commands, sliding up as the row opens. The card under the pointer (or the same
        //friend pointed at in THE BAND row) steps up and takes the frame of the skill's kind.
        private void DrawFriendRow(SpriteBatch sb)
        {
            if (friendOpen <= 0f || friendBox.Length == 0) return;
            float e = Smooth(friendOpen);
            int lift = (int)((1f - e) * FriendLift);
            float pulse = 0.65f + 0.35f * (float)Math.Sin(time * 5f);
            Color gets = Color.Lerp(aimColor, Color.White, 0.25f);

            for (int k = 0; k < friendPick.Count && k < friendBox.Length; k++)
            {
                Fighter f = friendPick[k];
                bool over = k == friendHover || (view == View.Target && hover == f);
                Rectangle r = friendBox[k];
                r.Y += lift - (over ? 4 : 0);
                float a = e;

                Gfx.Rect(sb, new Rectangle(r.X + 5, r.Y + 5, r.Width, r.Height), Color.Black * (0.5f * a));
                Gfx.Rect(sb, r, Palette.Void * (0.95f * a));
                Gfx.Rect(sb, r.X, r.Y, 5, r.Height, aimColor * a);                                  // the kind's band, like the skill card
                Gfx.RectOutline(sb, r, (over ? aimColor : Palette.LineGrey * 0.6f) * a, over ? 2 : 1);

                //The Face : the art at the size it was drawn, the empty frame until it comes (docs/UI_ART_PLAN.md 2.1), in the middle
                //of the card's height
                Rectangle face = new Rectangle(r.X + 12, r.Y + (r.Height - FriendFace) / 2, FriendFace, FriendFace);
                Gfx.Rect(sb, face, Palette.CanvasDark * a);
                ArtBank.DrawOrSlot(sb, UiArt.FaceOf(f.Musician), face, Palette.Paper, a);

                //The Name, a stroke of their own colour under it, and the number to press
                float x = face.Right + 12;
                float room = r.Right - 10 - x;
                Gfx.TextSpaced(sb, Game.Font, f.Name, x, r.Y + 9, Palette.Highlight * a, TextSize.Label, 2f);
                Gfx.Rect(sb, x, r.Y + 27, Math.Min(room - 30f, Gfx.SpacedWidth(Game.Font, f.Name, TextSize.Label, 2f)), 2f, SpotOf(f).Tint * a);
                if (k < friendKey.Length) Ui.KeyChipRight(sb, friendKey[k], r.Right - 10, r.Y + 17, (over ? Palette.Highlight : Palette.PaperDim) * a);

                //The Life, and the part a heal would give back blinking (MARKS AND AIM)
                Rectangle bar = new Rectangle((int)x, r.Y + 35, (int)room, 5);
                Gfx.Bar(sb, bar, f.Hp / (float)f.MaxHp, Palette.Stage * a, Palette.Paper * a);
                int gain = f.Slot < friendAim[k].Gain.Length ? friendAim[k].Gain[f.Slot] : 0;
                DrawLifePreview(sb, bar, f, 0, gain, a);
                string hp = NumberText.Get(f.Hp);
                Gfx.Text(sb, Game.Font, hp, x, r.Y + 43, Palette.Highlight * a, TextSize.Body);
                Gfx.Text(sb, Game.Font, maxLabel[f], x + 4 + Gfx.TextWidth(Game.Font, hp, TextSize.Body), r.Y + 48, Palette.LineGrey * a, TextSize.Tiny);

                //What They Get : the icon (SHIELD, THE LESSON) and the words in the kind's colour
                float gx = x;
                float gy = r.Y + 70;
                if (friendIconOn)
                {
                    Ui.Icon(sb, new Rectangle((int)gx, (int)gy - 2, IconSize, IconSize), icoArt.Length > (int)friendIcon ? icoArt[(int)friendIcon] : null, IcoCode[(int)friendIcon], aimColor, false, 0, a);
                    gx += IconSize + 6f;
                }
                Gfx.Text(sb, Game.Font, friendGets[k], gx, gy, gets * a, TextSize.Label);
                if (friendMore[k].Length > 0) Gfx.Text(sb, Game.Font, friendMore[k], gx, gy + 17, Palette.PaperDim * a, TextSize.Tiny);

                //The Foot : the move they have written on the right, and on the line under it TACET's mark and the attack aimed at
                //them on the left. Each has a line of its own, so a long move name (NAPOLEON'S DENT) never runs into INCOMING.
                float fy = r.Bottom - 15;
                if (friendMove[k].Length > 0) Gfx.TextRight(sb, Game.Font, friendMove[k], r.Right - 10, fy - 8f - FriendLine, Palette.LineGrey * a, TextSize.Tiny);
                if (friendIncoming[k].Length > 0)
                {
                    DrawTargetSign(sb, x + 6f, fy, 6f, Palette.Accent * (pulse * a));
                    Gfx.Text(sb, Game.Font, friendIncoming[k], x + 18f, fy - 8f, Palette.Accent * a, TextSize.Tiny);
                }
            }
        }
    }
}
