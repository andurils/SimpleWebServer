using System.Net;
using System.Net.Sockets;

namespace Anduril.WebServer
{
    /// <summary>
    /// A lean and mean web server. 一个精简高效的Web 服务器
    /// </summary>
    public static class Server
    {
        private static HttpListener listener;
        public static int maxSimultaneousConnections = 20;
        private static Semaphore sem = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);

        /// <summary>
        /// Returns list of IP addresses assigned to localhost network devices, such as hardwired ethernet, wireless, etc.
        /// </summary>
        private static List<IPAddress> GetLocalHostIPs()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            List<IPAddress> ret = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();

            return ret;
        }

        private static HttpListener InitializeListener(List<IPAddress> localhostIPs)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            //listener.Start();
            // Listen to IP address as well.
            //localhostIPs.ForEach(ip =>
            //{
            //    Console.WriteLine("Listening on IP " + "http://" + ip.ToString() + "/");
            //    listener.Prefixes.Add("http://" + ip.ToString() + "/");
            //});

            return listener;
        }

        /// <summary>
        /// Begin listening to connections on a separate worker thread.
        /// </summary>
        private static void Start(HttpListener listener)
        {
            listener.Start();
            Task.Run(() => RunServer(listener));
        }

        /// <summary>
        /// Start awaiting for connections, up to the "maxSimultaneousConnections" value.
        /// This code runs in a separate thread.
        /// </summary>
        private static void RunServer(HttpListener listener)
        {
            while (true)
            {
                sem.WaitOne();
                StartConnectionListener(listener);
            }
        }

        /// <summary>
        /// Await connections.
        /// </summary>
        private static async void StartConnectionListener(HttpListener listener)
        {
            // Wait for a connection. Return to caller while we wait.
            HttpListenerContext context = await listener.GetContextAsync();

            HttpListenerRequest request = context.Request;
            // Obtain a response object.
            HttpListenerResponse response = context.Response;
            // Construct a response.
            string responseString = "<HTML><TITLE>HttpListener</TITLE><BODY> Hello world!</BODY></HTML>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();

            // Release the semaphore so that another listener can be immediately started up.
            sem.Release();

            // We have a connection, do something...
            Log(context.Request);
        }

        /// <summary>
        /// Starts the web server.
        /// </summary>
        public static void Start()
        {
            List<IPAddress> localHostIPs = GetLocalHostIPs();
            HttpListener listener = InitializeListener(localHostIPs);
            Start(listener);
        }

        /// <summary>
        /// Log requests.
        /// </summary>
        public static void Log(HttpListenerRequest request)
        {
            Console.WriteLine(request.RemoteEndPoint + " " + request.HttpMethod + " /" + request.Url.AbsoluteUri.RightOf('/', 3));
        }

    }
}