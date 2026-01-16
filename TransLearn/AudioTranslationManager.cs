
using System;
using System.Threading.Tasks;
using TransLearn.Data;
using TransLearn.Services.Audio;
using TransLearn.Services.Stt;

namespace TransLearn.Services
{
    public class AudioTranslationManager : IDisposable
    {
        private readonly WasapiCaptureService _captureService;
        private readonly ISttService _sttService;
        private readonly TransLearnDao _dao;
        // private readonly ITranslationService _translationService; 

        public event Action<string, string>? OnNewTranslationAvailable;

        public AudioTranslationManager(ISttService sttService, TransLearnDao dao /*, ITranslationService translationService */)
        {
            _captureService = new WasapiCaptureService();
            _sttService = sttService;
            _dao = dao;
            // _translationService = translationService;
            _captureService.DataAvailable += OnAudioDataAvailable;
        }

        public void Start() => _captureService.StartRecording();
        public void Stop() => _captureService.StopRecording();

        private async void OnAudioDataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
        {
            try
            {
                if (e.BytesRecorded == 0) return;

                string sourceText = await _sttService.TranscribeAudioAsync(e.Buffer, e.BytesRecorded);
                if (string.IsNullOrWhiteSpace(sourceText)) return;

                // string translatedText = await _translationService.TranslateAsync(sourceText);
                string translatedText = $"[Translated] {sourceText}"; // Placeholder

                OnNewTranslationAvailable?.Invoke(sourceText, translatedText);

                var rawEntry = new RawDataEntry
                {
                    SourceText = sourceText,
                    TranslatedText = translatedText,
                    MediaType = MediaType.Audio
                };
                await _dao.AddRawDataAsync(rawEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during audio processing: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _captureService.DataAvailable -= OnAudioDataAvailable;
            _captureService?.Dispose();
        }
    }
}
