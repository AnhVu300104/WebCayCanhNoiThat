using Models.EF;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteNoiThat.Models;
using Rotativa;
using WebsiteNoiThat.Common;
using PagedList;
using System.Net;
using WebsiteNoiThat.Areas.Admin.Models;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class OrderController : HomeController
    {
        DBNoiThat db = new DBNoiThat();

        // ================== THỐNG KÊ DOANH THU ==================
        [HttpGet]
        [HasCredential(RoleId = "VIEW_ORDER")]
        public ActionResult Viewmodel()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            // ✅ LẤY TẤT CẢ ĐƠN HÀNG (Hiển thị)
            var allOrders = (from a in db.Orders
                             join b in db.OrderDetails on a.OrderId equals b.OrderId
                             join c in db.Products on b.ProductId equals c.ProductId
                             join d in db.Status on a.StatusId equals d.StatusId
                             select new
                             {
                                 b.OrderDetailId,
                                 a.OrderId,
                                 b.ProductId,
                                 a.ShipAddress,
                                 a.ShipName,
                                 a.ShipPhone,
                                 b.Price,
                                 b.Quantity,
                                 c.Discount,
                                 a.UpdateDate,
                                 a.CreatedDate,
                                 a.StatusId,
                                 StatusName = d.Name,
                                 a.UserId,
                                 c.Name
                             })
                .AsEnumerable()
                .Select(o => new OrderViewModel
                {
                    OrderDetailId1 = o.OrderDetailId,
                    OrderId = o.OrderId,
                    ProductId = o.ProductId,
                    ProductName = o.Name,
                    ShipAddress = o.ShipAddress,
                    ShipName = o.ShipName,
                    ShipPhone = int.TryParse(o.ShipPhone, out int phone) ? phone : (int?)null,
                    Price = o.Price,
                    Quantity = o.Quantity,
                    Discount = o.Discount,
                    UpdateDate = o.UpdateDate ?? o.CreatedDate,
                    StatusId = o.StatusId,
                    StatusName = o.StatusName,
                    UserId = o.UserId
                }).ToList();

            // ⭐ CHỈ TÍNH DOANH THU TỪ ĐƠN ĐÃ GIAO (StatusId = 4)
            var deliveredOrders = allOrders.Where(x => x.StatusId == 4).ToList();
            var totalRevenue = deliveredOrders.Sum(item => (item.Quantity ?? 0) * (item.Price ?? 0));

            ViewBag.AllOrders = allOrders;
            ViewBag.TotalRevenue = totalRevenue; // ⭐ CHỈ TỪ ĐƠN ĐÃ GIAO
            ViewBag.DeliveredCount = deliveredOrders.Select(x => x.OrderId).Distinct().Count();
            ViewBag.DeliveredProducts = deliveredOrders.Sum(x => x.Quantity ?? 0);

            return View(allOrders);
        }

        [HttpPost]
        [HasCredential(RoleId = "VIEW_ORDER")]
        public ActionResult Viewmodel(DateTime? dfr, DateTime? dto)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            // ✅ VALIDATE NGÀY THÁNG
            if (!dfr.HasValue || !dto.HasValue)
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ ngày bắt đầu và ngày kết thúc!";
                return RedirectToAction("Viewmodel");
            }

            if (dfr.Value > dto.Value)
            {
                TempData["Error"] = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc!";
                return RedirectToAction("Viewmodel");
            }

            // ✅ LẤY TẤT CẢ ĐƠN HÀNG TRONG KHOẢNG THỜI GIAN
            var allOrders = (from a in db.Orders
                             join b in db.OrderDetails on a.OrderId equals b.OrderId
                             join c in db.Products on b.ProductId equals c.ProductId
                             join d in db.Status on a.StatusId equals d.StatusId
                             select new
                             {
                                 b.OrderDetailId,
                                 a.OrderId,
                                 b.ProductId,
                                 a.ShipAddress,
                                 a.ShipName,
                                 a.ShipPhone,
                                 b.Price,
                                 b.Quantity,
                                 c.Discount,
                                 a.UpdateDate,
                                 a.CreatedDate,
                                 a.StatusId,
                                 StatusName = d.Name,
                                 a.UserId,
                                 c.Name
                             })
                .AsEnumerable()
                .Select(x => new OrderViewModel
                {
                    OrderDetailId1 = x.OrderDetailId,
                    OrderId = x.OrderId,
                    ProductId = x.ProductId,
                    ProductName = x.Name,
                    ShipAddress = x.ShipAddress,
                    ShipName = x.ShipName,
                    ShipPhone = int.TryParse(x.ShipPhone, out int phone) ? phone : (int?)null,
                    Price = x.Price,
                    Quantity = x.Quantity,
                    Discount = x.Discount,
                    UpdateDate = x.UpdateDate ?? x.CreatedDate,
                    StatusId = x.StatusId,
                    StatusName = x.StatusName,
                    UserId = x.UserId
                })
                .Where(n => n.UpdateDate >= dfr.Value && n.UpdateDate <= dto.Value)
                .ToList();

            // ⭐ CHỈ TÍNH DOANH THU TỪ ĐƠN ĐÃ GIAO (StatusId = 4)
            var deliveredOrders = allOrders.Where(x => x.StatusId == 4).ToList();
            var totalRevenue = deliveredOrders.Sum(item => (item.Quantity ?? 0) * (item.Price ?? 0));

            ViewBag.AllOrders = allOrders;
            ViewBag.TotalRevenue = totalRevenue; // ⭐ CHỈ TỪ ĐƠN ĐÃ GIAO
            ViewBag.DeliveredCount = deliveredOrders.Select(x => x.OrderId).Distinct().Count();
            ViewBag.DeliveredProducts = deliveredOrders.Sum(x => x.Quantity ?? 0);
            ViewBag.FromDate = dfr.Value.ToString("dd/MM/yyyy");
            ViewBag.ToDate = dto.Value.ToString("dd/MM/yyyy");

            return View(allOrders);
        }

        // ================== CÁC ACTION CŨ (GIỮ NGUYÊN) ==================
        [HttpGet]
        [HasCredential(RoleId = "VIEW_ORDER")]
        public ActionResult Show()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            List<Status> list = db.Status.ToList();
            ViewBag.StatusList = new SelectList(list, "StatusId", "Name");

            var model = (from a in db.Orders
                         join b in db.OrderDetails on a.OrderId equals b.OrderId
                         join c in db.Products on b.ProductId equals c.ProductId
                         join d in db.Status on a.StatusId equals d.StatusId
                         select new
                         {
                             b.OrderDetailId,
                             a.OrderId,
                             b.ProductId,
                             a.ShipAddress,
                             a.ShipName,
                             a.ShipPhone,
                             b.Price,
                             b.Quantity,
                             c.Discount,
                             a.UpdateDate,
                             a.StatusId,
                             StatusName = d.Name,
                             a.UserId
                         })
             .AsEnumerable()
             .Select(o => new OrderViewModel
             {
                 OrderDetailId1 = o.OrderDetailId,
                 OrderId = o.OrderId,
                 ProductId = o.ProductId,
                 ShipAddress = o.ShipAddress,
                 ShipName = o.ShipName,
                 ShipPhone = int.TryParse(o.ShipPhone, out int phone) ? phone : (int?)null,
                 Price = o.Price,
                 Quantity = o.Quantity,
                 Discount = o.Discount,
                 UpdateDate = o.UpdateDate,
                 StatusId = o.StatusId,
                 StatusName = o.StatusName,
                 UserId = o.UserId
             }).ToList();

            ViewBag.Status = db.Status.ToList();
            return View(model);
        }

        [HasCredential(RoleId = "VIEW_ORDER")]
        public ActionResult Details(int? id)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session.Username;

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            ViewBag.aaaa = db.Status.SingleOrDefault(x => x.StatusId == order.StatusId).Name;
            if (order == null)
            {
                return HttpNotFound();
            }
            else
            {
                var orderproducts = (
                                 from a in db.OrderDetails
                                 join b in db.Orders on a.OrderId equals b.OrderId
                                 join c in db.Products on a.ProductId equals c.ProductId
                                 select new OrderProduct
                                 {
                                     OrderId = a.OrderId,
                                     ProductName = c.Name,
                                     Quantity = a.Quantity,
                                     Price = a.Price,
                                     ProductId = c.ProductId
                                 }
                         ).Where(o => o.OrderId == order.OrderId).ToList();
                ViewBag.orderproducts = orderproducts;

                decimal? total = 0;
                foreach (OrderProduct item in orderproducts)
                {
                    total += item.Price;
                }
                ViewBag.total = total;

                return View(order);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}