using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfImage = System.Windows.Controls.Image;
using WpfBrushes = System.Windows.Media.Brushes;
using RobloxLightingOverlay.Effects;

namespace RobloxLightingOverlay
{
    public sealed class MotionBlurOverlay
    {
        private OverlayWindow _window;

        public void Start()
        {
            if (_window != null)
                return;

            _window = new OverlayWindow();
            _window.Start();
        }

        public void Stop()
        {
            _window?.Close();
            _window = null;
        }
    }

    internal sealed class OverlayWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private readonly WpfImage _image;

        private Process _roblox;
        private TemporalSmoother _smoother;

        public OverlayWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = WpfBrushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            IsHitTestVisible = false;

            _image = new WpfImage
            {
                Stretch = Stretch.Fill,
                Opacity = 0.45,
                IsHitTestVisible = false
            };

            Content = _image;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _timer.Tick += OnTick;
        }

        public void Start()
        {
            _roblox = RobloxHelper.FindRoblox();
            if (_roblox == null)
                return;

            MotionBlurManager.Start();
            _smoother = new TemporalSmoother(0.78f);

            Show();
            _timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (_roblox == null || _roblox.HasExited)
            {
                _timer.Stop();
                _roblox?.Dispose();
                _roblox = null;
                MotionBlurManager.Stop();
                Close();
                return;
            }

            RobloxHelper.GetWindowRect(_roblox.MainWindowHandle, out var r);

            Left = r.Left;
            Top = r.Top;
            Width = r.Right - r.Left;
            Height = r.Bottom - r.Top;

            var frame = ScreenCapture.Capture(
                r.Left, r.Top, (int)Width, (int)Height);

            MotionBlurManager.Apply(frame);
            frame = _smoother.Smooth(frame);

            _image.Source = frame;
        }
    }

    internal static class RobloxHelper
    {
        public static Process FindRoblox()
        {
            var processes = Process.GetProcessesByName("RobloxPlayerBeta");
            try
            {
                if (processes.Length > 0)
                {
                    for (int i = 1; i < processes.Length; i++)
                    {
                        processes[i].Dispose();
                    }
                    return processes[0];
                }
            }
            catch
            {
                foreach (var p in processes) p.Dispose();
                throw;
            }
            return null;
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(
            IntPtr hWnd,
            out RECT lpRect);

        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }
    }

    internal static class ScreenCapture
    {
        private static Bitmap _cacheBmp;
        private static Graphics _cacheGraphics;
        private static WriteableBitmap _cacheWb;

        public static WriteableBitmap Capture(
            int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0)
                return null;

            if (_cacheBmp == null || _cacheBmp.Width != width || _cacheBmp.Height != height)
            {
                _cacheBmp?.Dispose();
                _cacheGraphics?.Dispose();

                _cacheBmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                _cacheGraphics = Graphics.FromImage(_cacheBmp);
                _cacheWb = new WriteableBitmap(
                    width, height,
                    96, 96,
                    PixelFormats.Bgra32, null);
            }

            _cacheGraphics.CopyFromScreen(x, y, 0, 0, _cacheBmp.Size);

            var rect = new Rectangle(0, 0, width, height);
            var bmpData = _cacheBmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            _cacheWb.Lock();
            unsafe
            {
                Buffer.MemoryCopy(
                    (void*)bmpData.Scan0,
                    (void*)_cacheWb.BackBuffer,
                    _cacheWb.BackBufferStride * _cacheWb.PixelHeight,
                    bmpData.Stride * height);
            }
            _cacheWb.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _cacheWb.Unlock();

            _cacheBmp.UnlockBits(bmpData);

            return _cacheWb;
        }

        public static void ClearCache()
        {
            _cacheBmp?.Dispose();
            _cacheBmp = null;
            _cacheGraphics?.Dispose();
            _cacheGraphics = null;
            _cacheWb = null;
        }
    }

    internal sealed class TemporalSmoother
    {
        private WriteableBitmap _previous;
        private readonly float _alpha;

        public TemporalSmoother(float alpha)
        {
            _alpha = Math.Clamp(alpha, 0.1f, 0.95f);
        }

        public WriteableBitmap Smooth(WriteableBitmap current)
        {
            if (current == null)
                return null;

            if (_previous == null ||
                _previous.PixelWidth != current.PixelWidth ||
                _previous.PixelHeight != current.PixelHeight)
            {
                _previous = new WriteableBitmap(
                    current.PixelWidth, current.PixelHeight,
                    current.DpiX, current.DpiY,
                    current.Format, null);
                CopyPixels(current, _previous);
                return current;
            }

            Blend(current, _previous, _alpha);
            CopyPixels(current, _previous);
            return current;
        }

        private unsafe void CopyPixels(WriteableBitmap src, WriteableBitmap dest)
        {
            src.Lock();
            dest.Lock();

            Buffer.MemoryCopy(
                (void*)src.BackBuffer,
                (void*)dest.BackBuffer,
                dest.BackBufferStride * dest.PixelHeight,
                src.BackBufferStride * src.PixelHeight);

            dest.AddDirtyRect(new Int32Rect(0, 0, dest.PixelWidth, dest.PixelHeight));

            dest.Unlock();
            src.Unlock();
        }

        private unsafe void Blend(
            WriteableBitmap cur,
            WriteableBitmap prev,
            float alpha)
        {
            cur.Lock();
            prev.Lock();

            int bytes = cur.PixelHeight * cur.BackBufferStride;
            byte* c = (byte*)cur.BackBuffer;
            byte* p = (byte*)prev.BackBuffer;

            int alphaI = (int)(alpha * 256f);
            int alphaInvI = 256 - alphaI;

            int numThreads = Environment.ProcessorCount;
            int chunk = bytes / numThreads;

            System.Threading.Tasks.Parallel.For(0, numThreads, t =>
            {
                int start = t * chunk;
                int end = (t == numThreads - 1) ? bytes : start + chunk;

                for (int i = start; i < end; i++)
                {
                    c[i] = (byte)((c[i] * alphaInvI + p[i] * alphaI) >> 8);
                }
            });

            cur.AddDirtyRect(
                new Int32Rect(0, 0, cur.PixelWidth, cur.PixelHeight));

            prev.Unlock();
            cur.Unlock();
        }
    }

    internal static class MotionBlurManager
    {
        private static MotionBlurEffect _blur;
        private static CameraMotionDetector _camera;

        public static bool IsEnabled => _blur != null;

        public static void Start()
        {
            if (IsEnabled)
                return;

            _blur = new MotionBlurEffect();
            _camera = new CameraMotionDetector();
        }

        public static void Apply(WriteableBitmap frame)
        {
            if (!IsEnabled || frame == null)
                return;

            _camera.Analyze(frame);
            _blur.Apply(
                frame,
                _camera.DirectionX,
                _camera.DirectionY,
                _camera.Strength);
        }

        public static void Stop()
        {
            _blur?.Dispose();
            _blur = null;
            _camera = null;
        }
    }


}
