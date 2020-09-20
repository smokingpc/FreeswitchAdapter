using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FSAdapter
{
    public partial class FSAdapter
    {
    /// <summary>
    /// Query current status of FreeSwitch via event socket.
    /// </summary>
    /// <param name="state">return the state of freeswitch.</param>
    /// <returns>true is query succeed.</returns>
        public bool QueryServerState(out FSState state)
        {
            //FSState state;
            string cmd = "";
            string resp = "";

            cmd = "api status";
            resp = SendCommand(cmd);

            return FSState.TryParse(out state, resp);
        }

        /// <summary>
        /// force FreeSwitch reload config files without restart service.
        /// </summary>
        /// <returns>true if reload succeed.</returns>
        public bool ReloadConfig()
        {
            string cmd = "";
            string resp = "";

            cmd = "api reloadxml";
            resp = SendCommand(cmd);

            if (resp.IndexOf("OK", StringComparison.InvariantCultureIgnoreCase) >= 0 &&
                resp.IndexOf("Success", StringComparison.InvariantCultureIgnoreCase) >= 0)
                return true;

            return false;
        }

        /// <summary>
        /// List all conference room.
        /// </summary>
        /// <param name="roomlist">return conference room data list</param>
        /// <returns>true if query succeed.</returns>
        public bool GetConferenceList(out List<ConferenceRoom> roomlist)
        {
            string cmd = "";
            string resp = "";
            roomlist = null;

            cmd = "api conference list";
            resp = SendCommand(cmd);

            if (resp.IndexOf("No active conferences") >= 0)
                return false;

            roomlist = new List<ConferenceRoom>();
            string[] text = Regex.Split(resp, "\\+OK");
            foreach (var item in text)
            {
                if (item == "")
                    continue;
                ConferenceRoom room;
                if (ConferenceRoom.TryParse(out room, item))
                    roomlist.Add(room);
            }

            return true;
        }

        /// <summary>
        /// List all users in specified conference room.
        /// </summary>
        /// <param name="userlist">User list </param>
        /// <param name="conf_id">conference room name caller want to query.</param>
        /// <returns>true if query succeed.</returns>
        public bool GetConferenceUserList(out List<ConferenceUser> userlist, string conf_id)
        {
            string cmd = "";
            string resp = "";
            userlist = null;

            cmd = string.Format("api conference {0} list", conf_id);
            resp = SendCommand(cmd);

            if (resp.IndexOf("-ERR") >= 0)
                return false;

            //todo: parse conference list
            userlist = new List<ConferenceUser>();
            using (var reader = new StringReader(resp))
            {
                string line = "";
                line = reader.ReadLine();
                while (line != null)
                {
                    ConferenceUser user;
                    if (ConferenceUser.TryParse(out user, line))
                        userlist.Add(user);

                    line = reader.ReadLine();
                }

            }

            return true;
        }

        /// <summary>
        /// Invite a SIP client to joine specified conference room.
        /// Caller ID which target sip see is %conf_id@%domain
        /// </summary>
        /// <param name="conf_id">Name of conference room (sip ID) </param>
        /// <param name="target">SIP account which you want to invite. (not including domain)</param>
        /// <param name="caller_name">caller display name which target sip see. this field can be empty string.</param>
        /// <returns> true if succeed. </returns>
        public bool InviteToConference(string conf_id, string target, string caller_name, bool in_background = false)
        {
            bool result = false;
            string cmd = "";
            string resp = "";

            //conference dial <endpoint_module_name>/<destination> <callerid number> <callerid name>
            //if you want non-blocking (async) command, just use "bgdial" to replace "dial".
            if (true == in_background)
                cmd = string.Format("api conference {0} bgdial user/{1} {2} {3}", conf_id, target, conf_id, caller_name);
            else
                cmd = string.Format("api conference {0} dial user/{1} {2} {3}", conf_id, target, conf_id, caller_name);

            resp = SendCommand(cmd);

            //+OK Call Requested: result: [SUCCESS]
            if (resp.IndexOf("+OK") >= 0 && resp.IndexOf("SUCCESS") >= 0)
                result = true;

            return result;
        }

        /// <summary>
        /// Invite a SIP client to joine specified conference room.
        /// Caller ID which target sip see is %conf_id@%domain
        /// 
        /// You can add custom headers to invite someone in this API.
        /// To add custom header in conference invite, just put them before dial string.
        /// example : assume you want to invite sip-1001@127.0.0.1, and you want add 2 custom header
        ///   MyHeader1 = "my_name"
        ///   MyHeader2 = "23939889"
        /// the command string should be :
        /// api conference conf-0001 dial [sip_h_X-MyHeader1=my_name,sip_h_X-MyHeader2=23939889]user/sip-1001 conf-0001 AutoCall@Conference
        /// The call answering side will see 2 custom header in sip : "MyHeader1" and "MyHeader2"
        /// </summary>
        /// <param name="conf_id">Name of conference room (sip ID) </param>
        /// <param name="target">SIP account which you want to invite. (not including domain)</param>
        /// <param name="caller_name">caller display name which target sip see. this field can be empty string.</param>
        /// <returns> true if succeed. </returns>

        //  api conference <%conference name> dial [<%variables>]<%target user> <%caller id> <%caller display name> 
        //  example:
        //  api conference conf-0001 dial [sip_h_X-MyTest=XYZ,sip_h_X-NewData=23939889]user/2002 conf-0001 AutoCall@Conference
        public bool InviteToConference(string conf_id, string target, string caller_name, Dictionary<string, string> header_map = null, bool in_background = false)
        {
            bool result = false;
            string cmd = "";
            string resp = "";
            string header = "";

            //build customized header
            if (null != header_map)
            {
                foreach (var item in header_map)
                {
                    //NOTE : DO NOT use space character in header and value string!
                    if ("" != header)
                        header = header + ",";

                    header = header + string.Format("sip_h_{0}={1}", item.Key, item.Value);
                }
                if (header != "")
                    header = "[" + header + "]";
            }
            //conference dial <endpoint_module_name>/<destination> <callerid number> <callerid name>
            //if you want non-blocking (async) command, just use "bgdial" to replace "dial".
            if (true == in_background)
                cmd = string.Format("api conference {0} bgdial {1}user/{2} {3} {4}", conf_id, header, target, conf_id, caller_name);
            else
                cmd = string.Format("api conference {0} dial {1}user/{2} {3} {4}", conf_id, header, target, conf_id, caller_name);

            resp = SendCommand(cmd);

            //+OK Call Requested: result: [SUCCESS]
            if (resp.IndexOf("+OK") >= 0 && resp.IndexOf("SUCCESS") >= 0)
                result = true;

            return result;
        }

        /// <summary>
        /// Kick someone from conference.
        /// NOTE: target id is NOT sip uid. It should be uuid used in freeswitch.
        /// You can get it from GetConferenceUserList() API.
        /// </summary>
        /// <param name="conf_id"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool KickFromConference(string conf_id, string target)
        {
            bool result = false;
            string cmd = "";
            string resp = "";

            //conference dial <endpoint_module_name>/<destination> <callerid number> <callerid name>
            cmd = string.Format("api conference {0} kick {1}", conf_id, target);
            resp = SendCommand(cmd);

            //+OK Call Requested: result: [SUCCESS]
            if (resp.IndexOf("+OK") >= 0 && resp.IndexOf("SUCCESS") >= 0)
                result = true;

            return result;
        }
    }
}
