using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anduril.WebServer
{
    public class Router
    {
        public string WebsitePath { get; set; } // The root path of the website.  网站的根路径

        public const string POST = "post";
        public const string GET = "get";
        public const string PUT = "put";
        public const string DELETE = "delete";

        private Dictionary<string, ExtensionInfo> extFolderMap; // Map of extensions to folders.  扩展名到文件夹的映射

        public Router()
        {
            extFolderMap = new Dictionary<string, ExtensionInfo>()
                                {
                                  {"ico", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/ico"}},
                                  {"png", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/png"}},
                                  {"jpg", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/jpg"}},
                                  {"gif", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/gif"}},
                                  {"bmp", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/bmp"}},
                                  {"html", new ExtensionInfo() {Loader=PageLoader, ContentType="text/html"}},
                                  {"css", new ExtensionInfo() {Loader=FileLoader, ContentType="text/css"}},
                                  {"js", new ExtensionInfo() {Loader=FileLoader, ContentType="text/javascript"}},
                                  {"", new ExtensionInfo() {Loader=PageLoader, ContentType="text/html"}},
                                };
        }

        /// <summary>
        /// Read in an image file and returns a ResponsePacket with the raw data. 
        /// 读取图像文件并返回一个ResponsePacket，其中包含原始数据。
        /// </summary>
        private ResponsePacket ImageLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            FileStream fStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fStream);
            ResponsePacket ret = new ResponsePacket() { Data = br.ReadBytes((int)fStream.Length), ContentType = extInfo.ContentType };
            br.Close();
            fStream.Close();

            return ret;
        }

        /// <summary>
        /// Read in what is basically a text file and return a ResponsePacket with the text UTF8 encoded.
        ///  读取基本上是一个文本文件，并返回一个ResponsePacket，其中包含UTF8编码的文本。
        /// </summary>
        private ResponsePacket FileLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            string text = File.ReadAllText(fullPath);
            ResponsePacket ret = new ResponsePacket() { Data = Encoding.UTF8.GetBytes(text), ContentType = extInfo.ContentType, Encoding = Encoding.UTF8 };

            return ret;
        }

        /// <summary>
        /// Load an HTML file, taking into account missing extensions and a file-less IP/domain, 
        /// which should default to index.html.
        ///  加载HTML文件，考虑到缺少扩展名和文件的IP/域，应该默认为index.html。
        /// </summary>
        private ResponsePacket PageLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            ResponsePacket ret = new ResponsePacket();
            // If nothing follows the domain name or IP, then default to loading index.html. 
            // 如果域名或IP后面没有任何内容，则默认加载index.html。
            if (fullPath == WebsitePath)
            {
                // Inject the "Pages" folder into the path and append "index.html" to the end.   
                // 将“Pages”文件夹注入路径并将“index.html”附加到末尾。
                ret = Route(GET, "/index.html", null);
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
                ret = FileLoader(fullPath, ext, extInfo);
            }

            return ret;
        }

        //  Route a request to the appropriate handler.  将请求路由到适当的处理程序。
        public ResponsePacket Route(string verb, string path, Dictionary<string, object> kvParams)
        {
            string ext = path.RightOf('.'); // Get the extension.  获取扩展名
            ExtensionInfo extInfo;
            ResponsePacket ret = null;

            if (extFolderMap.TryGetValue(ext, out extInfo))
            {
                // Strip off leading '/' and reformat as with windows path separator.    去掉前导“/”并重新格式化为Windows路径分隔符。 
                string fullPath = Path.Combine(WebsitePath, path);
                ret = extInfo.Loader(fullPath, ext, extInfo); // Call the appropriate loader.  调用适当的加载程序。
            }

            return ret;
        }
    }

    //public class Route
    //{
    //    public string Verb { get; set; }
    //    public string Path { get; set; }
    //    public Func<Dictionary<string, string>, string> Action { get; set; }
    //}

    /// <summary>
    ///  A class to hold information about a response.  用于保存响应信息的类
    /// </summary>
    public class ResponsePacket
    {
        /// <summary>
        ///  If this is not null, then the server will redirect to this URL.  如果不为空，则服务器将重定向到此URL
        /// </summary> 
        public string Redirect { get; set; }
        /// <summary>
        ///  The raw data to send back to the client.  发送给客户端的原始数据
        /// </summary>
        public byte[] Data { get; set; }
        /// <summary>
        ///  The content type of the response.  响应的内容类型
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        ///  The encoding of the response.  响应的编码
        /// </summary>
        public Encoding Encoding { get; set; }
    }

    /// <summary>
    ///  A class to hold information about a file extension.   用于保存文件扩展信息类
    /// </summary>
    internal class ExtensionInfo
    {
        /// <summary>
        ///  The content type of the file.  文件的内容类型
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        ///  The loader function to use to load the file.  用于加载文件的加载函数
        /// </summary>
        public Func<string, string, ExtensionInfo, ResponsePacket> Loader { get; set; }
    }
}
