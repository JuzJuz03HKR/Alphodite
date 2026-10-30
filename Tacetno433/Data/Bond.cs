using System.Collections.Generic;

namespace Tacetno433.Data
{
    //Bond : BONDS (Beta1 30 Oct, docs/BETA1_DIRECTION.md 11.7 point 8 and 13.3), a tie between a legend and a conductor (or an
    //enemy) that really happened. A small effect that shows (a legend tied to the conductor plays their first move of every
    //fight x TurnBondScale, TurnBattle.MoveScale), a tag on the legend's column at THE AUDITION, and a short line when a fight
    //starts (like the ties of Hades).
    public class Bond
    {
        public TurnKit Conductor = TurnKit.None;   // the conductor it ties to, None for an enemy bond
        public string Enemy = "";                  // the enemy it ties to (an enemy bond), "" for none
        public int Musician;                       // the legend's place in MusicianList.All
        public string Name = "";                   // "OP. 5", the tag on the column
        public string Fact = "";                   // what happened, said when the legend is picked
        public string Line = "";                   // what the legend says when a fight starts
    }

    //BondList : THE PLACE TO EDIT BONDS. The facts were checked (30 Oct) : Beethoven's cello sonatas Op. 5 were first played in
    //Berlin in 1796 by Beethoven and a Duport (most now say Jean-Louis, whose cello this is, some his brother), the Horn Sonata
    //Op. 17 was written for Punto and first played by the two of them in Vienna on 18 April 1800, Quantz taught Frederick the
    //Great the flute and the king gave Bach the theme of the Musical Offering (1747), Pfundt came to the Gewandhaus in 1835 at
    //the call of its conductor Felix Mendelssohn, Fanny's brother. PAGANINI and THE DEVIL'S STRING is the legend of the devil.
    public static class BondList
    {
        public static Bond[] All = new Bond[]
        {
            new Bond { Conductor = TurnKit.Beethoven, Musician = 6, Name = "OP. 5",
                       Fact = "BOND OP. 5 : THE CELLO SONATAS, BERLIN 1796  -  FIRST MOVE OF A FIGHT x1.5",
                       Line = "DUPORT : Berlin again, maestro. The cello remembers its part." },
            new Bond { Conductor = TurnKit.Beethoven, Musician = 7, Name = "OP. 17",
                       Fact = "BOND OP. 17 : THE HORN SONATA, VIENNA 1800  -  FIRST MOVE OF A FIGHT x1.5",
                       Line = "PUNTO : The horn first, maestro. As in Vienna." },
            new Bond { Conductor = TurnKit.Bach, Musician = 1, Name = "THE ROYAL THEME",
                       Fact = "BOND THE ROYAL THEME : QUANTZ'S PUPIL GAVE BACH THE THEME  -  FIRST MOVE OF A FIGHT x1.5",
                       Line = "QUANTZ : The king's theme. Play it as he never could." },
            new Bond { Conductor = TurnKit.Fanny, Musician = 2, Name = "LEIPZIG",
                       Fact = "BOND LEIPZIG : PFUNDT PLAYED IN FELIX'S GEWANDHAUS  -  FIRST MOVE OF A FIGHT x1.5",
                       Line = "PFUNDT : Felix kept the time in Leipzig. Tonight it is yours." },
            new Bond { Enemy = "THE DEVIL'S STRING", Musician = 0, Name = "THE DEVIL",
                       Fact = "BOND THE DEVIL : AGAINST THE DEVIL'S STRING, PAGANINI HITS x1.5",
                       Line = "PAGANINI : So you are the one they meant." },
        };

        //Of : the bond between this legend and this conductor, null for none
        public static Bond Of(Musician m, TurnKit conductor)
        {
            int i = System.Array.IndexOf(MusicianList.All, m);
            for (int k = 0; k < All.Length; k++)
                if (All[k].Conductor != TurnKit.None && All[k].Conductor == conductor && All[k].Musician == i) return All[k];
            return null;
        }

        //Count : how many legends of the band are bonded to the conductor
        public static int Count(List<Musician> band, TurnKit conductor)
        {
            int count = 0;
            for (int i = 0; i < band.Count; i++)
                if (Of(band[i], conductor) != null) count++;
            return count;
        }

        //Against : the enemy bond of this legend with this enemy, null for none
        public static Bond Against(Musician m, Enemy enemy)
        {
            if (enemy == null) return null;
            int i = System.Array.IndexOf(MusicianList.All, m);
            for (int k = 0; k < All.Length; k++)
                if (All[k].Enemy.Length > 0 && All[k].Enemy == enemy.Name && All[k].Musician == i) return All[k];
            return null;
        }

        //Opening Line : the line said when a fight starts, the first bond of the band with the conductor or one of the enemies,
        //"" for none
        public static string OpeningLine(List<Musician> band, TurnKit conductor, List<Enemy> enemies)
        {
            for (int i = 0; i < band.Count; i++)
                for (int e = 0; e < enemies.Count; e++)
                {
                    Bond b = Against(band[i], enemies[e]);
                    if (b != null) return b.Line;
                }
            for (int i = 0; i < band.Count; i++)
            {
                Bond b = Of(band[i], conductor);
                if (b != null) return b.Line;
            }
            return "";
        }
    }
}
