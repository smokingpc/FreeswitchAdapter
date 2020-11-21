﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FSAdapter
{
    public partial class FSAdapter
    {
        internal EVENT_MONITOR_TYPE ParseEventType(string json, out JObject jsondata)
        {
            EVENT_MONITOR_TYPE type = EVENT_MONITOR_TYPE.UNKNOWN;
            jsondata = null;
            try
            {
                jsondata = JObject.Parse(json);
                string action = "";
                string subclass = "";
                string event_class = jsondata["Event-Name"].ToString();
                if (jsondata.ContainsKey("Event-Subclass"))
                    subclass = jsondata["Event-Subclass"].ToString();
                if (jsondata.ContainsKey("Action"))
                    action = jsondata["Action"].ToString();

                switch (event_class)
                {
                    case "CHANNEL_CREATE":
                        {
                            string call_uuid = jsondata["Channel-Call-UUID"].ToString();
                            string leg_uuid = jsondata["Unique-ID"].ToString();
                            string direction = jsondata["Caller-Direction"].ToString();

                            if (call_uuid == leg_uuid && 0 == string.Compare(direction, "inbound", true))
                                type = EVENT_MONITOR_TYPE.CALL_TO_SWITCH;
                            else
                                type = EVENT_MONITOR_TYPE.SWITCH_CALL_USER;
                        }
                        break;
                    case "CHANNEL_ANSWER":
                        {
                            string call_uuid = jsondata["Channel-Call-UUID"].ToString();
                            string leg_uuid = jsondata["Unique-ID"].ToString();
                            string answer_state = jsondata["Answer-State"].ToString();

                            if (call_uuid == leg_uuid && 0 == string.Compare(answer_state, "answered", true))
                                type = EVENT_MONITOR_TYPE.ANSWER;
                        }
                        break;
                    case "CHANNEL_HANGUP":
                        {
                            string call_uuid = jsondata["Channel-Call-UUID"].ToString();
                            string leg_uuid = jsondata["Unique-ID"].ToString();
                            string answer_state = jsondata["Answer-State"].ToString();

                            if (call_uuid == leg_uuid && 0 == string.Compare(answer_state, "hangup", true))
                                type = EVENT_MONITOR_TYPE.HANGUP;
                        }
                        break;
                    case "CHANNEL_DESTROY":
                        {
                            type = EVENT_MONITOR_TYPE.DESTROY_CALL;
                        }
                        break;
                    case "CUSTOM":// && subclass == "conference::maintenance")
                        {
                            if (subclass == "conference::maintenance")
                            {
                                if (action == "conference-create")
                                    type = EVENT_MONITOR_TYPE.CONFERENCE_CREATE;
                                else if (action == "conference-destroy")
                                    type = EVENT_MONITOR_TYPE.CONFERENCE_DELETE;
                                else if (action == "add-member")
                                    type = EVENT_MONITOR_TYPE.JOIN_CONFERENCE;
                                else if (action == "del-member")
                                    type = EVENT_MONITOR_TYPE.LEAVE_CONFERENCE;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                type = EVENT_MONITOR_TYPE.UNKNOWN;
                string error_msg = ex.Message;
                string inner_msg = "";
                if (ex.InnerException != null)
                    inner_msg = ex.InnerException.Message;
                Log.Error(ex, $"Parse Event Type Failed: inner={inner_msg}");
            }

            return type;
        }

    }
}
