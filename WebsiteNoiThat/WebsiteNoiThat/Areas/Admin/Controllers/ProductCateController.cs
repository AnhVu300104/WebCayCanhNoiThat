using System;
using System.Linq;
using System.Web.Mvc;
using Models.EF;
using WebsiteNoiThat.Common;
using System.Data.Entity;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class ProductCateController : HomeController
    {
        private DBNoiThat db = new DBNoiThat();

        // =========================
        // DANH SÁCH DANH MỤC
        // =========================
        [HasCredential(RoleId = "VIEW_CATE")]
        public ActionResult Show()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;
            ViewBag.CurrentPath = "/Admin/ProductCate/Show";

            // ❗ Hiển thị cả active + inactive (để quản lý)
            var categories = db.Categories
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Name)
                .ToList();

            return View(categories);
        }

        // =========================
        // THÊM DANH MỤC
        // =========================
        [HttpGet]
        [HasCredential(RoleId = "ADD_CATE")]
        public ActionResult Add()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            return View(new Category
            {
                IsActive = true
            });
        }

        [HttpPost]
        [HasCredential(RoleId = "ADD_CATE")]
        [ValidateAntiForgeryToken]
        public ActionResult Add(Category model)
        {
            if (!ModelState.IsValid)
                return View(model);

            db.Categories.Add(model);
            db.SaveChanges();

            TempData["Success"] = "Thêm danh mục thành công.";
            return RedirectToAction("Show");
        }

        // =========================
        // SỬA DANH MỤC
        // =========================
        [HttpGet]
        [HasCredential(RoleId = "EDIT_CATE")]
        public ActionResult Edit(int categoryId)
        {
            var category = db.Categories.Find(categoryId);
            if (category == null)
                return HttpNotFound();

            return View(category);
        }

        [HttpPost]
        [HasCredential(RoleId = "EDIT_CATE")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Category model)
        {
            // ❗ Nếu đang muốn TẮT danh mục
            if (!model.IsActive)
            {
                bool hasActiveProducts = db.Products
                    .Any(p => p.CateId == model.CategoryId && p.IsActive);

                if (hasActiveProducts)
                {
                    // 🔴 GẮN LỖI TRỰC TIẾP VÀO FIELD IsActive
                    ModelState.AddModelError(
                        "IsActive",
                        "Không thể vô hiệu hóa danh mục vì vẫn còn sản phẩm đang bán."
                    );
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            db.Entry(model).State = EntityState.Modified;
            db.SaveChanges();

            TempData["Success"] = "Cập nhật danh mục thành công.";
            return RedirectToAction("Show");
        }


        // =========================
        // VÔ HIỆU HÓA (SOFT DELETE)
        // =========================
        [HttpGet]
        [HasCredential(RoleId = "DELETE_CATE")]
        public ActionResult Disable(int id)
        {
            var category = db.Categories.Find(id);
            if (category == null)
            {
                TempData["Error"] = "Danh mục không tồn tại.";
                return RedirectToAction("Show");
            }

            bool hasActiveProducts = db.Products
                .Any(p => p.CateId == id && p.IsActive);

            if (hasActiveProducts)
            {
                TempData["Error"] = "Không thể vô hiệu hóa. Danh mục còn sản phẩm đang bán.";
                return RedirectToAction("Show");
            }

            category.IsActive = false;
            db.SaveChanges();

            TempData["Success"] = "Danh mục đã được vô hiệu hóa.";
            return RedirectToAction("Show");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
