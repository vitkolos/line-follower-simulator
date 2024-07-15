using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using Bitmap = System.Drawing.Bitmap;
using System.Net.Http;

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

        using (MemoryStream stream = GetStreamFromPath(path)) {
            DrawMap(stream, size);
            Bitmap bitmap = new(new Bitmap(stream));
            BoolBitmap = new BoolBitmap(bitmap);
        }

        Scale = GetMapScale(size);
    }

    private void DrawMap(Stream stream, float mapSize) {
        _image.MaxHeight = mapSize;
        _image.MaxWidth = mapSize;
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = stream;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();
        _image.Source = bitmapImage;
        _canvas.Children.Add(_image);
    }

    private static MemoryStream GetStreamFromPath(string path) {
        var uri = new Uri(path);
        using Stream sourceStream = uri.IsFile ? File.OpenRead(path) : HttpClient.GetStreamAsync(path).Result;
        var memoryStream = new MemoryStream();
        sourceStream.CopyTo(memoryStream);
        return memoryStream;
    }

    private void PrepareCanvas(float size, float zoom) {
        _canvas.Height = size;
        _canvas.Width = size;
        _canvas.RenderTransform = new ScaleTransform {
            ScaleX = zoom,
            ScaleY = zoom,
            CenterX = size / 2,
            CenterY = 0
        };
    }

    private float GetMapScale(float mapSize) {
        return mapSize / Math.Max(BoolBitmap.Width, BoolBitmap.Height);
    }

    public void Dispose() {
        _canvas.Children.Remove(_image);
        BoolBitmap.Dispose();
    }
}
