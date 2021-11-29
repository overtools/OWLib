using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TankView.ObjectModel {
    public class RGBABitmapSource : BitmapSource {
    private readonly int BackingPixelHeight;
    private readonly int BackingPixelWidth;

    public RGBABitmapSource(Memory<byte> rgbaBuffer, int pixelWidth, int pixelHeight) {
        Buffer = rgbaBuffer;
        BackingPixelWidth = pixelWidth;
        BackingPixelHeight = pixelHeight;
    }

    public RGBABitmapSource(RGBABitmapSource rgba) {
        Buffer = rgba.Buffer;
        BackingPixelWidth = rgba.BackingPixelWidth;
        BackingPixelHeight = rgba.BackingPixelHeight;
    }

    private Memory<byte> Buffer { get; }

    public override double DpiX => 96;

    public override double DpiY => 96;

    public override PixelFormat Format => PixelFormats.Bgra32;

    public override int PixelWidth => BackingPixelWidth;

    public override int PixelHeight => BackingPixelHeight;

    public override double Width => BackingPixelWidth;

    public override double Height => BackingPixelHeight;

    public override void CopyPixels(Int32Rect sourceRect, Array pixels, int stride, int offset) {
        var span = Buffer.Span;

        for (var y = sourceRect.Y; y < sourceRect.Y + sourceRect.Height; y++) {
            for (var x = sourceRect.X; x < sourceRect.X + sourceRect.Width; x++) {
                var i = stride * y + 4 * x;
                var a = span[i + 3];
                var b = span[i + 2];
                var g = span[i + 1];
                var r = span[i + 0];

                pixels.SetValue(b, i + offset);
                pixels.SetValue(g, i + offset + 1);
                pixels.SetValue(r, i + offset + 2);
                pixels.SetValue(a, i + offset + 3);
            }
        }
    }

    protected override Freezable CreateInstanceCore() => new RGBABitmapSource(Buffer, PixelWidth, PixelHeight);

#pragma warning disable 67
    public override event EventHandler<DownloadProgressEventArgs> DownloadProgress;
    public override event EventHandler DownloadCompleted;
    public override event EventHandler<ExceptionEventArgs> DownloadFailed;
    public override event EventHandler<ExceptionEventArgs> DecodeFailed;
#pragma warning restore 67
    }
}