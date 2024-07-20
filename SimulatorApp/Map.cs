using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
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

    public Map(Canvas canvas, Stream stream, float size, float zoom) {
        _canvas = canvas;
        Size = size;
        PrepareCanvas(size, zoom);
        _image = new Image();
        DrawMap(stream, size);
        SKBitmap bitmap = GetBitmap(stream);
        BoolBitmap = new BoolBitmap(bitmap);
        Scale = GetMapScale(size);
    }

    public static async Task<MemoryStream> StreamFromPathAsync(string path) {
        var uri = new Uri(path);
        using Stream sourceStream = uri.IsFile ? File.OpenRead(path) : await HttpClient.GetStreamAsync(uri);
        var memoryStream = new MemoryStream();
        sourceStream.CopyTo(memoryStream);
        return memoryStream;
    }

    private void DrawMap(Stream stream, float mapSize) {
        _image.MaxHeight = mapSize;
        _image.MaxWidth = mapSize;
        stream.Seek(0, SeekOrigin.Begin);
        _image.Source = new Bitmap(stream);
        _canvas.Children.Add(_image);
    }

    private static SKBitmap GetBitmap(Stream stream) {
        stream.Seek(0, SeekOrigin.Begin);
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
