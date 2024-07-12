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

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        /*
            STEPS:
            - load a map
            - select the starting point and orientation
            - load an assembly with the robot (watch changes?)
            a) run simulation in real time
            b) run simulation in parallel, then display result
        */
        RunRobot(this);
    }

    private static async void RunRobot(Window window) {
        int centerX = 400;
        int centerY = 400;
        Path path = (Path)window.FindName("cursor");
        RotateTransform rotation = (RotateTransform)path.FindName("rotation");
        rotation.Angle = 0;

        int interval = 10;
        var pr = new SimulatedRobot(new Robot(), new Position());

        for (int i = 0; i < 800; i++) {
            await Task.Delay(interval);
            pr.MoveNext(interval);
            path.Margin = new Thickness(centerX + pr.Position.X, centerY - pr.Position.Y, 0, 0);
            rotation.Angle = -pr.Position.Rotation / Math.PI * 180;

            for (int j = 0; j < RobotBase.SensorsCount; j++) {
                Path sensor = (Path)window.FindName("sensor" + j);
                (float sensorX, float sensorY) = pr.SensorPositions[j];
                sensor.Margin = new Thickness(centerX + sensorX, centerY - sensorY, 0, 0);
            }
        }
    }
}
