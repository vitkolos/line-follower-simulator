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
    private Map? _map;
    private RealTimeSimulation? _realTimeSimulation;
    private Polyline? _oldPolyline;
    private float _scaleIcons;
    private float _scaleSpeed;
    private float _sensorOffset;

}
