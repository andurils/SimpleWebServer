using Anduril.WebServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anduril.WebServer
{
    /// <summary>
    /// The base class for route handlers. 
    /// 路由处理程序的基类。
    /// </summary>
    public abstract class RouteHandler
    {
        protected Func<Session, Dictionary<string, object>, string> handler;

        public RouteHandler(Func<Session, Dictionary<string, object>, string> handler)
        {
            this.handler = handler;
        }

        public abstract string Handle(Session session, Dictionary<string, object> parms);
    }

    /// <summary>
    /// Page is always visible.  匿名路由处理程序 (默认)
    /// </summary>
    public class AnonymousRouteHandler : RouteHandler
    {
        public AnonymousRouteHandler(Func<Session, Dictionary<string, object>, string> handler)
            : base(handler)
        {
        }

        public override string Handle(Session session, Dictionary<string, object> parms)
        {
            return handler(session, parms);
        }
    }

    /// <summary>
    /// Page is visible only to authorized users.   授权路由处理程序
    /// </summary>
    public class AuthenticatedRouteHandler : RouteHandler
    {
        public AuthenticatedRouteHandler(Func<Session, Dictionary<string, object>, string> handler)
            : base(handler)
        {
        }

        public override string Handle(Session session, Dictionary<string, object> parms)
        {
            string ret;

            if (session.Authorized)
            {
                ret = handler(session, parms); // 授权状态下，执行路由处理程序
            }
            else
            {
                ret = Server.OnError(ServerError.NotAuthorized); // 未授权状态下，执行错误处理程序
            }

            return ret;
        }
    }

    /// <summary>
    /// Page is visible only to authorized users whose session has not expired.
    /// 会话过期路由处理程序
    /// </summary>
    public class AuthenticatedExpirableRouteHandler : AuthenticatedRouteHandler
    {
        public AuthenticatedExpirableRouteHandler(Func<Session, Dictionary<string, object>, string> handler)
            : base(handler)
        {
        }

        public override string Handle(Session session, Dictionary<string, object> parms)
        {
            string ret;
            // 会话过期判断
            if (session.IsExpired(Server.ExpirationTimeSeconds))
            {
                session.Authorized = false;
                ret = Server.OnError(ServerError.ExpiredSession);
            }
            else
            {
                ret = base.Handle(session, parms);
            }

            return ret;
        }
    }
}

