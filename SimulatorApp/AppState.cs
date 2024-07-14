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
    public bool SimulationRunning => _realTimeSimulation?.Running ?? false;
    private Type _robotType;
    private RealTimeSimulation? _realTimeSimulation;
    private Polyline? _oldTrajectory;
    public RobotSetup RobotSetup { get; set; }
    private AssemblyLoadContext? _assemblyLoadContext;

    public AppState(Canvas canvas, Panel pinControlsContainer, Button stateButton) {
        _canvas = canvas;
        _pinControlsContainer = pinControlsContainer;
        _stateButton = stateButton;
        _robotType = typeof(DummyRobot);
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

    public void InitializeRealtimeSimulation() {
        if (_realTimeSimulation is not null) {
            _realTimeSimulation.Dispose();
        }

        if (Map is not null) {
            var robot = (RobotBase)Activator.CreateInstance(_robotType)!;
            _realTimeSimulation = new RealTimeSimulation(_canvas, robot, RobotSetup, Map, _pinControlsContainer);
            _realTimeSimulation.StateChange += running => _stateButton.Content = running ? "Pause" : "Run";
        }
    }

    public void ToggleSimulation() {
        if (_realTimeSimulation is not null) {
            if (SimulationRunning) {
                _realTimeSimulation.Pause();
            } else {
                _realTimeSimulation.Run();
            }
        }
    }

    public void DrawTrajectory() {
        if (_oldTrajectory is not null) {
            _canvas.Children.Remove(_oldTrajectory);
            _oldTrajectory = null;
        } else if (_realTimeSimulation is not null) {
            _oldTrajectory = _realTimeSimulation.DrawTrajectory();
        }
    }

    public RobotPosition? GetRobotPosition() {
        if (_realTimeSimulation is not null) {
            return _realTimeSimulation.RobotPosition;
        } else {
            return null;
        }
    }
}
