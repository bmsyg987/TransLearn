
using System.Data.SQLite;
using System.Threading.Tasks;
using System;
using System.Collections.Generic; // Required for List

namespace TransLearn.Data
{
    // Data Access Object for all database operations.
    public class TransLearnDao
    {
        private readonly string _connectionString;

        public TransLearnDao()
        {
            _connectionString = DatabaseHelper.ConnectionString;
        }

        // Adds a new entry to the RawTable.
        public async Task AddRawDataAsync(RawDataEntry entry)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new SQLiteCommand(
                    @"INSERT INTO RawTable (Timestamp, SourceText, TranslatedText, MediaType)
                      VALUES (@Timestamp, @SourceText, @TranslatedText, @MediaType)",
                    connection);

                command.Parameters.AddWithValue("@Timestamp", entry.Timestamp);
                command.Parameters.AddWithValue("@SourceText", entry.SourceText);
                command.Parameters.AddWithValue("@TranslatedText", entry.TranslatedText);
                command.Parameters.AddWithValue("@MediaType", entry.MediaType.ToString());

                await command.ExecuteNonQueryAsync();
            }
        }

        // Adds a new learning entry or updates an existing one (Upsert).
        public async Task AddOrUpdateLearnDataAsync(LearnDataEntry entry)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new SQLiteCommand(
                    @"INSERT INTO LearnTable (WordOrPhrase, Frequency, ContextSentence, LastSeenTimestamp, Difficulty)
                      VALUES (@WordOrPhrase, 1, @ContextSentence, @LastSeenTimestamp, @Difficulty)
                      ON CONFLICT(WordOrPhrase) DO UPDATE SET
                          Frequency = Frequency + 1,
                          ContextSentence = excluded.ContextSentence,
                          LastSeenTimestamp = excluded.LastSeenTimestamp;",
                    connection);

                command.Parameters.AddWithValue("@WordOrPhrase", entry.WordOrPhrase);
                command.Parameters.AddWithValue("@ContextSentence", entry.ContextSentence);
                command.Parameters.AddWithValue("@LastSeenTimestamp", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@Difficulty", entry.Difficulty);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
