using System;
using System.Collections.Generic;

namespace Tacetno433.Data
{
    //FloorMap (Beta1, 29 Oct, docs/BETA1_DIRECTION.md 3.1) : the whole floor, made when the floor opens, so the way
    //through it can be planned (go past the elite or not, find the shop before the boss or not), like Slay the Spire's map.
    //   ROWS   one row for every stage of the floor. Row 1 is the floor's opening (its movement card and the band's new
    //          faces), it holds no places. Rows 2 to the one before the last hold 2 to 4 places, the last row is the boss.
    //   WAYS   every place lists the places of the next row its ways lead to (RouteNode.Next). From the opening every
    //          place of row 2 can be reached.
    //The route page draws it as rings closing in on TACET's hole (Screens/RouteScreen.cs).
    //
    //ADVANCED PART : the floor is a DIRECTED ACYCLIC GRAPH (a DAG, docs/report : the chapter on graphs). Directed and
    //acyclic : a way only leads on to the next row, so the band can never come back to a place.
    //Two rows are joined by a STAIRCASE : two fingers start on the first place of each row, and every step moves one
    //finger or both to the next place on their row and draws a way between the places they now touch. The fingers only
    //ever move to the right, so :
    //   two ways never cross on the screen (both rows are drawn left to right)
    //   every place gets at least one way in and one way out (each finger touches every place of its row)
    //   the two ends of the rows are always joined, so the map never falls apart into pieces
    //Reachable checks it with a BFS (breadth first search) : it visits row by row from the opening and counts the places
    //it finds, which has to be every place on the map.
    public class FloorMap
    {
        //Rows : Rows[stage] is one row of places, left to right. Rows[0] and Rows[1] stay empty.
        public RouteNode[][] Rows = new RouteNode[0][];

        //Count : how many places row stage holds (0 for the opening, or a row that does not exist)
        public int Count(int stage)
        {
            return stage >= 0 && stage < Rows.Length ? Rows[stage].Length : 0;
        }

        //At : the place in row stage, column column, or null
        public RouteNode At(int stage, int column)
        {
            if (column < 0 || column >= Count(stage)) return null;
            return Rows[stage][column];
        }

        //Last Row : the boss's row
        public int LastRow
        {
            get { return Rows.Length - 1; }
        }

        //Join : the STAIRCASE between two rows (see the top of the file)
        public static void Join(RouteNode[] from, RouteNode[] to, Random dice)
        {
            int i = 0;
            int j = 0;
            Link(from[0], 0);
            while (i < from.Length - 1 || j < to.Length - 1)
            {
                bool canI = i < from.Length - 1;
                bool canJ = j < to.Length - 1;
                int step = canI && canJ ? dice.Next(3) : (canI ? 0 : 1);   // 0 the first finger moves, 1 the second, 2 both
                if (step != 1) i++;
                if (step != 0) j++;
                Link(from[i], j);
            }
        }

        //Link : one way from a place to the column of the next row, kept in order and never twice
        private static void Link(RouteNode node, int column)
        {
            if (node.Next.Contains(column)) return;
            node.Next.Add(column);
            node.Next.Sort();
        }

        //Reachable : how many places a band can reach from the opening (BFS, see the top of the file). Equal to the number
        //of places on the map when every place can be reached.
        public int Reachable()
        {
            if (Rows.Length < 3) return 0;
            bool[][] seen = new bool[Rows.Length][];
            for (int s = 0; s < Rows.Length; s++) seen[s] = new bool[Count(s)];

            //Start : the opening leads to every place of row 2
            Queue<RouteNode> waiting = new Queue<RouteNode>();
            for (int c = 0; c < Count(2); c++)
            {
                seen[2][c] = true;
                waiting.Enqueue(Rows[2][c]);
            }

            int found = 0;
            while (waiting.Count > 0)
            {
                RouteNode node = waiting.Dequeue();
                found++;
                int next = node.Row + 1;
                for (int k = 0; k < node.Next.Count; k++)
                {
                    int c = node.Next[k];
                    if (next >= Rows.Length || c < 0 || c >= Count(next) || seen[next][c]) continue;
                    seen[next][c] = true;
                    waiting.Enqueue(Rows[next][c]);
                }
            }
            return found;
        }

        //Places : every place on the map
        public int Places()
        {
            int n = 0;
            for (int s = 0; s < Rows.Length; s++) n += Count(s);
            return n;
        }

        //On The Way : whether column column of row stage can still be reached from the place the band stands on
        //(row from, column at, -1 = the opening). The route page dims the places the band can no longer get to.
        public bool OnTheWay(int from, int at, int stage, int column)
        {
            if (stage <= from) return false;
            if (at < 0 || from < 2) return true;                 // from the opening every place can be reached
            List<int> now = new List<int> { at };
            for (int s = from; s < stage; s++)
            {
                List<int> next = new List<int>();
                for (int k = 0; k < now.Count; k++)
                {
                    RouteNode node = At(s, now[k]);
                    if (node == null) continue;
                    for (int w = 0; w < node.Next.Count; w++)
                        if (!next.Contains(node.Next[w])) next.Add(node.Next[w]);
                }
                now = next;
            }
            return now.Contains(column);
        }

        //Write : the map as one line for the save file. Rows from 2 on, split by '/', places split by ',', each place is
        //its kind, ':' and its ways split by '.' : "0:0.1,1:1/3:0,0:0/5:"
        public string Write()
        {
            List<string> rows = new List<string>();
            for (int s = 2; s < Rows.Length; s++)
            {
                List<string> places = new List<string>();
                for (int c = 0; c < Rows[s].Length; c++)
                {
                    List<string> ways = new List<string>();
                    for (int k = 0; k < Rows[s][c].Next.Count; k++) ways.Add(Rows[s][c].Next[k].ToString());
                    places.Add((int)Rows[s][c].Type + ":" + string.Join(".", ways));
                }
                rows.Add(string.Join(",", places));
            }
            return string.Join("/", rows);
        }

        //Read : the kinds and ways of a map written by Write, row by row from row 2 (the run makes the places, so their
        //words are built the same way as a new map's). Null when the line is missing or broken.
        public static List<List<KeyValuePair<NodeType, List<int>>>> Read(string line)
        {
            if (string.IsNullOrEmpty(line)) return null;
            List<List<KeyValuePair<NodeType, List<int>>>> rows = new List<List<KeyValuePair<NodeType, List<int>>>>();
            string[] rowTexts = line.Split('/');
            for (int s = 0; s < rowTexts.Length; s++)
            {
                List<KeyValuePair<NodeType, List<int>>> row = new List<KeyValuePair<NodeType, List<int>>>();
                string[] places = rowTexts[s].Split(',');
                for (int c = 0; c < places.Length; c++)
                {
                    string[] bits = places[c].Split(':');
                    int type;
                    if (bits.Length != 2 || !int.TryParse(bits[0], out type) || type < 0 || type > (int)NodeType.Treasure) return null;
                    List<int> ways = new List<int>();
                    if (bits[1].Length > 0)
                    {
                        string[] wayTexts = bits[1].Split('.');
                        for (int k = 0; k < wayTexts.Length; k++)
                        {
                            int way;
                            if (!int.TryParse(wayTexts[k], out way)) return null;
                            ways.Add(way);
                        }
                    }
                    row.Add(new KeyValuePair<NodeType, List<int>>((NodeType)type, ways));
                }
                rows.Add(row);
            }
            return rows;
        }
    }
}
