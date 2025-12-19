using Models.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.DAO
{
    public class AttributeDao
    {
        DBNoiThat db = null;

        public AttributeDao()
        {
            db = new DBNoiThat();
        }

        // =============================================
        // QUẢN LÝ THUỘC TÍNH CHA (ATTRIBUTE)
        // =============================================

        /// <summary>
        /// Lấy tất cả thuộc tính
        /// </summary>
        public List<Models.EF.Attribute> ListAll()
        {
            return db.Attributes.OrderBy(x => x.DisplayOrder).ToList();
        }

        /// <summary>
        /// Xem chi tiết 1 thuộc tính
        /// </summary>
        public Models.EF.Attribute ViewDetail(int id)
        {
            return db.Attributes.Find(id);
        }

        /// <summary>
        /// Thêm thuộc tính mới
        /// </summary>
        public int Insert(Models.EF.Attribute entity)
        {
            try
            {
                // Kiểm tra trùng Code
                var existingCode = db.Attributes.FirstOrDefault(x => x.Code == entity.Code);
                if (existingCode != null)
                {
                    return -1; // Code đã tồn tại
                }

                db.Attributes.Add(entity);
                db.SaveChanges();
                return entity.AttributeId;
            }
            catch
            {
                return 0; // Thất bại
            }
        }

        /// <summary>
        /// Cập nhật thuộc tính
        /// </summary>
        public bool Update(Models.EF.Attribute entity)
        {
            try
            {
                var model = db.Attributes.Find(entity.AttributeId);
                if (model == null) return false;

                model.Name = entity.Name;
                model.Code = entity.Code;
                model.DisplayType = entity.DisplayType;
                model.IsRequired = entity.IsRequired;
                model.DisplayOrder = entity.DisplayOrder;
                model.IsActive = entity.IsActive;

                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Xóa thuộc tính (Cảnh báo: Nên kiểm tra ràng buộc trước khi xóa)
        /// </summary>
        public bool Delete(int id)
        {
            try
            {
                var model = db.Attributes.Find(id);
                if (model == null) return false;

                // Kiểm tra xem có giá trị con không
                var hasValues = db.AttributeValues.Any(x => x.AttributeId == id);
                if (hasValues)
                {
                    return false; // Không cho xóa nếu còn giá trị con
                }

                db.Attributes.Remove(model);
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // =============================================
        // QUẢN LÝ GIÁ TRỊ THUỘC TÍNH (ATTRIBUTE VALUES)
        // =============================================

        /// <summary>
        /// ✅ Lấy TẤT CẢ giá trị (cả active và inactive) - Dùng cho Admin
        /// </summary>
        public List<AttributeValue> GetAllValuesByAttributeId(int attributeId)
        {
            return db.AttributeValues
                     .Where(x => x.AttributeId == attributeId)
                     .OrderBy(x => x.DisplayOrder)
                     .ThenBy(x => x.Value)
                     .ToList();
        }

        /// <summary>
        /// ✅ Lấy CHỈ giá trị ĐANG HOẠT ĐỘNG - Dùng cho Client/Tạo sản phẩm
        /// </summary>
        public List<AttributeValue> GetValuesByAttributeId(int attributeId)
        {
            return db.AttributeValues
                     .Where(x => x.AttributeId == attributeId && x.IsActive == true)
                     .OrderBy(x => x.DisplayOrder)
                     .ThenBy(x => x.Value)
                     .ToList();
        }

        /// <summary>
        /// Thêm giá trị mới
        /// </summary>
        public bool InsertValue(AttributeValue entity)
        {
            try
            {
                db.AttributeValues.Add(entity);
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Cập nhật giá trị
        /// </summary>
        public bool UpdateValue(AttributeValue entity)
        {
            try
            {
                var model = db.AttributeValues.Find(entity.ValueId);
                if (model == null) return false;

                model.Value = entity.Value;
                model.DisplayValue = entity.DisplayValue;
                model.DisplayOrder = entity.DisplayOrder;
                model.IsActive = entity.IsActive;

                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ✅ CHUYỂN ĐỔI TRẠNG THÁI (Active <-> Inactive)
        /// </summary>
        public (bool success, bool isActive) ToggleValueStatus(int valueId)
        {
            try
            {
                var model = db.AttributeValues.Find(valueId);
                if (model == null)
                {
                    return (false, false);
                }

                // Đảo ngược trạng thái
                model.IsActive = !model.IsActive;
                db.SaveChanges();

                return (true, model.IsActive);
            }
            catch
            {
                return (false, false);
            }
        }

        /// <summary>
        /// Xóa vĩnh viễn giá trị (Nên cân nhắc trước khi dùng)
        /// </summary>
        public bool DeleteValue(int valueId)
        {
            try
            {
                var model = db.AttributeValues.Find(valueId);
                if (model == null) return false;

                // Kiểm tra xem giá trị có đang được sử dụng không
                var isUsed = db.VariantAttributeValues.Any(x => x.ValueId == valueId);
                if (isUsed)
                {
                    return false; // Không cho xóa nếu đang được sử dụng
                }

                db.AttributeValues.Remove(model);
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy giá trị theo ID
        /// </summary>
        public AttributeValue GetValueById(int valueId)
        {
            return db.AttributeValues.Find(valueId);
        }

        /// <summary>
        /// Lấy danh sách thuộc tính đang hoạt động (cho dropdown)
        /// </summary>
        public List<Models.EF.Attribute> GetActiveAttributes()
        {
            return db.Attributes
                     .OrderBy(x => x.DisplayOrder)
                     .ThenBy(x => x.Name)
                     .ToList();
        }
        /// <summary>
        /// ✅ Lấy danh sách thuộc tính kèm các giá trị đang hoạt động
        /// Dùng cho màn tạo / sửa biến thể
        /// </summary>
        public List<Models.EF.Attribute> GetAttributesWithValues()
        {
            return db.Attributes
                     .Include("AttributeValues")
                     .Where(a => a.AttributeValues.Any(v => v.IsActive))
                     .OrderBy(a => a.DisplayOrder)
                     .ThenBy(a => a.Name)
                     .ToList();
        }


    }
}