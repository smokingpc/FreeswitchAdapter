using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace FSAdapter
{
    public delegate void DelegateEventRaw(object sender, string json);
    public delegate void DelegateEventCall(object sender, CCallEvent arg);
    public delegate void DelegateEventAnswer(object sender, CAnswerEvent arg);
    public delegate void DelegateEventHangUp(object sender, CHangUpEvent arg);
    public delegate void DelegateEventConferenceCreate(object sender, CConferenceCreateEvent arg);
    public delegate void DelegateEventConferenceDelete(object sender, CConferenceDeleteEvent arg);
    public delegate void DelegateEventJoinConference(object sender, CJoinConferenceEvent arg);
    public delegate void DelegateEventLeaveConference(object sender, CLeaveConferenceEvent arg);

    public partial class FSAdapter
    {
        private TcpClient SockEvent = null;
        private ManualResetEventSlim EventStop = new ManualResetEventSlim(false);
        public event DelegateEventRaw OnEventRawString;
        public event DelegateEventCall OnCall;
        public event DelegateEventCall OnCallDestroy;
        public event DelegateEventAnswer OnAnswer;
        public event DelegateEventHangUp OnHangUp;
        public event DelegateEventConferenceCreate OnRoomCreate;
        public event DelegateEventConferenceDelete OnRoomDelete;
        public event DelegateEventJoinConference OnJoinRoom;
        public event DelegateEventLeaveConference OnLeaveRoom;
        

        //因為必須在conference裡面中轉各個customized header，不得已只好自己維護live channel的資料...
        //private Dictionary<string, JObject> CallLegs = new Dictionary<string, JObject>();

        //FreeSwitch Event是Async event，所以先用queue存下來再依序處理，避免time issue
        //這個queue用來放event回傳的JSON string
        private ConcurrentQueue<string> EventJsonQueue = new ConcurrentQueue<string>();

        public bool SubscribeEvent()
        {
            bool ret = false;

            if (SockEvent != null)
                SockEvent.Close();
            try
            {
                SockEvent = new TcpClient();
                try
                {
                    SockEvent.Connect(AddrESL);
                    EventStop.Reset();

                    string event_list = BuildEventSubscribeList();

                    Task.Run(() => ThreadEventDispatch());
                    Task.Run(() => ThreadEventRecv(event_list));
                    ret = true;
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    if (null != OnException)
                        OnException(null, null);
                }
                
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("SubscibeEvent Failed! StackTrack:{0}, Message:{1}",ex.StackTrace,ex.Message);
            }
            return ret;
        }
        public void UnsubscribeEvent()
        {
            EventStop.Set();
            if (SockEvent != null)
            {
                SockEvent.Close();
                SockEvent = null;
            }
        }

        //private void ProcessEvent(string json)
        //{
        //}
        private void ThreadEventRecv(string event_list)
        {
            string msg = "";

            try
            {
                using (var stream = SockEvent.GetStream())
                using (var reader = new StreamReader(stream))
                using (var writer = new StreamWriter(stream))
                {
                    try
                    {
                        msg = ReadMessages(reader);
                        Log.DebugFormat("connect to ESL: [{0}]", msg);

                        string cmd = "auth " + AuthPwd;
                        WriteCmd(writer, cmd);
                        msg = ReadCmdResp(reader);
                        Log.DebugFormat("ESL auth: [{0}]", msg);

                        cmd = "event json " + event_list;
                        WriteCmd(writer, cmd);
                        Log.DebugFormat("subscribe events: [{0}]", cmd);

                        msg = ReadCmdResp(reader);
                        Log.DebugFormat("subscribe result: [{0}]", msg);
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorFormat("ThreadEvent Exception: {0}", ex.Message);
                        if (ex.InnerException != null)
                            Log.ErrorFormat("InnerException: {0}", ex.InnerException.Message);
                        //System.Diagnostics.Debug.WriteLine("[thread event exception] => " + ex.Message);
                    }

                    while (false == EventStop.Wait(1))
                    {
                        try
                        {
                            string content_msg = ReadMessages(reader);
                            msg = "";
                            var match = Regex.Match(content_msg, RegexContentLength);
                            if (match.Success)
                            {
                                int size = int.Parse(match.Groups[1].Value);
                                msg = ReadContent(reader, size);

                                //ProcessEvent(msg);
                                //把JSON string塞進queue裡面等後面的dispatcher處理
                                EventJsonQueue.Enqueue(msg);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("ThreadEvent Exception: {0}", ex.Message);
                            if (ex.InnerException != null)
                                Log.ErrorFormat("InnerException: {0}", ex.InnerException.Message);
                        }
                    }

                    WriteCmd(writer, "noevents");
                    msg = ReadCmdResp(reader);
                    Log.DebugFormat("unsubscribe event: [{0}]", msg);
                }
            }
            catch (System.InvalidOperationException ex)
            {
                Log.ErrorFormat("沒有開啟FreeSwtichService,ex message:{0},ex stacktrace:{1}",ex.Message,ex.StackTrace);
                if (null != OnException)
                    OnException(null, null);
            }
        }
        private void ThreadEventDispatch()
        {
            while (false == EventStop.Wait(1))
            {
                string json = "";
                bool pop_ok = EventJsonQueue.TryDequeue(out json);

                if (!pop_ok || json == null || json == "")
                    continue;

                if (OnEventRawString != null)
                    OnEventRawString(this, json);

                string call_uuid = "";
                JObject jdata = null;
                EVENT_MONITOR_TYPE type = ParseEventType(json, out jdata);

                //因為必須在conference裡面中轉各個customized header，不得已只好自己維護live channel的資料...
                //收到CHANNEL_CREATE事件時就收錄channel資訊
                //留在Conference Create時撈出來看 variable_sip_h_X-* 變數....
                switch (type)
                {
                    case EVENT_MONITOR_TYPE.CALL_TO_SWITCH:
                        call_uuid = jdata["Channel-Call-UUID"].ToString();
                        CallLegs[call_uuid] = jdata;
                        if (null != OnCall)
                            OnCall(this, new CCallEvent(jdata));
                        break;

                    case EVENT_MONITOR_TYPE.SWITCH_CALL_USER:
                        call_uuid = jdata["Channel-Call-UUID"].ToString();
                        CallLegs[call_uuid] = jdata;
                        if (null != OnCall)
                            OnCall(this, new CCallEvent(jdata));
                        break;

                    case EVENT_MONITOR_TYPE.ANSWER:
                        if (null != OnAnswer)
                            OnAnswer(this, new CAnswerEvent(jdata));
                        break;
                    case EVENT_MONITOR_TYPE.HANGUP:
                        //call_uuid = jdata["Channel-Call-UUID"].ToString();
                        //CallLegs.Remove(call_uuid);
                        if (null != OnHangUp)
                            OnHangUp(this, new CHangUpEvent(jdata));
                        break;
                    case EVENT_MONITOR_TYPE.DESTROY_CALL:
                        call_uuid = jdata["Channel-Call-UUID"].ToString();
                        CallLegs.Remove(call_uuid);
                        if (null != OnCallDestroy)
                            OnCallDestroy(this, new CCallEvent(jdata));
                        break;
                    case EVENT_MONITOR_TYPE.CONFERENCE_CREATE:
                        if (null != OnRoomCreate)
                            OnRoomCreate(this, new CConferenceCreateEvent(jdata));
                        break;
                    case EVENT_MONITOR_TYPE.CONFERENCE_DELETE:
                        if (null != OnRoomDelete)
                            OnRoomDelete(this, new CConferenceDeleteEvent(jdata));
                        break;
                    case EVENT_MONITOR_TYPE.JOIN_CONFERENCE:
                        if (null != OnJoinRoom)
                        {
                            string call_id = jdata["Caller-Unique-ID"].ToString();

                            JObject channel = null;
                            if (CallLegs.ContainsKey(call_id))
                                channel = CallLegs[call_id];

                            OnJoinRoom(this, new CJoinConferenceEvent(jdata, channel));
                        }
                        break;
                    case EVENT_MONITOR_TYPE.LEAVE_CONFERENCE:
                        if (null != OnLeaveRoom)
                        {
                            string call_id = jdata["Caller-Unique-ID"].ToString();


                            JObject leg = null;
                            if (CallLegs.ContainsKey(call_id))
                                leg = CallLegs[call_id];

                            OnLeaveRoom(this, new CLeaveConferenceEvent(jdata, leg));
                        }
                        break;
                    default:
                        Log.WarnFormat("Unsupported event type {0}, skip it...", type.ToString());
                        break;
                }
            }

            //Queue沒有Clear()，只能用這種蠢方法清除
            while(EventJsonQueue.Count > 0)
            {
                string msg = "";
                EventJsonQueue.TryDequeue(out msg);
            }
        }

        private string BuildEventSubscribeList()
        {
            string event_list = "";

            List<string> class_list = new List<string>() 
                {
                    "CHANNEL_CREATE",
                    "CHANNEL_ANSWER",
                    "CHANNEL_HANGUP",
                    "CHANNEL_DESTROY",
                };
            List<string> subclass_list = new List<string>() 
                {
                    "conference::maintenance",
                };

            //refer to FreeSwitch online manual.
            //command format for event subscription: (you can also try it in telent to ESL)
            //  event <%format%> <%class-1%> <%class-2%> ....<%class-N%> CUSTOM <%subclass-1%> ...<%subclass-N%>
            //<%subclass> MUST be placed AFTER classes!! 
            //If there is subclass to subscribe, you have to put "CUSTOM" tag between class and subclass.
            foreach (var item in class_list)
                event_list = event_list + " " + item + " ";

            if (subclass_list.Count > 0)
            {
                event_list = event_list + " CUSTOM ";
                foreach (var item in subclass_list)
                    event_list = event_list + " " + item + " ";
            }

            if (event_list == "")
                event_list = " ALL ";

            return event_list;
        }
    }
}
