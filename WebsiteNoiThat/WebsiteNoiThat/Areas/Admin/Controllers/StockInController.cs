using Models.DAO;
using Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using WebsiteNoiThat.Common;
using System.Data.Entity;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class StockInController : Controller
    {
        private readonly WarehouseDao dao = new WarehouseDao();
        private readonly DBNoiThat db = new DBNoiThat();

        // ====================== INDEX ======================
        [HasCredential(RoleId = "VIEW_STOCK_IN")]
        public ActionResult Index()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            var stockIns = dao.ListStockIn();
            return View(stockIns);
        }

        // ====================== CREATE GET ======================
        [HttpGet]
        [HasCredential(RoleId = "ADD_STOCK_IN")]
        public ActionResult Create()
        {
            LoadDropdowns();
            var model = new StockIn
            {
                Code = "PN" + DateTime.Now.ToString("ddMMyyHHmmss")
            };
            ViewBag.ProductJson = new JavaScriptSerializer().Serialize(
                db.ProductVariants.Where(x => x.IsActive == true).Select(x => new {
                    x.VariantId,
                    x.SKU,
                    ProductName = x.Product.Name,
                    x.ImageVariant,
                    x.Price
                }).ToList()
            );

            return View(model);
        }

        // ====================== CREATE POST ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasCredential(RoleId = "ADD_STOCK_IN")]
        public ActionResult Create(StockIn model, string stockDetailsJson)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            LoadDropdowns();

            ViewBag.OriginalStockDetailsJson = string.IsNullOrEmpty(stockDetailsJson) ? "[]" : stockDetailsJson;

            model.Code = "PN" + DateTime.Now.ToString("ddMMyyHHmmss");
            model.CreatedAt = DateTime.Now;
            model.CreatedBy = session?.UserId;
            model.TotalAmount = 0;
            model.Type = string.IsNullOrEmpty(model.Type) ? "purchase" : model.Type;

            // Kiểm tra xem user có phải ADMIN không
            //bool isAdmin = session != null && session.GroupId == "ADMIN";

            //if (isAdmin)
            //{
            //    model.Status = "approved";
            //    model.ApprovedBy = session.UserId;
            //    model.ApprovedAt = DateTime.Now;
            //}
            //else
            //{
                model.Status = "pending";
            //}

            // Validate: Kho và Nhà cung cấp phải tồn tại VÀ đang kích hoạt
            if (!db.Warehouses.Any(x => x.WarehouseId == model.WarehouseId && x.IsActive == true))
                ModelState.AddModelError("WarehouseId", "Kho không hợp lệ hoặc đã bị tắt");

            if (!db.Providers.Any(x => x.ProviderId == model.ProviderId && x.IsActive == true))
                ModelState.AddModelError("ProviderId", "Nhà cung cấp không hợp lệ hoặc đã bị tắt");

            if (string.IsNullOrEmpty(stockDetailsJson))
                ModelState.AddModelError("", "Chưa có sản phẩm nhập kho");

            if (!ModelState.IsValid)
                return View(model);

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Lưu StockIn
                    db.StockIns.Add(model);
                    db.SaveChanges();

                    // Lưu chi tiết
                    var details = new JavaScriptSerializer().Deserialize<List<StockDetailVM>>(stockDetailsJson);

                    foreach (var d in details)
                    {
                        db.StockInDetails.Add(new StockInDetail
                        {
                            StockInId = model.StockInId,
                            VariantId = d.VariantId,
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice,
                            Note = d.Note
                        });

                        model.TotalAmount += d.Quantity * d.UnitPrice;

                        //// ✅ NẾU ADMIN TẠO PHIẾU -> TỰ ĐỘNG CỘNG TỒN KHO LUÔN
                        //if (isAdmin)
                        //{
                        //    var variant = db.ProductVariants.Find(d.VariantId);
                        //    if (variant != null)
                        //    {
                        //        variant.StockQuantity = (variant.StockQuantity ?? 0) + d.Quantity;
                        //    }
                        //}
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    //if (isAdmin)
                    //{
                    //    TempData["Success"] = "✅ Tạo phiếu nhập kho thành công và đã tự động cộng tồn kho.";
                    //}
                    //else
                    //{
                        TempData["Success"] = "✅ Tạo phiếu nhập kho thành công. Hãy kiểm tra lại phiếu và xác nhận.";
                    //}

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                    return View(model);
                }
            }
        }

        // ====================== Helper ======================
        private void LoadDropdowns()
        {
            // Chỉ load Kho và Nhà cung cấp đang kích hoạt (IsActive = true)
            ViewBag.Warehouses = new SelectList(
                db.Warehouses.Where(x => x.IsActive == true).ToList(),
                "WarehouseId",
                "Name"
            );

            ViewBag.Providers = new SelectList(
                db.Providers.Where(x => x.IsActive == true).ToList(),
                "ProviderId",
                "Name"
            );

            // Chỉ load sản phẩm đang kích hoạt
            var products = db.ProductVariants
                .Where(x => x.IsActive == true)
                .Select(x => new
                {
                    x.VariantId,
                    x.SKU,
                    ProductName = x.Product.Name,
                    x.ImageVariant,
                    x.Price
                }).ToList();

            ViewBag.ProductJson = new JavaScriptSerializer().Serialize(products);
        }

        [HasCredential(RoleId = "VIEW_STOCK_IN")]
        public ActionResult Details(int id)
        {
            var stockIn = db.StockIns
                            .Include("StockInDetails.ProductVariant.Product")
                            .Include("StockInDetails.ProductVariant.VariantAttributeValues.Attribute")
                            .Include("UserCreated")
                            .Include("Warehouse")
                            .Include("Provider")
                            .FirstOrDefault(x => x.StockInId == id);

            if (stockIn == null)
                return HttpNotFound("Phiếu nhập kho không tồn tại");

            return View(stockIn);
        }

        // ====================== APPROVE ======================
        [HttpPost]
        [HasCredential(RoleId = "APPROVE_STOCK_IN")]
        public JsonResult Approve(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
                    var stock = db.StockIns
                                  .Include(x => x.StockInDetails)
                                  .FirstOrDefault(x => x.StockInId == id);

                    if (stock == null)
                        return Json(new { status = false, message = "Phiếu nhập không tồn tại" });

                    if (stock.Status != "pending")
                        return Json(new { status = false, message = "Phiếu chỉ có thể duyệt khi đang chờ" });

                    // ✅ CỘNG TRỰC TIẾP VÀO ProductVariant.StockQuantity
                    foreach (var detail in stock.StockInDetails)
                    {
                        var variant = db.ProductVariants.Find(detail.VariantId);
                        if (variant != null)
                        {
                            // Cộng số lượng nhập vào tồn kho
                            variant.StockQuantity = (variant.StockQuantity ?? 0) + detail.Quantity;
                        }
                    }

                    // Cập nhật trạng thái phiếu
                    stock.Status = "approved";
                    stock.ApprovedBy = session?.UserId;
                    stock.ApprovedAt = DateTime.Now;

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { status = true, message = "✅ Phiếu đã được duyệt và tồn kho đã cập nhật!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { status = false, message = "❌ Lỗi: " + ex.Message });
                }
            }
        }

        // ====================== CANCEL ======================
        [HttpPost]
        [HasCredential(RoleId = "CANCEL_STOCK_IN")]
        public JsonResult Cancel(int id)
        {
            try
            {
                var stock = db.StockIns.FirstOrDefault(x => x.StockInId == id);
                if (stock == null)
                    return Json(new { status = false, message = "Phiếu nhập không tồn tại" });

                if (stock.Status != "pending")
                    return Json(new { status = false, message = "Chỉ có phiếu đang chờ mới hủy được" });

                stock.Status = "cancelled";
                db.SaveChanges();

                return Json(new { status = true, message = "⚠️ Phiếu nhập đã được hủy" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // ====================== DELETE ======================
        [HttpPost]
        [HasCredential(RoleId = "DELETE_STOCK_IN")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var stockIn = db.StockIns.Find(id);
            if (stockIn == null)
            {
                TempData["Error"] = "Phiếu nhập kho không tồn tại.";
                return RedirectToAction("Index");
            }

            if (stockIn.Status != "pending")
            {
                TempData["Error"] = "⚠️ Chỉ phiếu đang chờ mới được xóa.";
                return RedirectToAction("Index");
            }

            try
            {
                db.StockIns.Remove(stockIn);
                db.SaveChanges();
                TempData["Success"] = "✅ Phiếu nhập kho đã bị xóa.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Xảy ra lỗi khi xóa phiếu: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ====================== VM ======================
        public class StockDetailVM
        {
            public int VariantId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string Note { get; set; }
        }
    }
}