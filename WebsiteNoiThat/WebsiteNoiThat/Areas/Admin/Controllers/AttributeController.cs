using Models.DAO;
using Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteNoiThat.Common;
using WebsiteNoiThat.Models;

// Alias để tránh xung đột với System.Attribute
using DbAttribute = Models.EF.Attribute;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class AttributeController : Controller
    {
        AttributeDao dao = new AttributeDao();

        // =============================================
        // 1. QUẢN LÝ THUỘC TÍNH CHA (ATTRIBUTE)
        // =============================================

        [HasCredential(RoleId = "VIEW_ATTRIBUTE")]
        public ActionResult Index()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            var model = dao.ListAll();
            return View(model);
        }

        [HttpGet]
        [HasCredential(RoleId = "ADD_ATTRIBUTE")]
        public ActionResult Create()
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;
            return View();
        }

        [HttpPost]
        [HasCredential(RoleId = "ADD_ATTRIBUTE")]
        public ActionResult Create(DbAttribute model)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            if (ModelState.IsValid)
            {
                var result = dao.Insert(model);
                if (result > 0)
                {
                    TempData["Success"] = "Thêm thuộc tính thành công";
                    return RedirectToAction("Index");
                }
                else if (result == -1)
                {
                    ModelState.AddModelError("", "Mã thuộc tính (Code) đã tồn tại.");
                }
                else
                {
                    ModelState.AddModelError("", "Thêm thất bại. Vui lòng thử lại.");
                }
            }
            return View(model);
        }

        [HttpGet]
        [HasCredential(RoleId = "EDIT_ATTRIBUTE")]
        public ActionResult Edit(int id)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            var model = dao.ViewDetail(id);
            return View(model);
        }

        [HttpPost]
        [HasCredential(RoleId = "EDIT_ATTRIBUTE")]
        public ActionResult Edit(DbAttribute model)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            if (ModelState.IsValid)
            {
                var result = dao.Update(model);
                if (result)
                {
                    TempData["Success"] = "Cập nhật thành công";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Cập nhật thất bại.");
                }
            }
            return View(model);
        }

        [HttpGet]
        [HasCredential(RoleId = "DELETE_ATTRIBUTE")]
        public ActionResult Delete(int id)
        {
            var result = dao.Delete(id);
            if (result)
            {
                TempData["Success"] = "Xóa thành công";
            }
            else
            {
                TempData["Error"] = "Xóa thất bại. Có thể thuộc tính đang được sử dụng.";
            }
            return RedirectToAction("Index");
        }

        // =============================================
        // 2. QUẢN LÝ GIÁ TRỊ THUỘC TÍNH (ATTRIBUTE VALUES)
        // =============================================

        /// <summary>
        /// Trang chi tiết để quản lý các giá trị con (VD: Vào Size để thêm S, M, L)
        /// </summary>
        [HttpGet]
        [HasCredential(RoleId = "VIEW_ATTRIBUTE")]
        public ActionResult Details(int id)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            // Lấy thông tin thuộc tính cha
            var attribute = dao.ViewDetail(id);
            if (attribute == null) return RedirectToAction("Index");

            // Lấy TẤT CẢ giá trị (cả active và inactive)
            ViewBag.Values = dao.GetAllValuesByAttributeId(id);

            return View(attribute);
        }

        /// <summary>
        /// Thêm giá trị mới cho thuộc tính
        /// </summary>
        [HttpPost]
        [HasCredential(RoleId = "EDIT_ATTRIBUTE")]
        public ActionResult AddValue(AttributeValue model)
        {
            if (ModelState.IsValid)
            {
                if (model.DisplayOrder == null) model.DisplayOrder = 0;
                model.IsActive = true; // Mặc định là hoạt động

                bool res = dao.InsertValue(model);
                if (res)
                {
                    TempData["Success"] = "Thêm giá trị thành công";
                }
                else
                {
                    TempData["Error"] = "Lỗi khi thêm giá trị";
                }
            }
            return RedirectToAction("Details", new { id = model.AttributeId });
        }

        /// <summary>
        /// ✅ CHUYỂN ĐỔI TRẠNG THÁI (thay vì xóa)
        /// </summary>
        [HttpGet]
        [HasCredential(RoleId = "EDIT_ATTRIBUTE")]
        public ActionResult ToggleValueStatus(int valueId, int parentId)
        {
            var result = dao.ToggleValueStatus(valueId);
            if (result.success)
            {
                if (result.isActive)
                {
                    TempData["Success"] = "Đã kích hoạt giá trị thành công";
                }
                else
                {
                    TempData["Success"] = "Đã vô hiệu hóa giá trị thành công";
                }
            }
            else
            {
                TempData["Error"] = "Không thể cập nhật trạng thái";
            }
            return RedirectToAction("Details", new { id = parentId });
        }

        /// <summary>
        /// XÓA HOÀN TOÀN (nếu cần - có thể bỏ hoặc giữ cho trường hợp đặc biệt)
        /// </summary>
        [HttpGet]
        [HasCredential(RoleId = "DELETE_ATTRIBUTE")]
        public ActionResult DeleteValue(int valueId, int parentId)
        {
            bool res = dao.DeleteValue(valueId);
            if (res)
            {
                TempData["Success"] = "Xóa giá trị thành công";
            }
            else
            {
                TempData["Error"] = "Xóa thất bại";
            }
            return RedirectToAction("Details", new { id = parentId });
        }

        [HttpGet]
        [HasCredential(RoleId = "EDIT_ATTRIBUTE")]
        public ActionResult CreateValue(int attributeId)
        {
            var session = (UserLogin)Session[Commoncontent.user_sesion_admin];
            ViewBag.username = session?.Username;

            var attribute = dao.ViewDetail(attributeId);
            if (attribute == null) return HttpNotFound();

            var model = new AttributeValue();
            model.AttributeId = attributeId;

            ViewBag.AttributeName = attribute.Name;
            ViewBag.DisplayType = attribute.DisplayType;

            return View(model);
        }

        [HttpPost]
        [HasCredential(RoleId = "EDIT_ATTRIBUTE")]
        public ActionResult CreateValue(AttributeValue model)
        {
            if (ModelState.IsValid)
            {
                if (model.DisplayOrder == null) model.DisplayOrder = 0;
                model.IsActive = true;

                bool res = dao.InsertValue(model);
                if (res)
                {
                    TempData["Success"] = "Thêm giá trị thành công";
                    return RedirectToAction("Edit", new { id = model.AttributeId });
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi thêm giá trị");
                }
            }

            var attribute = dao.ViewDetail(model.AttributeId);
            ViewBag.AttributeName = attribute.Name;
            ViewBag.DisplayType = attribute.DisplayType;

            return View(model);
        }

        /// <summary>
        /// API trả về danh sách giá trị ĐANG HOẠT ĐỘNG (Dùng cho Ajax khi tạo sản phẩm)
        /// </summary>
        [HttpPost]
        public JsonResult GetValuesJson(int attributeId)
        {
            var data = dao.GetValuesByAttributeId(attributeId) // Chỉ lấy IsActive = true
                          .Select(x => new { x.ValueId, x.DisplayValue, x.Value })
                          .ToList();
            return Json(new { status = true, data = data });
        }
    }
}