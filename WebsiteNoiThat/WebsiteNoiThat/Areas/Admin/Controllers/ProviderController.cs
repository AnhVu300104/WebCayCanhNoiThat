using Models.EF;
using System.Linq;
using System.Web.Mvc;
using WebsiteNoiThat.Common;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class ProviderController : HomeController
    {
        DBNoiThat db = new DBNoiThat();

        [HasCredential(RoleId = "VIEW_PROVIDER")]
        public ActionResult Show()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;
            return View(db.Providers.ToList());
        }

        [HttpGet]
        [HasCredential(RoleId = "ADD_PROVIDER")]
        public ActionResult Add()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;
            return View();
        }

        [HttpPost]
        [HasCredential(RoleId = "ADD_PROVIDER")]
        public ActionResult Add(Provider n)
        {
            if (db.Providers.Any(a => a.ProviderId == n.ProviderId))
            {
                ModelState.AddModelError("ProError", "Mã nhà cung cấp đã tồn tại");
                return View();
            }

            n.IsActive = true;
            db.Providers.Add(n);
            db.SaveChanges();

            TempData["Message"] = "Thêm nhà cung cấp thành công.";
            return RedirectToAction("Show");
        }

        [HttpGet]
        [HasCredential(RoleId = "EDIT_PROVIDER")]
        public ActionResult Edit(int ProviderId)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            var provider = db.Providers.SingleOrDefault(p => p.ProviderId == ProviderId);
            if (provider == null)
            {
                TempData["Message"] = "Nhà cung cấp không tồn tại.";
                return RedirectToAction("Show");
            }
            return View(provider);
        }

        [HttpPost]
        [HasCredential(RoleId = "EDIT_PROVIDER")]
        public ActionResult Edit(Provider n)
        {
            if (ModelState.IsValid)
            {
                var existing = db.Providers.Find(n.ProviderId);
                if (existing != null)
                {
                    existing.Name = n.Name;
                    existing.Address = n.Address;
                    existing.Phone = n.Phone;
                    existing.IsActive = n.IsActive;
                    db.SaveChanges();

                    TempData["Message"] = "Cập nhật nhà cung cấp thành công.";
                    return RedirectToAction("Show");
                }
            }
            ModelState.AddModelError("", "Dữ liệu không hợp lệ");
            return View(n);
        }

        [HttpGet]
        [HasCredential(RoleId = "DELETE_PROVIDER")]
        public ActionResult Delete(int? ProviderId)
        {
            if (ProviderId == null)
            {
                TempData["Message"] = "Nhà cung cấp không tồn tại.";
                return RedirectToAction("Show");
            }

            var provider = db.Providers.Find(ProviderId.Value);
            if (provider == null)
            {
                TempData["Message"] = "Nhà cung cấp không tồn tại.";
                return RedirectToAction("Show");
            }

            bool hasStockIn = db.StockIns.Any(s => s.ProviderId == ProviderId.Value);

            if (!hasStockIn)
            {
                db.Providers.Remove(provider);
                db.SaveChanges();
                TempData["Message"] = "Xóa nhà cung cấp thành công.";
            }
            else
            {
                provider.IsActive = false;
                db.SaveChanges();
                TempData["Message"] = "Nhà cung cấp đang có liên quan đến phiếu nhập kho, không thể xóa. Đã ngừng hoạt động.";
            }

            return RedirectToAction("Show");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
