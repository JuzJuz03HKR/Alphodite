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
                game.Screens.Change(new EraChoiceScreen(true));
            else
                game.Screens.Change(new RouteScreen());
        }
    }
}
