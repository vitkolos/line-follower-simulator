using System.Reflection;
using Path = System.IO.Path;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace SimulatorApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();

        _defaultValues = new Dictionary<TextBox, string>() {
            {TrackFileName, ""},
            {CanvasSize, "800"},
            {CanvasZoom, 0.9f.ToString()},
            {RobotX, "100"},
            {RobotY, "100"},
            {RobotRotation, "45"},
            {AssemblyFileName, ""},
            {RobotSize, "4"},
            {SensorDistance, "10"},
            {RobotSpeed, 1.5f.ToString()},
        };

        WriteDefaultValues();
        Canvas canvas = Canvas;
        Panel internalStateContainer = StatePanel;
        ContentControl stateButton = StateButton;
        ContentControl assemblyLabel = LoadedAssembly;
        _appState = new AppState(this, canvas, internalStateContainer, stateButton, assemblyLabel);
        LoadRobotSetupFromControls();
        _appState.TrajectoryButtonsChange += UpdateTrajectoryButtons;
        UpdateTrajectoryButtons(_appState.VisibleTrajectories);
    }

    private readonly AppState _appState;

    private readonly Dictionary<TextBox, string> _defaultValues;

    private void UpdateTrajectoryButtons(AppState.VisibleTrajectoriesState trajectoriesState) {
        LiveTrajectoryButton.Content =
            trajectoriesState == AppState.VisibleTrajectoriesState.Live ? "Hide Trajectory" : "Draw Trajectory";
        ParallelButton.Content =
            trajectoriesState == AppState.VisibleTrajectoriesState.Parallel ? "Clear" : (_appState.ParallelSimulationRunning ? "Stop" : "Run");
    }

    private void WriteDefaultValues() {
        foreach (var entry in _defaultValues) {
            entry.Key.Text = entry.Value;
        }
    }

    private string GetTextBoxValue(TextBox textBox) => textBox.Text ?? "";

    private float GetTextBoxFloat(TextBox textBox) {
        string text = textBox.Text ?? "";
        bool result = float.TryParse(text, out float value);

        if (result) {
            textBox.Text = value.ToString();
            return value;
        } else {
            textBox.Text = _defaultValues[textBox];
            return GetTextBoxFloat(textBox);
        }
    }

    private void SetCoordinateTextBoxes(float x, float y, float? rotation = null) {
        RobotX.Text = double.Round(x, 2).ToString();
        RobotY.Text = double.Round(y, 2).ToString();

        if (rotation is not null) {
            RobotRotation.Text = double.Round(rotation.Value / Math.PI * 180, 2).ToString();
        }
    }

    private void LoadRobotSetupFromControls() {
        _appState.RobotSetup = new RobotSetup {
            Position = new RobotPosition {
                X = GetTextBoxFloat(RobotX),
                Y = GetTextBoxFloat(RobotY),
                Rotation = (float)(GetTextBoxFloat(RobotRotation) / 180 * Math.PI)
            },
            Config = new RobotConfig {
                Size = GetTextBoxFloat(RobotSize),
                SensorDistance = GetTextBoxFloat(SensorDistance),
                Speed = GetTextBoxFloat(RobotSpeed)
            }
        };
    }

    private async Task<string?> OpenFilePickerAsync(string title, IReadOnlyList<FilePickerFileType>? fileTypeFilter, string path) {
        IStorageFolder? startLocation = await StorageProvider.TryGetFolderFromPathAsync(path);
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileTypeFilter,
            SuggestedStartLocation = startLocation
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async void BrowseTrack(object sender, RoutedEventArgs e) {
        string oldFileName = GetTextBoxValue(TrackFileName);
        string path = Path.GetDirectoryName(oldFileName) ?? "";

        if (path == "") {
            path = Assembly.GetExecutingAssembly().Location;

            for (int i = 0; i < 4; i++) {
                path = Path.GetDirectoryName(path) ?? "";
            }

            path = Path.Join(path, "Assets");
        }

        string? fileName = await OpenFilePickerAsync("Open Track File", [FilePickerFileTypes.ImageAll], path);

        if (fileName is not null) {
            TrackFileName.Text = fileName;
            ShowTrack(sender, e);
        }
    }

    private void ShowTrack(object sender, RoutedEventArgs e) {
        string imagePath = GetTextBoxValue(TrackFileName);
        float zoom = GetTextBoxFloat(CanvasZoom);
        float size = GetTextBoxFloat(CanvasSize);
        CanvasContainer.Height = size * zoom;
        _appState.LoadMap(imagePath, zoom, size, TrackProgressBar);
    }

    private async void BrowseAssembly(object sender, RoutedEventArgs e) {
        string oldFileName = GetTextBoxValue(AssemblyFileName);
        string path = Path.GetDirectoryName(oldFileName) ?? "";

        if (path == "") {
            path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string word = "SimulatorApp";
            int index = path.LastIndexOf(word);
            path = path.Substring(0, index) + "UserDefinedRobot" + path.Substring(index + word.Length);
            Console.WriteLine(path);
        }

        string? fileName = await OpenFilePickerAsync("Open Assembly", [new FilePickerFileType("Assemblies") { Patterns = ["*.dll"] }], path);

        if (fileName is not null) {
            AssemblyFileName.Text = fileName;
            LoadAssembly(sender, e);
        }
    }

    private void LoadAssembly(object sender, RoutedEventArgs e) {
        string assemblyPath = GetTextBoxValue(AssemblyFileName);
        _appState.LoadAssembly(assemblyPath);
        _appState.InitializeLiveSimulation();
    }

    private void CanvasClicked(object sender, PointerPressedEventArgs e) {
        PointerPoint point = e.GetCurrentPoint(sender as Control);

        if (point.Properties.IsLeftButtonPressed && _appState.Map is not null) {
            // #coordinates
            SetCoordinateTextBoxes((float)point.Position.X, _appState.Map.Size - (float)point.Position.Y);

            if (!_appState.LiveSimulationRunning) {
                LoadRobotSetupFromControls();
                _appState.InitializeLiveSimulation();
            }
        }
    }

    private void CanvasMouseWheel(object sender, PointerWheelEventArgs e) {
        PointerPoint point = e.GetCurrentPoint(sender as Control);

        if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed) {
            if (_appState.Map is not null && !_appState.LiveSimulationRunning) {
                e.Handled = true; // prevents default (scrolling)
                int scroll = (int)e.Delta.Y * 12;
                RobotRotation.Text = ((float)Math.Round(GetTextBoxFloat(RobotRotation)) + scroll).ToString();
                LoadRobotSetupFromControls();
                _appState.InitializeLiveSimulation();
            }
        }
    }

    private void ShowRobot(object sender, RoutedEventArgs e) {
        LoadRobotSetupFromControls();
        _appState.InitializeLiveSimulation();
    }

    private void NewSimulation(object sender, RoutedEventArgs e) {
        _appState.InitializeLiveSimulation();
    }

    private void ToggleSimulation(object sender, RoutedEventArgs e) {
        _appState.ToggleSimulation();

        if (!_appState.LiveSimulationRunning) {
            RobotPosition? rp = _appState.GetRobotPosition();

            if (rp is not null) {
                SetCoordinateTextBoxes(rp.Value.X, rp.Value.Y, rp.Value.Rotation);
            }
        }
    }

    private void DrawTrajectory(object sender, RoutedEventArgs e) {
        _appState.DrawTrajectory();
    }

    private void SimulateParallel(object sender, RoutedEventArgs e) {
        _appState.SimulateParallel(ParallelProgressBar);
    }
}
