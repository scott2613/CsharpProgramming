using System.Collections.Generic;

namespace ComputerStore.Core.Entities
{
    // Vai trò phân quyền tài khoản (1: Admin, 2: Customer)
    public class Role
    {
        // Khóa chính (1: Admin, 2: Customer)
        public int RoleId { get; set; }

        // Tên vai trò
        public string RoleName { get; set; } = string.Empty;

        // Danh sách các tài khoản thuộc vai trò này
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
