using System;

namespace ServiceStack.Smoothie.Test.HeartBeats
{
    public static class HeartBeatExts
    {
        public static string Topic(this DateTime d)
        {
            https://fr.wikipedia.org/wiki/Cron
            // minute hour day month day of week mm hh jj MMM JJJ
            /*
            * : à chaque unité (0, 1, 2, 3, 4...)
            5,8 : les unités 5 et 8
            2-5 : les unités de 2 à 5 (2, 3, 4, 5)
            * /3 : toutes les 3 unités (0, 3, 6, 9...)
            10-20/3 : toutes les 3 unités, entre la dixième et la vingtième (10, 13, 16, 19)
             */
            
            var t = $"d.{d.Day}.wd.{d.DayOfWeek.ToString().ToLower()}.h.{d.Hour}.m.{d.Minute}.s.{d.Second}.ms.{d.Millisecond}";
            return t;
        }
    }
}