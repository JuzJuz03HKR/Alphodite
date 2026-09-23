using Tacetno433.Audio;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //RunFlow : what page comes next once a stage is finished.
    //Every page that ends a stage (result, shop, event, rest, recruit, reward) calls this,
    //so the rule lives in one place.
    public static class RunFlow
    {
        //Next Stage : close any fight, step forward, and open the right page
        public static void NextStage(TacetGame game)
        {
            RunState run = game.CurrentRun;
            run.FinishBattle();
            run.Advance();

            if (run.RunComplete)
                game.Screens.Change(new CurtainCallScreen(true));
            else if (run.AtFloorOpening)
                game.Screens.Change(new ChapterScreen());    // a new floor opens with its movement card
            else
                game.Screens.Change(new RouteScreen());
        }

        //Fight Music : ONE track for the whole fight, from the stage page through every round,
        //so it never restarts when the pages swap between rounds. Keep it quiet and without a
        //strong beat : the melody of a duel comes from the baton (see SoundBank phrase notes).
        public static Music FightMusic(RunState run)
        {
            if (run.Battle != null && run.Battle.Enemy.Kind == EnemyKind.Boss) return Music.Boss;
            return Music.Battle;
        }
    }
}
