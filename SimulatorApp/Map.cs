using System.IO;
using System.Net.Http;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace SimulatorApp;

class Map : IDisposable {
    private readonly Canvas _canvas;
    public float Scale { get; private set; }
    public float Size { get; private set; }
    public BoolBitmap BoolBitmap;
    private readonly Image _image;
    private static readonly HttpClient HttpClient = new();

    public Map(Canvas canvas, string path, float size, float zoom) {
        _canvas = canvas;
        Size = size;
        PrepareCanvas(size, zoom);
        _image = new Image();
        DrawMap(path, size);
        SKBitmap bitmap = GetBitmap(path);
        BoolBitmap = new BoolBitmap(bitmap);
        Scale = GetMapScale(size);
    }

    private static Stream StreamFromPath(string path) {
        var uri = new Uri(path);
        return uri.IsFile ? File.OpenRead(path) : HttpClient.GetStreamAsync(uri).Result;
    }

    private void DrawMap(string path, float mapSize) {
        using Stream stream = StreamFromPath(path);
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        _image.MaxHeight = mapSize;
        _image.MaxWidth = mapSize;
        var bitmapImage = new Bitmap(memoryStream);
        _image.Source = bitmapImage;
        _canvas.Children.Add(_image);
    }

    private static SKBitmap GetBitmap(string path) {
        using Stream stream = StreamFromPath(path);
        var image = SKImage.FromEncodedData(stream);
        return SKBitmap.FromImage(image);
    }

    private void PrepareCanvas(float size, float zoom) {
        _canvas.Height = size;
        _canvas.Width = size;
        _canvas.RenderTransform = new ScaleTransform {
            ScaleX = zoom,
            ScaleY = zoom
        };
        _canvas.RenderTransformOrigin = new RelativePoint(new Point(0.5, 0), RelativeUnit.Relative); // top center
    }

    private float GetMapScale(float mapSize) {
        return mapSize / Math.Max(BoolBitmap.Width, BoolBitmap.Height);
    }

    public void Dispose() {
        _canvas.Children.Remove(_image);
        BoolBitmap.Dispose();
    }
}
