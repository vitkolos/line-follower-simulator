using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Runtime.Loader;
using System.Reflection;

using CoreLibrary;

namespace SimulatorApp;

class AppState {
    private readonly Canvas _canvas;
    private readonly Panel _internalStateContainer;
    private readonly ContentControl _stateButton;
    private readonly ContentControl _assemblyLabel;
    public Map? Map { get; private set; }
    public RobotSetup RobotSetup { get; set; }
    public bool SimulationRunning => _simulationLive?.Running ?? false;
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
            VisibleTrajectoriesChange(value);
        }
    }
    public event Action<VisibleTrajectoriesState> VisibleTrajectoriesChange = _ => { };

    public enum VisibleTrajectoriesState { None, Live, Parallel }

    public AppState(Canvas canvas, Panel internalStateContainer, ContentControl stateButton, ContentControl assemblyLabel) {
        _canvas = canvas;
        _internalStateContainer = internalStateContainer;
        _stateButton = stateButton;
        _assemblyLabel = assemblyLabel;
        _robotType = typeof(DummyRobot);
        _oldTrajectories = [];
        VisibleTrajectories = VisibleTrajectoriesState.None;
    }

    public void LoadMap(string imagePath, float zoom, float size) {
        _simulationLive?.Dispose(); // prevents bitmap reading conflicts
        ClearTrajectories();
        Map?.Dispose();

        try {
            Map = new Map(_canvas, imagePath, size, zoom);
        } catch (Exception exception) {
            Map = null;
            MessageBox.Show(exception.Message, "Map Loading Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void LoadAssembly(string assemblyPath) {
        _assemblyLoadContext?.Unload();
        _assemblyLoadContext = new AssemblyLoadContext(null, true);
        Assembly assembly;
        IEnumerable<Type> robotTypes;
        Exception? exceptionThrown = null;

        try {
            using (var stream = System.IO.File.OpenRead(assemblyPath)) {
                assembly = _assemblyLoadContext.LoadFromStream(stream);
            }

            robotTypes = from type in assembly.GetTypes() where type.BaseType == typeof(RobotBase) select type;
        } catch (Exception exception) {
            robotTypes = [];
            exceptionThrown = exception;
        }

        _robotType = typeof(DummyRobot);

        if (exceptionThrown is not null) {
            MessageBox.Show(exceptionThrown.Message, "Assembly Loading Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        } else if (!robotTypes.Any()) {
            MessageBox.Show($"There is no class deriving from RobotBase, using {_robotType.Name}.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        } else if (robotTypes.Skip(1).Any()) {
            _robotType = robotTypes.First();
            MessageBox.Show($"There are multiple classes deriving from RobotBase, using the first one ({_robotType.FullName}).", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _robotType = robotTypes.FirstOrDefault(typeof(DummyRobot));
        _assemblyLabel.Content = _robotType == typeof(DummyRobot) ? "" : $"{_robotType.FullName} loaded";
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
            if (SimulationRunning) {
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
            progressBar.Visibility = Visibility.Visible;
            _simulationParallel = new SimulationParallel(_canvas, Map, _robotType, RobotSetup);
            // simulation can be cancelled by setting to null, therefore we have to check

            await Task.Run(() => {
                _simulationParallel?.Prepare();
                _simulationParallel?.Run();
            });

            _oldTrajectories = _simulationParallel?.DrawTrajectories() ?? [];
            VisibleTrajectories = VisibleTrajectoriesState.Parallel;
            progressBar.Visibility = Visibility.Hidden; // cannot be bound on StateChanged (UI can only be modified in the main thread)
            _simulationParallel = null;
        }
    }
}
