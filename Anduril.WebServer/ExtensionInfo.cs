using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anduril.WebServer {
    /// <summary>
    ///  A class to hold information about a file extension.   用于保存文件扩展信息类
    /// </summary>
    public class ExtensionInfo {
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
