using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Example2
{
    public class Echo : WebSocketService
    {
        protected override void OnMessage(object sender, MessageEventArgs e)
        {
            var msg = Ext.Exists(QueryString, "name")
                    ? String.Format("'{0}' returns to {1}", e.Data, QueryString["name"])
                    : e.Data;
            Send(msg);
        }
    }
}