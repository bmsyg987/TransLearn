using System;

namespace TransLearn.Data
{
    public enum MediaType
    {
        OCR,
        Audio
    }

    public class RawDataEntry
    {
        public int Id { get; set; }
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

        // [수정] string 뒤에 ? 붙임 (Null 허용)
        public string? SourceText { get; set; }
        public string? TranslatedText { get; set; }
        public MediaType MediaType { get; set; }
    }

    public class LearnDataEntry
    {
        public int Id { get; set; }
        public string? WordOrPhrase { get; set; }
        public int Frequency { get; set; }
        public double? Difficulty { get; set; }
        public string? ContextSentence { get; set; }
        public string LastSeenTimestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }
}