using System.Net;
using System.Reflection;
using System.Text;

namespace Anduril.WebServer.ConsoleHost {
    internal class Program {
        public static Server server;

        static void Main(string[] args) {
            //var prefixes = new string[] { "http://localhost:8090/" };
            //SimpleListenerExample(prefixes);
            string websitePath = GetWebsitePath();
            server = new Server();
            //server.PublicIP = "www.yourdomain.com"; // 设置域名
            server.OnError = ErrorHandler;

            // Never expire, always authorize (for demo purposes)  
            // 永不过期，始终授权（用于演示目的）
            server.OnRequest = (session, context) => {
                session.Authorized = true;
                session.UpdateLastConnectionTime();
            };

            // register a route handler for this verb and path
            // Server.AddRoute(new Route() { Verb = Router.POST, Path = "/demo/redirect", Action = RedirectMe }); 

            //Server.AddRoute(new Route() {
            //    Verb = Router.POST,
            //    Path = "/demo/redirect",
            //    Handler = new AnonymousRouteHandler(RedirectMe)
            //});

            server.AddRoute(new Route() {
                Verb = Router.POST,
                Path = "/demo/redirect",
                Handler = new AuthenticatedRouteHandler(RedirectMe)
            });

            // Server.AddRoute(new Route()
            // {
            //     Verb = Router.POST,
            //     Path = "/demo/redirect",
            //     Handler = new AuthenticatedExpirableRouteHandler(RedirectMe)
            // });

            server.AddRoute(new Route() {
                Verb = Router.PUT,
                Path = "/demo/ajax",
                Handler = new AnonymousRouteHandler(AjaxResponder)
            });

            server.AddRoute(new Route() {
                Verb = Router.GET,
                Path = "/demo/ajax_get",
                Handler = new AnonymousRouteHandler(AjaxGetResponder)
            });

            server.Start(websitePath);
            Console.ReadLine();
        }

        // public static string RedirectMe(Session session, Dictionary<string, object> parms)
        // {
        //     return "/demo/clicked";
        // }

        // public static string AjaxResponder(Session session, Dictionary<string, object> parms)
        // {
        //     return "what???";
        // }

        public static ResponsePacket RedirectMe(Session session, Dictionary<string, object> parms) {
            return server.Redirect("/demo/clicked");
        }

        public static ResponsePacket AjaxResponder(Session session, Dictionary<string, object> parms) {
            string data = "You said " + parms["number"].ToString();
            ResponsePacket ret = new ResponsePacket() { Data = Encoding.UTF8.GetBytes(data), ContentType = "text" };

            return ret;
        }

        public static ResponsePacket AjaxGetResponder(Session session, Dictionary<string, object> parms) {
            ResponsePacket ret = null;

            if (parms.Count != 0) {
                string data = "You said " + parms["number"].ToString();
                ret = new ResponsePacket() { Data = Encoding.UTF8.GetBytes(data), ContentType = "text" };
            }

            return ret;
        }

        public static string GetWebsitePath() {
            // Path of our exe.
            string websitePath = Assembly.GetExecutingAssembly().Location;
            websitePath = websitePath.LeftOfRightmostOf("\\").LeftOfRightmostOf("\\").LeftOfRightmostOf("\\").LeftOfRightmostOf("\\") + "\\Website";
            //websitePath = websitePath.LeftOfRightmostOf("\\")+ "\\Website";

            return websitePath;
        }

        public static string ErrorHandler(ServerError error) {
            string ret = null;

            switch (error) {
                case ServerError.ExpiredSession:
                    ret = "/ErrorPages/expiredSession.html";
                    break;
                case ServerError.FileNotFound:
                    ret = "/ErrorPages/fileNotFound.html";
                    break;
                case ServerError.NotAuthorized:
                    ret = "/ErrorPages/notAuthorized.html";
                    break;
                case ServerError.PageNotFound:
                    ret = "/ErrorPages/pageNotFound.html";
                    break;
                case ServerError.ServerError:
                    ret = "/ErrorPages/serverError.html";
                    break;
                case ServerError.UnknownType:
                    ret = "/ErrorPages/unknownType.html";
                    break;
                case ServerError.ValidationError:
                    ret = "/ErrorPages/validationError.html";
                    break;
            }

            return ret;
        }
    }
}