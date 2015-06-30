using DiskUsageReporter.Library;
using System.Collections.Generic;
using System.Web.Mvc;

namespace WebApplication.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(string rootDirPath = @"C:\users", bool rescan = false)
        {
            List<DirStat> list = DirStat.GetStatForDirAndItsChildren(rootDirPath, rescan);
            return View(list);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}