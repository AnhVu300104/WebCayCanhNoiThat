using Models.DAO;
using Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models; // Sử dụng UserLogin
using System.Data.Entity;
using System.Net;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class StockHistoryController : Controller
    {
        private readonly DBNoiThat db = new DBNoiThat();
        private readonly StockHistoryDao dao = new StockHistoryDao(); // Giả định có DAO cho StockHistory

        // =============================================
        // 1. XEM LỊCH SỬ GIAO DỊCH (Index)
        // Action này được gọi từ nút "Xem lịch sử" trong Inventory View
        // =============================================

        [HasCredential(RoleId = "VIEW_STOCK_HISTORY")]
        public ActionResult Index(int warehouseId, int variantId)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            var warehouse = db.Warehouses.Find(warehouseId);
            var variant = db.ProductVariants.Include(v => v.Product).FirstOrDefault(v => v.VariantId == variantId);

            if (warehouse == null || variant == null)
            {
                TempData["Error"] = "Không tìm thấy kho hoặc biến thể sản phẩm.";
                return RedirectToAction("Index", "Warehouse");
            }

            ViewBag.WarehouseName = warehouse.Name;
            ViewBag.VariantName = variant.Product.Name + " (ID: " + variant.VariantId + ")";

            // Load lịch sử giao dịch từ bảng StockHistory
            var model = db.StockHistories
                          .Include(h => h.UserCreated)
                          .Where(h => h.WarehouseId == warehouseId && h.VariantId == variantId)
                          .OrderByDescending(h => h.CreatedAt)
                          .ToList();

            return View(model);
        }

        // =============================================
        // 2. XEM PHIẾU LIÊN QUAN (Detail)
        // Dùng để xem chi tiết Phiếu Nhập, Phiếu Xuất, hoặc Phiếu Kiểm kê
        // =============================================

        [HasCredential(RoleId = "VIEW_STOCK_HISTORY")]
        public ActionResult ViewReference(string type, int referenceId)
        {
            // Chuyển hướng đến Controller/Action tương ứng
            if (type == "StockIn")
            {
                return RedirectToAction("Detail", "StockIn", new { id = referenceId }); // Giả định bạn có StockInController
            }
            else if (type == "StockOut")
            {
                return RedirectToAction("Detail", "StockOut", new { id = referenceId }); // Giả định bạn có StockOutController
            }
            else if (type == "StockCheck")
            {
                return RedirectToAction("Detail", "StockCheck", new { id = referenceId }); // Chuyển hướng đến Detail StockCheck
            }

            return new HttpStatusCodeResult(HttpStatusCode.NotFound, "Không tìm thấy tham chiếu phiếu.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}