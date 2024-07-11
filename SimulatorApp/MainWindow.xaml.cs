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

using SimulatorConsoleApp;
using CoreLibrary;
using UserDefinedRobot;

namespace SimulatorApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        RunRobot(this);
    }

    private static async void RunRobot(Window window) {
        int centerX = 400;
        int centerY = 400;
        Path path = (Path)window.FindName("cursor");
        RotateTransform rotation = (RotateTransform)path.FindName("rotation");
        rotation.Angle = 0;

        int interval = 10;

        var pr = new PositionedRobot(new Robot());
        pr.Robot.Setup();

        for (int i = 0; i < 400; i++) {
            await Task.Delay(interval);
            pr.Robot.AddMillis(interval);
            pr.Robot.Loop();
            // Console.WriteLine(pr);
            SimulationCore.EvaluatePosition(pr, interval);
            path.Margin = new Thickness(centerX + pr.Position.X, centerY - pr.Position.Y, 0, 0);
            rotation.Angle = -pr.Position.Rotation / Math.PI * 180;
        }
    }
}
