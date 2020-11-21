using NLog;
using System;
using System.Net;

namespace FSAdapter
{
    public partial class FSAdapter : IDisposable
    {
        public const string DefaultServiceName = "freeswitch";
        private static Logger Log = LogManager.GetLogger("FSAdapter");
        
        private IPEndPoint AddrESL = null;  //address of Event Socket Layer
        private const int EventSocketPort = 8021;
        private string AuthPwd = "";
        protected bool IsDisposed = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="base_path">install path of FreeSwitch.</param>
        /// <param name="ip"> FreeSwitch event socket IP.</param>
        /// <param name="port">port of event socket.</param>
        /// <param name="auth_pwd">authentication password of event socket. it is configured in event_socket.conf.xml.</param>
        public FSAdapter(string ip, int port, string auth_pwd)
        {
            this.AuthPwd = auth_pwd;
            this.AddrESL = new IPEndPoint(IPAddress.Parse(ip), port);
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="base_path">install path of FreeSwitch.</param>
        /// <param name="ip"> FreeSwitch event socket IP.</param>
        /// <param name="auth_pwd">authentication password of event socket.</param>
        public FSAdapter(string ip, string auth_pwd)
            : this(ip, FSAdapter.EventSocketPort, auth_pwd)
        { }

        ~FSAdapter() { Dispose(false); }
        protected void Dispose(bool from_public)
        {
            if (IsDisposed)
                return;

            if (from_public)
            {
                //free managed resources
                throw new NotImplementedException();
            }
            IsDisposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
