using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BoardGameGeekJsonApi.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.BaseUrl = new Uri(Request.Url, "/").ToString();
            return View();
        }
    }
}
