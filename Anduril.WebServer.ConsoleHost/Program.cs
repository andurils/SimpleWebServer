using System.Net;
using System.Reflection;

namespace Anduril.WebServer.ConsoleHost
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //var prefixes = new string[] { "http://localhost:8090/" };
            //SimpleListenerExample(prefixes);
            string websitePath = GetWebsitePath();
            Server.Start(websitePath);
            Console.ReadLine();
        }

        public static string GetWebsitePath()
        {
            // Path of our exe.
            string websitePath = Assembly.GetExecutingAssembly().Location;
            websitePath = websitePath.LeftOfRightmostOf("\\").LeftOfRightmostOf("\\").LeftOfRightmostOf("\\").LeftOfRightmostOf("\\") + "\\Website";
            //websitePath = websitePath.LeftOfRightmostOf("\\")+ "\\Website";

            return websitePath;
        }

        // https://www.codeproject.com/Articles/859108/Writing-a-Web-Server-from-Scratch
        // https://www.codeproject.com/Articles/826383/REST-A-Simple-REST-framework?msg=4969159#xx4969159xx

        // This example requires the System and System.Net namespaces.
        // https://learn.microsoft.com/zh-cn/dotnet/api/system.net.httplistener?view=net-6.0
        /* Here is the explanation for the code above:
        1. First, we check if the HttpListener class is supported by the current operating system.
        2. Next, we check if the prefixes argument is null or empty.
        3. We create a new instance of HttpListener.
        4. We add the prefixes to the listener.
        5. We start the listener, and display a message in the console.
        6. We call the GetContext method to wait for a request.
        7. We get the request object.
        8. We get the response object.
        9. We construct a response.
        10. We get the output stream and write the response to it.
        11. We close the output stream.
        12. We stop the listener. */
        public static void SimpleListenerExample(string[] prefixes)
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // Create a listener.
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }
            listener.Start();
            Console.WriteLine("Listening...");
            // Note: The GetContext method blocks while waiting for a request.
            HttpListenerContext context = listener.GetContext();
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
            listener.Stop();
        }
    }
}