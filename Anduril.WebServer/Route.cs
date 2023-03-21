using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anduril.WebServer
{

    /// <summary>
    ///   A class to hold information about a route.  用于保存路由信息的类
    /// </summary>

    public class Route
    {

        /// <summary>
        ///  The HTTP verb to match.  匹配的HTTP动词
        /// </summary>
        public string Verb { get; set; }

        /// <summary>
        ///  The path to match.  匹配的路径
        /// </summary>
        public string Path { get; set; }

        // /// <summary>
        // ///   回调函数，该函数将URL参数传递给，并期望“可选”重定向URL。
        // /// </summary>
        // public Func<Dictionary<string, object>, string> Action { get; set; }

        /// <summary>
        ///  The handler to call when the route is matched.  匹配路由时要调用的处理程序
        /// </summary>
        public RouteHandler Handler { get; set; }
    }

}
