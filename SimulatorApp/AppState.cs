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

class AppState {
    private readonly Canvas _canvas;
    public Map? Map;
    // private RealTimeSimulation? _realTimeSimulation;
    // private Polyline? _oldPolyline;

    public AppState(Canvas canvas) {
        _canvas = canvas;
    }

    public void LoadMap(string imagePath, float zoom, float size) {
        if (Map is not null) {
            Map.Dispose();
        }

        Map = new Map(_canvas, imagePath, size, zoom);
    }

    public void StartRealtimeSimulation() {
        // _realTimeSimulation = new RealTimeSimulation(_canvas, new Robot(), robotPosition, _map, pinControlsContainer, _scaleIcons, _scaleSpeed, _sensorOffset);
    }
}
