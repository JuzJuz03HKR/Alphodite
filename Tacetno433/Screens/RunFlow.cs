using Tacetno433.Audio;
using Tacetno433.Data;

namespace Tacetno433.Screens
{
    //RunFlow : what page comes next once a stage is finished.
    //Every page that ends a stage (result, shop, event, rest, recruit, reward) calls this,
    //so the rule lives in one place. Walking into a place, and carrying on a saved run, live
    //here too, for the same reason.
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

        //Enter : walk into a place on the route. The enemy and the event may already be known
        //(a loaded save walks back into the place it left), otherwise pass null to roll them.
        //The run is saved as soon as the place is picked, so quitting inside it comes back here.
        public static void Enter(TacetGame game, RouteNode node, Enemy enemy, GameEvent ev)
        {
            RunState run = game.CurrentRun;
            run.Chosen = node;

            if (node.Type == NodeType.Battle || node.Type == NodeType.Elite || node.Type == NodeType.Boss)
            {
                //Fight : set up the battle and step straight into the duel. There is nothing to
                //arrange first (round 14), every player's part is written on them.
                run.BeginBattle(enemy);
                game.Screens.Change(new DuelScreen());
            }
            else if (node.Type == NodeType.EraShift)
            {
                game.Screens.Change(new EraChoiceScreen(false));
            }
            else if (node.Type == NodeType.Shop)
            {
                game.Screens.Change(new ShopScreen());
            }
            else if (node.Type == NodeType.Rest)
            {
                game.Screens.Change(new RestScreen());
            }
            else
            {
                run.CurrentEvent = ev != null ? ev : EventList.Pick(run.Rng);
                game.Screens.Change(new EventScreen());
            }

            SaveFile.SaveRun(run);
        }

        //Continue : pick up the saved run where it was left. Returns false when there is none.
        public static bool Continue(TacetGame game)
        {
            RunState run = SaveFile.LoadRun(game);
            if (run == null) return false;

            game.CurrentRun = run;
            if (run.AtFloorOpening)
                game.Screens.Change(new ChapterScreen());
            else if (run.Chosen != null)
                Enter(game, run.Chosen, SaveFile.LoadedEnemy, SaveFile.LoadedEvent);
            else
                game.Screens.Change(new RouteScreen());
            return true;
        }

        //Fight Music : ONE track for the whole fight, through every round, so it never restarts. Keep it quiet and without a
        //strong beat : the melody of a duel comes from the baton (see SoundBank phrase notes).
        public static Music FightMusic(RunState run)
        {
            if (run.Battle != null && run.Battle.Enemy.Kind == EnemyKind.Boss) return Music.Boss;
            return Music.Battle;
        }
    }
}
