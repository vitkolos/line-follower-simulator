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

        WriteDefaultValues();
        var canvas = (Canvas)FindName("Canvas");
        var pinControlsContainer = (Panel)FindName("Pins");
        var stateButton = (Button)FindName("StateButton");
        _appState = new AppState(canvas, pinControlsContainer, stateButton);
        LoadRobotSetupFromControls();
    }

    private readonly AppState _appState;

    private readonly Dictionary<string, string> _defaultValues = new Dictionary<string, string> {
        {"TrackFileName", @"C:\Users\vitko\Downloads\track.png"},
        {"CanvasSize", "800"},
        {"CanvasZoom", "1"},
        {"RobotX", "100"},
        {"RobotY", "100"},
        {"RobotRotation", "45"},
        {"AssemblyFileName", @"D:\OneDrive - Univerzita Karlova\Code\Csharp\semestr4\line-follower\UserDefinedRobot\bin\Release\net8.0\UserDefinedRobot.dll"},
        {"RobotSize", "4"},
        {"SensorDistance", "10"},
        {"RobotSpeed", 1.5f.ToString()},
    };

    private void WriteDefaultValues() {
        foreach (KeyValuePair<string, string> entry in _defaultValues) {
            ((TextBox)FindName(entry.Key)).Text = entry.Value;
        }
    }

    private string GetTextBoxValue(string name) => ((TextBox)FindName(name)).Text;

    private float GetTextBoxFloat(string name) {
        var tb = (TextBox)FindName(name);
        string text = tb.Text;
        bool result = float.TryParse(text, out float value);

        if (result) {
            tb.Text = value.ToString();
            return value;
        } else {
            tb.Text = _defaultValues[name];
            return GetTextBoxFloat(name);
        }
    }

    private void SetPositionByMouse(MouseEventArgs e) {
        if (_appState.Map is not null) {
            var canvas = (Canvas)FindName("Canvas");
            Point positionClicked = e.GetPosition(canvas);
            SetCoordinateTextBoxes((float)positionClicked.X, _appState.Map.Size - (float)positionClicked.Y);
        }
    }

    private void SetCoordinateTextBoxes(float x, float y, float? rotation = null) {
        ((TextBox)FindName("RobotX")).Text = double.Round(x, 2).ToString();
        ((TextBox)FindName("RobotY")).Text = double.Round(y, 2).ToString();

        if (rotation is not null) {
            ((TextBox)FindName("RobotRotation")).Text = double.Round(rotation.Value / Math.PI * 180, 2).ToString();
        }
    }

    private void LoadRobotSetupFromControls() {
        _appState.RobotSetup = new RobotSetup {
            Position = new RobotPosition {
                X = GetTextBoxFloat("RobotX"),
                Y = GetTextBoxFloat("RobotY"),
                Rotation = (float)(GetTextBoxFloat("RobotRotation") / 180 * Math.PI)
            },
            Config = new RobotConfig {
                Size = GetTextBoxFloat("RobotSize"),
                SensorDistance = GetTextBoxFloat("SensorDistance"),
                Speed = GetTextBoxFloat("RobotSpeed")
            }
        };
    }

    private void BrowseTrack(object sender, EventArgs e) {
        var dialog = new Microsoft.Win32.OpenFileDialog();
        bool? result = dialog.ShowDialog();

        if (result == true) {
            ((TextBox)FindName("TrackFileName")).Text = dialog.FileName;
            ShowTrack(sender, e);
        }
    }

    private void ShowTrack(object sender, EventArgs e) {
        string imagePath = GetTextBoxValue("TrackFileName");
        float zoom = GetTextBoxFloat("CanvasZoom");
        float size = GetTextBoxFloat("CanvasSize");
        ((Panel)FindName("CanvasContainer")).Height = size * zoom;
        _appState.LoadMap(imagePath, zoom, size);
    }

    private void BrowseAssembly(object sender, EventArgs e) {
        var dialog = new Microsoft.Win32.OpenFileDialog {
            Filter = "Assemblies (.dll)|*.dll"
        };
        bool? result = dialog.ShowDialog();

        if (result == true) {
            ((TextBox)FindName("AssemblyFileName")).Text = dialog.FileName;
            LoadAssembly(sender, e);
        }
    }

    private void LoadAssembly(object sender, EventArgs e) {
        string assemblyPath = GetTextBoxValue("AssemblyFileName");
        _appState.LoadAssembly(assemblyPath);
    }

    private void CanvasClicked(object sender, MouseEventArgs e) {
        SetPositionByMouse(e);

        if (!_appState.SimulationRunning) {
            LoadRobotSetupFromControls();
            _appState.InitializeLiveSimulation();
        }
    }

    private void ShowRobot(object sender, EventArgs e) {
        LoadRobotSetupFromControls();
        _appState.InitializeLiveSimulation();
    }

    private void NewSimulation(object sender, EventArgs e) {
        _appState.InitializeLiveSimulation();
    }

    private void ToggleSimulation(object sender, EventArgs e) {
        _appState.ToggleSimulation();

        if (!_appState.SimulationRunning) {
            RobotPosition? rp = _appState.GetRobotPosition();

            if (rp is not null) {
                SetCoordinateTextBoxes(rp.Value.X, rp.Value.Y, rp.Value.Rotation);
            }
        }
    }
    private void DrawTrajectory(object sender, EventArgs e) {
        _appState.DrawTrajectory();
    }
    private void SimulateParallel(object sender, EventArgs e) { }
}
