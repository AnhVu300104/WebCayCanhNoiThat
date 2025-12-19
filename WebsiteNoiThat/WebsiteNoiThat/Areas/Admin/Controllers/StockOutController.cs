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
    public class StockOutController : Controller
    {
        private readonly WarehouseDao dao = new WarehouseDao();
        private readonly DBNoiThat db = new DBNoiThat();

        // ====================== INDEX ======================
        [HasCredential(RoleId = "VIEW_STOCK_OUT")]
        public ActionResult Index()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            var stockOuts = dao.ListStockOut();
            return View(stockOuts);
        }

        // ====================== CREATE GET ======================
        [HttpGet]
        [HasCredential(RoleId = "ADD_STOCK_OUT")]
        public ActionResult Create()
        {
            LoadDropdowns();
            var model = new StockOut
            {
                Code = "PX" + DateTime.Now.ToString("ddMMyyHHmmss")
            };

            return View(model);
        }

        // ====================== CREATE POST ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasCredential(RoleId = "ADD_STOCK_OUT")]
        public ActionResult Create(StockOut model, string stockDetailsJson)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            LoadDropdowns();

            ViewBag.OriginalStockDetailsJson = string.IsNullOrEmpty(stockDetailsJson) ? "[]" : stockDetailsJson;

            model.Code = "PX" + DateTime.Now.ToString("ddMMyyHHmmss");
            model.CreatedAt = DateTime.Now;
            model.CreatedBy = session?.UserId;
            model.Type = string.IsNullOrEmpty(model.Type) ? "Sale" : model.Type;

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

            // Validate: Kho phải tồn tại VÀ đang kích hoạt
            if (!db.Warehouses.Any(x => x.WarehouseId == model.WarehouseId && x.IsActive == true))
                ModelState.AddModelError("WarehouseId", "Kho không hợp lệ hoặc đã bị tắt");

            if (string.IsNullOrEmpty(stockDetailsJson))
                ModelState.AddModelError("", "Chưa có sản phẩm xuất kho");

            if (!ModelState.IsValid)
                return View(model);

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var details = new JavaScriptSerializer().Deserialize<List<StockDetailVM>>(stockDetailsJson);

                    // ✅ KIỂM TRA TỒN KHO TRƯỚC KHI LƯU
                    foreach (var d in details)
                    {
                        var variant = db.ProductVariants.Find(d.VariantId);

                        if (variant == null)
                        {
                            ModelState.AddModelError("", $"Sản phẩm VariantId {d.VariantId} không tồn tại");
                            return View(model);
                        }

                        if (variant.IsActive != true)
                        {
                            ModelState.AddModelError("", $"Sản phẩm {variant.SKU} đã bị vô hiệu hóa");
                            return View(model);
                        }

                        int currentStock = variant.StockQuantity ?? 0;

                        if (currentStock < d.Quantity)
                        {
                            ModelState.AddModelError("",
                                $"⚠️ Sản phẩm {variant.SKU} không đủ tồn kho! " +
                                $"Hiện có: {currentStock}, Cần xuất: {d.Quantity}");
                            return View(model);
                        }
                    }

                    // Lưu StockOut
                    db.StockOuts.Add(model);
                    db.SaveChanges();

                    // Lưu chi tiết và trừ tồn kho nếu ADMIN tạo
                    foreach (var d in details)
                    {
                        db.StockOutDetails.Add(new StockOutDetail
                        {
                            StockOutId = model.StockOutId,
                            VariantId = d.VariantId,
                            Quantity = d.Quantity,
                            Note = d.Note
                        });

                        //// ✅ NẾU ADMIN TẠO PHIẾU -> TỰ ĐỘNG TRỪ TỒN KHO LUÔN
                        //if (isAdmin)
                        //{
                        //    var variant = db.ProductVariants.Find(d.VariantId);
                        //    if (variant != null)
                        //    {
                        //        variant.StockQuantity = (variant.StockQuantity ?? 0) - d.Quantity;
                        //    }
                        //}
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    //if (isAdmin)
                    //{
                    //    TempData["Success"] = "✅ Tạo phiếu xuất kho thành công và đã tự động trừ tồn kho.";
                    //}
                    //else
                    //{
                        TempData["Success"] = "✅ Tạo phiếu xuất kho thành công. Hãy kiểm tra lại phiếu và xác nhận.";
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
            // Chỉ load Kho đang kích hoạt (IsActive = true)
            ViewBag.Warehouses = new SelectList(
                db.Warehouses.Where(x => x.IsActive == true).ToList(),
                "WarehouseId",
                "Name"
            );

            // Chỉ load Đơn hàng đang xử lý
            var orders = db.Orders
                .Where(x => x.StatusId == 2 || x.StatusId == 3)
                .OrderByDescending(x => x.UpdateDate)
                .ToList();
            ViewBag.Orders = new SelectList(orders, "OrderId", "OrderId");

            // Chỉ load sản phẩm đang kích hoạt VÀ CÓ TỒN KHO
            var products = db.ProductVariants
                .Where(x => x.IsActive == true)
                .Select(x => new
                {
                    x.VariantId,
                    x.SKU,
                    ProductName = x.Product.Name,
                    x.ImageVariant,
                    StockQty = x.StockQuantity ?? 0
                }).ToList();

            ViewBag.ProductJson = new JavaScriptSerializer().Serialize(products);
        }

        // ====================== DETAILS ======================
        [HasCredential(RoleId = "VIEW_STOCK_OUT")]
        public ActionResult Details(int id)
        {
            var stockOut = db.StockOuts
                            .Include("StockOutDetails.ProductVariant.Product")
                            .Include("StockOutDetails.ProductVariant.VariantAttributeValues.Attribute")
                            .Include("UserCreated")
                            .Include("Warehouse")
                            .FirstOrDefault(x => x.StockOutId == id);

            if (stockOut == null)
                return HttpNotFound("Phiếu xuất kho không tồn tại");

            return View(stockOut);
        }

        // ====================== APPROVE ======================
        [HttpPost]
        [HasCredential(RoleId = "APPROVE_STOCK_OUT")]
        public JsonResult Approve(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
                    var stock = db.StockOuts
                                  .Include(x => x.StockOutDetails)
                                  .FirstOrDefault(x => x.StockOutId == id);

                    if (stock == null)
                        return Json(new { status = false, message = "Phiếu xuất không tồn tại" });

                    if (stock.Status != "pending")
                        return Json(new { status = false, message = "Phiếu chỉ có thể duyệt khi đang chờ" });

                    // ✅ KIỂM TRA TỒN KHO TRƯỚC KHI DUYỆT
                    foreach (var detail in stock.StockOutDetails)
                    {
                        var variant = db.ProductVariants.Find(detail.VariantId);

                        if (variant == null)
                            return Json(new { status = false, message = $"Sản phẩm VariantId {detail.VariantId} không tồn tại" });

                        if (variant.IsActive != true)
                            return Json(new { status = false, message = $"Sản phẩm {variant.SKU} đã bị vô hiệu hóa" });

                        int currentStock = variant.StockQuantity ?? 0;

                        if (currentStock < detail.Quantity)
                        {
                            return Json(new
                            {
                                status = false,
                                message = $"⚠️ Sản phẩm {variant.SKU} không đủ tồn kho! Hiện có: {currentStock}, Cần xuất: {detail.Quantity}"
                            });
                        }
                    }

                    // ✅ TRỪ TRỰC TIẾP VÀO ProductVariant.StockQuantity
                    foreach (var detail in stock.StockOutDetails)
                    {
                        var variant = db.ProductVariants.Find(detail.VariantId);
                        if (variant != null)
                        {
                            variant.StockQuantity = (variant.StockQuantity ?? 0) - detail.Quantity;
                        }
                    }

                    // Cập nhật trạng thái phiếu
                    stock.Status = "approved";
                    stock.ApprovedBy = session?.UserId;
                    stock.ApprovedAt = DateTime.Now;

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { status = true, message = "✅ Phiếu đã được duyệt và tồn kho đã được trừ!" });
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
        [HasCredential(RoleId = "CANCEL_STOCK_OUT")]
        public JsonResult Cancel(int id)
        {
            try
            {
                var stock = db.StockOuts.FirstOrDefault(x => x.StockOutId == id);
                if (stock == null)
                    return Json(new { status = false, message = "Phiếu xuất không tồn tại" });

                if (stock.Status != "pending")
                    return Json(new { status = false, message = "Chỉ có phiếu đang chờ mới hủy được" });

                stock.Status = "cancelled";
                db.SaveChanges();

                return Json(new { status = true, message = "⚠️ Phiếu xuất đã được hủy" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        // ====================== DELETE ======================
        [HttpPost]
        [HasCredential(RoleId = "DELETE_STOCK_OUT")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var stockOut = db.StockOuts.Find(id);
            if (stockOut == null)
            {
                TempData["Error"] = "Phiếu xuất kho không tồn tại.";
                return RedirectToAction("Index");
            }

            if (stockOut.Status != "pending")
            {
                TempData["Error"] = "⚠️ Chỉ phiếu đang chờ mới được xóa.";
                return RedirectToAction("Index");
            }

            try
            {
                db.StockOuts.Remove(stockOut);
                db.SaveChanges();
                TempData["Success"] = "✅ Phiếu xuất kho đã bị xóa.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Xảy ra lỗi khi xóa phiếu: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ====================== API LẤY CHI TIẾT ĐƠN HÀNG ======================
        [HttpGet]
        public JsonResult GetOrderItems(int orderId)
        {
            try
            {
                var items = (from d in db.OrderDetails
                             join v in db.ProductVariants on d.VariantId equals v.VariantId into pv
                             from v in pv.DefaultIfEmpty()
                             join p in db.Products on d.ProductId equals p.ProductId
                             where d.OrderId == orderId && (v == null || v.IsActive == true)
                             select new
                             {
                                 VariantId = d.VariantId ?? db.ProductVariants
                                     .Where(x => x.ProductId == d.ProductId && x.IsActive == true)
                                     .Select(x => x.VariantId)
                                     .FirstOrDefault(),
                                 SKU = v != null ? v.SKU : ("SP" + p.ProductId),
                                 ProductName = p.Name + (string.IsNullOrEmpty(d.VariantInfo) ? "" : " (" + d.VariantInfo + ")"),
                                 Quantity = d.Quantity,
                                 StockQty = v != null ? (v.StockQuantity ?? 0) : 0,
                                 ImageVariant = v != null ? v.ImageVariant : ""
                             }).ToList();

                return Json(new { status = true, data = items }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ====================== VM ======================
        public class StockDetailVM
        {
            public int VariantId { get; set; }
            public int Quantity { get; set; }
            public string Note { get; set; }
        }
    }
}