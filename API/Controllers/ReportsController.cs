using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace MunicipalityWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private const long MaximumImageSize = 5 * 1024 * 1024;
        private readonly ReportDB reportDB;
        private readonly UserDB userDB;
        private readonly IWebHostEnvironment environment;

        public ReportsController(ReportDB reportDB, UserDB userDB, IWebHostEnvironment environment)
        {
            this.reportDB = reportDB;
            this.userDB = userDB;
            this.environment = environment;
        }

        [HttpGet("mine/{userId:int}")]
        public async Task<IActionResult> GetMine(int userId)
        {
            User? user = await GetSignedInUserAsync();
            if (user == null) return Unauthorized();
            if (user.Id != userId || user.RoleName != "Resident")
            {
                return Forbid();
            }
            return Ok(await reportDB.GetUserReportsAsync(userId));
        }

        [HttpGet("admin/open")]
        public async Task<IActionResult> GetOpenForAdmin([FromQuery] int adminId)
        {
            User? user = await GetSignedInUserAsync();
            if (user == null) return Unauthorized();
            if (user.RoleName != "Admin" || !await userDB.IsAdminAsync(user.Id) || user.Id != adminId) return Forbid();
            return Ok(await reportDB.GetOpenReportsAsync());
        }

        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllForAdmin([FromQuery] int adminId)
        {
            User? user = await GetSignedInUserAsync();
            if (user == null) return Unauthorized();
            if (user.RoleName != "Admin" || !await userDB.IsAdminAsync(user.Id) || user.Id != adminId) return Forbid();
            return Ok(await reportDB.GetAllReportsAsync());
        }

        [HttpPost("admin/{reportId:int}/resolve")]
        public async Task<IActionResult> Resolve(int reportId, [FromQuery] int adminId)
        {
            User? user = await GetSignedInUserAsync();
            if (user == null) return Unauthorized();
            if (user.RoleName != "Admin" || user.Id != adminId) return Forbid();
            bool resolved = await reportDB.ResolveReportAsync(reportId, user.Id);
            if (!resolved) return Conflict("הפנייה כבר נסגרה או שלא נמצאה.");
            return Ok(new { message = "הפנייה סומנה כסודרה; העדכון יוצג למשתמש באזור הפניות שלו." });
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaximumImageSize + 1024 * 1024)]
        public async Task<IActionResult> Create([FromForm] CreateReportForm form)
        {
            if (form.UserId <= 0 || string.IsNullOrWhiteSpace(form.Title) ||
                string.IsNullOrWhiteSpace(form.Description) || string.IsNullOrWhiteSpace(form.Address) ||
                string.IsNullOrWhiteSpace(form.CategoryName))
            {
                return BadRequest("יש למלא כותרת, תיאור, קטגוריה וכתובת.");
            }

            User? submittingUser = await GetSignedInUserAsync();
            if (submittingUser == null) return Unauthorized();
            if (submittingUser.RoleName != "Resident")
            {
                return Forbid();
            }
            form.UserId = submittingUser.Id;

            if (form.Image == null || form.Image.Length == 0)
            {
                return BadRequest("יש לצרף תמונה לפני שליחת הפנייה.");
            }

            if ((form.Latitude.HasValue && !form.Longitude.HasValue) ||
                (!form.Latitude.HasValue && form.Longitude.HasValue))
            {
                return BadRequest("יש לשלוח גם קו רוחב וגם קו אורך.");
            }

            if ((form.Latitude.HasValue && (form.Latitude < -90 || form.Latitude > 90)) ||
                (form.Longitude.HasValue && (form.Longitude < -180 || form.Longitude > 180)))
            {
                return BadRequest("קואורדינטות המיקום אינן תקינות.");
            }

            if (form.Image != null && form.Image.Length > MaximumImageSize)
            {
                return BadRequest("התמונה גדולה מדי. הגודל המרבי הוא 5MB.");
            }

            string? imageUrl = null;
            string? imagePath = null;
            if (form.Image != null && form.Image.Length > 0)
            {
                string extension = Path.GetExtension(form.Image.FileName).ToLowerInvariant();
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
                if (!allowedExtensions.Contains(extension) || !form.Image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("אפשר להעלות תמונת JPG, PNG או WEBP בלבד.");
                }

                string webRootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
                string uploadFolder = Path.Combine(webRootPath, "uploads");
                Directory.CreateDirectory(uploadFolder);

                string savedFileName = Guid.NewGuid().ToString("N") + extension;
                imagePath = Path.Combine(uploadFolder, savedFileName);
                imageUrl = "/uploads/" + savedFileName;

                using (FileStream fileStream = System.IO.File.Create(imagePath))
                {
                    await form.Image.CopyToAsync(fileStream);
                }
            }

            try
            {
                ReportSubmission report = new ReportSubmission
                {
                    Title = form.Title.Trim(),
                    Description = form.Description.Trim(),
                    Address = form.Address.Trim(),
                    CategoryName = form.CategoryName,
                    UserId = form.UserId,
                    Latitude = form.Latitude,
                    Longitude = form.Longitude
                };

                int reportId = await reportDB.AddReportAsync(report, imageUrl);
                return StatusCode(StatusCodes.Status201Created, new { id = reportId });
            }
            catch (InvalidOperationException ex)
            {
                DeleteImage(imagePath);
                return BadRequest(ex.Message);
            }
            catch
            {
                DeleteImage(imagePath);
                throw;
            }
        }

        private static void DeleteImage(string? imagePath)
        {
            if (!string.IsNullOrWhiteSpace(imagePath) && System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        private async Task<User?> GetSignedInUserAsync()
        {
            string? authorization = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;
            return await userDB.GetUserForSessionAsync(authorization.Substring("Bearer ".Length).Trim());
        }

        public class CreateReportForm
        {
            public int UserId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
            public IFormFile? Image { get; set; }
        }
    }
}
