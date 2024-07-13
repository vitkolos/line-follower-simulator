using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using Bitmap = System.Drawing.Bitmap;

using CoreLibrary;

namespace SimulatorApp;

class Map {
    private readonly Canvas _canvas;
    public float Scale { get; private set; }
    public Bitmap Bitmap;

    public Map(Canvas canvas, string path, float size) {
        _canvas = canvas;
        DrawMap(path, size);
        Bitmap = LoadBitmap(path);
        ScaleCanvas();
        Scale = GetMapScale(size);
    }

    private void DrawMap(string mapFilePath, float mapSize) {
        Image image = (Image)_canvas.FindName("image");
        image.MaxHeight = mapSize;
        image.MaxWidth = mapSize;
        image.Source = new BitmapImage(new Uri(mapFilePath));
    }

    private static Bitmap LoadBitmap(string mapFilePath) {
        Bitmap bmp;

        using (var stream = System.IO.File.OpenRead(mapFilePath)) {
            bmp = new Bitmap(stream);
        }

        return bmp;
    }

    private void ScaleCanvas() {
        _canvas.RenderTransform = new ScaleTransform {
            ScaleX = 3,
            ScaleY = 3
        };
    }

    private float GetMapScale(float mapSize) {
        if (Math.Max(Bitmap.Width, Bitmap.Height) > mapSize) {
            return mapSize / Math.Max(Bitmap.Width, Bitmap.Height);
        } else {
            return 1f;
        }
    }
}
