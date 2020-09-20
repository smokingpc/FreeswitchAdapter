using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace FSAdapter
{
    public partial class FSAdapter : IDisposable
    {
        public const string DefaultServiceName = "freeswitch";
        private IPEndPoint AddrESL = null;  //address of Event Socket Layer

        private const int EventSocketPort = 8021;
        private string AuthPwd = "";
        private string BasePath = "";
        protected bool IsDisposed = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="base_path">install path of FreeSwitch.</param>
        /// <param name="ip"> FreeSwitch event socket IP.</param>
        /// <param name="port">port of event socket.</param>
        /// <param name="auth_pwd">authentication password of event socket. it is configured in event_socket.conf.xml.</param>
        public FSAdapter(string base_path, string ip, int port, string auth_pwd)
        {
            this.AuthPwd = auth_pwd;
            this.AddrESL = new IPEndPoint(IPAddress.Parse(ip), port);
            this.BasePath = base_path;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="base_path">install path of FreeSwitch.</param>
        /// <param name="ip"> FreeSwitch event socket IP.</param>
        /// <param name="auth_pwd">authentication password of event socket.</param>
        public FSAdapter(string base_path, string ip, string auth_pwd)
            : this(base_path, ip, FSAdapter.EventSocketPort, auth_pwd)
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
