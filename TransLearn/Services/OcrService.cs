using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging; // 여기를 명확히 함
using Windows.Media.Ocr;

namespace TransLearn.Services
{
    public class OcrService
    {
        private readonly OcrEngine? _ocrEngine; // null 가능하게 변경

        public OcrService()
        {
            var language = new Language(System.Globalization.CultureInfo.CurrentCulture.Name);

            // OCR 엔진 생성 시도 (실패 시 null)
            _ocrEngine = OcrEngine.IsLanguageSupported(language)
                ? OcrEngine.TryCreateFromLanguage(language)
                : OcrEngine.TryCreateFromUserProfileLanguages();
        }

        public async Task<string> RecognizeTextAsync(Bitmap bitmap)
        {
            if (bitmap == null || _ocrEngine == null) return string.Empty;

            try
            {
                SoftwareBitmap? softwareBitmap = await ConvertToSoftwareBitmap(bitmap);
                if (softwareBitmap == null) return string.Empty;

                OcrResult result = await _ocrEngine.RecognizeAsync(softwareBitmap);

                var stringBuilder = new StringBuilder();
                foreach (var line in result.Lines)
                {
                    stringBuilder.AppendLine(line.Text);
                }

                return stringBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR failed: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task<SoftwareBitmap?> ConvertToSoftwareBitmap(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Bmp);
                stream.Position = 0;

                // [수정 포인트] 모호한 참조 해결 (전체 이름 명시)
                var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
                return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }
        }
    }
}