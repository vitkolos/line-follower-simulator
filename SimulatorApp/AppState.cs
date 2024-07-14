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

using UserDefinedRobot; // fixme

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
    private RobotSetup _robotSetup;

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
        // todo
        _robotType = typeof(Robot);
    }

    public void InitializeRealtimeSimulation() {
        if (_realTimeSimulation is not null) {
            _realTimeSimulation.Dispose();
        }

        if (Map is not null) {
            var robot = (RobotBase)Activator.CreateInstance(_robotType)!;
            _realTimeSimulation = new RealTimeSimulation(_canvas, robot, _robotSetup, Map, _pinControlsContainer);
            _realTimeSimulation.StateChange += running => { _stateButton.Content = running ? "Pause" : "Run"; };
        }
    }

    public void SetRobotSetup(RobotSetup robotSetup) {
        _robotSetup = robotSetup;
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
