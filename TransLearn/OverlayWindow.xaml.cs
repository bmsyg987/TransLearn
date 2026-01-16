using System;
using System.Windows;
using System.Windows.Input;

namespace TransLearn
{
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();
        }

        // 창의 '가운데(내부)'를 클릭하고 끌면 -> 창이 이동합니다.
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // 현재 캡처 영역 계산 (DPI 대응)
        public System.Drawing.Rectangle GetCaptureArea()
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            double dpiX = 1.0, dpiY = 1.0;

            if (source != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            int x = (int)(this.Left * dpiX);
            int y = (int)(this.Top * dpiY);

            // 테두리 두께(4)만큼 안쪽을 캡처
            int borderThickness = 4;
            int width = (int)((this.ActualWidth - borderThickness * 2) * dpiX);
            int height = (int)((this.ActualHeight - borderThickness * 2) * dpiY);

            if (width <= 0) width = 1;
            if (height <= 0) height = 1;

            return new System.Drawing.Rectangle(x + (int)(borderThickness * dpiX), y + (int)(borderThickness * dpiY), width, height);
        }
    }
}