using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Tacetno433.Core
{
    //Input : keeps this frame's and last frame's keyboard/mouse state,
    //so we can ask "was this key just pressed" instead of "is it held".
    public static class Input
    {
        private static KeyboardState keyNow, keyOld;
        private static MouseState mouseNow, mouseOld;

        public static Vector2 MousePos;

        //View : where the game picture sits inside the window, set by TacetGame.
        //In a window it is exactly the game size, in full screen it is bigger and moved.
        private static Rectangle view = new Rectangle(0, 0, TacetGame.ScreenW, TacetGame.ScreenH);

        public static void SetView(Rectangle r)
        {
            if (r.Width > 0 && r.Height > 0) view = r;
        }

        //Input Update : call once per frame, before any screen updates
        public static void Update()
        {
            keyOld = keyNow;
            keyNow = Keyboard.GetState();

            mouseOld = mouseNow;
            mouseNow = Mouse.GetState();

            //Mouse In Game Pixels : take off where the picture starts, then scale the rest
            //back down to 1280 x 720. Every page can keep using the positions it was written with.
            float scale = TacetGame.ScreenW / (float)view.Width;
            MousePos = new Vector2((mouseNow.X - view.X) * scale, (mouseNow.Y - view.Y) * scale);
        }

        //Key Held : true every frame the key is down
        public static bool KeyDown(Keys key)
        {
            return keyNow.IsKeyDown(key);
        }

        //Key Pressed : true only on the frame the key goes down
        public static bool KeyPressed(Keys key)
        {
            if (key == PretendPress && key != Keys.None) return true;
            return keyNow.IsKeyDown(key) && keyOld.IsKeyUp(key);
        }

        //Pretend Press : DEVELOPER TOOL ONLY. DebugShots sets a key here for a single frame,
        //so a picture can show what that key does (the pause, the signature). Never set in play.
        public static Keys PretendPress = Keys.None;

        //Pretend Held : DEVELOPER TOOL ONLY. DebugShots sets it so the baton can be seen
        //conducting in its pictures. It is never set during normal play.
        public static bool PretendHeld;

        //Mouse Held
        public static bool MouseDown()
        {
            return PretendHeld || mouseNow.LeftButton == ButtonState.Pressed;
        }

        //Mouse Clicked : true only on the frame the button goes down
        public static bool MouseClicked()
        {
            return mouseNow.LeftButton == ButtonState.Pressed && mouseOld.LeftButton == ButtonState.Released;
        }

        //Mouse Released : true only on the frame the button comes back up (end of a drag)
        public static bool MouseReleased()
        {
            return mouseNow.LeftButton == ButtonState.Released && mouseOld.LeftButton == ButtonState.Pressed;
        }

        //Mouse Right Clicked
        public static bool MouseRightClicked()
        {
            return mouseNow.RightButton == ButtonState.Pressed && mouseOld.RightButton == ButtonState.Released;
        }

        //Mouse Over : is the cursor inside this box
        public static bool MouseOver(Rectangle box)
        {
            return box.Contains((int)MousePos.X, (int)MousePos.Y);
        }

        //Mouse Clicked Box : clicked while inside this box
        public static bool ClickedOn(Rectangle box)
        {
            return MouseClicked() && MouseOver(box);
        }
    }
}
