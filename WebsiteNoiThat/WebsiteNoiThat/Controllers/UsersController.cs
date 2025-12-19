using Models.EF;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models;

namespace WebsiteNoiThat.Controllers
{
    public class UsersController : Controller
    {
        private DBNoiThat db = new DBNoiThat();

        // GET: Users/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }


        // GET: Users/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "UserId,Name,Address,Phone,Username,Password,Email,GroupId,Status")] User user)
        {
            if (ModelState.IsValid)
            {
                user.Password = Encryptor.MD5Hash(user.Password);
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            User user = db.Users.Find(id);
            db.Users.Remove(user);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        //
        // =============================================
        // LỊCH SỬ ĐỚN HÀNG
        // =============================================
        public ActionResult OrderHistory()
        {
            var userSession = (UserLogin)Session[Commoncontent.user_sesion];
            if (userSession == null)
            {
                return RedirectToAction("Login");
            }

            var orders = (from o in db.Orders
                          join s in db.Status on o.StatusId equals s.StatusId
                          where o.UserId == userSession.UserId
                          orderby o.CreatedDate descending
                          select new HistoryCart
                          {
                              OrderId = o.OrderId,
                              CreatedDate = o.CreatedDate,
                              StatusId = o.StatusId ?? 0,
                              NameStatus = s.Name
                          }).ToList();

            return View(orders);
        }

        // =============================================
        // CHI TIẾT ĐƠN HÀNG
        // =============================================
        public ActionResult OrderDetails(int id)
        {
            var userSession = (UserLogin)Session[Commoncontent.user_sesion];
            if (userSession == null)
            {
                return RedirectToAction("Login");
            }

            var order = db.Orders.FirstOrDefault(o => o.OrderId == id && o.UserId == userSession.UserId);
            if (order == null)
            {
                return HttpNotFound();
            }

            var orderDetails = (from od in db.OrderDetails
                                join p in db.Products on od.ProductId equals p.ProductId
                                where od.OrderId == id
                                select new HistoryCart
                                {
                                    OrderDetailId = od.OrderDetailId,
                                    ProductId = od.ProductId ?? 0,
                                    Name = p.Name,
                                    Photo = p.Photo,
                                    VariantInfo = od.VariantInfo,
                                    Quantity = od.Quantity,
                                    Price = od.Price
                                }).ToList();

            ViewBag.Order = order;
            ViewBag.Status = db.Status.Find(order.StatusId)?.Name;
            ViewBag.Total = orderDetails.Sum(x => (x.Quantity ?? 0) * (x.Price ?? 0));

            return View(orderDetails);
        }

        // =============================================
        // HỦY ĐƠN HÀNG
        // =============================================
        [HttpPost]
        public JsonResult CancelOrder(int orderId)
        {
            try
            {
                var userSession = (UserLogin)Session[Commoncontent.user_sesion];
                if (userSession == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var order = db.Orders.FirstOrDefault(o =>
                    o.OrderId == orderId &&
                    o.UserId == userSession.UserId
                );

                if (order == null)
                {
                    return Json(new { success = false, message = "Đơn hàng không tồn tại" });
                }

                // Chỉ cho phép hủy đơn hàng mới (StatusId = 1)
                if (order.StatusId != 1)
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng này" });
                }

                // Cập nhật trạng thái thành "Đã hủy" (StatusId = 5)
                order.StatusId = 5;
                order.UpdateDate = DateTime.Now;

                // Hoàn kho
                var orderDetails = db.OrderDetails.Where(od => od.OrderId == orderId).ToList();
                foreach (var item in orderDetails)
                {
                    if (item.VariantId.HasValue)
                    {
                        var variant = db.ProductVariants.Find(item.VariantId);
                        if (variant != null)
                        {
                            variant.StockQuantity = (variant.StockQuantity ?? 0) + item.Quantity;
                        }
                    }
                    else
                    {
                        var product = db.Products.Find(item.ProductId);
                        if (product != null)
                        {
                            product.Quantity = (product.Quantity ?? 0) + item.Quantity;
                        }
                    }
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Đã hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
