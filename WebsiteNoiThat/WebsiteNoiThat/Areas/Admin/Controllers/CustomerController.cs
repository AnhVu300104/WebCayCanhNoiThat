using Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models; // Chứa UserModelView và UserLogin
using System.Data.Entity; // Cần cho AsNoTracking

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class CustomerController : HomeController
    {
        private DBNoiThat db = new DBNoiThat();

        // ================== DANH SÁCH KHÁCH HÀNG ==================
        [HasCredential(RoleId = "VIEW_USER")]
        public ActionResult Show()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            // Lấy dữ liệu khách hàng (GroupId = "USER") và thông tin Card
            var user = (from a in db.Users
                        join b in db.Cards on a.UserId equals b.UserId into g
                        from d in g.DefaultIfEmpty() // Left Join với Card
                        where a.GroupId == "USER"
                        select new UserModelView
                        {
                            UserId = a.UserId,
                            Name = a.Name,
                            Address = a.Address,
                            Phone = a.Phone,
                            Username = a.Username,
                            Password = a.Password, // Chỉ nên để hash
                            Email = a.Email,
                            Status = a.Status,
                            GroupId = a.GroupId,
                            NumberCard = d.NumberCard,
                            Indentification = d.Identification
                        }).ToList();

            return View(user);
        }

        // ================== THÊM KHÁCH HÀNG (GET) ==================
        [HttpGet]
        [HasCredential(RoleId = "ADD_USER")]
        public ActionResult Add()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;
            return View();
        }

        // ================== THÊM KHÁCH HÀNG (POST) ==================
        [HttpPost]
        [HasCredential(RoleId = "ADD_USER")]
        [ValidateAntiForgeryToken] // Nên thêm
        public ActionResult Add(UserModelView n)
        {
            if (ModelState.IsValid)
            {
                // 1. Tạo User
                var userModel = new User();
                userModel.Name = n.Name;
                userModel.Address = n.Address;
                userModel.Phone = n.Phone;
                userModel.Username = n.Username;
                userModel.Password = Encryptor.MD5Hash(n.Password);
                userModel.Email = n.Email;
                userModel.GroupId = "USER";
                userModel.Status = n.Status;
                db.Users.Add(userModel);
                db.SaveChanges(); // Lưu lần 1 để lấy UserId

                // 2. Tạo Card (Chỉ tạo sau khi có UserId)
                var cardModel = new Card();
                cardModel.NumberCard = n.NumberCard.GetValueOrDefault(0); // Lấy giá trị hoặc 0
                cardModel.UserNumber = 0; // Đặt giá trị mặc định nếu cần
                cardModel.UserId = userModel.UserId; // Lấy ID mới tạo
                cardModel.Identification = n.Indentification;
                db.Cards.Add(cardModel);
                db.SaveChanges();

                TempData["Success"] = "Thêm khách hàng thành công.";
                return RedirectToAction("Show");
            }
            return View(n);
        }

        // ================== SỬA KHÁCH HÀNG (GET) ==================
        [HttpGet]
        [HasCredential(RoleId = "EDIT_USER")]
        public ActionResult Edit(int UserId)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            var models = (from a in db.Users
                          join b in db.Cards on a.UserId equals b.UserId into g
                          from d in g.DefaultIfEmpty()
                          where a.UserId == UserId
                          select new UserModelView
                          {
                              UserId = a.UserId,
                              Name = a.Name,
                              Address = a.Address,
                              Phone = a.Phone,
                              Username = a.Username,
                              Password = a.Password,
                              Email = a.Email,
                              Status = a.Status,
                              GroupId = a.GroupId,
                              NumberCard = d.NumberCard,
                              Indentification = d.Identification
                          }).FirstOrDefault(); // Lấy một User duy nhất

            if (models == null || models.GroupId != "USER")
            {
                TempData["Error"] = "Khách hàng không tồn tại hoặc không phải là khách hàng.";
                return RedirectToAction("Show");
            }
            return View(models);
        }

        // ================== SỬA KHÁCH HÀNG (POST) ==================
        [HttpPost]
        [HasCredential(RoleId = "EDIT_USER")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserModelView n)
        {
            if (ModelState.IsValid)
            {
                var userModel = db.Users.SingleOrDefault(a => a.UserId == n.UserId);
                if (userModel == null) return HttpNotFound();

                // 1. Cập nhật User
                userModel.Name = n.Name;
                userModel.Address = n.Address;
                userModel.Phone = n.Phone;
                userModel.Username = n.Username;
                // Chỉ mã hóa nếu mật khẩu được thay đổi, nếu không giữ nguyên mật khẩu cũ
                // if (!string.IsNullOrEmpty(n.Password)) userModel.Password = Encryptor.MD5Hash(n.Password);
                userModel.Email = n.Email;
                userModel.GroupId = "USER"; // Giữ nguyên GroupId
                userModel.Status = n.Status;
                db.SaveChanges();

                // 2. Cập nhật Card (có thể cần xử lý nếu Card chưa tồn tại)
                var cardModel = db.Cards.Where(a => a.UserId == userModel.UserId).SingleOrDefault();
                if (cardModel != null)
                {
                    cardModel.NumberCard = n.NumberCard;
                    cardModel.Identification = n.Indentification;
                    db.SaveChanges();
                }

                TempData["Success"] = "Cập nhật thông tin khách hàng thành công.";
                return RedirectToAction("Show");
            }
            // Nếu ModelState không hợp lệ
            return View(n);
        }

        // ========================================================
        // HÀM DELETE ĐƠN LẺ (GET) - FIX LỖI 404
        // ========================================================
        [HttpGet]
        [HasCredential(RoleId = "DELETE_USER")]
        public ActionResult Delete(int? UserId) // Nhận tham số từ URL
        {
            if (UserId == null)
            {
                TempData["Error"] = "Không có mã khách hàng để xóa.";
                return RedirectToAction("Show");
            }

            var model = db.Users.Find(UserId);

            if (model != null && model.GroupId == "USER")
            {
                try
                {
                    // Xóa Card liên quan trước
                    var card = db.Cards.SingleOrDefault(n => n.UserId == model.UserId);
                    if (card != null)
                    {
                        db.Cards.Remove(card);
                    }

                    // Xóa User
                    db.Users.Remove(model);
                    db.SaveChanges();

                    TempData["Success"] = $"Đã xóa khách hàng ID #{UserId} thành công.";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Xóa thất bại. Khách hàng này có thể có đơn hàng liên quan.";
                }
            }
            else
            {
                TempData["Error"] = "Không tìm thấy khách hàng.";
            }

            return RedirectToAction("Show");
        }

        // ================== XÓA NHIỀU MỤC (POST) ==================
        [HttpPost]
        [HasCredential(RoleId = "DELETE_USER")]
        [ValidateAntiForgeryToken] // Nên có
        public ActionResult DeleteMultiple(FormCollection formCollection)
        {
            // Logic xóa hàng loạt bằng FormCollection (Giữ nguyên hoặc dùng logic đơn giản hơn)
            // ... (Bạn có thể giữ nguyên logic xóa hàng loạt của bạn nếu nó đã hoạt động)
            TempData["Error"] = "Chức năng xóa hàng loạt chưa được cấu hình hoàn chỉnh.";
            return RedirectToAction("Show");
        }
    }
}