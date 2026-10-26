namespace ComputerStore.Core.Entities
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty; // "Admin", "Staff", "Customer"

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}