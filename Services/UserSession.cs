namespace MunicipalityWeb.Services
{
    public class UserSession
    {
        public int UserId { get; private set; }
        public string FullName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string RoleName { get; private set; } = string.Empty;
        public bool IsAdmin => RoleName == "Admin";
        public string AccessToken { get; private set; } = string.Empty;
        public bool IsSignedIn { get; private set; }

        public void SignIn(int userId, string fullName, string email, string roleName, string accessToken)
        {
            UserId = userId;
            FullName = fullName;
            Email = email;
            RoleName = roleName;
            AccessToken = accessToken;
            IsSignedIn = true;
        }

        public void SignOut()
        {
            UserId = 0;
            FullName = string.Empty;
            Email = string.Empty;
            RoleName = string.Empty;
            AccessToken = string.Empty;
            IsSignedIn = false;
        }
    }
}
