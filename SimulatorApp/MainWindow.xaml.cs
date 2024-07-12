using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Bitmap = System.Drawing.Bitmap;

using CoreLibrary;

using UserDefinedRobot;

namespace SimulatorApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        /*
            STEPS:
            - load a map
            - select the starting point and orientation
            - load an assembly with the robot (watch changes?)
            a) run simulation in real time
            b) run simulation in parallel, then display result
        */
        Image image = (Image)FindName("image");
        image.MaxHeight = _maxDimension;
        image.MaxWidth = _maxDimension;
        image.Source = new BitmapImage(new Uri(_imagePath));
        var st = new ScaleTransform {
            ScaleX = 3,
            ScaleY = 3
        };
        var canvas = (Canvas)FindName("canvas");
        canvas.RenderTransform = st;
    }

    private int _token = 0;
    private readonly string _imagePath = @"C:\Users\vitko\Downloads\track.png";
    private readonly float _maxDimension = 200f; // fixme (make robot larger)


    private async void RunRobot(RobotPosition initialPosition) {
        int token = ++_token;
        int centerX = 0;
        int centerY = 0;
        Path path = (Path)FindName("cursor");

        RotateTransform rotation = (RotateTransform)path.FindName("rotation");
        rotation.Angle = 0;

        int interval = 10;
        Bitmap bmp;

        using (var str = System.IO.File.OpenRead(_imagePath)) {
            bmp = new Bitmap(str);
        }


        float mapScale = 1f;

        if (Math.Max(bmp.Width, bmp.Height) > _maxDimension) {
            mapScale = _maxDimension / Math.Max(bmp.Width, bmp.Height);
        }

        var pr = new SimulatedRobot(new Robot(), initialPosition, bmp, mapScale);

        for (int i = 0; i < 800; i++) {
            await Task.Delay(interval);

            if (_token != token) {
                break;
            }

            pr.MoveNext(interval);
            Canvas.SetLeft(path, centerX + pr.Position.X);
            Canvas.SetTop(path, centerY - pr.Position.Y);
            rotation.Angle = -pr.Position.Rotation / Math.PI * 180;

            for (int j = 0; j < RobotBase.SensorsCount; j++) {
                Path sensor = (Path)FindName("sensor" + j);
                Canvas.SetLeft(sensor, centerX + pr.SensorPositions[j].X);
                Canvas.SetTop(sensor, centerY - pr.SensorPositions[j].Y);
                sensor.Stroke = pr.Robot.DigitalRead(pr.Robot.FirstSensorPin + j) ? Brushes.LightGreen : Brushes.Red;
            }
        }

        var history = pr.GetPositionHistory();
        var points = new PointCollection();

        foreach (var item in history) {
            points.Add(new Point(item.Position.X, -item.Position.Y));
        }

        Polyline polyline = (Polyline)FindName("polyline");
        polyline.Points = points;
    }

    private void TrackClicked(object sender, MouseEventArgs e) {
        var image = (Image)sender;
        System.Windows.Point positionClicked = e.GetPosition(image);
        // Console.WriteLine(bitmap.GetPixel((int)(positionClicked.X / image.ActualWidth * bitmap.Width), (int)(positionClicked.Y / image.ActualHeight * bitmap.Height)));
        RobotPosition robotPosition = new RobotPosition((float)positionClicked.X, (float)-positionClicked.Y, 0);
        RunRobot(robotPosition);
    }
}
