namespace Model
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public User() { }

        // משתמש שעדיין לא נשמר בבסיס הנתונים (אין לו מזהה)
        public User(string fullName, string email, string password)
        {
            FullName = fullName;
            Username = email;
            Email = email;
            Password = password;
        }

        // משתמש שנשלף מבסיס הנתונים
        public User(int id, string fullName, string username, string email, string password,
                    int roleId, string roleName, DateTime createdAt) : base(id)
        {
            FullName = fullName;
            Username = username;
            Email = email;
            Password = password;
            RoleId = roleId;
            RoleName = roleName;
            CreatedAt = createdAt;
        }

        public override string ToString() => $"{FullName} ({RoleName})";
    }
}
