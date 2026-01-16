
using System;
using System.Drawing;
using System.Threading.Tasks;
using TransLearn.Data;

namespace TransLearn.Services
{
    // This class orchestrates the entire screen translation process.
    public class ScreenTranslationManager
    {
        private readonly ScreenCaptureService _captureService;
        private readonly OcrService _ocrService;
        private readonly TransLearnDao _dao;
        // private readonly ITranslationService _translationService;

        public ScreenTranslationManager(TransLearnDao dao /*, ITranslationService translationService */)
        {
            _captureService = new ScreenCaptureService();
            _ocrService = new OcrService();
            _dao = dao;
            // _translationService = translationService;
        }

        public async Task<(string? sourceText, string? translatedText)> ProcessScreenAreaAsync(Rectangle area)
        {
            try
            {
                using (Bitmap screenshot = _captureService.CaptureScreenArea(area))
                {
                    if (screenshot == null) return (null, null);

                    string sourceText = await _ocrService.RecognizeTextAsync(screenshot);
                    if (string.IsNullOrWhiteSpace(sourceText)) return (null, null);

                    // string translatedText = await _translationService.TranslateAsync(sourceText);
                    string translatedText = $"[Translated] {sourceText}"; // Placeholder

                    var rawEntry = new RawDataEntry
                    {
                        SourceText = sourceText,
                        TranslatedText = translatedText,
                        MediaType = MediaType.OCR
                    };
                    await _dao.AddRawDataAsync(rawEntry);

                    return (sourceText, translatedText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in ScreenTranslationManager: {ex.Message}");
                return (null, null);
            }
        }
    }
}
