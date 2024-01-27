using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.Configuration;
using Newtonsoft.Json;
using System.Net;

namespace APEnvAuditAPI.Controllers
{
    // WHERE I'M AT:
    // line 42, populating first drop-down in Services View using service list model
    // reference: http://stackoverflow.com/questions/37778489/how-to-make-check-box-list-in-asp-net-mvc (NOT a drop-down)
    
    public class HomeController : Controller
    {

        public ActionResult Index() // APIs Default entry point
        {
            return View();
        }
    }

}
