#if NOTIFY
using Notifications;
#endif
using System;
using System.Collections;
using System.Threading;
using WebSocketSharp;

namespace Example
{
  public struct NfMessage
  {
    public string Summary;
    public string Body;
    public string Icon;
  }

  public class ThreadState
  {
    public bool           Enabled      { get; set; }
    public AutoResetEvent Notification { get; private set; }

    public ThreadState()
    {
      Enabled      = true;
      Notification = new AutoResetEvent(false);
    }
  }

  public class Program
  {
    private static Queue _msgQ = Queue.Synchronized(new Queue());

    private static void enNfMessage(string summary, string body, string icon)
    {
      var msg = new NfMessage
      {
        Summary = summary,
        Body    = body,
        Icon    = icon
      };

      _msgQ.Enqueue(msg);
    }

    public static void Main(string[] args)
    {
      ThreadState ts = new ThreadState();

      WaitCallback notifyMsg = state => 
      {
        while (ts.Enabled || _msgQ.Count > 0)
        {
          Thread.Sleep(500);

          if (_msgQ.Count > 0)
          {
            NfMessage msg = (NfMessage)_msgQ.Dequeue();
            #if NOTIFY
            Notification nf = new Notification(msg.Summary,
                                               msg.Body,
                                               msg.Icon);
            nf.AddHint("append", "allowed");
            nf.Show();
            #else
            Console.WriteLine("{0}: {1}", msg.Summary, msg.Body);
            #endif
          }
        }

        ts.Notification.Set();
      };

      ThreadPool.QueueUserWorkItem(notifyMsg);

      using (WebSocket ws = new WebSocket("ws://echo.websocket.org", "echo"))
      //using (WebSocket ws = new WebSocket("wss://echo.websocket.org", "echo"))
      //using (WebSocket ws = new WebSocket("ws://localhost:4649"))
      //using (WebSocket ws = new WebSocket("ws://localhost:4649/Echo"))
      //using (WebSocket ws = new WebSocket("ws://localhost:4649/Echo?name=nobita"))
      //using (WebSocket ws = new WebSocket("ws://localhost:4649/エコー?name=のび太"))
      //using (WebSocket ws = new WebSocket("ws://localhost:4649/Chat"))
      //using (WebSocket ws = new WebSocket("ws://localhost:4649/Chat?name=nobita"))
      //using (WebSocket ws = new WebSocket("ws://localhost:4649/チャット?name=のび太"))
      {
        ws.OnOpen += (sender, e) =>
        {
          ws.Send("Hi, all!");
        };

        ws.OnMessage += (sender, e) =>
        {
          if (!String.IsNullOrEmpty(e.Data))
          {
            enNfMessage("[WebSocket] Message", e.Data, "notification-message-im");
          }
        };

        ws.OnError += (sender, e) =>
        {
          enNfMessage("[WebSocket] Error", e.Message, "notification-message-im");
        };

        ws.OnClose += (sender, e) =>
        {
          enNfMessage(
            String.Format("[WebSocket] Close({0})", e.Code),
            e.Reason,
            "notification-message-im");
        };

        ws.Connect();

        Thread.Sleep(500);
        Console.WriteLine("\nType \"exit\" to exit.\n");

        string data;
        while (true)
        {
          Thread.Sleep(500);

          Console.Write("> ");
          data = Console.ReadLine();
          if (data == "exit")
          //if (data == "exit" || !ws.IsAlive)
          {
            break;
          }

          ws.Send(data);
        }
      }

      ts.Enabled = false;
      ts.Notification.WaitOne();
    }
  }
}
