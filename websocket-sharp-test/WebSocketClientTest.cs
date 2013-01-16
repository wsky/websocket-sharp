using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace WebSocketSharp
{
    public class WebSocketClientTest
    {
        public void Send_Receive_Test()
        {
            var handle = new EventWaitHandle(false, EventResetMode.AutoReset);

            WebSocket ws = new WebSocket("ws://localhost:8080/frontend");
            ws.OnMessage += (s, e) => { Console.WriteLine(e.Data); handle.Set(); };
            ws.OnOpen += (s, e) => { Thread.Sleep(2000); ws.Send(this.GetMessageBuffer()); };
            ws.OnError += (s, e) => { Console.WriteLine("---- OnError:{0}", e.Message); handle.Set(); };
            ws.OnClose += (s, e) => { Console.WriteLine("---- OnClose:{0}|{1}", e.Code, e.Reason); handle.Set(); };
            ws.Origin = "csharp";
            ws.Connect();

            handle.WaitOne();
        }

        public void PingTest()
        {
            Exception error = null;
            var handle = new EventWaitHandle(false, EventResetMode.AutoReset);

            WebSocket ws = new WebSocket("ws://localhost:8080/frontend");
            ws.ExtraHeaders = new Dictionary<string, string>() { { "id", "abc" } };
            ws.OnClose += (s, e) =>
            {
                Console.WriteLine("---- OnClose:{0}|{1}", e.Code, e.Reason);
                if (e.Code == 1002)
                    error = new Exception(e.Reason);
                handle.Set();
            };
            ws.OnOpen += (s, e) =>
            {
                if (ws.IsAlive)
                {
                    ws.Ping();
                    handle.Set();
                }
            };
            ws.Origin = "csharp";
            ws.Connect();

            handle.WaitOne();

            if (error != null)
                throw error;
        }

        private byte[] GetMessageBuffer()
        {
            var buffer = new byte[22];
            var binary = new BinaryWriter(new MemoryStream(buffer));
            //type 1
            binary.Write((byte)2);
            //to 8
            binary.Write((byte)' ');
            binary.Write((byte)' ');
            binary.Write((byte)'c');
            binary.Write((byte)'s');
            binary.Write((byte)'h');
            binary.Write((byte)'a');
            binary.Write((byte)'r');
            binary.Write((byte)'p');
            //format 1
            binary.Write((byte)1);
            //remaing 4
            binary.Write(SwapInt32(8));
            //messageid 8
            binary.Write(SwapInt64(8L));
            return buffer;
        }
        private short SwapInt16(short v)
        {
            return (short)(((v & 0xff) << 8) | ((v >> 8) & 0xff));
        }
        private int SwapInt32(int v)
        {
            return (int)(((SwapInt16((short)v) & 0xffff) << 0x10) | (SwapInt16((short)(v >> 0x10)) & 0xffff));
        }
        private long SwapInt64(long v)
        {
            return (long)(((SwapInt32((int)v) & 0xffffffffL) << 0x20) | (SwapInt32((int)(v >> 0x20)) & 0xffffffffL));
        }
    }
}