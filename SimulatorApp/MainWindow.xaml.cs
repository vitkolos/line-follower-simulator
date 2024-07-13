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
    private RealTimeSimulation? _realTimeSimulation;
    private Polyline? _oldPolyline;
    private float _scaleIcons;
    private float _scaleSpeed;
    private float _sensorOffset;

    private void LoadMap(string imagePath) {
        var canvas = (Canvas)FindName("Canvas");
        float zoom = 1f;
        float maxDimension = 800f;
        _scaleIcons = 4f;
        _scaleSpeed = 1.5f;
        _sensorOffset = 10f;
        _map = new Map(canvas, imagePath, maxDimension, zoom);
    }

    private void CanvasClicked(object sender, MouseEventArgs e) {
        var canvas = (Canvas)sender;
        var pinControlsContainer = (Panel)FindName("Pins");
        Point positionClicked = e.GetPosition(canvas);
        var robotPosition = new RobotPosition((float)positionClicked.X, (float)-positionClicked.Y, 0);

        if (_oldPolyline is not null) {
            canvas.Children.Remove(_oldPolyline);
        }

        if (_realTimeSimulation is not null) {
            _oldPolyline = _realTimeSimulation.DrawTrajectory();
            _realTimeSimulation.Dispose();
        }

        if (_map is not null) {
            _realTimeSimulation = new RealTimeSimulation(canvas, new Robot(), robotPosition, _map, pinControlsContainer, _scaleIcons, _scaleSpeed, _sensorOffset);
            _realTimeSimulation.Run();
        }
    }

    private void BrowseTrack(object sender, EventArgs e) {
        var dialog = new Microsoft.Win32.OpenFileDialog();
        bool? result = dialog.ShowDialog();

        if (result == true) {
            string filename = dialog.FileName;
            Console.WriteLine(filename);
        }
    }

    private void ShowTrack(object sender, EventArgs e) { }
    private void ApplyCanvas(object sender, EventArgs e) { }
    private void BrowseAssembly(object sender, EventArgs e) { }
    private void LoadAssembly(object sender, EventArgs e) { }
    private void ApplyRobot(object sender, EventArgs e) { }
    private void NewSimulation(object sender, EventArgs e) { }
    private void ToggleSimulation(object sender, EventArgs e) { }
    private void DrawTrajectory(object sender, EventArgs e) { }
    private void SimulateParallel(object sender, EventArgs e) { }
}
