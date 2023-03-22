using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Anduril.WebServer
{
    /// <summary>
    /// 路由配置
    /// </summary>
    public class Router
    {

        protected Server server;
        public string WebsitePath { get; set; } // The root path of the website.  网站的根路径

        public const string POST = "post";
        public const string GET = "get";
        public const string PUT = "put";
        public const string DELETE = "delete";

        private Dictionary<string, ExtensionInfo> extFolderMap; // Map of extensions to folders.  扩展名到文件夹的映射
        protected List<Route> routes;

        public Router(Server server)
        {
            this.server = server;
            routes = new List<Route>();
            extFolderMap = new Dictionary<string, ExtensionInfo>()
                                {
                                  {".ico", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/ico"}},
                                  {".png", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/png"}},
                                  {".jpg", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/jpg"}},
                                  {".gif", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/gif"}},
                                  {".bmp", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/bmp"}},
                                  {".html", new ExtensionInfo() {Loader=PageLoader, ContentType="text/html"}},
                                  {".css", new ExtensionInfo() {Loader=FileLoader, ContentType="text/css"}},
                                  {".js", new ExtensionInfo() {Loader=FileLoader, ContentType="text/javascript"}},
                                  {"", new ExtensionInfo() {Loader=PageLoader, ContentType="text/html"}},
                                };
        }
        public void AddRoute(Route route)
        {
            routes.Add(route);
        }
        /// <summary>
        /// Read in an image file and returns a ResponsePacket with the raw data. 
        /// 读取图像文件并返回一个ResponsePacket，其中包含原始数据。
        /// </summary>
        private ResponsePacket ImageLoader(Session session, string fullPath, string ext, ExtensionInfo extInfo)
        {
            ResponsePacket ret;

            if (!File.Exists(fullPath))
            {
                ret = new ResponsePacket() { Error = ServerError.FileNotFound };
                Console.WriteLine("!!! File not found: " + fullPath);
            }
            else
            {
                FileStream fStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fStream);
                ret = new ResponsePacket() { Data = br.ReadBytes((int)fStream.Length), ContentType = extInfo.ContentType };
                br.Close();
                fStream.Close();
            }
            return ret;
        }

        /// <summary>
        /// Read in what is basically a text file and return a ResponsePacket with the text UTF8 encoded.
        ///  读取基本上是一个文本文件，并返回一个ResponsePacket，其中包含UTF8编码的文本。
        /// </summary>
        private ResponsePacket FileLoader(Session session, string fullPath, string ext, ExtensionInfo extInfo)
        {
            ResponsePacket ret;

            if (!File.Exists(fullPath))
            {
                ret = new ResponsePacket() { Error = ServerError.FileNotFound };
                Console.WriteLine("!!! File not found: " + fullPath);
            }
            else
            {
                string text = File.ReadAllText(fullPath);

                // post processing option, such as adding a validation token.  处理选项，例如添加验证令牌。
                text = server.PostProcess(session, text);

                ret = new ResponsePacket() { Data = Encoding.UTF8.GetBytes(text), ContentType = extInfo.ContentType, Encoding = Encoding.UTF8 };
            }
            return ret;
        }

        /// <summary>
        /// Load an HTML file, taking into account missing extensions and a file-less IP/domain, 
        /// which should default to index.html.
        ///  加载HTML文件，考虑到缺少扩展名和文件的IP/域，应该默认为index.html。
        /// </summary>
        private ResponsePacket PageLoader(Session session, string fullPath, string ext, ExtensionInfo extInfo)
        {
            ResponsePacket ret = new ResponsePacket();
            // If nothing follows the domain name or IP, then default to loading index.html. 
            // 如果域名或IP后面没有任何内容，则默认加载index.html。
            if (fullPath == WebsitePath)
            {
                // Inject the "Pages" folder into the path and append "index.html" to the end.   
                // 将“Pages”文件夹注入路径并将“index.html”附加到末尾。
                //ret = Route(GET, "/index.html", null);
                ret = Route(null, GET, "/index.html", null);

            }
            else
            {
                if (String.IsNullOrEmpty(ext))
                {
                    // No extension, so we make it ".html"  没有扩展名，所以我们把它变成“.html”
                    fullPath = fullPath + ".html";
                }

                // Inject the "Pages" folder into the path  将“Pages”文件夹注入路径
                fullPath = WebsitePath + "\\Pages" + fullPath.RightOf(WebsitePath);


                if (!File.Exists(fullPath))
                {
                    ret = new ResponsePacket() { Error = ServerError.PageNotFound };
                    Console.WriteLine("!!! File not found: " + fullPath);
                }
                else
                {
                    ret = FileLoader(session, fullPath, ext, extInfo);
                }
            }

            return ret;
        }

        //  Route a request to the appropriate handler.  将请求路由到适当的处理程序。
        public ResponsePacket Route(Session session, string verb, string path, Dictionary<string, object> kvParams)
        {
            // 获取扩展名
            string ext = Path.GetExtension(path); //path.RightOfRightmostOf('.'); 
            ExtensionInfo extInfo;
            ResponsePacket ret = null;

            verb = verb.ToLower();

            if (extFolderMap.TryGetValue(ext, out extInfo))
            {
                // Strip off leading '/' and reformat as with windows path separator.    去掉前导“/”并重新格式化为Windows路径分隔符。 
                string wpath = path.Substring(1).Replace('/', '\\');
                string fullPath = Path.Combine(WebsitePath, wpath);

                // Check for a route handler.  检查路由处理程序。  
                //Route route = routes.SingleOrDefault(r => verb == r.Verb.ToLower() && path == r.Path);
                Route routeHandler = routes.SingleOrDefault(r => verb == r.Verb.ToLower() && path == r.Path.ToLower());

                if (routeHandler != null)
                {
                    // Application has a handler for this route. 应用程序有一个处理程序来处理这个路由。
                    // string redirect = route.Action(kvParams);
                    //string redirect = route.Handler.Handle(session, kvParams);

                    ResponsePacket handlerResponse = null;
                    // If a handler exists:
                    routeHandler.Handler.IfNotNull((h) => handlerResponse = h.Handle(session, kvParams));



                    //if (String.IsNullOrEmpty(redirect)) {
                    //    // Respond with default content loader. 响应默认内容加载程序。
                    //    ret = extInfo.Loader(fullPath, ext, extInfo);
                    //}
                    //else {
                    //    // Respond with redirect. 响应重定向。
                    //    ret = new ResponsePacket() { Redirect = redirect };
                    //}
                    // ret = extInfo.Loader(fullPath, ext, extInfo); // Call the appropriate loader.  调用适当的加载程序。

                    if (handlerResponse == null)
                    {
                        // Respond with default content loader.
                        ret = extInfo.Loader(session, fullPath, ext, extInfo);
                    }
                    else
                    {
                        // Respond with redirect.
                        ret = handlerResponse;
                    }

                }
                else
                {
                    // Attempt default behavior  尝试默认行为
                    ret = extInfo.Loader(session, fullPath, ext, extInfo);
                }
            }
            else
            {
                ret = new ResponsePacket() { Error = ServerError.UnknownType };
            }

            return ret;
        }

    }

}
