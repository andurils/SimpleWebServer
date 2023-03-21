using Anduril.WebServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Anduril.WebServer
{
    /// <summary>
    /// Sessions are associated with the client IP. 
    /// They are used to track the last time a client connected to the server 
    /// and to track whether the client is authorized to access the server.
    /// Sessions are stored in the SessionManager.
    /// 会话是与客户端IP相关联的。它们用于跟踪客户端上次连接到服务器的时间，并跟踪客户端是否被授权访问服务器。会话存储在SessionManager中。
    /// </summary>
    public class Session
    {
        public DateTime LastConnection { get; set; } // 最后一次连接时间
        public bool Authorized { get; set; } // 是否授权

        /// <summary>
        /// Can be used by controllers to add additional information that needs to persist in the session.
        /// 可以由控制器用于添加需要在会话中保留的其他信息。
        /// </summary>
        public Dictionary<string, string> Objects { get; set; }

        public Session()
        {
            Objects = new Dictionary<string, string>();
            UpdateLastConnectionTime();
        }

        /// <summary>
        ///  Updates the last connection time to the current time.  zh-CN:更新最后一次连接时间为当前时间。
        /// </summary>
        public void UpdateLastConnectionTime()
        {
            LastConnection = DateTime.Now;
        }


        /// <summary>
        /// Returns true if the last request exceeds the specified expiration time in seconds. zh-CN:如果最后一次请求超过指定的过期时间（以秒为单位），则返回true。
        /// </summary>
        public bool IsExpired(int expirationInSeconds)
        {
            return (DateTime.Now - LastConnection).TotalSeconds > expirationInSeconds;
        }
    }
}
