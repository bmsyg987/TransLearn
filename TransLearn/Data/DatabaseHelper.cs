
using System;
using System.Data.SQLite;
using System.IO;

namespace TransLearn.Data
{
    public static class DatabaseHelper
    {
        // Define the database file name.
        // It will be stored in the user's local app data folder for privacy and persistence.
        private static readonly string DbFileName = "translearn.sqlite";
        private static readonly string DbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TransLearn",
            DbFileName
        );

        // Public property for the connection string
        public static string ConnectionString => $"Data Source={DbPath};Version=3;";

        // Initializes the database. Creates the directory and file if they don't exist.
        public static void InitializeDatabase()
        {
            try
            {
                string? directory = Path.GetDirectoryName(DbPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(DbPath))
                {
                    SQLiteConnection.CreateFile(DbPath);
                }

                // Create tables if they don't exist
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    string createRawTableQuery = @"
                        CREATE TABLE IF NOT EXISTS RawTable (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Timestamp TEXT NOT NULL,
                            SourceText TEXT NOT NULL,
                            TranslatedText TEXT NOT NULL,
                            MediaType TEXT NOT NULL CHECK(MediaType IN ('OCR', 'Audio'))
                        );";

                    string createLearnTableQuery = @"
                        CREATE TABLE IF NOT EXISTS LearnTable (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            WordOrPhrase TEXT NOT NULL UNIQUE,
                            Frequency INTEGER NOT NULL DEFAULT 1,
                            Difficulty REAL,
                            ContextSentence TEXT,
                            LastSeenTimestamp TEXT NOT NULL
                        );";

                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = createRawTableQuery;
                        command.ExecuteNonQuery();

                        command.CommandText = createLearnTableQuery;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // In a real app, log this exception
                Console.WriteLine($"Database initialization failed: {ex.Message}");
                throw;
            }
        }
    }
}
