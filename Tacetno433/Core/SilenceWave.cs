using Microsoft.Xna.Framework.Graphics;

namespace Tacetno433.Core
{
    //SilenceWave : TACET's torn edge (see TacetField) washes in from the right and swallows the
    //page, then pulls back the way it came to show the next one, like a tide. Round 15 : only
    //going into a duel or the curtain call (ScreenManager), the plain pages use SlantWipe.
    public static class SilenceWave
    {
        //Draw : covered 0 is nothing, 1 is the whole screen dark. time keeps the edge moving.
        public static void Draw(SpriteBatch sb, float covered, float time)
        {
            if (covered <= 0f) return;
            if (covered > 1f) covered = 1f;

            //Smoothstep : starts slowly, rushes, then settles
            float ease = covered * covered * (3f - 2f * covered);
            float edge = TacetGame.ScreenW + 30f - ease * (TacetGame.ScreenW + 150f);   // off the right .. off the left
            TacetField.Draw(sb, edge, time, 0.5f);
        }
    }
}
