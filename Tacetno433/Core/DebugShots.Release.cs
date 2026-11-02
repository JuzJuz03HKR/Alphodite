#if !DEBUG
using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //DebugShots in a Release build (Beta1 2 Nov, block H item 2) : the capture and test tool (--shots, Core/DebugShots*.cs) is
    //the team's and builds only in Debug. The game that ships asks a few things of it, and here they are all off : no pictures,
    //no fixed dice, no pretend mouse. Change nothing here : add to the real tool, and only add a line here when the game itself
    //starts asking the tool something new.
    public static class DebugShots
    {
        public static bool Active;
        public static float Scale = 1f;
        public static bool Steady;
        public static RenderTarget2D Target;

        public static void Parse(string[] args) { }
        public static void FakeMouse(float dt) { }
        public static void Begin(TacetGame game) { }
        public static void AfterDraw(TacetGame game) { }
    }
}
#endif
