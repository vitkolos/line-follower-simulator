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

        string imagePath = @"C:\Users\vitko\Downloads\track.png";
        LoadMap(imagePath);
    }

    private Map? _map;
    private RealTimeSimulation? _sim;
    private Polyline? _oldPolyline;
    private float _scaleIcons;
    private float _scaleSpeed;
    private float _sensorOffset;

    private void LoadMap(string imagePath) {
        var canvas = (Canvas)FindName("canvas");
        float zoom = 1f;
        float maxDimension = 800f;
        _scaleIcons = 4f;
        _scaleSpeed = 1.5f;
        _sensorOffset = 10f;
        _map = new Map(canvas, imagePath, maxDimension, zoom);
    }

    private void CanvasClicked(object sender, MouseEventArgs e) {
        var canvas = (Canvas)sender;
        Point positionClicked = e.GetPosition(canvas);
        var robotPosition = new RobotPosition((float)positionClicked.X, (float)-positionClicked.Y, 0);

        if (_oldPolyline is not null) {
            canvas.Children.Remove(_oldPolyline);
        }

        if (_sim is not null) {
            _oldPolyline = _sim.DrawTrajectory();
            _sim.Dispose();
        }

        if (_map is not null) {
            _sim = new RealTimeSimulation(canvas, new Robot(), robotPosition, _map, _scaleIcons, _scaleSpeed, _sensorOffset);
            _sim.Run();
        }
    }
}
