using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Models.DAO;
using Models.EF;

namespace WebsiteNoiThat.Controllers
{
    public class NewsController : Controller
    {
        // GET: News
        DBNoiThat db = new DBNoiThat();
        public ActionResult Index()
        {
            var model = db.News.OrderByDescending(n => n.DateUpdate).ToList();
            return View(model);
        }

        public ActionResult NewsHot()
        {
            var model = new NewsDao().NewsHot();
            return PartialView(model);
        }
        public ActionResult Show(int? NewsId)
        {
            if (NewsId == null)
                return RedirectToAction("Index"); // Hoặc trả về 404

            var model = db.News.SingleOrDefault(n => n.NewsId == NewsId);

            if (model == null)
                return HttpNotFound();

            return View(model);
        }


    }
}