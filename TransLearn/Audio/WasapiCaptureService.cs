
using NAudio.Wave;
using System;

namespace TransLearn.Services.Audio
{
    public class WasapiCaptureService : IDisposable
    {
        private readonly WasapiLoopbackCapture _capture;
        public event EventHandler<WaveInEventArgs>? DataAvailable;

        public WasapiCaptureService()
        {
            _capture = new WasapiLoopbackCapture();
            _capture.DataAvailable += OnDataAvailable;
        }

        public void StartRecording()
        {
            try { _capture.StartRecording(); }
            catch (Exception ex) { Console.WriteLine($"Failed to start audio capture: {ex.Message}"); }
        }

        public void StopRecording()
        {
            try { _capture.StopRecording(); }
            catch (Exception ex) { Console.WriteLine($"Failed to stop audio capture: {ex.Message}"); }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            DataAvailable?.Invoke(this, e);
        }

        public void Dispose()
        {
            _capture?.Dispose();
        }
    }
}
