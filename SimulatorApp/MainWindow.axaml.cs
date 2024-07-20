namespace SimulatorApp;

using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();

        _defaultValues = new Dictionary<TextBox, string>() {
            {TrackFileName, @"C:\Users\vitko\Downloads\track.png"},
            {CanvasSize, "800"},
            {CanvasZoom, 0.9f.ToString()},
            {RobotX, "100"},
            {RobotY, "100"},
            {RobotRotation, "45"},
            {AssemblyFileName, @"D:\OneDrive - Univerzita Karlova\Code\Csharp\semestr4\line-follower\UserDefinedRobot\bin\Release\net8.0\UserDefinedRobot.dll"},
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
        _appState.VisibleTrajectoriesChange += UpdateTrajectoryButtons;
        UpdateTrajectoryButtons(_appState.VisibleTrajectories);
    }

    private readonly AppState _appState;

    private readonly Dictionary<TextBox, string> _defaultValues;

    private void UpdateTrajectoryButtons(AppState.VisibleTrajectoriesState trajectoriesState) {
        LiveTrajectoryButton.Content =
            trajectoriesState == AppState.VisibleTrajectoriesState.Live ? "Hide Trajectory" : "Draw Trajectory";
        ParallelButton.Content =
            trajectoriesState == AppState.VisibleTrajectoriesState.Parallel ? "Clear" : "Run";
    }

    private void WriteDefaultValues() {
        foreach (var entry in _defaultValues) {
            entry.Key.Text = entry.Value;
        }
    }

    private string GetTextBoxValue(TextBox textBox) => textBox.Text!;

    private float GetTextBoxFloat(TextBox textBox) {
        string text = textBox.Text!;
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

    private async Task<string?> OpenFilePickerAsync(string title, IReadOnlyList<FilePickerFileType>? fileTypeFilter) {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileTypeFilter
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async void BrowseTrack(object sender, RoutedEventArgs e) {
        string? fileName = await OpenFilePickerAsync("Open Track File", [FilePickerFileTypes.ImageAll]);

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
        _appState.LoadMap(imagePath, zoom, size);
    }

    private async void BrowseAssembly(object sender, RoutedEventArgs e) {
        string? fileName = await OpenFilePickerAsync("Open Assembly", [new FilePickerFileType("Assemblies") { Patterns = ["*.dll"] }]);

        if (fileName is not null) {
            AssemblyFileName.Text = fileName;
            LoadAssembly(sender, e);
        }
    }

    private void LoadAssembly(object sender, RoutedEventArgs e) {
        string assemblyPath = GetTextBoxValue(AssemblyFileName);
        _appState.LoadAssembly(assemblyPath);
    }

    private void CanvasClicked(object sender, PointerPressedEventArgs e) {
        PointerPoint point = e.GetCurrentPoint(sender as Control);

        if (point.Properties.IsLeftButtonPressed && _appState.Map is not null) {
            // #coordinates
            SetCoordinateTextBoxes((float)point.Position.X, _appState.Map.Size - (float)point.Position.Y);

            if (!_appState.SimulationRunning) {
                LoadRobotSetupFromControls();
                _appState.InitializeLiveSimulation();
            }
        }
    }

    private void CanvasMouseWheel(object sender, PointerWheelEventArgs e) {
        PointerPoint point = e.GetCurrentPoint(sender as Control);

        if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed) {
            if (_appState.Map is not null && !_appState.SimulationRunning) {
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

        if (!_appState.SimulationRunning) {
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
        _appState.SimulateParallel(ProgressBar);
    }
}
