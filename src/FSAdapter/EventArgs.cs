﻿using Newtonsoft.Json.Linq;
using System;

namespace FSAdapter
{
    public class CBaseEventArg
    {
        public JObject JsonData { get; private set; }
        public FS_DEF_EVENTS EventClass
        {
            get { return (FS_DEF_EVENTS)Enum.Parse(typeof(FS_DEF_EVENTS), JsonData["Event-Name"].ToString()); }
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

        public CBaseEventArg(string json)
            : this(JObject.Parse(json))
        { }
        public CBaseEventArg(JObject json)
        {
            JsonData = json;
        }
    }
    public class CCallEvent : CBaseEventArg
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
                Int64 milli_sec = Int64.Parse(data) / 1000;
                return DateTimeOffset.FromUnixTimeMilliseconds(milli_sec).DateTime;
            }
        }
        public CCallEvent(string json) : base(json) { }
        public CCallEvent(JObject json) : base(json) { }
    }
    public class CAnswerEvent : CBaseEventArg
    {
        public string CallerName
        {
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
                Int64 milli_sec = Int64.Parse(data) / 1000;
                return DateTimeOffset.FromUnixTimeMilliseconds(milli_sec).DateTime;
            }
        }
        public CAnswerEvent(string json) : base(json) { }
        public CAnswerEvent(JObject json) : base(json) { }
    }
    public class CHangUpEvent : CBaseEventArg
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
                    if (array != null && array.Length > 0)
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
                Int64 milli_sec = Int64.Parse(data) / 1000;
                return DateTimeOffset.FromUnixTimeMilliseconds(milli_sec).DateTime;
            }
        }
        public CHangUpEvent(string json) : base(json) { }
        public CHangUpEvent(JObject json) : base(json) { }
    }
    public class CConferenceCreateEvent : CBaseEventArg
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
                Int64 milli_sec = Int64.Parse(data) / 1000;
                return DateTimeOffset.FromUnixTimeMilliseconds(milli_sec).DateTime;
            }
        }
        public CConferenceCreateEvent(string json) : base(json) { }
        public CConferenceCreateEvent(JObject json) : base(json) { }
    }
    public class CConferenceDeleteEvent : CBaseEventArg
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
                Int64 milli_sec = Int64.Parse(data) / 1000;
                return DateTimeOffset.FromUnixTimeMilliseconds(milli_sec).DateTime;
            }
        }
        public CConferenceDeleteEvent(string json) : base(json) { }
        public CConferenceDeleteEvent(JObject json) : base(json) { }
    }
    public class CJoinConferenceEvent : CBaseEventArg
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
                Int64 milli_sec = Int64.Parse(data) / 1000;
                return DateTimeOffset.FromUnixTimeMilliseconds(milli_sec).DateTime;
            }
        }

        //who joined this conference room?
        public string SipID
        {
            get 
            {
                string ret = "";
                if (Direction == CALL_DIR.inbound)
                {
                    ret = JsonData["Caller-Orig-Caller-ID-Number"].ToString();
                }
                else if (Direction == CALL_DIR.outbound)
                {
                    ret = JsonData["Caller-Destination-Number"].ToString();
                }

                return ret;
            }
        }

        public string RoomMemberID
        { get { return JsonData["Member-ID"].ToString(); } }

        private CALL_DIR Direction
        {
            get { return (CALL_DIR)Enum.Parse(typeof(CALL_DIR), (JsonData["Call-Direction"].ToString())); }
        }

        public CJoinConferenceEvent(string conference)
            : base(conference)
        { }
        public CJoinConferenceEvent(JObject conference)
            : base(conference)
        { }
    }
    public class CLeaveConferenceEvent : CBaseEventArg
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
                Int64 milli_sec = Int64.Parse(data) / 1000;
                return DateTimeOffset.FromUnixTimeMilliseconds(milli_sec).DateTime;
            }
        }
        public string LeaveMember
        { get { return JsonData["Member-ID"].ToString(); } }

        private CALL_DIR Direction
        {
            get { return (CALL_DIR)Enum.Parse(typeof(CALL_DIR), (JsonData["Call-Direction"].ToString())); }
        }

        //who leave this conference room?
        public string SipID
        {
            get
            {
                string ret = "";
                if (Direction == CALL_DIR.inbound)
                {
                    ret = JsonData["Caller-Orig-Caller-ID-Number"].ToString();
                }
                else if (Direction == CALL_DIR.outbound)
                {
                    ret = JsonData["Caller-Destination-Number"].ToString();
                }

                return ret;
            }
        }

        public CLeaveConferenceEvent(string json) : base(json) { }
        public CLeaveConferenceEvent(JObject json) : base(json) { }
    }

    public class CSipRegister : CBaseEventArg
    {
        public string SipID
        {
            get
            {
                string ret = JsonData["username"].ToString();
                return ret;
            }
        }
        public string SipDomain
        {
            get
            {
                string ret = JsonData["domain_name"].ToString();
                return ret;
            }
        }
        public string FromIP
        {
            get
            {
                string ret = JsonData["network-ip"].ToString();
                return ret;
            }
        }
        public DateTime RegisterTime
        {
            get
            {
                string data = JsonData["Event-Date-Timestamp"].ToString();
                Int64 milli_sec = Int64.Parse(data) / 1000;
                return DateTimeOffset.FromUnixTimeMilliseconds(milli_sec).DateTime;
            }
        }
        public CSipRegister(string json) : base(json) { }
        public CSipRegister(JObject json) : base(json) { }
    }
}
