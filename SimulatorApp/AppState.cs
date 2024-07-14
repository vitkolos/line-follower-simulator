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
    public bool SimulationRunning => _liveSimulation?.Running ?? false;
    private Type _robotType;
    private LiveSimulation? _liveSimulation;
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
        if (_liveSimulation is not null) {
            _liveSimulation.Dispose();
        }

        if (Map is not null && Map.BoolBitmap is not null) {
            var robot = (RobotBase)Activator.CreateInstance(_robotType)!;
            _liveSimulation = new LiveSimulation(_canvas, robot, RobotSetup, Map, _pinControlsContainer);
            _liveSimulation.StateChange += running => _stateButton.Content = running ? "Pause" : "Run";
        }
    }

    public void ToggleSimulation() {
        if (_liveSimulation is not null) {
            if (SimulationRunning) {
                _liveSimulation.Pause();
            } else {
                _liveSimulation.Run();
            }
        }
    }

    public void DrawTrajectory() {
        if (_oldTrajectories.Count > 0) {
            foreach (var item in _oldTrajectories) {
                _canvas.Children.Remove(item);
            }

            _oldTrajectories = Array.Empty<Polyline>();
        } else if (_liveSimulation is not null) {
            _oldTrajectories = [_liveSimulation.DrawTrajectory()];
        }
    }

    public RobotPosition? GetRobotPosition() {
        if (_liveSimulation is not null) {
            return _liveSimulation.RobotPosition;
        } else {
            return null;
        }
    }

    public void SimulateParallel() {
        if (_oldTrajectories.Count > 0) {
            foreach (var item in _oldTrajectories) {
                _canvas.Children.Remove(item);
            }

            _oldTrajectories = Array.Empty<Polyline>();
        } else if (Map is not null && Map.BoolBitmap is not null) {
            var sim = new ParallelSimulation(_canvas, _robotType, RobotSetup, Map);
            _oldTrajectories = sim.DrawTrajectories();
        }
    }
}
