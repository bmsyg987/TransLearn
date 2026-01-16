
using System;
using System.Threading.Tasks;

namespace TransLearn.Services.Stt
{
    public interface ISttService
    {
        Task<string> TranscribeAudioAsync(byte[] audioData, int bytesRecorded);
    }

    public class MockSttService : ISttService
    {
        private int _callCount = 0;
        public Task<string> TranscribeAudioAsync(byte[] audioData, int bytesRecorded)
        {
            _callCount++;
            string recognizedText = $"[Recognized speech chunk #{_callCount}]";
            Console.WriteLine($"MockSttService: Processed {bytesRecorded} bytes of audio.");
            return Task.FromResult(recognizedText);
        }
    }
}
