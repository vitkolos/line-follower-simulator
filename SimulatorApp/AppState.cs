using System.Runtime.Loader;
using System.Reflection;
using System.IO;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using CoreLibrary;

namespace SimulatorApp;

class AppState {
    private readonly Window _window;
    private readonly Canvas _canvas;
    private readonly Panel _internalStateContainer;
    private readonly ContentControl _stateButton;
    private readonly ContentControl _assemblyLabel;
    public Map? Map { get; private set; }
    public RobotSetup RobotSetup { get; set; }
    public bool LiveSimulationRunning => _simulationLive?.Running ?? false;
    public bool ParallelSimulationRunning => _simulationParallel is not null;
    private Type _robotType;
    private SimulationLive? _simulationLive;
    private SimulationParallel? _simulationParallel;
    private IReadOnlyList<Polyline> _oldTrajectories;
    private AssemblyLoadContext? _assemblyLoadContext;
    private VisibleTrajectoriesState _visibleTrajectories;
    public VisibleTrajectoriesState VisibleTrajectories {
        get => _visibleTrajectories;
        set {
            _visibleTrajectories = value;
            TrajectoryButtonsChange(value);
        }
    }
    public event Action<VisibleTrajectoriesState> TrajectoryButtonsChange = _ => { };

    public enum VisibleTrajectoriesState { None, Live, Parallel }

    public AppState(Window window, Canvas canvas, Panel internalStateContainer, ContentControl stateButton, ContentControl assemblyLabel) {
        _window = window;
        _canvas = canvas;
        _internalStateContainer = internalStateContainer;
        _stateButton = stateButton;
        _assemblyLabel = assemblyLabel;
        _robotType = typeof(DummyRobot);
        _oldTrajectories = [];
        VisibleTrajectories = VisibleTrajectoriesState.None;
    }

    private void ShowMessageBox(string title, string content) {
        MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams {
            ContentTitle = title,
            ContentMessage = content,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            MaxWidth = Math.Min(_window.Width * 0.95, 800),
            CanResize = true
        }).ShowWindowDialogAsync(_window);
    }

    public async void LoadMap(string imagePath, float zoom, float size, ProgressBar progressBar) {
        _simulationLive?.Dispose(); // prevents bitmap reading conflicts
        ClearTrajectories();
        Map?.Dispose();
        progressBar.IsVisible = true;
        Stream? stream = null;

        try {
            stream = await Map.StreamFromPathAsync(imagePath);
            Map = new Map(_canvas, stream, size, zoom);
        } catch (Exception exception) {
            Map = null;
            ShowMessageBox("Map Loading Failed", exception.Message);
        } finally {
            stream?.Dispose();
        }

        progressBar.IsVisible = false;
    }

    public void LoadAssembly(string assemblyPath) {
        _assemblyLoadContext?.Unload();
        _assemblyLoadContext = new AssemblyLoadContext(null, true);
        Assembly assembly;
        IEnumerable<Type> robotTypes;
        Exception? exceptionThrown = null;

        try {
            using (var stream = File.OpenRead(assemblyPath)) {
                assembly = _assemblyLoadContext.LoadFromStream(stream);
            }

            robotTypes = from type in assembly.GetTypes() where type.BaseType == typeof(RobotBase) select type;
        } catch (Exception exception) {
            robotTypes = [];
            exceptionThrown = exception;
        }

        _robotType = typeof(DummyRobot);

        if (exceptionThrown is not null) {
            ShowMessageBox("Assembly Loading Failed", exceptionThrown.Message);
        } else if (!robotTypes.Any()) {
            ShowMessageBox("Warning", $"There is no class deriving from RobotBase, a {_robotType.Name} will be used instead.");
        } else if (robotTypes.Skip(1).Any()) {
            _robotType = robotTypes.First();
            ShowMessageBox("Warning", $"There are multiple classes deriving from RobotBase, the first one ({_robotType.FullName}) will be used.");
        }

        _robotType = robotTypes.FirstOrDefault(typeof(DummyRobot));
        string time = DateTime.Now.ToString("T");
        _assemblyLabel.Content = _robotType == typeof(DummyRobot) ? "" : $"{_robotType.FullName} loaded at {time}";
    }

    public void InitializeLiveSimulation() {
        _simulationLive?.Dispose();

        if (Map is not null) {
            _simulationLive = new SimulationLive(_canvas, Map, _robotType, RobotSetup, _internalStateContainer);
            _simulationLive.StateChange += running => _stateButton.Content = running ? "Pause" : "Run";
        }
    }

    public void ToggleSimulation() {
        if (_simulationLive is not null) {
            if (LiveSimulationRunning) {
                _simulationLive.Pause();
            } else {
                _simulationLive.Run();
            }
        }
    }

    private void ClearTrajectories() {
        foreach (Polyline trajectory in _oldTrajectories) {
            _canvas.Children.Remove(trajectory);
        }

        _oldTrajectories = [];
        VisibleTrajectories = VisibleTrajectoriesState.None;
    }

    public void DrawTrajectory() {
        var previouslyVisible = VisibleTrajectories;
        ClearTrajectories();

        if (_simulationLive is not null && previouslyVisible != VisibleTrajectoriesState.Live) {
            _oldTrajectories = [_simulationLive.DrawTrajectory()];
            VisibleTrajectories = VisibleTrajectoriesState.Live;
        }
    }

    public RobotPosition? GetRobotPosition() => _simulationLive?.RobotPosition;

    public async void SimulateParallel(ProgressBar progressBar) {
        var previouslyVisible = VisibleTrajectories;
        ClearTrajectories();

        if (_simulationParallel is not null) {
            // cancel an ongoing simulation
            _simulationParallel.Dispose();
            _simulationParallel = null;
        } else if (Map is not null && previouslyVisible != VisibleTrajectoriesState.Parallel) {
            _simulationLive?.Pause(); // prevents bitmap reading conflicts
            progressBar.IsVisible = true;
            _simulationParallel = new SimulationParallel(_canvas, Map, _robotType, RobotSetup);
            // simulation can be cancelled by setting to null, therefore we have to check
            TrajectoryButtonsChange(VisibleTrajectories);

            await Task.Run(() => {
                _simulationParallel?.Prepare();
                _simulationParallel?.Run();
            });

            if (_simulationParallel is null) {
                _oldTrajectories = [];
                VisibleTrajectories = VisibleTrajectoriesState.None;
            } else {
                _oldTrajectories = _simulationParallel.DrawTrajectories();
                VisibleTrajectories = VisibleTrajectoriesState.Parallel;

                if (_robotType == typeof(DummyRobot)) {
                    ShowMessageBox("Notice", $"You need to load an assembly with a valid RobotBase child. \nCurrently, a {_robotType.Name} is used instead.");
                } else if (!_simulationParallel.AnyRobotMoved) {
                    ShowMessageBox("Notice", "It seems that no simulated robots have moved. Make sure that after clicking Initialize and Run, the robot starts to move on its own. To use parallel simulation, no user action (e.g. pressing a “hardware” button) should be required for the robot to start driving.");
                }
            }

            progressBar.IsVisible = false; // cannot be bound on StateChanged (UI can only be modified in the main thread)
            _simulationParallel = null;
        }
    }
}
