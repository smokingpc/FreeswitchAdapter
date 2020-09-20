using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using System.Net;
using System.Net.Sockets;

namespace FSAdapter
{
    public partial class FSAdapter
    {
        /// <summary>
        /// FreeSwitch有異常的時候，丟出去告訴使用者，請重新開啟
        /// </summary>
        public event EventHandler<Exception> OnException;
        
        //exmaple of responsed OK:
        //Content-Type: api/response
        //Content-Length: 14
        //+OK [Success]

        private string RegexContentType = @"Content-Type: (.+)/(.+)";
        private string RegexContentLength = @"Content-Length: (\d+)";
        private string RegexReplyText = "Reply-Text:(.+)";

        private string SendCommand(string cmd)
        {
            string response = "";

            using (var sock = new TcpClient())
            {
                try
                {
                    sock.Connect(AddrESL);
                    //Freeswitch command is similar as HTTP rules.
                    //Send a empty line to finish command.
                    using (var stream = sock.GetStream())
                    using (var reader = new StreamReader(stream))
                    using (var writer = new StreamWriter(stream))
                    {
                        string msg = ReadMessages(reader);

                        string cmd_auth = "auth " + AuthPwd;
                        WriteCmd(writer, cmd_auth);
                        msg = ReadCmdResp(reader);

                        WriteCmd(writer, cmd);
                        msg = ReadCmdResp(reader);
                        response = msg;
                    }
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    if (null != OnException)
                        OnException(this, ex);
                }
                catch (Exception ex)
                {
                    if (null != OnException)
                        OnException(this, ex);
                }

            }

            return response;
        }

        private void WriteCmd(StreamWriter writer, string cmd)
        {
            try
            {
                writer.WriteLine(cmd);
                writer.WriteLine("");
                writer.Flush();         //flush buffer to avoid hanging...
            }
            catch (System.IO.IOException ex)
            {
                if (null != OnException)
                    OnException(this, ex);
            }
            catch (Exception ex)
            {
                if (null != OnException)
                    OnException(this, ex);
            }
        }
        private string ReadCmdResp(StreamReader reader)
        {
            //Command Reply Example:
            //Content-Type: command/reply
            //Reply-Text: -ERR command not found
            //----or----
            //Content-Type: api/response
            //Content-Length: 38

            //First line of console reply is content-type. Second line is reply-text.
            //The api command should obey rule similar as HTTP : 
            //  First line is Content-Type, second line is Content-Length.
            //  After Content-Length, followed by N bytes content.

            string reply = "";
            string msg = "";

            //Read all lines then parse them. 
            msg = ReadMessages(reader);

            //如果下面的處理動作都沒處理到reply，那就不是我們想(or能)處理的msg，
            //回傳完整訊息讓caller傷腦筋去...
            //If all following processing skipped by reply, this replied message is NOT
            // the message we want(or not we can process).
            //Just send it back to caller....caller should take care of it.
            reply = msg;

            var match = Regex.Match(msg, RegexContentType);
            if (match.Success)
            {
                string src = match.Groups[1].Value;
                string act = match.Groups[2].Value;
                //native command and reply will not have Content-Length field.
                if (0 == string.Compare(src, "command", true) && 0 == string.Compare(act, "reply", true))
                {
                    match = Regex.Match(msg, RegexReplyText);
                    if (match.Success)
                    {
                        reply = match.Groups[1].Value;
                    }
                }
                else if (0 == string.Compare(src, "api", true) && 0 == string.Compare(act, "response", true))
                {
                    match = Regex.Match(msg, RegexContentLength);
                    if (match.Success)
                    {
                        int size = int.Parse(match.Groups[1].Value);
                        reply = ReadContent(reader, size);
                    }
                }
            }
            return reply;
        }

        //Reply is a "single line" response. no payload length.
        //Just use readline to get it.
        private string ReadReply(StreamReader reader)
        {
            string line = reader.ReadLine();
            var match = Regex.Match(line, RegexReplyText);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return "";
        }

        private string ReadMessages(StreamReader reader)
        {
            string msg = "";
            string line = "ok";

            while (line != "")
            {
                try
                {

                    line = reader.ReadLine();
                    if (msg != "")
                        msg = msg + "\n";
                    msg = msg + line;

                }
                catch (System.IO.IOException ex)
                {
                    if (null != OnException)
                        OnException(this, ex);
                    line = "";
                }
                catch (ObjectDisposedException ex)
                {
                    if (null != OnException)
                        OnException(this, ex);
                    line = "";

                }
                catch (Exception ex)
                {
                    if (null != OnException)
                        OnException(this, ex);
                }
            }
            return msg;
        }

        //Mostly, the response content used in command which start with "api" .
        //example(list all conference rooms): api conference list
        //It provides longer data in response, so there is Content-Length field.
        //Parsing method is different from Reply
        private string ReadContent(StreamReader reader, int size)
        {
            var buffer = new char[size];
            int out_size = 0;
            int read_size = size;
            int start_index = 0;
            string msg = "";

            while (read_size > 0)
            {
                out_size = reader.Read(buffer, start_index, read_size);
                read_size = read_size - out_size;
                start_index = start_index + out_size;
            }

            msg = new string(buffer);
            if (buffer[size - 1] != '}')
            { }

            return msg;
        }
    }
}
