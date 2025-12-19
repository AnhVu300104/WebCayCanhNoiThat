using System;
using System.Collections.Generic;
using System.Linq;
using Models.EF;

namespace Models.DAO
{
    public class CategoryDao
    {
        DBNoiThat db = new DBNoiThat();

        /// <summary>
        /// Lấy danh sách tất cả các danh mục
        /// </summary>
        public List<Category> ListCategory()
        {
            return db.Categories.ToList();
        }

        /// <summary>
        /// Lấy danh sách các danh mục đang kích hoạt
        /// </summary>
        public List<Category> ListActiveCategory()
        {
            return db.Categories.Where(c => c.IsActive).ToList();
        }

        /// <summary>
        /// Lấy danh mục theo ParentId
        /// </summary>
        public List<Category> ListCategoryByParentId(int? parentId)
        {
            return db.Categories.Where(c => c.ParId == parentId && c.IsActive).ToList();
        }

        /// <summary>
        /// Xem chi tiết danh mục
        /// </summary>
        public Category ViewDetail(int id)
        {
            return db.Categories.SingleOrDefault(n => n.CategoryId == id);
        }

        /// <summary>
        /// Xóa danh mục
        /// </summary>
        public bool DeleteCate(int id)
        {
            try
            {
                var model = db.Categories.SingleOrDefault(n => n.CategoryId == id);
                if (model != null)
                {
                    db.Categories.Remove(model);
                    db.SaveChanges();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Bật/Tắt trạng thái danh mục
        /// </summary>
        public bool ToggleActive(int id)
        {
            try
            {
                var category = db.Categories.Find(id);
                if (category != null)
                {
                    category.IsActive = !category.IsActive;
                    db.SaveChanges();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}