// Test program for the report "A report of data structures used in game development 1".
// It compares the draft TURN QUEUE with a copy of TurnBattle.BuildOrder, runs the TargetNode tree
// and checks the route map graph with BFS. Not part of the game build.
﻿using System;
using System.Collections.Generic;

// Stubs that mirror the game's types closely enough to test the report's sketches.
public enum NodeType { Battle = 0, Event = 1, Shop = 2, Elite = 3, Rest = 4, Boss = 5, EraShift = 6 }

public class Fighter
{
    public string Name = "";
    public int Speed;
    public bool Down;
    public bool Twice;      // TWICE A BAR (STUTTER)
    public bool Taunt;
    public int Attacks;
    public override string ToString() { return Name; }
}

public class BarMove
{
    public Fighter Who;
}

public static class BattleRules
{
    public static int TurnCutInSpeed = 5;
    public static int TurnCutInAfter = 2;
    public static int TurnTwiceFirst = 1;
    public static int TurnTwiceSecond = 3;
}

public static class Original
{
    // Faithful copy of TurnBattle.BuildOrder (enemy part), band list already holds FEATHER moves.
    public static void BuildOrder(List<BarMove> band, List<Fighter> enemies, List<Fighter> order, List<BarMove> moves)
    {
        order.Clear();
        moves.Clear();
        List<Fighter> fast = new List<Fighter>();
        List<Fighter> slow = new List<Fighter>();
        List<Fighter> twice = new List<Fighter>();
        for (int i = 0; i < enemies.Count; i++)
        {
            Fighter e = enemies[i];
            if (e.Down) continue;
            if (e.Twice) { twice.Add(e); continue; }
            List<Fighter> list = e.Speed >= BattleRules.TurnCutInSpeed ? fast : slow;
            int at = list.Count;
            while (at > 0 && list[at - 1].Speed < e.Speed) at--;
            list.Insert(at, e);
        }
        for (int i = 0; i < band.Count; i++)
        {
            order.Add(band[i].Who);
            moves.Add(band[i]);
            if (i + 1 == BattleRules.TurnTwiceFirst) Add(twice, order, moves);
            if (i + 1 == BattleRules.TurnCutInAfter) Add(fast, order, moves);
            if (i + 1 == BattleRules.TurnTwiceSecond) Add(twice, order, moves);
        }
        if (band.Count < BattleRules.TurnTwiceFirst) Add(twice, order, moves);
        if (band.Count < BattleRules.TurnCutInAfter) Add(fast, order, moves);
        if (band.Count < BattleRules.TurnTwiceSecond) Add(twice, order, moves);
        Add(slow, order, moves);
    }

    static void Add(List<Fighter> enemies, List<Fighter> order, List<BarMove> moves)
    {
        for (int i = 0; i < enemies.Count; i++) { order.Add(enemies[i]); moves.Add(null); }
    }
}

// ===== Sketch 1 : TURN QUEUE (as printed in the report) =====
public class TurnSlot
{
    public Fighter Who;
    public BarMove Move;      // null for an enemy
}

public static class TurnQueue
{
    //After : the beat right after the band's n-th move (band move i plays on beat i * 10)
    static bool Has(Fighter e) { return e.Twice; }
    static int After(int n) { return n * 10 - 5; }
    const int AfterBand = 10000;

    public static void BuildOrder(List<BarMove> band, List<Fighter> enemies, List<Fighter> order, List<BarMove> moves)
    {
        PriorityQueue<TurnSlot, (int Beat, int Speed, int Seq)> queue = new PriorityQueue<TurnSlot, (int, int, int)>();
        int seq = 0;

        for (int i = 0; i < band.Count; i++)
        {
            TurnSlot move = new TurnSlot { Who = band[i].Who, Move = band[i] };
            queue.Enqueue(move, (i * 10, 0, seq++));
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            Fighter e = enemies[i];
            if (e.Down) continue;
            if (Has(e))                                                    // TWICE A BAR
            {
                int first = After(BattleRules.TurnTwiceFirst);
                int second = After(BattleRules.TurnTwiceSecond);
                queue.Enqueue(new TurnSlot { Who = e }, (first, 0, seq++));
                queue.Enqueue(new TurnSlot { Who = e }, (second, 0, seq++));
                continue;
            }
            bool cutIn = e.Speed >= BattleRules.TurnCutInSpeed;
            int beat = cutIn ? After(BattleRules.TurnCutInAfter) : AfterBand;
            queue.Enqueue(new TurnSlot { Who = e }, (beat, -e.Speed, seq++));
        }

        order.Clear();
        moves.Clear();
        while (queue.Count > 0)
        {
            TurnSlot slot = queue.Dequeue();
            order.Add(slot.Who);
            moves.Add(slot.Move);
        }
    }
}

// ===== Sketch 2 : TARGET TREE =====
public enum TargetTest { None, SomeoneTaunts, OnCue, ByTouch, FirstInLine }
public enum TargetPick { Taunter, LastCalled, NextSeat, FirstInLine, Anyone }

public class TargetNode
{
    public TargetTest Test;      // None = a leaf
    public TargetPick Pick;      // what a leaf chooses
    public TargetNode Yes;
    public TargetNode No;

    public static TargetNode Leaf(TargetPick pick) { return new TargetNode { Pick = pick }; }
    public static TargetNode Ask(TargetTest test, TargetNode yes, TargetNode no) { return new TargetNode { Test = test, Yes = yes, No = no }; }
}

public class FakeBattle
{
    public List<Fighter> Party = new List<Fighter>();
    public bool Cued, ByTouch, FirstInLineTrait;
    public Fighter LastCalled;
    public Random random = new Random(1);

    public static readonly TargetNode Usual =
        TargetNode.Ask(TargetTest.SomeoneTaunts, TargetNode.Leaf(TargetPick.Taunter),
        TargetNode.Ask(TargetTest.OnCue, TargetNode.Leaf(TargetPick.LastCalled),
        TargetNode.Ask(TargetTest.ByTouch, TargetNode.Leaf(TargetPick.NextSeat),
        TargetNode.Ask(TargetTest.FirstInLine, TargetNode.Leaf(TargetPick.FirstInLine),
                                               TargetNode.Leaf(TargetPick.Anyone)))));

    public Fighter Decide(TargetNode root, Fighter enemy)
    {
        TargetNode node = root;
        while (node.Test != TargetTest.None)
            node = Check(node.Test, enemy) ? node.Yes : node.No;
        return Choose(node.Pick, enemy);
    }

    bool Check(TargetTest test, Fighter enemy)
    {
        switch (test)
        {
            case TargetTest.SomeoneTaunts: return Find(true) != null;
            case TargetTest.OnCue: return Cued && LastCalled != null && !LastCalled.Down;
            case TargetTest.ByTouch: return ByTouch;
            case TargetTest.FirstInLine: return FirstInLineTrait;
        }
        return false;
    }

    Fighter Choose(TargetPick pick, Fighter enemy)
    {
        List<Fighter> standing = new List<Fighter>();
        for (int i = 0; i < Party.Count; i++) if (!Party[i].Down) standing.Add(Party[i]);
        if (standing.Count == 0) return null;
        switch (pick)
        {
            case TargetPick.Taunter: return Find(true);
            case TargetPick.LastCalled: return LastCalled;
            case TargetPick.NextSeat: return standing[enemy.Attacks % standing.Count];
            case TargetPick.FirstInLine: return standing[0];
        }
        return standing[random.Next(standing.Count)];
    }

    Fighter Find(bool taunt)
    {
        for (int i = 0; i < Party.Count; i++) if (Party[i].Taunt && !Party[i].Down) return Party[i];
        return null;
    }
}

// ===== Sketch 3 : ROUTE MAP GRAPH =====
public class MapNode
{
    public NodeType Type;
    public int Stage;
    public List<MapNode> Next = new List<MapNode>();
    public override string ToString() { return Type + "@" + Stage; }
}

public static class RouteMap
{
    public static List<List<MapNode>> Build(int stages, Random random)
    {
        List<List<MapNode>> rows = new List<List<MapNode>>();
        for (int s = 1; s < stages; s++)
        {
            int count = 2 + random.Next(3);                      // two to four, like RollOptions
            List<MapNode> row = new List<MapNode>();
            for (int i = 0; i < count; i++)
                row.Add(new MapNode { Type = (NodeType)random.Next(5), Stage = s });
            rows.Add(row);
        }
        rows.Add(new List<MapNode> { new MapNode { Type = NodeType.Boss, Stage = stages } });

        for (int r = 0; r + 1 < rows.Count; r++)
        {
            List<MapNode> here = rows[r];
            List<MapNode> next = rows[r + 1];
            for (int i = 0; i < here.Count; i++)
            {
                int j = i * next.Count / here.Count;
                Link(here[i], next[j]);
                if (j + 1 < next.Count && random.Next(2) == 0) Link(here[i], next[j + 1]);
            }
            for (int j = 0; j < next.Count; j++)
                if (!HasWayIn(here, next[j])) Link(here[j * here.Count / next.Count], next[j]);
        }
        return rows;
    }

    static void Link(MapNode from, MapNode to)
    {
        if (!from.Next.Contains(to)) from.Next.Add(to);
    }

    static bool HasWayIn(List<MapNode> row, MapNode node)
    {
        for (int i = 0; i < row.Count; i++)
            if (row[i].Next.Contains(node)) return true;
        return false;
    }

    public static HashSet<MapNode> Reach(MapNode start)
    {
        HashSet<MapNode> seen = new HashSet<MapNode>();
        Queue<MapNode> queue = new Queue<MapNode>();
        seen.Add(start);
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            MapNode node = queue.Dequeue();
            for (int i = 0; i < node.Next.Count; i++)
            {
                MapNode to = node.Next[i];
                if (seen.Contains(to)) continue;
                seen.Add(to);
                queue.Enqueue(to);
            }
        }
        return seen;
    }
}

public static class Program
{
    public static void Main()
    {
        Random rng = new Random(7);
        int same = 0, total = 20000;
        for (int t = 0; t < total; t++)
        {
            List<BarMove> band = new List<BarMove>();
            int bn = rng.Next(0, 7);
            for (int i = 0; i < bn; i++) band.Add(new BarMove { Who = new Fighter { Name = "B" + i } });
            List<Fighter> enemies = new List<Fighter>();
            int en = rng.Next(0, 7);
            for (int i = 0; i < en; i++)
                enemies.Add(new Fighter { Name = "E" + i, Speed = rng.Next(1, 9), Down = rng.Next(6) == 0, Twice = rng.Next(5) == 0 });

            List<Fighter> o1 = new List<Fighter>(), o2 = new List<Fighter>();
            List<BarMove> m1 = new List<BarMove>(), m2 = new List<BarMove>();
            Original.BuildOrder(band, enemies, o1, m1);
            TurnQueue.BuildOrder(band, enemies, o2, m2);
            bool ok = o1.Count == o2.Count;
            for (int i = 0; ok && i < o1.Count; i++) ok = o1[i] == o2[i] && m1[i] == m2[i];
            if (ok) same++;
            else if (total - same < 3) Console.WriteLine("DIFF: " + string.Join(",", o1) + " | " + string.Join(",", o2));
        }
        Console.WriteLine("TurnQueue same order: " + same + "/" + total);

        FakeBattle fb = new FakeBattle();
        for (int i = 0; i < 4; i++) fb.Party.Add(new Fighter { Name = "M" + i });
        fb.Party[2].Taunt = true;
        Console.WriteLine("Tree taunt -> " + fb.Decide(FakeBattle.Usual, new Fighter()));
        fb.Party[2].Taunt = false; fb.ByTouch = true;
        Console.WriteLine("Tree bytouch attacks=5 -> " + fb.Decide(FakeBattle.Usual, new Fighter { Attacks = 5 }));

        int okMaps = 0;
        for (int t = 0; t < 2000; t++)
        {
            List<List<MapNode>> rows = RouteMap.Build(8, new Random(t));
            MapNode boss = rows[rows.Count - 1][0];
            bool all = true;
            for (int i = 0; i < rows[0].Count; i++) if (!RouteMap.Reach(rows[0][i]).Contains(boss)) all = false;
            // every node must have a way in (except the first row) and a way out (except the boss)
            for (int r = 0; r < rows.Count - 1; r++) for (int i = 0; i < rows[r].Count; i++) if (rows[r][i].Next.Count == 0) all = false;
            if (all) okMaps++;
        }
        Console.WriteLine("Maps where every start reaches the boss: " + okMaps + "/2000");
        List<List<MapNode>> demo = RouteMap.Build(8, new Random(3));
        foreach (var row in demo)
        {
            List<string> parts = new List<string>();
            foreach (var n in row) { List<string> nx = new List<string>(); foreach (var x in n.Next) nx.Add(demo[x.Stage - 1].IndexOf(x).ToString()); parts.Add(n.Type + "->[" + string.Join(",", nx) + "]"); }
            Console.WriteLine("S" + row[0].Stage + ": " + string.Join("  ", parts));
        }
    }
}
