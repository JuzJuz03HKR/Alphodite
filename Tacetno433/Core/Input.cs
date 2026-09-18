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

        //Input Update : call once per frame, before any screen updates
        public static void Update()
        {
            keyOld = keyNow;
            keyNow = Keyboard.GetState();

            mouseOld = mouseNow;
            mouseNow = Mouse.GetState();
            MousePos = new Vector2(mouseNow.X, mouseNow.Y);
        }

        //Key Held : true every frame the key is down
        public static bool KeyDown(Keys key)
        {
            return keyNow.IsKeyDown(key);
        }

        //Key Pressed : true only on the frame the key goes down
        public static bool KeyPressed(Keys key)
        {
            return keyNow.IsKeyDown(key) && keyOld.IsKeyUp(key);
        }

        //Mouse Held
        public static bool MouseDown()
        {
            return mouseNow.LeftButton == ButtonState.Pressed;
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
