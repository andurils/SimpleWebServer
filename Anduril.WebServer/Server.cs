using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Anduril.WebServer
{
    /// <summary>
    /// A lean and mean web server. 一个精简高效的Web 服务器
    /// </summary>
    public static class Server
    {

        private static Router router = new Router(); // 路由器

        private static SessionManager sessionManager = new SessionManager(); // 会话管理器

        public static int ExpirationTimeSeconds { get; set; }
        public static string ValidationTokenName { get; set; }


        private static HttpListener listener; // HTTP 协议侦听器

        public static Func<ServerError, string> OnError { get; set; } // 错误处理
        public static Func<Session, string, string, string> PostProcess { get; set; }
        public static Action<Session, HttpListenerContext> OnRequest;  // 请求处理

        public static int maxSimultaneousConnections = 20;  // 最大同时连接数
        // 使用 Semaphore 类控制对资源池的访问 限制可同时访问某一资源或资源池的线程数
        private static Semaphore sem = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);

        /// <summary>
        /// Returns list of IP addresses assigned to localhost network devices, such as hardwired ethernet, wireless, etc.
        /// 返回分配给本地主机网络设备的IP地址列表，例如硬件以太网，无线等。
        /// </summary>
        private static List<IPAddress> GetLocalHostIPs()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            List<IPAddress> ret = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();
            return ret;
        }

        /// <summary>
        ///  Initialize the listener.
        /// </summary> 
        private static HttpListener InitializeListener(List<IPAddress> localhostIPs)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");

            // Listen to IP address as well. 笔记本有多个IP地址
            //localhostIPs.ForEach(ip =>
            //{
            //    Console.WriteLine("Listening on IP " + "http://" + ip.ToString() + ":8080/");
            //    listener.Prefixes.Add("http://" + ip.ToString() + ":8080/");
            //});
            return listener;
        }

        /// <summary>
        /// Begin listening to connections on a separate worker thread. 
        /// 开始监听连接在一个单独的工作线程
        /// </summary>
        private static void Start(HttpListener listener)
        {
            listener.Start();
            Task.Run(() => RunServer(listener));
        }

        /// <summary>
        /// Start awaiting for connections, up to the "maxSimultaneousConnections" value.
        /// This code runs in a separate thread.
        ///  开始等待连接，直到“maxSimultaneousConnections”值。
        /// </summary>
        private static void RunServer(HttpListener listener)
        {
            while (true)
            {
                // 使用 Semaphore 类控制对资源池的访问。 线程通过调用 WaitOne 从类继承 WaitHandle 的方法输入信号灯，
                sem.WaitOne();
                StartConnectionListener(listener);
            }
        }

        /// <summary>
        /// Await connections.  连接监听器
        /// </summary>
        private static async void StartConnectionListener(HttpListener listener)
        {
            ResponsePacket resp = null; // Response packet. 响应包

            // Wait for a connection. Return to caller while we wait.
            // 等待连接 返回调用者  等待传入请求以作为异步操作。
            HttpListenerContext context = await listener.GetContextAsync();
            Session session = sessionManager.GetSession(context.Request.RemoteEndPoint);
            OnRequest.IfNotNull(r => r(session, context));

            //// Obtain a response object. 获取响应对象
            //HttpListenerResponse response = context.Response;
            //// Construct a response. 构造响应
            //string responseString = "<html><head><meta http-equiv='content-type' content='text/html; charset=utf-8'/></head>Hello Browser!</html>";
            //// Get a response stream and write the response to it. 获取响应流并将响应写入它
            //byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);  // 编码
            //response.ContentLength64 = buffer.Length; // 设置响应内容长度
            //System.IO.Stream output = response.OutputStream; // 获取响应输出流
            //output.Write(buffer, 0, buffer.Length); // 写入响应输出流
            //// You must close the output stream.  
            //output.Close(); // 关闭输出流

            // Release the semaphore so that another listener can be immediately started up. 
            sem.Release(); // 调用 Release 该方法释放信号灯。

            // We have a connection, do something...
            Log(context.Request);

            //  Obtain a request object. 获取请求对象   
            HttpListenerRequest request = context.Request;

            try
            {
                string path = request.RawUrl.LeftOf("?"); // Only the path, not any of the parameters  只有路径，而不是任何参数
                string verb = request.HttpMethod; // get, post, delete, etc.  请求谓词 
                // Params on the URL itself follow the URL and are separated by a ? 参数在URL本身跟随URL并由?分隔
                string parms = request.RawUrl.RightOf("?");
                Dictionary<string, object> kvParams = GetKeyValues(parms); // Extract into key-value entries. 提取到字典  
                string data = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();
                GetKeyValues(data, kvParams);
                Log(kvParams);

                resp = router.Route(session, verb, path, kvParams);

                // Update session last connection after getting the response, 
                // as the router itself validates session expiration only on pages requiring authentication.
                // 在获取响应后更新会话最后连接，因为路由器本身仅在需要身份验证的页面上验证会话过期。
                session.UpdateLastConnectionTime();


                if (resp.Error != ServerError.OK)
                {
                    // resp = router.Route("get", OnError(resp.Error), null);
                    resp.Redirect = OnError(resp.Error);
                }

                try
                {
                    Respond(request, context.Response, resp);
                }
                catch (Exception reallyBadException)
                {
                    // The response failed!
                    // TODO: We need to put in some decent logging!
                    Console.WriteLine(reallyBadException.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                resp = new ResponsePacket() { Redirect = OnError(ServerError.ServerError) };
                Respond(request, context.Response, resp);
            }
        }

        // private static void Respond(HttpListenerResponse response, ResponsePacket resp)
        // {
        //     response.ContentType = resp.ContentType;
        //     response.ContentLength64 = resp.Data.Length;
        //     response.OutputStream.Write(resp.Data, 0, resp.Data.Length);
        //     response.ContentEncoding = resp.Encoding;
        //     response.StatusCode = (int)HttpStatusCode.OK;
        //     response.OutputStream.Close();
        // }

        private static void Respond(HttpListenerRequest request, HttpListenerResponse response, ResponsePacket resp)
        {
            if (String.IsNullOrEmpty(resp.Redirect))
            {
                response.ContentType = resp.ContentType;
                response.ContentLength64 = resp.Data.Length;
                response.OutputStream.Write(resp.Data, 0, resp.Data.Length);
                response.ContentEncoding = resp.Encoding;
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.Redirect;
                //response.Redirect("http://" + request.UserHostAddress + resp.Redirect);
                response.Redirect("http://" + request.UserHostName + resp.Redirect);

            }

            // 关闭输出流，否则浏览器可能会挂起，等待数据
            response.OutputStream.Close();
        }

        /// <summary>
        /// Starts the web server. 启动Web服务器
        /// </summary>
        public static void Start(string websitePath)
        {
            router.WebsitePath = websitePath;
            List<IPAddress> localHostIPs = GetLocalHostIPs(); // 获取本地IP地址
            HttpListener listener = InitializeListener(localHostIPs); // 初始化监听器
            Start(listener); // 开始监听
        }

        /// <summary>
        /// Log requests. 记录请求
        /// </summary>
        public static void Log(HttpListenerRequest request)
        {
            Console.WriteLine(request.RemoteEndPoint + " " + request.HttpMethod + " /" + request.Url?.AbsoluteUri.RightOf('/', 3));
        }

        /// <summary>
		/// Log parameters.
		/// </summary>
		private static void Log(Dictionary<string, object> kv)
        {
            kv.ForEach(kvp => Console.WriteLine(kvp.Key + " : " + Uri.UnescapeDataString(kvp.Value.ToString())));
        }

        /// <summary>
		/// Separate out key-value pairs, delimited by & and into individual key-value instances, separated by =
        ///  分离出键值对，由&和分隔的单个键值实例，由=分隔
		/// Ex input: username=abc&password=123
		/// </summary>
		private static Dictionary<string, object> GetKeyValues(string data, Dictionary<string, object> kv = null)
        {
            kv.IfNull(() => kv = new Dictionary<string, object>());
            data.If(d => d.Length > 0, (d) => d.Split('&').ForEach(keyValue => kv[keyValue.LeftOf('=')] = System.Uri.UnescapeDataString(keyValue.RightOf('='))));

            return kv;
        }

        public static void AddRoute(Route route)
        {
            router.AddRoute(route);
        }
    }
}