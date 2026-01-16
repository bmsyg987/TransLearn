
using System;
using System.Drawing;

namespace TransLearn.Services
{
    // Service responsible for capturing a specific region of the screen.
    public class ScreenCaptureService
    {
        public Bitmap? CaptureScreenArea(Rectangle area)
        {
            IntPtr desktopDC = IntPtr.Zero;
            IntPtr compatibleDC = IntPtr.Zero;
            IntPtr compatibleBitmap = IntPtr.Zero;

            try
            {
                desktopDC = PInvokeHelper.GetDC(IntPtr.Zero);
                compatibleDC = PInvokeHelper.CreateCompatibleDC(desktopDC);
                compatibleBitmap = PInvokeHelper.CreateCompatibleBitmap(desktopDC, area.Width, area.Height);
                IntPtr oldBitmap = PInvokeHelper.SelectObject(compatibleDC, compatibleBitmap);
                PInvokeHelper.BitBlt(compatibleDC, 0, 0, area.Width, area.Height, desktopDC, area.X, area.Y, PInvokeHelper.SRCCOPY);
                PInvokeHelper.SelectObject(compatibleDC, oldBitmap);
                Bitmap screenshot = Image.FromHbitmap(compatibleBitmap);
                return screenshot;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Screen capture failed: {ex.Message}");
                return null;
            }
            finally
            {
                if (compatibleBitmap != IntPtr.Zero) PInvokeHelper.DeleteObject(compatibleBitmap);
                if (compatibleDC != IntPtr.Zero) PInvokeHelper.DeleteObject(compatibleDC);
                if (desktopDC != IntPtr.Zero) PInvokeHelper.ReleaseDC(IntPtr.Zero, desktopDC);
            }
        }
    }
}
