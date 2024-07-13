using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using Bitmap = System.Drawing.Bitmap;

using CoreLibrary;

namespace SimulatorApp;

class Map : IDisposable {
    private readonly Canvas _canvas;
    public float Scale { get; private set; }
    public Bitmap Bitmap;
    private readonly Image _image;

    public Map(Canvas canvas, string path, float size, float zoom) {
        _canvas = canvas;
        PrepareCanvas(size, zoom);
        _image = new Image();
        DrawMap(path, size);
        Bitmap = LoadBitmap(path);
        Scale = GetMapScale(size);
    }

    private void DrawMap(string mapFilePath, float mapSize) {
        _image.MaxHeight = mapSize;
        _image.MaxWidth = mapSize;
        _image.Source = new BitmapImage(new Uri(mapFilePath));
        _canvas.Children.Add(_image);
    }

    private static Bitmap LoadBitmap(string mapFilePath) {
        Bitmap bmp;

        using (var stream = System.IO.File.OpenRead(mapFilePath)) {
            bmp = new Bitmap(stream);
        }

        return bmp;
    }

    private void PrepareCanvas(float size, float zoom) {
        _canvas.Height = size;
        _canvas.Width = size;
        _canvas.RenderTransform = new ScaleTransform {
            ScaleX = zoom,
            ScaleY = zoom,
            CenterX = size / 2,
            CenterY = size / 2
        };
    }

    private float GetMapScale(float mapSize) {
        if (Math.Max(Bitmap.Width, Bitmap.Height) > mapSize) {
            return mapSize / Math.Max(Bitmap.Width, Bitmap.Height);
        } else {
            return 1f;
        }
    }

    public void Dispose() {
        _canvas.Children.Remove(_image);
        Bitmap.Dispose();
    }
}
