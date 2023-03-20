using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anduril.WebServer {

    /// <summary>
    ///  A class to hold information about a response.  用于保存响应信息的类
    /// </summary>
    public class ResponsePacket {
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

        /// <summary>
        ///  The error code to send back to the client.  发送给客户端的错误代码
        /// </summary>
        public ServerError Error { get; set; }
    }
}
