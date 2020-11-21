using System;

namespace FSAdapter
{
    //Default Global Log Level - value is one of debug,info,notice,warning,err,crit,alert
    public enum FS_LOG_LEVEL
    {
        debug = 0,
        info = 1,
        notice = 2,
        warning = 3,
        err = 5,
        crit = 10,
        alert = 20,
    }

    public enum FS_ACL
    {
        deny = 0,
        allow = 1,
    }

    //event callback type to notify caller of FSController
    public enum EVENT_TYPE
    {
        UNKNOWN = 0,
        REGISTER = 900,             //SIP REGISTER

        CALL_TO_SWITCH = 1001,      //A-LEG
        SWITCH_CALL_USER = 1002,    //B-LEG
        ANSWER = 1003,
        HANGUP = 1004,
        DESTROY_CALL = 1005,

        CONFERENCE_CREATE = 2001,
        CONFERENCE_DELETE = 2002,
        JOIN_CONFERENCE = 2004,
        LEAVE_CONFERENCE = 2005,
    }

    public enum CALL_DIR
    {
        inbound = 0,
        outbound = 1,
    }

    //freeswitch defined events
    public enum FS_DEF_EVENTS
    {
        UNKNOWN = 0,
        ADD_SCHEDULE = 1,
        API,
        BACKGROUND_JOB,
        CALL_DETAIL,
        CALL_SECURE,
        CALL_SETUP_REQ,
        CALL_UPDATE,
        CDR,
        CHANNEL_ANSWER,
        CHANNEL_APPLICATION,
        CHANNEL_BRIDGE,
        CHANNEL_CALLSTATE,
        CHANNEL_CREATE,
        CHANNEL_DATA,
        CHANNEL_DESTROY,
        CHANNEL_EXECUTE,
        CHANNEL_EXECUTE_COMPLETE,
        CHANNEL_GLOBAL,
        CHANNEL_HANGUP,
        CHANNEL_HANGUP_COMPLETE,
        CHANNEL_HOLD,
        CHANNEL_ORIGINATE,
        CHANNEL_OUTGOING,
        CHANNEL_PARK,
        CHANNEL_PROGRESS,
        CHANNEL_PROGRESS_MEDIA,
        CHANNEL_STATE,
        CHANNEL_UNBRIDGE,
        CHANNEL_UNHOLD,
        CHANNEL_UNPARK,
        CHANNEL_UUID,
        CLONE,
        CODEC,
        COMMAND,
        CONFERENCE_DATA,
        CONFERENCE_DATA_QUERY,
        CUSTOM,
        DEL_SCHEDULE,
        DETECTED_SPEECH,
        DETECTED_TONE,
        DEVICE_STATE,
        DTMF,
        EXE_SCHEDULE,
        FAILURE,
        GENERAL,
        HEARTBEAT,
        LOG,
        MEDIA_BUG_START,
        MEDIA_BUG_STOP,
        MESSAGE,
        MESSAGE_QUERY,
        MESSAGE_WAITING,
        MODULE_LOAD,
        MODULE_UNLOAD,
        NAT,
        NOTALK,
        NOTIFY,
        NOTIFY_IN,
        PHONE_FEATURE,
        PHONE_FEATURE_SUBSCRIBE,
        PLAYBACK_START,
        PLAYBACK_STOP,
        PRESENCE_IN,
        PRESENCE_OUT,
        PRESENCE_PROBE,
        PRIVATE_COMMAND,
        PUBLISH,
        QUEUE_LEN,
        RECORD_START,
        RECORD_STOP,
        RECV_INFO,
        RECV_MESSAGE,
        RECV_RTCP_MESSAGE,
        RECYCLE,
        RELOADXML,
        REQUEST_PARAMS,
        RE_SCHEDULE,
        ROSTER,
        SEND_INFO,
        SEND_MESSAGE,
        SESSION_HEARTBEAT,
        SHUTDOWN,
        STARTUP,
        SUBCLASS_ANY,
        TALK,
        TRAP,
        UNPUBLISH,
    }
}
