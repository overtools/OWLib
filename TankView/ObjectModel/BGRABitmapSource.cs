using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TankView.ObjectModel {
    public class BGRABitmapSource : BitmapSource {
    private readonly int BackingPixelHeight;
    private readonly int BackingPixelWidth;

    public BGRABitmapSource(Memory<byte> bgraBuffer, int pixelWidth, int pixelHeight) {
        Buffer = bgraBuffer;
        BackingPixelWidth = pixelWidth;
        BackingPixelHeight = pixelHeight;
    }

    public BGRABitmapSource(BGRABitmapSource bgra) {
        Buffer = bgra.Buffer;
        BackingPixelWidth = bgra.BackingPixelWidth;
        BackingPixelHeight = bgra.BackingPixelHeight;
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
        span.Slice(0, pixels.Length).CopyTo((byte[])pixels);
    }

    protected override Freezable CreateInstanceCore() => new BGRABitmapSource(Buffer, PixelWidth, PixelHeight);

#pragma warning disable 67
    public override event EventHandler<DownloadProgressEventArgs> DownloadProgress;
    public override event EventHandler DownloadCompleted;
    public override event EventHandler<ExceptionEventArgs> DownloadFailed;
    public override event EventHandler<ExceptionEventArgs> DecodeFailed;
#pragma warning restore 67
    }
}