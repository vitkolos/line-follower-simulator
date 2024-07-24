using SkiaSharp;
using SimulatorApp;

namespace TestSuite;

public class BoolBitmapTests {
    [Fact]
    public void Dimensions() {
        // Arrange
        int width = 5;
        int height = 10;
        var bitmap = new SKBitmap(width, height);

        // Act
        var boolBitmap = new BoolBitmap(bitmap);

        // Assert
        Assert.Equal(height, boolBitmap.Height);
        Assert.Equal(width, boolBitmap.Width);
    }

    [Fact]
    public void Cached() {
        var bitmap = new SKBitmap(5, 15);
        var boolBitmap = new BoolBitmap(bitmap);

        Assert.False(boolBitmap.Cached);
        boolBitmap.PopulateCache();
        Assert.True(boolBitmap.Cached);
        boolBitmap.PopulateCache();
        Assert.True(boolBitmap.Cached);
    }

    [Fact]
    public void Copy_WithoutCaching() {
        var bitmap = new SKBitmap(2, 3);
        var boolBitmap = new BoolBitmap(bitmap);
        Assert.Throws<ArgumentException>(() => new BoolBitmap(boolBitmap));
    }

    [Fact]
    public void Copy_WithCaching() {
        int width = 5;
        int height = 10;
        var bitmap = new SKBitmap(width, height);

        var boolBitmap = new BoolBitmap(bitmap);
        boolBitmap.PopulateCache();
        var boolBitmapCopy = new BoolBitmap(boolBitmap);

        Assert.True(boolBitmapCopy.Cached);
        Assert.Equal(width, boolBitmapCopy.Width);
        Assert.Equal(height, boolBitmapCopy.Height);
    }

    [Theory]
    [InlineData(10, 20)]
    [InlineData(5, 25)]
    [InlineData(15, 2)]
    [InlineData(-1, 2)]
    [InlineData(1, -2)]
    public void ReadPixel_Outside(int x, int y) {
        var bitmap = new SKBitmap(10, 20);
        var boolBitmap = new BoolBitmap(bitmap);

        Assert.Throws<ArgumentOutOfRangeException>(() => boolBitmap[x, y]);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(9, 19)]
    [InlineData(5, 3)]
    public void ReadPixel_Inside(int x, int y) {
        var bitmap = new SKBitmap(10, 20);
        var boolBitmap = new BoolBitmap(bitmap);
        _ = boolBitmap[x, y];
    }

    public static IEnumerable<object[]> GetColors(bool light) {
        return light ? [
            [SKColors.White],
            [SKColors.LightYellow],
            [SKColors.Yellow],
            [SKColors.AliceBlue],
        ] : [
            [SKColors.Black],
            [SKColors.DarkBlue],
            [SKColors.DarkRed],
        ];
    }

    [Theory]
    [MemberData(nameof(GetColors), false)]
    public void ReadPixel_Dark(SKColor darkColor) {
        var bitmap = new SKBitmap(1, 2);
        var boolBitmap = new BoolBitmap(bitmap);

        bitmap.SetPixel(0, 1, darkColor);

        Assert.False(boolBitmap[0, 1]);
    }

    [Theory]
    [MemberData(nameof(GetColors), true)]
    public void ReadPixel_Light(SKColor lightColor) {
        var bitmap = new SKBitmap(1, 2);
        var boolBitmap = new BoolBitmap(bitmap);

        bitmap.SetPixel(0, 1, lightColor);

        Assert.True(boolBitmap[0, 1]);
    }

    [Fact]
    public void ReadPixel_NoCache() {
        var bitmap = new SKBitmap(1, 2);
        var boolBitmap = new BoolBitmap(bitmap);

        bitmap.SetPixel(0, 1, SKColors.White);
        bitmap.SetPixel(0, 1, SKColors.Black);

        Assert.False(boolBitmap[0, 1]);
    }

    [Fact]
    public void ReadPixel_Cached() {
        var bitmap = new SKBitmap(1, 2);
        var boolBitmap = new BoolBitmap(bitmap);

        bitmap.SetPixel(0, 1, SKColors.White);
        boolBitmap.PopulateCache();
        bitmap.SetPixel(0, 1, SKColors.Black);

        Assert.True(boolBitmap[0, 1]);
    }

    [Fact]
    public void CheckCoordinatesOrder() {
        var bitmap = new SKBitmap(5, 5);
        var boolBitmap = new BoolBitmap(bitmap);

        bitmap.SetPixel(2, 3, SKColors.White);
        bitmap.SetPixel(3, 2, SKColors.Black);

        Assert.True(boolBitmap[2, 3]);
        Assert.False(boolBitmap[3, 2]);
    }
}
