using MySql.Data.MySqlClient;

namespace DAL
{
    public static class DatabaseInitializer
    {
        private const string DatabaseName = "MunicipalityDB";

        public static async Task InitializeAsync(string serverConnectionString)
        {
            await CreateDatabaseAsync(serverConnectionString);

            string databaseConnectionString = serverConnectionString + "Database=" + DatabaseName + ";";
            await CreateTablesAsync(databaseConnectionString);
            await UpdateUsersTableAsync(databaseConnectionString);
            await AddDefaultRolesAsync(databaseConnectionString);
            await AddDefaultCategoriesAsync(databaseConnectionString);
        }

        private static async Task UpdateUsersTableAsync(string connectionString)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string fullNameSql = @"
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @DatabaseName
                      AND TABLE_NAME = 'Users'
                      AND COLUMN_NAME = 'FullName';";

                using (MySqlCommand command = new MySqlCommand(fullNameSql, connection))
                {
                    command.Parameters.AddWithValue("@DatabaseName", DatabaseName);
                    long columnCount = Convert.ToInt64(await command.ExecuteScalarAsync());
                    if (columnCount == 0)
                    {
                        string addFullNameSql = "ALTER TABLE Users ADD COLUMN FullName VARCHAR(100) NOT NULL DEFAULT '' AFTER UserID;";
                        using (MySqlCommand alterCommand = new MySqlCommand(addFullNameSql, connection))
                        {
                            await alterCommand.ExecuteNonQueryAsync();
                        }
                    }
                }

                string passwordSql = @"
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @DatabaseName
                      AND TABLE_NAME = 'Users'
                      AND COLUMN_NAME = 'Password';";

                using (MySqlCommand command = new MySqlCommand(passwordSql, connection))
                {
                    command.Parameters.AddWithValue("@DatabaseName", DatabaseName);
                    long columnCount = Convert.ToInt64(await command.ExecuteScalarAsync());
                    if (columnCount == 0)
                    {
                        string renamePasswordSql = "ALTER TABLE Users CHANGE COLUMN PasswordHash Password VARCHAR(255) NOT NULL;";
                        using (MySqlCommand alterCommand = new MySqlCommand(renamePasswordSql, connection))
                        {
                            await alterCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        private static async Task CreateDatabaseAsync(string connectionString)
        {
            string sql = "CREATE DATABASE IF NOT EXISTS `" + DatabaseName + "` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private static async Task CreateTablesAsync(string connectionString)
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS Roles (
                    RoleID INT AUTO_INCREMENT PRIMARY KEY,
                    RoleName VARCHAR(30) NOT NULL UNIQUE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS Users (
                    UserID INT AUTO_INCREMENT PRIMARY KEY,
                    FullName VARCHAR(100) NOT NULL,
                    Username VARCHAR(50) NOT NULL UNIQUE,
                    Email VARCHAR(100) NOT NULL UNIQUE,
                    Password VARCHAR(255) NOT NULL,
                    RoleID INT NOT NULL,
                    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
                        ON DELETE RESTRICT ON UPDATE CASCADE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS Categories (
                    CategoryID INT AUTO_INCREMENT PRIMARY KEY,
                    CategoryName VARCHAR(70) NOT NULL UNIQUE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS Reports (
                    ReportID INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(150) NOT NULL,
                    Description TEXT NOT NULL,
                    Address VARCHAR(200) NOT NULL,
                    Latitude DECIMAL(10, 7) NULL,
                    Longitude DECIMAL(10, 7) NULL,
                    CategoryID INT NOT NULL,
                    UserID INT NOT NULL,
                    Status VARCHAR(30) NOT NULL DEFAULT 'Received',
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME NULL,
                    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
                        ON DELETE RESTRICT ON UPDATE CASCADE,
                    FOREIGN KEY (UserID) REFERENCES Users(UserID)
                        ON DELETE RESTRICT ON UPDATE CASCADE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS ReportImages (
                    ImageID INT AUTO_INCREMENT PRIMARY KEY,
                    ReportID INT NOT NULL,
                    ImageUrl VARCHAR(500) NOT NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (ReportID) REFERENCES Reports(ReportID)
                        ON DELETE CASCADE ON UPDATE CASCADE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS ReportStatusHistory (
                    StatusHistoryID INT AUTO_INCREMENT PRIMARY KEY,
                    ReportID INT NOT NULL,
                    UpdatedByUserID INT NULL,
                    Status VARCHAR(30) NOT NULL,
                    Note VARCHAR(500) NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (ReportID) REFERENCES Reports(ReportID)
                        ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (UpdatedByUserID) REFERENCES Users(UserID)
                        ON DELETE SET NULL ON UPDATE CASCADE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS ReportComments (
                    CommentID INT AUTO_INCREMENT PRIMARY KEY,
                    ReportID INT NOT NULL,
                    UserID INT NOT NULL,
                    CommentText TEXT NOT NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (ReportID) REFERENCES Reports(ReportID)
                        ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (UserID) REFERENCES Users(UserID)
                        ON DELETE RESTRICT ON UPDATE CASCADE
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS UserSessions (
                    SessionID INT AUTO_INCREMENT PRIMARY KEY,
                    UserID INT NOT NULL,
                    TokenHash CHAR(64) NOT NULL UNIQUE,
                    ExpiresAt DATETIME NOT NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (UserID) REFERENCES Users(UserID)
                        ON DELETE CASCADE ON UPDATE CASCADE,
                    INDEX IX_UserSessions_UserID (UserID),
                    INDEX IX_UserSessions_ExpiresAt (ExpiresAt)
                ) ENGINE=InnoDB;
            ";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private static async Task AddDefaultRolesAsync(string connectionString)
        {
            string sql = @"
                INSERT IGNORE INTO Roles (RoleName) VALUES
                    ('Resident'),
                    ('MunicipalityStaff'),
                    ('Admin');";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private static async Task AddDefaultCategoriesAsync(string connectionString)
        {
            string sql = @"
                INSERT IGNORE INTO Categories (CategoryName) VALUES
                    ('תאורה'),
                    ('כבישים ומדרכות'),
                    ('ניקיון'),
                    ('גינון'),
                    ('ריהוט רחוב'),
                    ('אחר');";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
