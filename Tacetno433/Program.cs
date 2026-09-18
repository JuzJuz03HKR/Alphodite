//Developer Capture : only does anything when the game is started with --shots (see DebugShots)
Tacetno433.Core.DebugShots.Parse(args);

using var game = new Tacetno433.TacetGame();
game.Run();
