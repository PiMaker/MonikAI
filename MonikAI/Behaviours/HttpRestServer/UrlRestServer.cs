using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;


using uhttpsharp;
using uhttpsharp.Handlers;
using uhttpsharp.Listeners;
using uhttpsharp.RequestProviders;

using receive_http_request_from_monkey_script;

namespace MonikAI.Behaviours.HttpRestServer
{
    class UrlRestServer
    {
        public static string URL { get => lastURL; }
        static string lastURL = null;

        //may be offset this to a thread?
        public static Task StartServer()
        {
            var httpServer = new HttpServer(new HttpRequestProvider());

            // listen to 127.0.0.1:82 :
            httpServer.Use(new TcpListenerAdapter(new TcpListener(IPAddress.Loopback, 82)));

            // Request handling : 
            httpServer.Use((context, next) =>
            {
                return next();
            });

            // Handler request : 
            httpServer.Use(new HttpRouter().With("monikai", new RestHandler<string>(new RestController((url) => lastURL = url), JsonResponseProvider.Default)));

            httpServer.Start();

            return Task.Run(() => httpServer.Dispose());
        }
    }
}
