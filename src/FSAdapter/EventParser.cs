using Newtonsoft.Json.Linq;
using System;

namespace FSAdapter
{
    public partial class FSAdapter
    {
        internal FS_EVENT_TYPE ParseEventType(string json, out JObject jsondata)
        {
            FS_EVENT_TYPE type = FS_EVENT_TYPE.UNKNOWN;
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
                                type = FS_EVENT_TYPE.CALL_TO_SWITCH;
                            else
                                type = FS_EVENT_TYPE.SWITCH_CALL_USER;
                        }
                        break;
                    case "CHANNEL_ANSWER":
                        {
                            string call_uuid = jsondata["Channel-Call-UUID"].ToString();
                            string leg_uuid = jsondata["Unique-ID"].ToString();
                            string answer_state = jsondata["Answer-State"].ToString();

                            if (call_uuid == leg_uuid && 0 == string.Compare(answer_state, "answered", true))
                                type = FS_EVENT_TYPE.ANSWER;
                        }
                        break;
                    case "CHANNEL_HANGUP":
                        {
                            string call_uuid = jsondata["Channel-Call-UUID"].ToString();
                            string leg_uuid = jsondata["Unique-ID"].ToString();
                            string answer_state = jsondata["Answer-State"].ToString();

                            if (call_uuid == leg_uuid && 0 == string.Compare(answer_state, "hangup", true))
                                type = FS_EVENT_TYPE.HANGUP;
                        }
                        break;
                    case "CHANNEL_DESTROY":
                        {
                            type = FS_EVENT_TYPE.DESTROY_CALL;
                        }
                        break;
                    case "CUSTOM":// && subclass == "conference::maintenance")
                        {
                            if (subclass == "conference::maintenance")
                            {
                                if (action == "conference-create")
                                    type = FS_EVENT_TYPE.CONFERENCE_CREATE;
                                else if (action == "conference-destroy")
                                    type = FS_EVENT_TYPE.CONFERENCE_DELETE;
                                else if (action == "add-member")
                                    type = FS_EVENT_TYPE.JOIN_CONFERENCE;
                                else if (action == "del-member")
                                    type = FS_EVENT_TYPE.LEAVE_CONFERENCE;
                            }
                            else if (subclass == "sofia::register")
                            {
                                type = FS_EVENT_TYPE.REGISTER;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                type = FS_EVENT_TYPE.UNKNOWN;
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
