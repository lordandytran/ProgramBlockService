using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSPP
{
    [Serializable]
    public class Schedule
    {
        private List<Tuple<string, List<Tuple<string, string>>>> times;
        public Schedule()
        {
            times = new List<Tuple<string, List<Tuple<string, string>>>>();
        }

        public void AddDay(string s, List<Tuple<string, string>> t)
        {
            times.Add(new Tuple<string, List<Tuple<string, string>>>(s, t));
        }

        public List<Tuple<string, List<Tuple<string, string>>>> getSchedule()
        {
            return times;
        }
    }
}
