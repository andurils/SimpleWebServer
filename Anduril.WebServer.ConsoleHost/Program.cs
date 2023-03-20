using System.Net;
using System.Reflection;

namespace Anduril.WebServer.ConsoleHost {
    internal class Program {
        static void Main(string[] args) {
            //var prefixes = new string[] { "http://localhost:8090/" };
            //SimpleListenerExample(prefixes);
            string websitePath = GetWebsitePath();
            Server.OnError = ErrorHandler;

            // register a route handler for this verb and path
            Server.AddRoute(new Route() { Verb = Router.POST, Path = "/demo/redirect", Action = RedirectMe });

            Server.Start(websitePath);
            Console.ReadLine();
        }

        public static string RedirectMe(Dictionary<string, object> parms) {
            return "/demo/clicked";
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