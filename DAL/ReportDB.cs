using Model;
using MySql.Data.MySqlClient;

namespace DAL
{
    public class ReportDB
    {
        private readonly string connectionString;

        public ReportDB(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<int> AddReportAsync(ReportSubmission report, string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                throw new InvalidOperationException("יש לצרף תמונה לפנייה.");
            }

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    using (MySqlCommand userLock = new MySqlCommand(
                        "SELECT UserID FROM Users WHERE UserID = @UserID AND IsActive = TRUE FOR UPDATE;", connection, transaction))
                    {
                        userLock.Parameters.AddWithValue("@UserID", report.UserId);
                        if (await userLock.ExecuteScalarAsync() == null)
                        {
                            throw new InvalidOperationException("המשתמש לא נמצא או שאינו פעיל.");
                        }
                    }

                    using (MySqlCommand countCommand = new MySqlCommand(@"
                        SELECT COUNT(*) FROM Reports
                        WHERE UserID = @UserID AND Status NOT IN ('Resolved', 'Closed');", connection, transaction))
                    {
                        countCommand.Parameters.AddWithValue("@UserID", report.UserId);
                        if (Convert.ToInt32(await countCommand.ExecuteScalarAsync()) >= 2)
                        {
                            throw new InvalidOperationException("אפשר להחזיק עד שתי פניות פעילות במקביל. סגרו פנייה קיימת כדי לשלוח פנייה נוספת.");
                        }
                    }

                    string insertReportSql = @"
                        INSERT INTO Reports
                            (Title, Description, Address, Latitude, Longitude, CategoryID, UserID, Status)
                        SELECT @Title, @Description, @Address, @Latitude, @Longitude,
                               CategoryID, @UserID, 'Received'
                        FROM Categories
                        WHERE CategoryName = @CategoryName;";

                    int reportId;
                    using (MySqlCommand command = new MySqlCommand(insertReportSql, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@Title", report.Title);
                        command.Parameters.AddWithValue("@Description", report.Description);
                        command.Parameters.AddWithValue("@Address", report.Address);
                        command.Parameters.AddWithValue("@Latitude", report.Latitude.HasValue ? report.Latitude.Value : DBNull.Value);
                        command.Parameters.AddWithValue("@Longitude", report.Longitude.HasValue ? report.Longitude.Value : DBNull.Value);
                        command.Parameters.AddWithValue("@UserID", report.UserId);
                        command.Parameters.AddWithValue("@CategoryName", report.CategoryName);

                        int rows = await command.ExecuteNonQueryAsync();
                        if (rows == 0)
                        {
                            throw new InvalidOperationException("קטגוריית הדיווח לא נמצאה.");
                        }

                        reportId = (int)command.LastInsertedId;
                    }

                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        string insertImageSql = @"
                            INSERT INTO ReportImages (ReportID, ImageUrl)
                            VALUES (@ReportID, @ImageUrl);";

                        using (MySqlCommand command = new MySqlCommand(insertImageSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@ReportID", reportId);
                            command.Parameters.AddWithValue("@ImageUrl", imageUrl);
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    transaction.Commit();
                    return reportId;
                }
            }
        }

        public async Task<List<ReportRecord>> GetUserReportsAsync(int userId)
        {
            return await GetReportsAsync(userId, false);
        }

        public async Task<List<ReportRecord>> GetOpenReportsAsync()
        {
            return await GetReportsAsync(null, true);
        }

        public async Task<List<ReportRecord>> GetAllReportsAsync()
        {
            return await GetReportsAsync(null, false);
        }

        private async Task<List<ReportRecord>> GetReportsAsync(int? userId, bool activeOnly)
        {
            string sql = @"
                SELECT r.ReportID, r.UserID, u.FullName, u.Email, r.Title, r.Description,
                       r.Address, c.CategoryName, r.Status, r.Latitude, r.Longitude,
                       r.CreatedAt, r.UpdatedAt,
                       (SELECT h.Note FROM ReportStatusHistory h WHERE h.ReportID = r.ReportID
                        ORDER BY h.CreatedAt DESC, h.StatusHistoryID DESC LIMIT 1) AS StatusNote,
                       (SELECT i.ImageUrl FROM ReportImages i WHERE i.ReportID = r.ReportID
                        ORDER BY i.CreatedAt DESC, i.ImageID DESC LIMIT 1) AS ImageUrl
                FROM Reports r
                INNER JOIN Users u ON u.UserID = r.UserID
                INNER JOIN Categories c ON c.CategoryID = r.CategoryID
                WHERE (@UserID IS NULL OR r.UserID = @UserID)
                  AND (@ActiveOnly = FALSE OR r.Status NOT IN ('Resolved', 'Closed'))
                ORDER BY r.CreatedAt DESC;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserID", userId.HasValue ? userId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@ActiveOnly", activeOnly);
            using var reader = await command.ExecuteReaderAsync();
            var reports = new List<ReportRecord>();
            while (await reader.ReadAsync())
            {
                reports.Add(new ReportRecord
                {
                    Id = reader.GetInt32(reader.GetOrdinal("ReportID")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserID")),
                    UserName = reader.GetString(reader.GetOrdinal("FullName")),
                    UserEmail = reader.GetString(reader.GetOrdinal("Email")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    Address = reader.GetString(reader.GetOrdinal("Address")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    Latitude = reader.IsDBNull(reader.GetOrdinal("Latitude")) ? null : reader.GetDecimal(reader.GetOrdinal("Latitude")),
                    Longitude = reader.IsDBNull(reader.GetOrdinal("Longitude")) ? null : reader.GetDecimal(reader.GetOrdinal("Longitude")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                    StatusNote = reader.IsDBNull(reader.GetOrdinal("StatusNote")) ? null : reader.GetString(reader.GetOrdinal("StatusNote")),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ImageUrl"))
                });
            }
            return reports;
        }

        public async Task<bool> ResolveReportAsync(int reportId, int adminUserId)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();
            using var update = new MySqlCommand(@"
                UPDATE Reports SET Status = 'Resolved', UpdatedAt = CURRENT_TIMESTAMP
                WHERE ReportID = @ReportID AND Status NOT IN ('Resolved', 'Closed');", connection, (MySqlTransaction)transaction);
            update.Parameters.AddWithValue("@ReportID", reportId);
            if (await update.ExecuteNonQueryAsync() == 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            const string note = "הבקשה טופלה וסומנה כסודרה על ידי נציג העירייה.";
            using var history = new MySqlCommand(@"
                INSERT INTO ReportStatusHistory (ReportID, UpdatedByUserID, Status, Note)
                VALUES (@ReportID, @AdminUserID, 'Resolved', @Note);", connection, (MySqlTransaction)transaction);
            history.Parameters.AddWithValue("@ReportID", reportId);
            history.Parameters.AddWithValue("@AdminUserID", adminUserId);
            history.Parameters.AddWithValue("@Note", note);
            await history.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return true;
        }
    }
}
