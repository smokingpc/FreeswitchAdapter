using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSAdapter
{
    //"api status" command responsed string example:
    //UP 0 years, 1 days, 18 hours, 36 minutes, 0 seconds, 982 milliseconds, 465 microseconds
    //FreeSWITCH (Version 1.8.5  64bit) is ready
    //8 session(s) since startup
    //0 session(s) - peak 2, last 5min 0
    //0 session(s) per Sec out of max 30, peak 1, last 5min 0
    //1000 session(s) max
    //min idle cpu 0.00/96.30
    public struct FSState
    {
        private const string Pattern1 = @"UP (\d+) year[s]?, (\d+) day[s]?, (\d+) hour[s]?, (\d+) minute[s]?, (\d+) second[s]?, (\d+) millisecond[s]?";
        private const string Pattern2 = @"(.+) \(Version ([\d.]+)\s+(\d+\s?bit)\) is ready";
        private const string Pattern3 = @"(\d+) session\(s\) max";
        private const string Pattern4 = @"min idle cpu\s+([\d.]+)\/([\d.]+)";

        public string PBXName { get; set; }
        public TimeSpan UpTime { get; set; }
        public string Version { get; set; }
        public int MaxSession { get; set; }

        //current cpu loading in percentage.
        //example: 93.6% => CpuLoading field will be 93.6
        public float CpuLoading { get; set; }

        //CPU lower bound of idling. If idle rate(percentage) lower than this threshold, 
        //FreeSwitch will refuse any new calls.
        public float CpuMinIdle { get; set; }

        /// <summary>
        /// Parse FSState struct by EventSocket responsed strings.
        /// </summary>
        /// <param name="status">parsed result</param>
        /// <param name="text">response string from EventSocket</param>
        /// <returns>parsing succeed or not?</returns>
        public static bool TryParse(out FSState status, string text)
        { 
            bool ok = false;
            status = new FSState();

            try
            {
                var match = Regex.Match(text, Pattern1);
                if (match.Success)
                {
                    int days = int.Parse(match.Groups[1].Value) * 365 + int.Parse(match.Groups[2].Value);
                    int hour = int.Parse(match.Groups[3].Value);
                    int minute = int.Parse(match.Groups[4].Value);
                    int second = int.Parse(match.Groups[5].Value);
                    int msec = int.Parse(match.Groups[6].Value);

                    status.UpTime = new TimeSpan(days, hour, minute, second, msec);
                }
                else
                    throw new FormatException("UP Time parsing failed.");

                match = Regex.Match(text, Pattern2);
                if (match.Success)
                {
                    status.PBXName = match.Groups[1].Value;
                    status.Version = match.Groups[2].Value + "  " + match.Groups[3].Value;
                }
                else
                    throw new FormatException("Version and SwitchName parsing failed.");

                match = Regex.Match(text, Pattern3);
                if (match.Success)
                {
                    status.MaxSession = int.Parse(match.Groups[1].Value);
                }
                else
                    throw new FormatException("Max Session parsing failed.");

                match = Regex.Match(text, Pattern4);
                if (match.Success)
                {
                    status.CpuMinIdle = float.Parse(match.Groups[1].Value);
                    status.CpuLoading = 100.0f - float.Parse(match.Groups[2].Value);
                }
                else
                    throw new FormatException("Cpu min-idle threshold parsing failed.");

                ok = true;
            }
            catch (Exception ex)
            {
                ok = false;
            }

            return ok;
        }

        public override string ToString()
        {
            //return base.ToString();
            string result = "";

            result = string.Format("PBXName={0} , Version={1}\r\n", PBXName, Version);
            result = result + string.Format("UpTime={0:%d}days {0:%h}:{0:%m}:{0:%s}\r\n", UpTime);
            result = result + string.Format("MaxSession={0}\r\n", MaxSession);

            result = result + string.Format("CpuLoading={0:##.##}%, MinIdle Threshold={1}%\r\n", CpuLoading, CpuMinIdle);
            return result;
        } 
    }

    public struct ConferenceRoom
    {
        private const string Pattern1 = @"Conference (.+) \((\d+) member[s]* rate: (\d+) flag[s]*: (.+)\)";

        public string ID { get; set; }
        public int VoiceRate { get; set; } //audio sample rate, default value is 8000(Hz)
        public List<ConferenceUser> UserList { get; set; }
        public List<string> FlagList { get; set; }

        /// <summary>
        /// Parse ConferenceRoom struct by EventSocket responsed strings.
        /// </summary>
        /// <param name="room">parsed result</param>
        /// <param name="text">response string from EventSocket</param>
        /// <returns>parsing succeed or not?</returns>
        public static bool TryParse(out ConferenceRoom room, string text)
        {
            bool ok = false;

            room = new ConferenceRoom();
            room.UserList = new List<ConferenceUser>();
            room.FlagList = new List<string>();
            try
            {
                using (var reader = new StringReader(text))
                {
                    string line = reader.ReadLine();
                    var match = Regex.Match(line, Pattern1);
                    if (match.Success)
                    {
                        room.ID = match.Groups[1].Value;
                        room.VoiceRate = int.Parse(match.Groups[3].Value);
                        room.FlagList.AddRange(match.Groups[4].Value.Split('|'));

                        //read following lines by user counts
                        int count = int.Parse(match.Groups[2].Value);
                        for (int i = 0; i < count; i++)
                        {
                            line = reader.ReadLine();
                            ConferenceUser user;
                            if (ConferenceUser.TryParse(out user, line))
                                room.UserList.Add(user);
                        }
                        ok = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ok = false;
            }

            return ok;
        }
    }

    public struct ConferenceUser
    {
        private const string Pattern1 = @"(\d+);(.+);([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12});(.[^;]+);(.[^;]+);(.[^;]+);.+";


        public string ID { get; set; }          //member id in conference room
        public string EndPoint { get; set; }    //internal endpoint, example: sofia/internal/sip-1003@192.168.0.130
        public string UUID { get; set; }        //internal uuid of this user in FreeSwitch
        public string SipID { get; set; }

        /// <summary>
        /// Parse ConferenceUser struct by EventSocket responsed strings.
        /// </summary>
        /// <param name="user">parsed result</param>
        /// <param name="text">response string from EventSocket</param>
        /// <returns>parsing succeed or not?</returns>
        public static bool TryParse(out ConferenceUser user, string text)
        {
            bool ok = false;

            //example of variable "text"
            //28;sofia/internal/sip-1003@192.168.0.130;6d103bef-8bbe-4e4b-ad75-ff6bce2537a5;sip-1003;sip-1003;hear|speak;0;0;100
            user = new ConferenceUser();
            try
            {
                if (null != text)
                {
                    var match = Regex.Match(text, Pattern1);
                    if (match.Success)
                    {
                        user.ID = match.Groups[1].Value;
                        user.EndPoint = match.Groups[2].Value;
                        user.UUID = match.Groups[3].Value;
                        user.SipID = match.Groups[5].Value;

                        ok = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ok = false;
            }

            return ok;
        }
    }
}
