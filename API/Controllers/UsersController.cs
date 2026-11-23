using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace MunicipalityWeb.Controllers
{
    /// <summary>
    /// נקודות קצה לטבלת Users. כל פעולה מפעילה שאילתה אחת מ-UserDB
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserDB userDB;

        // UserDB מגיע אוטומטית מהרישום ב-Program.cs
        public UsersController(UserDB userDB)
        {
            this.userDB = userDB;
        }

        // GET api/users
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            User? currentUser = await GetSignedInUserAsync();
            if (currentUser == null) return Unauthorized();
            if (currentUser.RoleName != "Admin") return Forbid();
            List<User> users = await userDB.GetAllUsersAsync();
            return Ok(users.Select(user => new { user.Id, user.FullName, user.Username, user.Email, user.RoleName, user.CreatedAt }));
        }

        // GET api/users/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            User? currentUser = await GetSignedInUserAsync();
            if (currentUser == null) return Unauthorized();
            if (currentUser.Id != id && currentUser.RoleName != "Admin") return Forbid();
            User? user = await userDB.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new { user.Id, user.FullName, user.Username, user.Email, user.RoleName, user.CreatedAt });
        }

        // GET api/users/exists/admin
        [HttpGet("exists/{username}")]
        public async Task<ActionResult<bool>> UsernameExists(string username)
        {
            return Ok(await userDB.UsernameExistsAsync(username));
        }

        // GET api/users/email-exists/admin@example.com
        [HttpGet("email-exists/{email}")]
        public async Task<ActionResult<bool>> EmailExists(string email)
        {
            return Ok(await userDB.EmailExistsAsync(email));
        }

        // GET api/users/count-by-role
        [HttpGet("count-by-role")]
        public async Task<ActionResult<Dictionary<string, int>>> CountByRole()
        {
            User? currentUser = await GetSignedInUserAsync();
            if (currentUser == null) return Unauthorized();
            if (currentUser.RoleName != "Admin") return Forbid();
            return Ok(await userDB.CountUsersByRoleAsync());
        }

        // POST api/users?roleName=Resident
        // גוף הבקשה: username, email, password
        [HttpPost]
        public async Task<ActionResult<User>> Add([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                user.FullName = user.Username;
            }

            if (await userDB.UsernameExistsAsync(user.Username))
                return Conflict("Username already exists.");

            if (await userDB.EmailExistsAsync(user.Email))
                return Conflict("Email already exists.");

            try
            {
                await userDB.AddUserAsync(user, "Resident");
            }
            catch (InvalidOperationException ex)
            {
                // תפקיד שלא קיים בטבלת Roles
                return BadRequest(ex.Message);
            }

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { user.Id, user.FullName, user.Email });
        }

        // POST api/users/login
        // גוף הבקשה: email, password
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User credentials)
        {
            User? user = await userDB.CheckLoginAsync(credentials.Email, credentials.Password);
            if (user == null)
            {
                return Unauthorized();
            }

            string token = await userDB.CreateSessionAsync(user.Id);

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.RoleName,
                token
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            string? token = GetToken();
            if (string.IsNullOrWhiteSpace(token)) return Unauthorized();
            await userDB.RevokeSessionAsync(token);
            return NoContent();
        }

        // PUT api/users/5/role?roleName=MunicipalityStaff
        [HttpPut("{id:int}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromQuery] string roleName)
        {
            if (!await IsAdminRequestAsync()) return Unauthorized();
            bool updated = await userDB.UpdateUserRoleAsync(id, roleName);
            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }

        // PUT api/users/5/password
        // גוף הבקשה: הסיסמה החדשה כמחרוזת JSON, למשל "NewPass123"
        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] string newPassword)
        {
            if (!await IsAdminRequestAsync()) return Unauthorized();
            bool updated = await userDB.UpdatePasswordAsync(id, newPassword);
            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }

        // PUT api/users/5/email
        // גוף הבקשה: האימייל החדש כמחרוזת JSON
        [HttpPut("{id:int}/email")]
        public async Task<IActionResult> UpdateEmail(int id, [FromBody] string newEmail)
        {
            if (!await IsAdminRequestAsync()) return Unauthorized();
            if (await userDB.EmailExistsAsync(newEmail))
                return Conflict("Email already exists.");

            bool updated = await userDB.UpdateEmailAsync(id, newEmail);
            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE api/users/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsAdminRequestAsync()) return Unauthorized();
            bool deleted = await userDB.DeleteUserAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        private string? GetToken()
        {
            string? authorization = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
            return authorization.Substring("Bearer ".Length).Trim();
        }

        private async Task<User?> GetSignedInUserAsync()
        {
            string? token = GetToken();
            return string.IsNullOrWhiteSpace(token) ? null : await userDB.GetUserForSessionAsync(token);
        }

        private async Task<bool> IsAdminRequestAsync()
        {
            User? user = await GetSignedInUserAsync();
            return user != null && user.RoleName == "Admin";
        }
    }
}
