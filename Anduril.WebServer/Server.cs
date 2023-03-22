using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Anduril.WebServer
{
    /// <summary>
    /// A lean and mean web server. 一个精简高效的Web 服务器
    /// </summary>
    public class Server
    {

        private Router router { get; set; } // 路由器 
        private SessionManager sessionManager { get; set; } // 会话管理器 
        public int ExpirationTimeSeconds { get; set; }
        public string ValidationTokenName { get; set; } = "__CSRFToken__";
        protected string ValidationTokenScript { get; set; } = "<%AntiForgeryToken%>";
        public int MaxSimultaneousConnections { get; set; }
        public string PublicIP { get; set; }

        private HttpListener listener { get; set; } // HTTP 协议侦听器
        private Semaphore sem { get; set; }

        public Func<ServerError, string> OnError { get; set; } // 错误处理
        // post-process the HTML before it is returned to the browser.  在返回到浏览器之前对HTML进行后处理
        // Cross-Site Request Forgery (CSRF) 处理
        public Func<Session, string, string> PostProcess { get; set; }
        public Action<Session, HttpListenerContext> OnRequest;  // 请求处理

        // 使用 Semaphore 类控制对资源池的访问 限制可同时访问某一资源或资源池的线程数

        public Server()
        {
            // This needs to be externally settable before initializing the semaphore.  在初始化信号量之前，需要外部设置
            // 最大同时连接数
            MaxSimultaneousConnections = 20;
            ExpirationTimeSeconds = 60;      // default expires in 1 minute.
            ValidationTokenName = "__CSRFToken__";


            sem = new Semaphore(MaxSimultaneousConnections, MaxSimultaneousConnections);
            router = new Router(this);
            sessionManager = new SessionManager(this);
            PostProcess = DefaultPostProcess;
        }

        /// <summary>
        /// Returns list of IP addresses assigned to localhost network devices, such as hardwired ethernet, wireless, etc.
        /// 返回分配给本地主机网络设备的IP地址列表，例如硬件以太网，无线等。
        /// </summary>
        private List<IPAddress> GetLocalHostIPs()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            List<IPAddress> ret = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();
            return ret;
        }

        /// <summary>
        ///  Initialize the listener.
        /// </summary> 
        private HttpListener InitializeListener(List<IPAddress> localhostIPs)
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
        private void Start(HttpListener listener)
        {
            listener.Start();
            Task.Run(() => RunServer(listener));
        }

        /// <summary>
        /// Start awaiting for connections, up to the "maxSimultaneousConnections" value.
        /// This code runs in a separate thread.
        ///  开始等待连接，直到“maxSimultaneousConnections”值。
        /// </summary>
        private void RunServer(HttpListener listener)
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
        private async void StartConnectionListener(HttpListener listener)
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

                // 非GET动词实现CSRF检查
                verb = verb.ToLower();
                if (verb != "get")
                {
                    if (!VerifyCSRF(session, kvParams))
                    {
                        Console.WriteLine("CSRF did not match.  Terminating connection.");

                        resp = Redirect(OnError(ServerError.ValidationError));
                        Respond(request, context.Response, resp);
                        return;
                    }
                }

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

        // private  void Respond(HttpListenerResponse response, ResponsePacket resp)
        // {
        //     response.ContentType = resp.ContentType;
        //     response.ContentLength64 = resp.Data.Length;
        //     response.OutputStream.Write(resp.Data, 0, resp.Data.Length);
        //     response.ContentEncoding = resp.Encoding;
        //     response.StatusCode = (int)HttpStatusCode.OK;
        //     response.OutputStream.Close();
        // }

        private void Respond(HttpListenerRequest request, HttpListenerResponse response, ResponsePacket resp)
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
                //response.Redirect("http://" + request.UserHostName + resp.Redirect);
                if (String.IsNullOrEmpty(PublicIP))
                {
                    response.Redirect("http://" + request.UserHostName + resp.Redirect);
                }
                else
                {
                    response.Redirect("http://" + PublicIP + resp.Redirect);
                }

            }

            // 关闭输出流，否则浏览器可能会挂起，等待数据
            response.OutputStream.Close();
        }

        /// <summary>
        /// Starts the web server. 启动Web服务器
        /// </summary>
        public void Start(string websitePath, int port = 80, bool acquirePublicIP = false)
        {


            if (acquirePublicIP)
            {
                PublicIP = GetExternalIP();
                Console.WriteLine("public IP: " + PublicIP);
            }

            router.WebsitePath = websitePath;
            List<IPAddress> localHostIPs = GetLocalHostIPs(); // 获取本地IP地址
            HttpListener listener = InitializeListener(localHostIPs); // 初始化监听器
            Start(listener); // 开始监听
        }

        /// <summary>
        /// Log requests. 记录请求
        /// </summary>
        public void Log(HttpListenerRequest request)
        {
            Console.WriteLine(request.RemoteEndPoint + " " + request.HttpMethod + " /" + request.Url?.AbsoluteUri.RightOf('/', 3));
        }

        /// <summary>
		/// Log parameters.
		/// </summary>
		private void Log(Dictionary<string, object> kv)
        {
            kv.ForEach(kvp => Console.WriteLine(kvp.Key + " : " + Uri.UnescapeDataString(kvp.Value.ToString())));
        }

        /// <summary>
		/// Separate out key-value pairs, delimited by & and into individual key-value instances, separated by =
        ///  分离出键值对，由&和分隔的单个键值实例，由=分隔
		/// Ex input: username=abc&password=123
		/// </summary>
		private Dictionary<string, object> GetKeyValues(string data, Dictionary<string, object> kv = null)
        {
            kv.IfNull(() => kv = new Dictionary<string, object>());
            data.If(d => d.Length > 0, (d) => d.Split('&').ForEach(keyValue => kv[keyValue.LeftOf('=')] = System.Uri.UnescapeDataString(keyValue.RightOf('='))));

            return kv;
        }

        public void AddRoute(Route route)
        {
            router.AddRoute(route);
        }

        /// <summary>
		/// Return a ResponsePacket with the specified URL and an optional (singular) parameter.
        /// 返回具有指定URL和可选（单个）参数的ResponsePacket。
		/// </summary>
		public ResponsePacket Redirect(string url, string parm = null)
        {
            ResponsePacket ret = new ResponsePacket() { Redirect = url };
            parm.IfNotNull((p) => ret.Redirect += "?" + p);

            return ret;
        }




        private string GetExternalIP()
        {
            string externalIP = "";
            try
            {
                using (WebClient client = new WebClient())
                {
                    externalIP = client.DownloadString("https://api.ipify.org");
                    externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")).Matches(externalIP)[0].ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            return externalIP;
        }


        private string DefaultPostProcess(Session session, string html)
        {
            string ret = html.Replace(ValidationTokenScript,
              @$"<input name='{ValidationTokenName}' type='hidden' value='{session.Objects[ValidationTokenName].ToString()}' id='#__csrf__' />");

            return ret;
        }

        /// <summary>
        /// If a CSRF validation token exists, verify it matches our session value.  
        /// If one doesn't exist, issue a warning to the console. 
        /// 如果存在CSRF验证令牌，请验证它是否与我们的会话值匹配。如果不存在，请在控制台上发出警告。
        /// </summary>
        private bool VerifyCSRF(Session session, Dictionary<string, object> kvParams)
        {
            bool ret = true;
            object token;

            if (kvParams.TryGetValue(ValidationTokenName, out token))
            {
                ret = session.Objects[ValidationTokenName].ToString() == token.ToString();
            }
            else
            {
                Console.WriteLine("Warning - CSRF token is missing. Consider adding it to the request.");
            }

            return ret;
        }

    }
}