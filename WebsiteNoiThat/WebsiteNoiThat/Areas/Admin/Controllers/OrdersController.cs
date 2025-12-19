using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Models.EF;
using WebsiteNoiThat.Areas.Admin.Models;
using WebsiteNoiThat.Common;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class OrdersController : HomeController
    {
        private DBNoiThat db = new DBNoiThat();

        // ================== DANH SÁCH ĐƠN HÀNG ==================
        [HasCredential(RoleId = "VIEW_ORDER")]
        public ActionResult Show()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            var model = (from o in db.Orders
                         join s in db.Status on o.StatusId equals s.StatusId
                         select new OrderView
                         {
                             OrderId = o.OrderId,
                             ShipName = o.ShipName,
                             ShipPhone = o.ShipPhone,
                             ShipEmail = o.ShipEmail,
                             ShipAddress = o.ShipAddress,
                             UpdateDate = o.UpdateDate,
                             StatusName = s.Name,
                             UserId = o.UserId
                         }).ToList();

            return View(model);
        }

        // ================== CHI TIẾT ĐƠN HÀNG ==================
        [HasCredential(RoleId = "VIEW_ORDER")]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Order order = db.Orders.Find(id);
            if (order == null)
                return HttpNotFound();

            ViewBag.aaaa = db.Status
                .SingleOrDefault(x => x.StatusId == order.StatusId)?.Name;

            var orderProducts = (
                from d in db.OrderDetails
                join p in db.Products on d.ProductId equals p.ProductId
                where d.OrderId == order.OrderId
                select new OrderProduct
                {
                    OrderId = d.OrderId,
                    ProductId = p.ProductId,
                    ProductName = p.Name,
                    Quantity = d.Quantity,
                    Price = d.Price,
                    VariantInfo = d.VariantInfo
                }).ToList();

            ViewBag.orderproducts = orderProducts;
            ViewBag.total = orderProducts.Sum(x => x.Price);

            return View(order);
        }

        // ================== TẠO ĐƠN HÀNG ==================
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Order order)
        {
            if (ModelState.IsValid)
            {
                db.Orders.Add(order);
                db.SaveChanges();
                return RedirectToAction("Show");
            }
            return View(order);
        }

        // ================== SỬA ĐƠN HÀNG (GET) ==================
        [HasCredential(RoleId = "EDIT_ORDER")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var order = db.Orders.Find(id);
            if (order == null)
                return HttpNotFound();

            // Truyền danh sách trạng thái
            ViewBag.ListStatus = new SelectList(
                db.Status.ToList(),
                "StatusId",
                "Name",
                order.StatusId
            );

            // Cờ kiểm tra đã huỷ hay chưa
            ViewBag.IsCanceled = (order.StatusId == 5);

            return View(order);
        }

        // ================== SỬA ĐƠN HÀNG (POST) ==================
        [HasCredential(RoleId = "EDIT_ORDER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Order order)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ListStatus = new SelectList(db.Status.ToList(), "StatusId", "Name");
                return View(order);
            }

            // 🔒 LẤY ĐƠN GỐC TỪ DB (không dùng AsNoTracking để có thể update)
            var oldOrder = db.Orders.Find(order.OrderId);

            if (oldOrder == null)
                return HttpNotFound();

            // ================== HOÀN KHO KHI HUỶ ==================
            // Chỉ chạy khi:
            // - Trước đó CHƯA huỷ
            // - Bây giờ chuyển sang HUỶ
            // ================== HOÀN KHO KHI HUỶ ==================
            if (oldOrder.StatusId != 5 && order.StatusId == 5)
            {
                var orderDetails = db.OrderDetails
                    .Where(x => x.OrderId == order.OrderId)
                    .ToList();

                foreach (var item in orderDetails)
                {
                    // ✅ HOÀN KHO VÀO BẢNG ProductVariant (nếu có VariantId)
                    if (item.VariantId.HasValue)
                    {
                        var variant = db.ProductVariants.Find(item.VariantId.Value);
                        if (variant != null)
                        {
                            variant.StockQuantity += item.Quantity; // HOÀN VỀ KHO BIẾN THỂ
                            db.Entry(variant).State = EntityState.Modified;
                        }
                    }
                    else
                    {
                        // ⚠️ TRƯỜNG HỢP DỰ PHÒNG: Nếu không có VariantId thì hoàn vào Product
                        var product = db.Products.Find(item.ProductId);
                        if (product != null)
                        {
                            product.Quantity += item.Quantity;
                            db.Entry(product).State = EntityState.Modified;
                        }
                    }
                }
            }

            // ❌ NẾU ĐÃ HUỶ TRƯỚC ĐÓ → KHÔNG CHO ĐỔI TRẠNG THÁI
            if (oldOrder.StatusId == 5)
            {
                order.StatusId = oldOrder.StatusId;
            }

            // ✅ CẬP NHẬT CHỈ CÁC TRƯỜNG CẦN THIẾT, GIỮ NGUYÊN CreatedDate
            oldOrder.ShipName = order.ShipName;
            oldOrder.ShipPhone = order.ShipPhone;
            oldOrder.ShipEmail = order.ShipEmail;
            oldOrder.ShipAddress = order.ShipAddress;
            oldOrder.StatusId = order.StatusId;
            oldOrder.UpdateDate = DateTime.Now;
            // ⚠️ KHÔNG CẬP NHẬT CreatedDate - giữ nguyên giá trị cũ

            db.SaveChanges();

            return RedirectToAction("Show");
        }


        // ================== XÓA ĐƠN HÀNG ==================

        [HasCredential(RoleId = "DELETE_ORDER")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var order = db.Orders.Find(id);
            if (order == null)
                return HttpNotFound();

            // Lấy StatusId của trạng thái "Đã huỷ"
            var cancelStatusId = db.Status
                .Where(s => s.Name == "Đã huỷ" || s.Name == "Đã hủy")
                .Select(s => s.StatusId)
                .FirstOrDefault();

            // Không cho vào trang xóa nếu chưa huỷ
            if (order.StatusId != cancelStatusId)
            {
                TempData["Error"] = "Chỉ được xóa đơn hàng đã huỷ.";
                return RedirectToAction("Show");
            }

            return View(order);
        }

        [HasCredential(RoleId = "DELETE_ORDER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null)
                return HttpNotFound();

            // Lấy StatusId của trạng thái "Đã huỷ"
            var cancelStatusId = db.Status
                .Where(s => s.Name == "Đã huỷ" || s.Name == "Đã hủy")
                .Select(s => s.StatusId)
                .FirstOrDefault();

            // Chặn xóa nếu chưa huỷ
            if (order.StatusId != cancelStatusId)
            {
                TempData["Error"] = "Không thể xóa đơn hàng chưa huỷ.";
                return RedirectToAction("Show");
            }

            db.Orders.Remove(order);
            db.SaveChanges();

            TempData["Success"] = "Xóa đơn hàng thành công.";
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
