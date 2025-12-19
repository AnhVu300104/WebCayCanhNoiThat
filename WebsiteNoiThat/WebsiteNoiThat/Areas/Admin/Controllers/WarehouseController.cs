using Models.DAO;
using Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models;
using System.Data.Entity;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class WarehouseController : Controller
    {
        WarehouseDao dao = new WarehouseDao();
        ProductDao productDao = new ProductDao();
        DBNoiThat db = new DBNoiThat();

        // =============================================
        // 1. QUẢN LÝ DANH SÁCH KHO (Index/Show)
        // =============================================

        [HasCredential(RoleId = "VIEW_WAREHOUSE")]
        public ActionResult Index()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            var model = dao.ListAll();
            return View(model);
        }

        [HttpGet]
        [HasCredential(RoleId = "ADD_WAREHOUSE")]
        public ActionResult Create()
        {
            return View(new Warehouse
            {
                IsActive = true
            });
        }

        [HttpPost]
        [HasCredential(RoleId = "ADD_WAREHOUSE")]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Warehouse model)
        {
            if (ModelState.IsValid)
            {
                var result = dao.Insert(model);
                if (result > 0)
                {
                    TempData["Success"] = "Thêm kho thành công";
                    return RedirectToAction("Index");
                }
                ModelState.AddModelError("", "Mã kho đã tồn tại hoặc lỗi hệ thống");
            }
            return View(model);
        }


        [HttpGet]
        [HasCredential(RoleId = "EDIT_WAREHOUSE")]
        public ActionResult Edit(int id)
        {
            var model = dao.ViewDetail(id);
            if (model == null) return HttpNotFound();
            return View(model);
        }

        [HttpPost]
        [HasCredential(RoleId = "EDIT_WAREHOUSE")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Warehouse model)
        {
            if (ModelState.IsValid)
            {
                var result = dao.Update(model);
                if (result)
                {
                    TempData["Success"] = "Cập nhật thành công";
                    return RedirectToAction("Index");
                }
                ModelState.AddModelError("", "Cập nhật thất bại.");
            }
            return View(model);
        }

        [HttpGet]
        [HasCredential(RoleId = "DELETE_WAREHOUSE")]
        public ActionResult Delete(int id)
        {
            var warehouse = db.Warehouses.Find(id);
            if (warehouse == null)
            {
                TempData["Error"] = "Kho không tồn tại.";
                return RedirectToAction("Index");
            }

            // 1. Kiểm tra kho còn hàng
            bool hasStock = db.WarehouseStocks.Any(ws => ws.WarehouseId == id && ws.Quantity > 0);
            if (hasStock)
            {
                TempData["Error"] = "Kho còn hàng tồn. Vui lòng chuyển hàng sang kho khác trước khi xóa.";
                return RedirectToAction("Index");
            }

            // 2. Kiểm tra kho có liên kết với phiếu nhập/xuất/kiểm kê
            bool hasRelatedDocuments = db.StockIns.Any(s => s.WarehouseId == id) ||
                                       db.StockOuts.Any(s => s.WarehouseId == id) ||
                                       db.StockChecks.Any(i => i.WarehouseId == id);
            if (hasRelatedDocuments)
            {
                warehouse.IsActive = false; // Ngừng hoạt động
                db.SaveChanges();
                TempData["Success"] = "Kho đang có phiếu liên quan. Đã cập nhật trạng thái 'Ngừng hoạt động'.";
                return RedirectToAction("Index");
            }

            // 3. Kho trống và không có liên kết -> xóa trực tiếp
            db.Warehouses.Remove(warehouse);
            db.SaveChanges();
            TempData["Success"] = "Xóa kho thành công.";
            return RedirectToAction("Index");
        }


        // =============================================
        // 2. XEM TỒN KHO (INVENTORY) - SỬA LẠI DỰA TRÊN ProductVariant
        // =============================================

        [HasCredential(RoleId = "VIEW_STOCK")]
        public ActionResult Inventory(int warehouseId)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            var warehouse = dao.ViewDetail(warehouseId);
            ViewBag.WarehouseName = warehouse?.Name;

            // LẤY DỮ LIỆU TỒN KHO TRỰC TIẾP TỪ ProductVariant
            var model = db.ProductVariants
                .Include(v => v.Product)
                .Where(v => v.WarehouseId == warehouseId && v.IsActive == true)
                .AsEnumerable()
                .Select(v => new WebsiteNoiThat.Models.InventoryViewModel
                {
                    VariantId = v.VariantId,
                    WarehouseId = v.WarehouseId ?? 0,
                    CurrentQuantity = v.StockQuantity ?? 0,

                    // Giá vốn trung bình = Giá gốc
                    AverageCost = v.Price,

                    // Giá bán (Sale Price)
                    SalePrice = v.SalePrice ?? v.Price, // Nếu không có giá sale thì lấy giá gốc

                    // Thông tin sản phẩm
                    ProductName = v.Product?.Name,
                    ProductCode = v.SKU, // Dùng SKU làm mã sản phẩm

                    // Tên biến thể (nếu có thuộc tính khác, cần lấy từ bảng ProductVariantAttribute)
                    VariantName = "SKU: " + v.SKU
                })
                .OrderByDescending(x => x.CurrentQuantity)
                .ToList();

            return View(model);
        }

        // =============================================
        // 3. NHẬP KHO (STOCK IN)
        // =============================================

        [HasCredential(RoleId = "VIEW_STOCK_IN")]
        public ActionResult StockInIndex()
        {
            var model = dao.ListStockIn();
            return View(model);
        }

        [HttpGet]
        [HasCredential(RoleId = "ADD_STOCK_IN")]
        public ActionResult CreateStockIn()
        {
            ViewBag.Warehouses = new SelectList(dao.ListAll(), "WarehouseId", "Name");
            ViewBag.Providers = new SelectList(db.Providers.ToList(), "ProviderId", "Name");
            ViewBag.Products = db.ProductVariants.Where(x => x.IsActive == true).ToList();

            return View();
        }

        [HttpPost]
        [HasCredential(RoleId = "ADD_STOCK_IN")]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStockIn(StockIn model, string stockDetailsJson)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];

            ViewBag.Warehouses = new SelectList(dao.ListAll(), "WarehouseId", "Name", model.WarehouseId);
            ViewBag.Providers = new SelectList(db.Providers.ToList(), "ProviderId", "Name", model.ProviderId);
            ViewBag.Products = db.ProductVariants.Where(x => x.IsActive == true).ToList();

            if (ModelState.IsValid)
            {
                try
                {
                    model.CreatedBy = session.UserId;
                    model.Code = "PN" + DateTime.Now.ToString("ddMMyyHHmm");
                    model.TotalAmount = 0;
                    int stockInId = dao.CreateStockIn(model);

                    if (stockInId > 0 && !string.IsNullOrEmpty(stockDetailsJson))
                    {
                        var details = new JavaScriptSerializer().Deserialize<List<StockDetailViewModel>>(stockDetailsJson);

                        foreach (var item in details)
                        {
                            var detail = new StockInDetail
                            {
                                StockInId = stockInId,
                                VariantId = item.VariantId,
                                Quantity = item.Quantity,
                                UnitPrice = item.UnitPrice,
                                Note = item.Note
                            };
                            dao.AddStockInDetail(detail);
                        }
                        TempData["Success"] = "Tạo phiếu nhập kho thành công. Chờ duyệt.";
                        return RedirectToAction("StockInIndex");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }
            return View(model);
        }

        [HttpPost]
        [HasCredential(RoleId = "APPROVE_STOCK")]
        public JsonResult ApproveStockIn(int id)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            bool result = dao.ApproveStockIn(id, session.UserId);

            return Json(new { status = result, message = result ? "Đã duyệt phiếu nhập!" : "Lỗi khi duyệt." }, JsonRequestBehavior.AllowGet);
        }

        // =============================================
        // 4. XUẤT KHO (STOCK OUT)
        // =============================================

        [HasCredential(RoleId = "VIEW_STOCK_OUT")]
        public ActionResult StockOutIndex()
        {
            var model = dao.ListStockOut();
            return View(model);
        }

        [HttpGet]
        [HasCredential(RoleId = "ADD_STOCK_OUT")]
        public ActionResult CreateStockOut()
        {
            ViewBag.Warehouses = new SelectList(dao.ListAll(), "WarehouseId", "Name");
            ViewBag.Orders = new SelectList(db.Orders.Where(x => x.StatusId == 2).ToList(), "OrderId", "ShipName");

            return View();
        }

        [HttpPost]
        [HasCredential(RoleId = "ADD_STOCK_OUT")]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStockOut(StockOut model, string stockDetailsJson)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];

            if (ModelState.IsValid)
            {
                try
                {
                    model.CreatedBy = session.UserId;
                    model.Code = "PX" + DateTime.Now.ToString("ddMMyyHHmm");

                    int stockOutId = dao.CreateStockOut(model);

                    if (stockOutId > 0 && !string.IsNullOrEmpty(stockDetailsJson))
                    {
                        var details = new JavaScriptSerializer().Deserialize<List<StockDetailViewModel>>(stockDetailsJson);
                        foreach (var item in details)
                        {
                            var detail = new StockOutDetail
                            {
                                StockOutId = stockOutId,
                                VariantId = item.VariantId,
                                Quantity = item.Quantity,
                                Note = item.Note
                            };
                            dao.AddStockOutDetail(detail);
                        }
                        TempData["Success"] = "Tạo phiếu xuất kho thành công.";
                        return RedirectToAction("StockOutIndex");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }
            // Load lại ViewBag nếu lỗi
            ViewBag.Warehouses = new SelectList(dao.ListAll(), "WarehouseId", "Name", model.WarehouseId);
            ViewBag.Orders = new SelectList(db.Orders.Where(x => x.StatusId == 2).ToList(), "OrderId", "ShipName", model.OrderId);
            return View(model);
        }

        [HttpPost]
        [HasCredential(RoleId = "APPROVE_STOCK")]
        public JsonResult ApproveStockOut(int id)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            bool result = dao.ApproveStockOut(id, session.UserId);

            return Json(new { status = result, message = result ? "Đã duyệt phiếu xuất!" : "Lỗi khi duyệt (Có thể do tồn kho không đủ)." }, JsonRequestBehavior.AllowGet);
        }

        // Helper: ViewModel để nhận JSON từ View
        public class StockDetailViewModel
        {
            public int VariantId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; } // Chỉ dùng cho nhập kho
            public string Note { get; set; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}