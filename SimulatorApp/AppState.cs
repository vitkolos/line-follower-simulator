using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Runtime.Loader;
using System.Reflection;

using CoreLibrary;

namespace SimulatorApp;

class AppState {
    private readonly Canvas _canvas;
    private readonly Panel _pinControlsContainer;
    private readonly Button _stateButton;
    public Map? Map;
    public bool SimulationRunning => _simulationLive?.Running ?? false;
    private Type _robotType;
    private SimulationLive? _simulationLive;
    private SimulationParallel? _simulationParallel;
    private IReadOnlyList<Polyline> _oldTrajectories;
    public RobotSetup RobotSetup { get; set; }
    private AssemblyLoadContext? _assemblyLoadContext;

    public AppState(Canvas canvas, Panel pinControlsContainer, Button stateButton) {
        _canvas = canvas;
        _pinControlsContainer = pinControlsContainer;
        _stateButton = stateButton;
        _robotType = typeof(DummyRobot);
        _oldTrajectories = Array.Empty<Polyline>();
    }

    public void LoadMap(string imagePath, float zoom, float size) {
        if (Map is not null) {
            Map.Dispose();
        }

        Map = new Map(_canvas, imagePath, size, zoom);
    }

    public void LoadAssembly(string assemblyPath) {
        _assemblyLoadContext?.Unload();
        _assemblyLoadContext = new AssemblyLoadContext(null, true);
        Assembly assembly;

        using (var stream = System.IO.File.OpenRead(assemblyPath)) {
            assembly = _assemblyLoadContext.LoadFromStream(stream);
        }

        IEnumerable<Type> robotTypes = from type in assembly.GetTypes() where type.BaseType == typeof(RobotBase) select type;
        _robotType = robotTypes.FirstOrDefault(typeof(DummyRobot));
    }

    public void InitializeLiveSimulation() {
        _simulationLive?.Dispose();

        if (Map is not null) {
            _simulationLive = new SimulationLive(_canvas, Map, _robotType, RobotSetup, _pinControlsContainer);
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
        foreach (var item in _oldTrajectories) {
            _canvas.Children.Remove(item);
        }

        _oldTrajectories = [];
    }

    public void DrawTrajectory() {
        if (_oldTrajectories.Count > 0) {
            ClearTrajectories();
        } else if (_simulationLive is not null) {
            _oldTrajectories = _simulationLive.DrawTrajectories();
        }
    }

    public RobotPosition? GetRobotPosition() => _simulationLive?.RobotPosition;

    public async void SimulateParallel(ProgressBar progressBar) {
        if (_oldTrajectories.Count > 0) {
            ClearTrajectories();
        } else if (_simulationParallel is not null) {
            _simulationParallel.Dispose();
            _simulationParallel = null;
        } else if (Map is not null) {
            progressBar.Visibility = Visibility.Visible;
            _simulationParallel = new SimulationParallel(_canvas, Map, _robotType, RobotSetup);

            await Task.Run(() => {
                _simulationParallel?.Prepare();
                _simulationParallel?.Run();
            });

            _oldTrajectories = _simulationParallel?.DrawTrajectories() ?? [];
            progressBar.Visibility = Visibility.Hidden;
            _simulationParallel = null;
        }
    }
}
