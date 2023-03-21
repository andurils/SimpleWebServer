using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Anduril.WebServer
{
    /// <summary>
    ///  Manages all sessions. zh-CN:管理所有会话。
    /// </summary>
    public class SessionManager
    {
        /// <summary>
        /// Track all sessions. The key is the remote endpoint IP address.
        /// zh-CN:跟踪所有会话。键是远程端点IP地址。
        /// </summary>
        protected Dictionary<IPAddress, Session> sessionMap = new Dictionary<IPAddress, Session>();

        // TODO: We need a way to remove very old sessions so that the server doesn't accumulate thousands of stale endpoints. 
        // zh-CN:我们需要一种方法来删除非常旧的会话，以便服务器不会累积数千个陈旧的端点。

        public SessionManager()
        {
            sessionMap = new Dictionary<IPAddress, Session>();
        }

        /// <summary>
        /// Creates or returns the existing session for this remote endpoint. 
        /// 为此远程端点创建或返回现有会话。
        /// </summary>
        public Session GetSession(IPEndPoint remoteEndPoint)
        {
            // The port is always changing on the remote endpoint, so we can only use IP portion. 
            // zh-CN:远程端点的端口始终在变化，因此我们只能使用IP部分。
            Session session = sessionMap.CreateOrGet(remoteEndPoint.Address);

            return session;
        }
    }
}
