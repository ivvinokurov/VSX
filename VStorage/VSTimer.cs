using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStorage
{
    public class VSTimer
    {
        /// <summary>
        /// DEBUG TIMER
        /// </summary>
        private int stack = -1;
        private int current = -1;                       // Current index
        private DateTime nulldatetime = new DateTime(0);

        private X_TIMER[] timer = new X_TIMER[256];
        int timer_length = 0;
        private struct X_TIMER
        {
            public int      parent;
            public string   name;
            public long     level;
            public DateTime started;
            public long     duration;
        }


        /// <summary>
        /// Start timer
        /// </summary>
        /// <param name="name"></param>
        public void START(string name)
        {
            DateTime start_datetime = DateTime.Now;
            string nm = name.ToLower().Trim();
            stack++;

            bool added = false;
            for (int i = 0; i < timer_length; i++)
            {
                if ((timer[i].name == nm) & (timer[i].level == stack) & (timer[i].started == nulldatetime))
                {
                    timer[i].started = start_datetime;
                    timer[i].parent = current;
                    current = i;
                    added = true;
                    break;
                }
            }

            if (!added)
            {
                timer[timer_length].name = nm;
                timer[timer_length].level = stack;
                timer[timer_length].started = start_datetime;
                timer[timer_length].duration = 0;
                timer[timer_length].parent = current;
                current = timer_length;
                timer_length++;
            }
        }

        /// <summary>
        /// Stop timer
        /// </summary>
        /// <param name="name"></param>
        public void END(string name)
        {
            DateTime end_datetime = DateTime.Now;
            string nm = name.ToLower().Trim();
            bool a = false;
            if (current >= 0)
            {
                if (timer[current].name == nm)
                {
                    stack--;
                    TimeSpan ts = end_datetime - timer[current].started;
                    long dur = ts.Ticks;
                    timer[current].duration += dur;
                    timer[current].started = new DateTime(0);
                    if (timer[current].parent >= 0)
                    {
                        timer[timer[current].parent].duration -= dur;
                    }
                    int i = timer[current].parent;
                    timer[current].parent = -1;
                    current = i;
                    a = true;
                }
            }
            if (!a)
                throw new Exception("Timer is not started for '" + name + "'");
        }

        /// <summary>
        /// Pring result
        /// </summary>
        public void PRINT()
        {
            string[] nm = new string[256];
            long[] dr = new long[256];
            int cnt = 0;
            decimal s = 0;
            for (int i = 0; i < timer_length; i++)
                s += timer[i].duration;

            if (s == 0)
                s = 1;
            System.Console.WriteLine("Detail:");
            for (int i = 0; i < timer_length; i++)
            {
                decimal d = timer[i].duration / 10000000;
                System.Console.WriteLine(timer[i].name + " " + timer[i].level.ToString() + " " + d.ToString("N2") + "  " + (timer[i].duration / s).ToString("P"));

                bool a = false;
                for (int j = 0; j < cnt; j++)
                {

                    if (nm[j] == timer[i].name)
                    {
                        a = true;
                        dr[j] += timer[i].duration;
                    }
                }
                if (!a)
                {
                    nm[cnt] = timer[i].name;
                    dr[cnt] = timer[i].duration;
                    cnt++;
                }
            }

            System.Console.WriteLine("Summary:");
            for (int i = 0; i < cnt; i++)
            {
                decimal d = dr[i] / 10000000;
                System.Console.WriteLine(nm[i] + " " + d.ToString("N2") + "  " + (dr[i] / s).ToString("P"));
            }
        }

        /// <summary>
        /// Reset timer
        /// </summary>
        /// <param name="name"></param>
        public void RESET(string name)
        {
            timer_length = 0;
        }
    }
}
