using Models.EF;
using System;
using System.Linq;
using System.Web.Mvc;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models; // Chứa UserLogin
using System.Data.Entity; // Cần cho EntityState

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class UserController : HomeController
    {
        // Khuyến nghị dùng private readonly cho DbContext
        private readonly DBNoiThat db = new DBNoiThat();

        public ActionResult Index()
        {
            return View();
        }

        // ================== DANH SÁCH NHÂN VIÊN/ADMIN ==================
        [HasCredential(RoleId = "VIEW_ADMIN")]
        public ActionResult Show()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            // Lấy danh sách tài khoản không phải là khách hàng
            var models = db.Users.Where(n => n.GroupId != "USER").ToList();
            return View(models);
        }

        // ================== THÊM NHÂN VIÊN (GET) ==================
        [HttpGet]
        [HasCredential(RoleId = "ADD_ADMIN")]
        public ActionResult Add()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            // Lấy danh sách nhóm quyền trừ nhóm "USER"
            ViewBag.ListGroups = new SelectList(db.UserGroups.Where(a => a.GroupId != "USER").ToList(), "GroupId", "Name");
            return View();
        }

        // ================== THÊM NHÂN VIÊN (POST) ==================
        [HttpPost]
        [HasCredential(RoleId = "ADD_ADMIN")]
        [ValidateAntiForgeryToken] // Thêm để bảo mật
        public ActionResult Add(User n)
        {
            ViewBag.ListGroups = new SelectList(db.UserGroups.Where(a => a.GroupId != "USER").ToList(), "GroupId", "Name", n.GroupId);

            if (ModelState.IsValid)
            {
                // Kiểm tra Username đã tồn tại chưa
                if (db.Users.Any(u => u.Username == n.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                    return View(n);
                }

                // Mã hóa mật khẩu và thiết lập các trường
                n.Password = Encryptor.MD5Hash(n.Password);
                n.Status = n.Status; // Thiết lập Status mặc định nếu cần

                db.Users.Add(n);
                db.SaveChanges();
                TempData["Success"] = "Thêm nhân viên mới thành công.";
                return RedirectToAction("Show");
            }
            return View(n);
        }

        // ================== SỬA NHÂN VIÊN (GET) ==================
        [HttpGet]
        [HasCredential(RoleId = "EDIT_ADMIN")]
        public ActionResult Edit(int UserId)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            ViewBag.ListGroups = new SelectList(db.UserGroups.Where(a => a.GroupId != "USER").ToList(), "GroupId", "Name");

            // Dùng Find hoặc SingleOrDefault an toàn hơn First()
            var models = db.Users.Find(UserId);

            if (models == null || models.GroupId == "USER")
            {
                // Response.StatusCode = 404; // Không cần thiết khi redirect
                TempData["Error"] = "Nhân viên không tồn tại hoặc không có quyền truy cập.";
                return RedirectToAction("Show");
            }
            return View(models);
        }

        // ================== SỬA NHÂN VIÊN (POST) ==================
        [HttpPost]
        [HasCredential(RoleId = "EDIT_ADMIN")]
        [ValidateAntiForgeryToken] // Thêm để bảo mật
        public ActionResult Edit(User n)
        {
            ViewBag.ListGroups = new SelectList(db.UserGroups.Where(a => a.GroupId != "USER").ToList(), "GroupId", "Name", n.GroupId);

            if (ModelState.IsValid)
            {
                // Lấy model gốc từ DB để giữ nguyên các trường khác
                var models = db.Users.Find(n.UserId);

                if (models == null) return HttpNotFound();

                models.Name = n.Name;
                models.Username = n.Username;

                // CHỈ CẬP NHẬT MẬT KHẨU NẾU NGƯỜI DÙNG NHẬP MỚI
                if (!string.IsNullOrEmpty(n.Password))
                {
                    models.Password = Encryptor.MD5Hash(n.Password);
                }

                models.GroupId = n.GroupId;
                models.Phone = n.Phone;
                models.Status = n.Status;
                models.Email = n.Email;
                models.Address = n.Address;

                db.Entry(models).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = "Cập nhật nhân viên thành công.";
                return RedirectToAction("Show");
            }
            else
            {
                // Không cần JavaScript, trả về View để hiển thị lỗi
                return View(n);
            }
        }

        // ========================================================
        // 3. XÓA ĐƠN LẺ (GET) - FIX LỖI 404
        // ========================================================
        [HttpGet]
        [HasCredential(RoleId = "DELETE_ADMIN")]
        public ActionResult Delete(int? UserId)
        {
            if (UserId == null)
            {
                TempData["Error"] = "Không có mã nhân viên để xóa.";
                return RedirectToAction("Show");
            }

            var model = db.Users.Find(UserId);

            if (model != null && model.GroupId != "USER") // Chỉ xóa Admin/Nhân viên
            {
                try
                {
                    db.Users.Remove(model);
                    db.SaveChanges();
                    TempData["Success"] = $"Đã xóa nhân viên ID #{UserId} thành công.";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Xóa thất bại. Tài khoản này có thể có dữ liệu liên quan.";
                }
            }
            else
            {
                TempData["Error"] = "Không tìm thấy nhân viên.";
            }

            return RedirectToAction("Show");
        }


        // ========================================================
        // 4. XÓA NHIỀU MỤC (POST) - Đã đổi tên thành DeleteMultiple
        // ========================================================
        [HttpPost]
        [HasCredential(RoleId = "DELETE_ADMIN")]
        [ActionName("Delete")] // Giữ nguyên tên Delete nếu form POST trỏ đến Delete
        [ValidateAntiForgeryToken] // Thêm để bảo mật
        public ActionResult DeleteConfirmed(FormCollection formCollection) // Đổi tên Action để tránh trùng GET/POST nếu không dùng ActionName
        {
            string[] ids = formCollection["UserId"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            int deletedCount = 0;

            foreach (string id in ids)
            {
                if (int.TryParse(id, out int userId))
                {
                    var model = db.Users.Find(userId);
                    if (model != null && model.GroupId != "USER")
                    {
                        // Bỏ qua việc xóa người dùng đang đăng nhập
                        var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
                        if (session != null && model.UserId == session.UserId) continue;

                        db.Users.Remove(model);
                        db.SaveChanges();
                        deletedCount++;
                    }
                }
            }
            TempData["Success"] = $"Đã xóa thành công {deletedCount} tài khoản nhân viên.";
            return RedirectToAction("Show");
        }

        // ================== USER PROFILE ==================

        [HttpGet]
        [HasCredential(RoleId = "VIEW_ADMIN")]
        public ActionResult UserProfile()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            var model = db.Users.First(n => n.UserId == session.UserId);
            return View(model);
        }

        [HttpPost]
        [HasCredential(RoleId = "EDIT_ADMIN")]
        public ActionResult UserProfile(User a)
        {
            // Tương tự Edit, cần xử lý Password nếu người dùng không nhập mới
            db.Entry(a).State = EntityState.Modified;
            db.SaveChanges();
            TempData["Success"] = "Cập nhật hồ sơ thành công.";
            return View(a);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

    }
}