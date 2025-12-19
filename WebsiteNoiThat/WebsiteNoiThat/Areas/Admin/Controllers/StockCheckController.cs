using Models.DAO;
using Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models;
using System.Data.Entity;
using System.Net;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class StockCheckController : Controller
    {
        private readonly DBNoiThat db = new DBNoiThat();
        private readonly StockCheckDao dao = new StockCheckDao();
        private readonly ProductDao productDao = new ProductDao();

        // =============================================
        // 1. DANH SÁCH PHIẾU KIỂM KÊ (Index)
        // =============================================
        [HasCredential(RoleId = "VIEW_STOCK_CHECK")]
        public ActionResult Index()
        {
            var model = db.StockChecks
                          .Include(sc => sc.Warehouse)
                          .Include(sc => sc.UserCreated)
                          .OrderByDescending(sc => sc.CheckDate)
                          .ToList();
            return View(model);
        }

        // =============================================
        // 2. XEM CHI TIẾT PHIẾU KIỂM KÊ (Detail)
        // =============================================
        [HasCredential(RoleId = "VIEW_STOCK_CHECK")]
        public ActionResult Detail(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var stockCheck = db.StockChecks
                                .Include(sc => sc.Warehouse)
                                .Include(sc => sc.StockCheckDetails)
                                .Include(sc => sc.UserCreated)
                                .FirstOrDefault(sc => sc.CheckId == id);

            if (stockCheck == null) return HttpNotFound();

            var variantIds = stockCheck.StockCheckDetails.Select(d => d.VariantId).Distinct().ToList();
            var variantDetails = db.ProductVariants
                                   .Include(v => v.Product)
                                   .Where(v => variantIds.Contains(v.VariantId))
                                   .ToList();

            ViewBag.VariantDetails = variantDetails.ToDictionary(v => v.VariantId);
            return View(stockCheck);
        }

        // =============================================
        // 3. TẠO PHIẾU KIỂM KÊ - GET
        // =============================================
        [HasCredential(RoleId = "CREATE_STOCK_CHECK")]
        public ActionResult Create()
        {
            // Load danh sách kho
            ViewBag.Warehouses = new SelectList(
                db.Warehouses.ToList(),
                "WarehouseId",
                "Name"
            );

            var model = new StockCheck
            {
                CheckDate = DateTime.Now,
                Code = GenerateStockCheckCode()
            };

            return View(model);
        }

        // =============================================
        // 4. TẠO PHIẾU KIỂM KÊ - POST
        // =============================================
        [HttpPost]
        [HasCredential(RoleId = "CREATE_STOCK_CHECK")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(StockCheck model, List<StockCheckDetailModel> details)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var userSession = (UserLogin)Session[Commoncontent.user_sesion_admin];

                // ===== THÔNG TIN PHIẾU =====
                model.CreatedBy = userSession.UserId;
                model.CreatedAt = DateTime.Now;

                // ❗ LUÔN LÀ PENDING KHI TẠO
                model.Status = "Pending";
                model.ApprovedBy = null;
                model.CompletedAt = null;

                db.StockChecks.Add(model);
                db.SaveChanges();

                // ===== CHI TIẾT KIỂM KÊ =====
                if (details != null && details.Any())
                {
                    foreach (var item in details)
                    {
                        var variant = db.ProductVariants.Find(item.VariantId);
                        if (variant == null) continue;

                        int systemQty = variant.StockQuantity ?? 0;
                        int actualQty = item.ActualQuantity;

                        db.StockCheckDetails.Add(new StockCheckDetail
                        {
                            CheckId = model.CheckId,
                            VariantId = item.VariantId,
                            SystemQuantity = systemQty,
                            ActualQuantity = actualQty,
                            Difference = actualQty - systemQty,
                            Note = item.Note
                        });
                    }

                    db.SaveChanges();
                }

                TempData["Success"] = "✅ Tạo phiếu kiểm kê thành công. Hãy kiểm tra lại phiếu và xác nhận.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
        }


        // =============================================
        // 5. DUYỆT PHIẾU KIỂM KÊ
        // =============================================
        [HttpPost]
        [HasCredential(RoleId = "APPROVE_STOCK_CHECK")]
        public JsonResult Approve(int id)
        {
            try
            {
                var userSession = (UserLogin)Session[Commoncontent.user_sesion_admin];
                var stockCheck = db.StockChecks
                                   .Include(sc => sc.StockCheckDetails)
                                   .FirstOrDefault(sc => sc.CheckId == id);

                if (stockCheck == null)
                {
                    return Json(new { status = false, message = "Không tìm thấy phiếu kiểm kê!" });
                }

                if (stockCheck.Status != "Pending")
                {
                    return Json(new { status = false, message = "Phiếu kiểm kê đã được duyệt hoặc đã hủy!" });
                }

                // Cập nhật trạng thái phiếu kiểm kê
                stockCheck.Status = "Approved";
                stockCheck.ApprovedBy = userSession.UserId;
                stockCheck.CompletedAt = DateTime.Now;

                // Cập nhật tồn kho
                UpdateStockQuantities(id, userSession.UserId);

                db.SaveChanges();

                return Json(new { status = true, message = "Duyệt phiếu kiểm kê thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Lỗi: " + ex.Message });
            }
        }

        // =============================================
        // 6. LẤY DANH SÁCH SẢN PHẨM THEO KHO (AJAX)
        // =============================================
        [HttpGet]
        public JsonResult GetVariantsByWarehouse(int warehouseId)
        {
            try
            {
                var variants = db.ProductVariants
                                 .Include(v => v.Product)
                                 .Where(v => v.WarehouseId == warehouseId && v.IsActive == true)
                                 .Select(v => new
                                 {
                                     v.VariantId,
                                     v.SKU,
                                     ProductName = v.Product.Name,
                                     StockQuantity = v.StockQuantity ?? 0
                                 })
                                 .ToList();

                return Json(variants, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // =============================================
        // PRIVATE METHODS
        // =============================================

        /// <summary>
        /// Tạo mã phiếu kiểm kê tự động
        /// </summary>
        private string GenerateStockCheckCode()
        {
            var lastCheck = db.StockChecks.OrderByDescending(sc => sc.CheckId).FirstOrDefault();
            int nextId = (lastCheck?.CheckId ?? 0) + 1;
            return $"KC{DateTime.Now:yyyyMMdd}{nextId:D4}";
        }
        // =============================================
        // 7. HỦY PHIẾU KIỂM KÊ
        // =============================================
        [HttpPost]
        [HasCredential(RoleId = "CANCEL_STOCK_CHECK")]
        public JsonResult Cancel(int id)
        {
            try
            {
                var userSession = (UserLogin)Session[Commoncontent.user_sesion_admin];
                var stockCheck = db.StockChecks
                                   .Include(sc => sc.StockCheckDetails)
                                   .FirstOrDefault(sc => sc.CheckId == id);

                if (stockCheck == null)
                    return Json(new { status = false, message = "Không tìm thấy phiếu kiểm kê!" });

                if (stockCheck.Status != "Pending")
                    return Json(new { status = false, message = "Chỉ phiếu đang chờ mới được hủy!" });

                // Cập nhật trạng thái hủy
                stockCheck.Status = "Cancelled"; // Hoặc "Đã hủy"
                stockCheck.CompletedAt = DateTime.Now;

                db.SaveChanges();

                return Json(new { status = true, message = "Hủy phiếu kiểm kê thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Lỗi: " + ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật tồn kho vào bảng ProductVariant
        /// </summary>
        private void UpdateStockQuantities(int checkId, int userId)
        {
            var check = db.StockChecks
                          .Include(sc => sc.StockCheckDetails)
                          .FirstOrDefault(sc => sc.CheckId == checkId);

            if (check == null) return;

            foreach (var detail in check.StockCheckDetails)
            {
                var variant = db.ProductVariants.Find(detail.VariantId);
                if (variant != null)
                {
                    int quantityBefore = variant.StockQuantity ?? 0;
                    int quantityChange = detail.ActualQuantity - quantityBefore;

                    // Cập nhật trực tiếp vào ProductVariant.StockQuantity
                    variant.StockQuantity = detail.ActualQuantity;

                    // Ghi lịch sử nếu có chênh lệch (nếu có bảng StockHistory)
                    if (quantityChange != 0 && db.StockHistories != null)
                    {
                        try
                        {
                            db.StockHistories.Add(new StockHistory
                            {
                                WarehouseId = check.WarehouseId,
                                VariantId = detail.VariantId,
                                Type = quantityChange > 0 ? "IN" : "OUT",
                                ReferenceType = "StockCheck",
                                ReferenceId = check.CheckId,
                                QuantityBefore = quantityBefore,
                                QuantityChange = Math.Abs(quantityChange),
                                QuantityAfter = detail.ActualQuantity,
                                Note = $"Kiểm kê kho - Chênh lệch: {(quantityChange > 0 ? "+" : "")}{quantityChange}",
                                CreatedBy = userId,
                                CreatedAt = DateTime.Now
                            });
                        }
                        catch
                        {
                            // Bỏ qua nếu không có bảng StockHistory
                        }
                    }
                }
            }

            db.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    // Model để binding dữ liệu từ form
    public class StockCheckDetailModel
    {
        public int VariantId { get; set; }
        public int ActualQuantity { get; set; }
        public string Note { get; set; }
    }
}