using Models.DAO;
using Models.EF;
using System.Linq;
using System.Web.Mvc;

namespace WebsiteNoiThat.Controllers
{
    public class CategoryProductController : Controller
    {
        private ProductDao productDao = new ProductDao();
        private CategoryDao categoryDao = new CategoryDao();
        private DBNoiThat db = new DBNoiThat();

        // =============================================
        // DANH SÁCH SẢN PHẨM THEO DANH MỤC
        // =============================================
        public ActionResult Index(int id, int page = 1, int pagesize = 12)
        {
            // Lấy thông tin danh mục
            var category = categoryDao.ViewDetail(id);
            if (category == null || !category.IsActive)
            {
                return HttpNotFound();
            }

            ViewBag.Category = category;

            // Lấy danh sách sản phẩm (chỉ active)
            int totalRecord = 0;
            var products = productDao.ListByCategoryId(id, ref totalRecord, page, pagesize);

            ViewBag.Total = totalRecord;
            ViewBag.Page = page;
            ViewBag.TotalPages = (totalRecord / pagesize) + (totalRecord % pagesize > 0 ? 1 : 0);

            // Danh sách danh mục để hiển thị menu
            ViewBag.Categories = categoryDao.ListActiveCategory();

            return View(products);
        }

        // =============================================
        // MENU DANH MỤC (PARTIAL VIEW)
        // =============================================
        public ActionResult Menu()
        {

            var model = new CategoryDao().ListCategory();
            return PartialView(model);
        }

        // =============================================
        // LẤY DANH MỤC CON (AJAX)
        // =============================================
        [HttpPost]
        public JsonResult GetSubCategories(int parentId)
        {
            var subCategories = categoryDao.ListCategoryByParentId(parentId)
                                          .Select(c => new { c.CategoryId, c.Name })
                                          .ToList();
            return Json(subCategories, JsonRequestBehavior.AllowGet);
        }
    }
}