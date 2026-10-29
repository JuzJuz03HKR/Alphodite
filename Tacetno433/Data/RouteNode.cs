using System.Collections.Generic;

namespace Tacetno433.Data
{
    //NodeType : the kinds of place a path can lead to.
    //KEEP THIS ORDER. The tables below are read using (int)NodeType as the index.
    public enum NodeType
    {
        Battle = 0,
        Event = 1,
        Shop = 2,
        Elite = 3,
        Rest = 4,
        Boss = 5,
        EraShift = 6
    }

    //RouteNode : one place on the floor's map (Beta1, 29 Oct, Data/FloorMap.cs), and one of the paths offered on the
    //route page when the band can walk to it
    public class RouteNode
    {
        public NodeType Type;
        public string Title = "";
        public string Caption = "";
        public string CaptionWrapped = "";   // built once when the node is made, never while drawing

        //Map : where the place is on the floor's map, and the places of the next row its ways lead to
        public int Row;                      // the stage of the floor it belongs to (2 and up, the opening is row 1)
        public int Column;                   // its place in that row, counted from the left
        public List<int> Next = new List<int>();
    }

    //RouteNodeInfo : THE PLACE TO EDIT what each kind of place is called and says.
    //The arrays line up with the NodeType numbers above.
    public static class RouteNodeInfo
    {
        public static string[] Titles =
        {
            "ENCOUNTER",   // Battle
            "???",         // Event
            "SHOP",        // Shop
            "ELITE",       // Elite
            "REST",        // Rest
            "BOSS",        // Boss
            "CROSSING"     // EraShift : NO MORE ERAS (29 Oct), never made now, an old save's CROSSING loads as a ??? (RunState.RestoreMap)
        };

        public static string[] Captions =
        {
            BattleRules.TurnBattleOn ? "A fight with TACET. Win and take whatever it leaves behind." : "A duel with TACET. Win and take whatever it leaves behind.",
            "Something is waiting here. A person, a gift, or a fight.",
            BattleRules.TurnBattleOn ? "Trade shards for motifs, life, seats and musicians." : "Trade shards for motifs, breath and seats.",
            "Harder than it looks. Better spoils, and sometimes a musician.",
            BattleRules.TurnBattleOn ? "Recover, or rehearse. The ensemble chooses one." : "Breathe, or rehearse. The ensemble chooses one.",
            "The last room of this floor. It has been waiting for you.",
            "A door out of this era. The world changes around you."
        };

        //Tags : the small words on the panel that say what kind of place it is
        public static string[] Tags =
        {
            "DUEL",
            "CHANCE",
            "TRADE",
            "DUEL  +",
            "RECOVER",
            "FINALE",
            "TRAVEL"
        };

        //Danger : how many of the three diamonds are filled on the panel
        public static int[] Danger = { 1, 0, 0, 2, 0, 3, 0 };

        //Marks : one letter per kind of place, on the route's road and tiles and in the curtain call
        public static string[] Marks = { "E", "?", "$", "!", "R", "B", "X" };

        //Caption Wrap : the route's detail box prints the whole caption at this width (round 12.2)
        public const float CaptionWrapWidth = 470f;
        public const int CaptionLines = 3;

        //Numbers : the index printed at the top of each panel, "01" to "04" (era and motif pages)
        public static string[] PanelNumbers = { "01", "02", "03", "04", "05", "06" };

        public static string TitleOf(NodeType type)
        {
            return Titles[(int)type];
        }

        public static string CaptionOf(NodeType type)
        {
            return Captions[(int)type];
        }
    }
}
