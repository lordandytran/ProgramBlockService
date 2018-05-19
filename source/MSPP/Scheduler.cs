using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Win32;
using System.Reflection;
using System.Globalization;

namespace MSPP
{
    public partial class Scheduler : ServiceBase
    {

        private Schedule schedule;
        private List<string> list;

        private Timer EnforceTimer;
        private Timer RemoveTimer;

        private List<TimeSpan> EnforceList;
        private List<TimeSpan> RemoveList;

        public Scheduler()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            SerializeProgramList();
            SerializeSchedule();
            try
            {
                string programpath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Program.bin");
                string schedulepath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Schedule.bin");
                BinaryFormatter bf = new BinaryFormatter();
                FileStream fs = new FileStream(schedulepath, FileMode.Open, FileAccess.Read, FileShare.None);
                BinaryFormatter bf2 = new BinaryFormatter();
                FileStream fs2 = new FileStream(programpath, FileMode.Open, FileAccess.Read, FileShare.None);
                schedule = new Schedule();
                list = new List<string>();
                schedule = (Schedule)bf.Deserialize(fs);
                list = (List<string>)bf2.Deserialize(fs2);
                Library.WriteErrorLog("Deserialization complete");
                EnforceList = new List<TimeSpan>();
                RemoveList = new List<TimeSpan>();
                PopulateScheduleList();
                int k = -1;
                int l = -1;
                for (int i = 0; i < schedule.getSchedule().Count; i++)
                {
                    if (schedule.getSchedule()[i].Item1.Equals(DateTime.Now.DayOfWeek.ToString()))
                    {
                        for (int j = 0; j < schedule.getSchedule()[i].Item2.Count; j++)
                        {
                            if(DateTime.Now >= DateTime.Parse(schedule.getSchedule()[i].Item2[j].Item1)
                                && DateTime.Now < DateTime.Parse(schedule.getSchedule()[i].Item2[j].Item2))
                            {
                                k = i;
                                l = j;
                            }
                        }
                    }
                }
                if(k != -1 && l != -1)
                {
                    RemovePolicy();
                }
                else
                {
                    EnforcePolicy();
                }
                RemoveTimer = new System.Timers.Timer();
                RemoveTimerReset();
                RemoveTimer.Elapsed += new System.Timers.ElapsedEventHandler(RemovePolicy);
                RemoveTimerReset();
                EnforceTimer = new System.Timers.Timer();
                EnforceTimerReset();
                EnforceTimer.Elapsed += new System.Timers.ElapsedEventHandler(EnforcePolicy);
                EnforceTimerReset();
            }
            catch
            {
                Library.WriteErrorLog("Failed runtime");
            }
        }

        protected override void OnStop()
        {
            Library.WriteErrorLog("Service stopped");
        }

        private void PopulateScheduleList()
        {
            CultureInfo providerRemove = CultureInfo.InvariantCulture;
            CultureInfo providerEnforce = CultureInfo.InvariantCulture;
            for (int i = 0; i < schedule.getSchedule().Count; i++)
            {
                if(schedule.getSchedule()[i].Item1.Equals(DateTime.Now.DayOfWeek.ToString())) {
                    for(int j = 0; j < schedule.getSchedule()[i].Item2.Count; j++)
                    {
                        RemoveList.Add(TimeSpan.ParseExact(schedule.getSchedule()[i].Item2[j].Item1, "g", providerRemove));
                        EnforceList.Add(TimeSpan.ParseExact(schedule.getSchedule()[i].Item2[j].Item2, "g", providerEnforce));
                    }
                }
            }
        }

        private void EnforcePolicy(object sender, System.Timers.ElapsedEventArgs e)
        {
            EnforceTimer.Enabled = false;
            //Kill each process
            foreach (Process p in Process.GetProcesses())
            {
                for (int i = 0; i < list.Count(); i++)
                {
                    string program = list[i].Replace(".exe", "");
                    if (p.ProcessName.Equals(program))
                    {
                        p.Kill();
                    }
                }
            }
            Library.WriteErrorLog("Killed all selected processes");
            //Prevent process from running
            //Registry policy at HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options
            RegistryKey ParentKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true);
            for (int i = 0; i < list.Count(); i++)
            {
                ParentKey.CreateSubKey(list[i]);
                RegistryKey SubKey = ParentKey.OpenSubKey(list[i], true);
                SubKey.SetValue("Debugger", "ntsd -c q", RegistryValueKind.String);
            }
            Library.WriteErrorLog("Enforcing Policy complete");
            EnforceTimerReset();
        }

        private void RemovePolicy(object sender, System.Timers.ElapsedEventArgs e)
        {
            RemoveTimer.Enabled = false;
            //Remove registry block
            RegistryKey ParentKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true);
            for (int i = 0; i < list.Count(); i++)
            {
                try
                {
                    ParentKey.DeleteSubKeyTree(list[i]);
                    Library.WriteErrorLog("Deleted " + list[i] + " subkey");
                }
                catch
                {
                    Library.WriteErrorLog("Unable to delete " + list[i] + " subkey. Does not exist!");
                }
            }
            Library.WriteErrorLog("Policy Removal Complete");
            RemoveTimerReset();
        }

        private void EnforcePolicy()
        {
            //Kill each process
            foreach (Process p in Process.GetProcesses())
            {
                for (int i = 0; i < list.Count(); i++)
                {
                    string program = list[i].Replace(".exe", "");
                    if (p.ProcessName.Equals(program))
                    {
                        p.Kill();
                    }
                }
            }
            Library.WriteErrorLog("Killed all selected processes");
            //Prevent process from running
            //Registry policy at HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options
            RegistryKey ParentKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true);
            for (int i = 0; i < list.Count(); i++)
            {
                ParentKey.CreateSubKey(list[i]);
                RegistryKey SubKey = ParentKey.OpenSubKey(list[i], true);
                SubKey.SetValue("Debugger", "ntsd -c q", RegistryValueKind.String);
            }
            Library.WriteErrorLog("Enforcing Policy complete");

        }

        private void RemovePolicy()
        {
            //Remove registry block
            RegistryKey ParentKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true);
            for (int i = 0; i < list.Count(); i++)
            {
                try
                {
                    ParentKey.DeleteSubKeyTree(list[i]);
                    Library.WriteErrorLog("Deleted " + list[i] + " subkey");
                }
                catch
                {
                    Library.WriteErrorLog("Unable to delete " + list[i] + " subkey. Does not exist!");
                }
            }
            Library.WriteErrorLog("Policy Removal Complete");
        }

        private void EnforceTimerReset()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            TimeSpan? nextRunTime = null;
            foreach (TimeSpan runTime in EnforceList)
            {

                if (currentTime < runTime)
                {
                    nextRunTime = runTime;
                    break;
                }
            }
            EnforceTimer.Interval = (nextRunTime.Value - currentTime).TotalMilliseconds;
            EnforceTimer.Enabled = true;
        }

        private void RemoveTimerReset()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            TimeSpan? nextRunTime = null;
            foreach (TimeSpan runTime in RemoveList)
            {

                if (currentTime < runTime)
                {
                    nextRunTime = runTime;
                    break;
                }
            }
            RemoveTimer.Interval = (nextRunTime.Value - currentTime).TotalMilliseconds;
            RemoveTimer.Enabled = true;
        }

        public void SerializeProgramList()
        {
            string textpath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Program.txt");
            if (File.Exists(textpath))
            {
                List<string> l = new List<string>();
                string line;
                StreamReader file = new StreamReader(textpath);
                while ((line = file.ReadLine()) != null)
                {
                    l.Add(line);
                }
                string binpath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Program.bin");
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(binpath, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, l);
                stream.Close();
                Library.WriteErrorLog("Serialized Programs");
            }
            else
            {
                Library.WriteErrorLog("Unable to serialize Programs");
            }
        }

        public void SerializeSchedule()
        {
            string textpath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Schedule.txt");
            if (File.Exists(textpath))
            {
                Schedule s = new Schedule();
                StreamReader file = new StreamReader(textpath);
                string line = "";
                if (file.Peek() > -1)
                {
                    line = file.ReadLine();
                    do
                    {
                        string day = "";
                        List<Tuple<string, string>> times = new List<Tuple<string, string>>();
                        if (Char.IsLetter(line.ToString()[0]))
                        {
                            day = line;
                            Console.WriteLine(day);
                            while (file.Peek() > -1)
                            {
                                int c = file.Peek();
                                if (!Char.IsLetter((char)c))
                                {
                                    line = file.ReadLine();
                                    string s1 = line;
                                    line = file.ReadLine();
                                    string s2 = line;
                                    Console.WriteLine(s1 + " - " + s2);
                                    times.Add(new Tuple<string, string>(s1, s2));
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        s.AddDay(day, times);
                    } while ((line = file.ReadLine()) != null);
                }
                string binpath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Schedule.bin");
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(binpath, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, s);
                stream.Close();
                Library.WriteErrorLog("Serialized Schedule");
            }
            else
            {
                Library.WriteErrorLog("Unable to serialize Schedule");
            }
        }
    }
}
