using SkiaSharp;

namespace SimulatorApp;

class BoolBitmap : IDisposable {
    private readonly bool[] _boolArray;
    private readonly SKBitmap? _bitmap;
    public int Height { get; init; }
    public int Width { get; init; }
    public bool Cached { get; private set; }

    public BoolBitmap(BoolBitmap boolBitmap) {
        if (!boolBitmap.Cached) {
            throw new ArgumentException("BoolBitmap is not cached");
        }

        _bitmap = null;
        Height = boolBitmap.Height;
        Width = boolBitmap.Width;
        _boolArray = (bool[])boolBitmap._boolArray.Clone();
        Cached = boolBitmap.Cached;
    }

    public BoolBitmap(SKBitmap bitmap) {
        _bitmap = bitmap;
        Height = bitmap.Height;
        Width = bitmap.Width;
        _boolArray = new bool[Height * Width];
        Cached = false;
    }

    public void PopulateCache() {
        if (!Cached) {
            if (_bitmap is null) {
                throw new NullReferenceException("bitmap is null");
            }

            for (int i = 0; i < Height; i++) {
                for (int j = 0; j < Width; j++) {
                    _boolArray[i * Width + j] = GetPixel(j, i);
                }
            }

            Cached = true;
        }
    }

    private bool GetPixel(int x, int y) {
        if (_bitmap is null) {
            throw new NullReferenceException("bitmap is null");
        }

        SKColor color = _bitmap.GetPixel(x, y);
        // returns true for white, false for black
        return (color.Red + color.Green + color.Blue) > 380;
    }

    public bool this[int x, int y] {
        get {
            if (x >= 0 && x < Width && y >= 0 && y < Height) {
                return Cached ? _boolArray[y * Width + x] : GetPixel(x, y);
            } else {
                throw new ArgumentOutOfRangeException("bitmap does not contain this pixel");
            }
        }
    }

    public void Dispose() {
        _bitmap?.Dispose();
    }
}
