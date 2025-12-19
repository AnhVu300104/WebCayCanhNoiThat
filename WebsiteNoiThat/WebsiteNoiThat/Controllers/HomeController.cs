using Models.DAO;
using Models.EF;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models;

namespace WebsiteNoiThat.Controllers
{
    public class HomeController : Controller
    {
        private ProductDao productDao = new ProductDao();
        private CategoryDao categoryDao = new CategoryDao();
        private DBNoiThat db = new DBNoiThat();

        // =============================================
        // TRANG CHỦ
        // =============================================
        public ActionResult Index()
        {
            // Sản phẩm mới (chỉ active)
            ViewBag.NewProducts = productDao.NewProduct();

            // Sản phẩm giảm giá (chỉ active)
            ViewBag.SaleProducts = productDao.SaleProduct();

            // Sản phẩm bán chạy (chỉ active)
            ViewBag.HotProducts = productDao.ProductHot();

            // Danh mục đang hoạt động
            ViewBag.Categories = categoryDao.ListActiveCategory();

            return View();
        }

        // =============================================
        // GIỚI THIỆU
        // =============================================
        public ActionResult About()
        {
            ViewBag.Message = "Trang giới thiệu về website nội thất.";
            return View();
        }

        // =============================================
        // LIÊN HỆ
        // =============================================
        public ActionResult Contact()
        {
            ViewBag.Message = "Trang liên hệ.";
            return View();
        }

        // =============================================
        // TÌM KIẾM SẢN PHẨM
        // =============================================
        [HttpGet]
        public ActionResult Search(string keyword, int page = 1, int pagesize = 12)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return RedirectToAction("Index");
            }

            int totalRecord = 0;
            var model = productDao.Search(keyword, ref totalRecord, page, pagesize);

            ViewBag.Keyword = keyword;
            ViewBag.Total = totalRecord;
            ViewBag.Page = page;
            ViewBag.TotalPages = (totalRecord / pagesize) + (totalRecord % pagesize > 0 ? 1 : 0);

            return View(model);
        }

        // =============================================
        // AUTOCOMPLETE TÌM KIẾM (AJAX)
        // =============================================
        [HttpPost]
        public JsonResult GetSearchSuggestions(string term)
        {
            var suggestions = productDao.ListName(term).Take(10);
            return Json(suggestions, JsonRequestBehavior.AllowGet);
        }
      

[ChildActionOnly]
    public PartialViewResult HeaderCart()
    {
        var session = (UserLogin)Session[Commoncontent.user_sesion];
        var cartProducts = new List<GH>();

        if (session != null)
        {
            int userId = session.UserId;
            cartProducts = db.GioHang
                .Where(e => e.UserId == userId)
                .Select(e => new GH
                {
                    UserId = userId,
                    GioHangId = e.GioHangId,
                    ProductId = e.ProductId,
                    Quantity = e.Quantity,
                    CreateDate = e.CreateDate,
                    UpdateDate = e.UpdateDate,
                    ProductName = e.ProductName,
                }).ToList();
        }

        ViewBag.TotalQuantity = cartProducts.Count;
        return PartialView("~/Views/Shared/HeaderCart.cshtml", cartProducts);
    }

}
}