using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using Bitmap = System.Drawing.Bitmap;

namespace SimulatorApp;

class Map : IDisposable {
    private readonly Canvas _canvas;
    public float Scale { get; private set; }
    public float Size { get; private set; }
    public BoolBitmap BoolBitmap;
    private readonly Image _image;

    public Map(Canvas canvas, string path, float size, float zoom) {
        _canvas = canvas;
        Size = size;
        PrepareCanvas(size, zoom);
        _image = new Image();
        BitmapImage bitmapImage = DrawMap(path, size);
        Bitmap bitmap = LoadBitmap(bitmapImage);
        BoolBitmap = new BoolBitmap(bitmap);
        Scale = GetMapScale(size);
    }

    private BitmapImage DrawMap(string mapFilePath, float mapSize) {
        _image.MaxHeight = mapSize;
        _image.MaxWidth = mapSize;
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.UriSource = new Uri(mapFilePath);
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        bitmapImage.EndInit();
        _image.Source = bitmapImage;
        _canvas.Children.Add(_image);
        return bitmapImage;
    }

    private static Bitmap LoadBitmap(BitmapImage bitmapImage) {
        // source: https://stackoverflow.com/questions/6484357/converting-bitmapimage-to-bitmap-and-vice-versa
        using (var outStream = new MemoryStream()) {
            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmapImage));
            enc.Save(outStream);
            var bitmap = new Bitmap(outStream);
            return new Bitmap(bitmap);
        }
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
