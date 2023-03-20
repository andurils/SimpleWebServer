using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anduril.WebServer {


    public enum ServerError {
        OK, // 200
        ExpiredSession, // 401
        NotAuthorized, // 403
        FileNotFound, // 404
        PageNotFound, // 404
        ServerError, // 500
        UnknownType, // 500
        ValidationError, // 422
        AjaxError, // 500
    }
}
