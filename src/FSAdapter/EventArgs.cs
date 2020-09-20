using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FSAdapter
{
    public class CEventArg
    {
        public string JsonString { get { return JsonData.ToString(); } }
        public JObject JsonData { get; private set; }
        public SW_EVENT_TYPE EventClass
        {
            get { return (SW_EVENT_TYPE)Enum.Parse(typeof(SW_EVENT_TYPE), JsonData["Event-Name"].ToString()); }
        }
        public string EventSubclass
        {
            get
            {
                if (JsonData.ContainsKey("Event-Subclass"))
                    return JsonData["Event-Subclass"].ToString();
                return "";
            }
        }
        public string Action
        {
            get
            {
                if (JsonData.ContainsKey("Action"))
                    return JsonData["Action"].ToString();
                return "";
            }
        }


        //public CEventArg() { }
        public CEventArg(string json)
            : this(JObject.Parse(json))
        { }
        public CEventArg(JObject json)
        {
            JsonData = json;
        }
    }
    public class CCallEvent : CEventArg
    {
        public string CallerName
        {
            get { return JsonData["Caller-Caller-ID-Number"].ToString(); }
        }
        public string DestName
        {
            get { return JsonData["Caller-Destination-Number"].ToString(); }
        }
        public DateTime CallTime
        {
            get
            {
                string data = JsonData["Event-Date-Timestamp"].ToString();
                Int64 microsecond = Int64.Parse(data);

                return Utils.FromLinuxEpoch64(microsecond);
            }
        }
        public CCallEvent(JObject json) : base(json)
        {
        }
    }
    public class CAnswerEvent : CEventArg
    {
        public string CallerName
        {//不確定是不是這個key...
            get { return JsonData["Caller-Caller-ID-Number"].ToString(); }
        }
        public string DestName
        {
            get { return JsonData["Caller-Destination-Number"].ToString(); }
        }
        public DateTime AnswerTime
        {
            get
            {
                string data = JsonData["Event-Date-Timestamp"].ToString();
                Int64 microsecond = Int64.Parse(data);

                return Utils.FromLinuxEpoch64(microsecond);
            }
        }
        public CAnswerEvent(JObject json) : base(json) { }
    }
    public class CHangUpEvent : CEventArg
    {
        public string InvokerName   //誰掛掉這電話的？
        {
            get
            {
                string result = "";
                if (JsonData.ContainsKey("variable_sip_req_uri"))
                {
                    string uri = JsonData["variable_sip_req_uri"].ToString();
                    var array = uri.Split('@');
                    //如果這欄位不是xxxx@oooo.ooo.ooo這樣的uri格式，就整個傳回去當做user id
                    //否則只取@符號之前的字串
                    if (array != null && array.Count() > 0)
                        result = array[0];
                    else
                        result = uri;
                }
                return result;
            }
        }
        public DateTime HangupTime
        {
            get
            {
                string data = JsonData["Event-Date-Timestamp"].ToString();
                Int64 microsecond = Int64.Parse(data);

                return Utils.FromLinuxEpoch64(microsecond);
            }
        }
        public CHangUpEvent(JObject json) : base(json) { }
    }

    public class CConferenceCreateEvent : CEventArg
    {
        public string RoomUUID
        {
            get { return JsonData["Conference-Unique-ID"].ToString(); }
        }
        public string RoomName
        {
            get { return JsonData["Conference-Name"].ToString(); }
        }
        public DateTime CreateTime
        {
            get
            {
                string data = JsonData["Event-Date-Timestamp"].ToString();
                Int64 microsecond = Int64.Parse(data);

                return Utils.FromLinuxEpoch64(microsecond);
            }
        }
        public CConferenceCreateEvent(JObject json) : base(json) { }
    }
    public class CConferenceDeleteEvent : CEventArg
    {
        public string RoomUUID
        {
            get { return JsonData["Conference-Unique-ID"].ToString(); }
        }
        public string RoomName
        {
            get { return JsonData["Conference-Name"].ToString(); }
        }
        public DateTime DeleteTime
        {
            get
            {
                string data = JsonData["Event-Date-Timestamp"].ToString();
                Int64 microsecond = Int64.Parse(data);

                return Utils.FromLinuxEpoch64(microsecond);
            }
        }
        public CConferenceDeleteEvent(JObject json) : base(json) { }
    }
    //public class CConferenceInviteEvent : CEventArg
    //{
    //    public string RoomUUID
    //    {
    //        get { return JsonData["Conference-Unique-ID"].ToString(); }
    //    }
    //    public string RoomName
    //    {
    //        get { return JsonData["Conference-Name"].ToString(); }
    //    }
    //    public string InviteTarget
    //    {
    //        get { return JsonData["Caller-Destination-Number"].ToString(); }
    //    }
    //    public DateTime InviteTime
    //    {
    //        get
    //        {
    //            string data = JsonData["Event-Date-Timestamp"].ToString();
    //            Int64 microsecond = Int64.Parse(data);

    //            return Utils.FromLinuxEpoch64(microsecond);
    //        }
    //    }
    //    public CConferenceInviteEvent(JObject json) : base(json) { }
    //}

    //someone join into a conference room
    public class CJoinConferenceEvent : CEventArg
    {
        public string RoomUUID
        {
            get { return JsonData["Conference-Unique-ID"].ToString(); }
        }
        public string RoomName
        {
            get { return JsonData["Conference-Name"].ToString(); }
        }
        public DateTime JoinTime
        {
            get
            {
                string data = JsonData["Event-Date-Timestamp"].ToString();
                Int64 microsecond = Int64.Parse(data);

                return Utils.FromLinuxEpoch64(microsecond);
            }
        }
        public string SipID { get; set; }

        public string RoomMemberID
        { get { return JsonData["Member-ID"].ToString(); } }

        public CALL_DIRECTION Direction
        {
            get { return (CALL_DIRECTION)Enum.Parse(typeof(CALL_DIRECTION), (JsonData["Call-Direction"].ToString())); }
        }

        public bool HasCustomHeader
        {
            get { return (CustomHeaders.Count > 0); }
        }
        public Dictionary<string, string> CustomHeaders = new Dictionary<string, string>();
        //private JObject JsonChannel = null;
        public CJoinConferenceEvent(JObject conference)
            : base(conference)
        { }
        public CJoinConferenceEvent(JObject conference, JObject channel)
            : this(conference)
        {
            SetChannelJsonData(channel);
        }

        public void SetChannelJsonData(JObject jdata)
        {
            if (jdata != null)
            {
                //利用LINQ撈出 "開頭為variable_sip_h_ 的變數"
                //這些CustomHeader資料會放在CHANNEL 資訊裡....
                var keys = jdata.Properties().Where(x => (x.Name.IndexOf("variable_sip_h_") == 0));

                if (keys.Count() > 0)
                {
                    //Dictionary<string, string> result = new Dictionary<string, string>();
                    foreach (var item in keys)
                    {
                        string key = item.Name.ToString();
                        //把variable_sip_h_開頭的字串拔掉，剩下原始(在wireshark裡看到)的 header name
                        string sip_header = key.Replace("variable_sip_h_", "");
                        CustomHeaders[sip_header] = jdata[key].ToString();
                    }
                }

                SipID = jdata["Caller-Destination-Number"].ToString();
            }
        }
    }
    public class CLeaveConferenceEvent : CEventArg
    {
        public string RoomUUID
        {
            get { return JsonData["Conference-Unique-ID"].ToString(); }
        }
        public string RoomName
        {
            get { return JsonData["Conference-Name"].ToString(); }
        }
        public DateTime LeaveTime
        {
            get
            {
                string data = JsonData["Event-Date-Timestamp"].ToString();
                Int64 microsecond = Int64.Parse(data);

                return Utils.FromLinuxEpoch64(microsecond);
            }
        }
        public string LeaveMember
        { get { return JsonData["Member-ID"].ToString(); } }
        public string SipID { get; set; }
        public CLeaveConferenceEvent(JObject json, JObject channel) : base(json)
        {
            if (null != channel)
                SipID = channel["Caller-Destination-Number"].ToString();
        }
    }
}
