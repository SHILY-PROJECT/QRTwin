using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace QRTwin.Maui.Controls;

public sealed class SvgIconView : SKCanvasView
{
    public static readonly BindableProperty IconNameProperty =
        BindableProperty.Create(nameof(IconName), typeof(string), typeof(SvgIconView), string.Empty, propertyChanged: OnIconChanged);

    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(SvgIconView), Colors.White, propertyChanged: OnIconChanged);

    private SKSvg? _svg;

    public string IconName
    {
        get => (string)GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    public Color IconColor
    {
        get => (Color)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public SvgIconView()
    {
        PaintSurface += OnPaintSurface;
        BackgroundColor = Colors.Transparent;
        IgnorePixelScaling = true;
    }

    private static void OnIconChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SvgIconView view)
        {
            view.LoadSvg();
            view.InvalidateSurface();
        }
    }

    private void LoadSvg()
    {
        _svg?.Dispose();
        _svg = null;

        if (!IconName.IsNotBlank())
        {
            return;
        }

        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync($"icons/{IconName}").GetAwaiter().GetResult();
            _svg = new SKSvg();
            _svg.Load(stream);
        }
        catch
        {
            _svg = null;
        }
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_svg?.Picture is not { } picture)
        {
            return;
        }

        var info = e.Info;
        var bounds = picture.CullRect;

        if (bounds is not { Width: > 0, Height: > 0 })
        {
            return;
        }

        var scale = Math.Min(info.Width / bounds.Width, info.Height / bounds.Height);
        var matrix = SKMatrix.CreateScale(scale, scale);
        var translatedX = (info.Width - bounds.Width * scale) / 2f - bounds.Left * scale;
        var translatedY = (info.Height - bounds.Height * scale) / 2f - bounds.Top * scale;
        matrix = matrix.PostConcat(SKMatrix.CreateTranslation(translatedX, translatedY));

        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateBlendMode(IconColor.ToSKColor(), SKBlendMode.SrcIn),
            IsAntialias = true
        };

        canvas.Save();
        canvas.SetMatrix(matrix);
        canvas.DrawPicture(picture, paint);
        canvas.Restore();
    }
}
