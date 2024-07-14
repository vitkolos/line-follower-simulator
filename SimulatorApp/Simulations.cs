using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using Bitmap = System.Drawing.Bitmap;

using CoreLibrary;
using System.Drawing.Drawing2D;

namespace SimulatorApp;

abstract class Simulation : IDisposable {
    public abstract void Dispose();
}

class RealTimeSimulation : Simulation {
    // is created with canvas, map, starting position and robot instance
    // can be started, paused (freezed) and disposed
    // trajectory can be drawed

    private readonly SimulatedRobot _simulatedRobot;
    private readonly Canvas _canvas;
    private readonly Panel _pinControlsContainer;
    private readonly List<PinControl> _pinControls = new();
    private readonly Map _map;
    private readonly Path _robotIcon = new();
    private readonly Path[] _sensorIcons = new Path[RobotBase.SensorsCount];
    private readonly RotateTransform _rotation = new();
    private const int IterationLimit = 100000;
    private const int IterationIntervalMs = 10;
    private bool _disposed = false;
    public bool Running {
        get => _running;
        private set {
            _running = value;
            StateChange(value);
        }
    }
    private bool _running = false;
    public event Action<bool> StateChange = _ => { };
    public RobotPosition RobotPosition => _simulatedRobot.Position;

    public RealTimeSimulation(Canvas canvas, RobotBase robot, RobotSetup robotSetup, Map map, Panel pinControlsContainer) {
        _canvas = canvas;
        _pinControlsContainer = pinControlsContainer;
        _map = map;
        _simulatedRobot = new SimulatedRobot(robot, robotSetup, _map.Bitmap, _map.Scale);
        PrepareIcons(robotSetup.Config.Size, robotSetup.Config.SensorDistance);
        RedrawRobot();
        SetupPinControls();
        ShowPinStatus();
    }

    public async void Run() {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Running = true;

        for (int i = 0; i < IterationLimit; i++) {
            await Task.Delay(IterationIntervalMs);

            if (!Running) {
                break;
            }

            _simulatedRobot.MoveNext(IterationIntervalMs);
            ShowPinStatus();
            RedrawRobot();
        }
    }

    public void Pause() {
        Running = false;
    }

    private void SetupPinControls() {
        foreach (int pin in _simulatedRobot.GetLeds()) {
            var control = new Label {
                Width = 100,
                Padding = new Thickness(5),
                Margin = new Thickness(5),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            var pc = new PinControl(pin, true, control);
            control.Tag = pc;
            _pinControlsContainer.Children.Add(control);
            _pinControls.Add(pc);
        }

        foreach (int pin in _simulatedRobot.GetButtons()) {
            var control = new Button {
                Width = 100,
                Margin = new Thickness(5)
            };
            var pc = new PinControl(pin, false, control);
            control.PreviewMouseDown += ButtonPress;
            control.PreviewMouseUp += ButtonRelease;
            control.Tag = pc;
            _pinControlsContainer.Children.Add(control);
            _pinControls.Add(pc);
        }
    }

    private void PrepareIcons(float scale, float sensorOffset) {
        // https://yqnn.github.io/svg-path-editor/
        // M 15 -10 V 10 M 10 -10 V 10 H -20 V -10 H 10 M 0 -10 V 10 M -2 0 H 2
        // M 10 -10 V 10 M 8 -10 V 10 H -14 V -10 H 8 M 0 -10 V 10 M -2 0 H 2
        _robotIcon.Data = Geometry.Parse("M " + sensorOffset.ToString() + " -10 V 10 M 8 -10 V 10 H -14 V -10 H 8 M 0 -10 V 10 M -2 0 H 2");
        _robotIcon.Stroke = Brushes.Black;
        _robotIcon.Fill = (Brush)new BrushConverter().ConvertFrom("#99cccccc")!;
        _robotIcon.StrokeThickness = 1 / scale;
        _robotIcon.RenderTransform = new TransformGroup {
            Children = [_rotation, new ScaleTransform(scale, scale)]
        };

        _canvas.Children.Add(_robotIcon);

        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            _sensorIcons[i] = new Path {
                Data = Geometry.Parse("M 0 0 m 1 0 a 1 1 90 1 0 -2 0 a 1 1 90 1 0 2 0"),
                RenderTransform = new ScaleTransform(scale, scale),
                StrokeThickness = 1 / scale
            };
            _canvas.Children.Add(_sensorIcons[i]);
        }
    }

    private void RedrawRobot() {
        Canvas.SetLeft(_robotIcon, _simulatedRobot.Position.X);
        Canvas.SetTop(_robotIcon, _map.Size - _simulatedRobot.Position.Y);
        _rotation.Angle = -_simulatedRobot.Position.Rotation / Math.PI * 180;

        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            Canvas.SetLeft(_sensorIcons[i], _simulatedRobot.SensorPositions[i].X);
            Canvas.SetTop(_sensorIcons[i], _map.Size - _simulatedRobot.SensorPositions[i].Y);
            _sensorIcons[i].Stroke = _simulatedRobot.Robot.DigitalRead(_simulatedRobot.Robot.FirstSensorPin + i) ? Brushes.Green : Brushes.Red;
        }
    }

    private void ShowPinStatus() {
        foreach (PinControl pinControl in _pinControls) {
            var control = (ContentControl)pinControl.Control;
            bool status = _simulatedRobot.PinStatus(pinControl.Pin);
            string statusText = status ? "HIGH" : "LOW";
            control.Content = $"pin {pinControl.Pin} {statusText}";

            if (pinControl.IsLed) {
                control.Background = status ? Brushes.Pink : Brushes.LightGray;
            }
        }
    }

    private void ButtonPress(object sender, MouseButtonEventArgs e) => ButtonAction(sender, true);
    private void ButtonRelease(object sender, MouseButtonEventArgs e) => ButtonAction(sender, false);

    private void ButtonAction(object sender, bool press) {
        var btn = (Button)sender;
        var pc = (PinControl)btn.Tag;
        _simulatedRobot.SetButton(pc.Pin, press);
    }

    public Polyline DrawTrajectory() {
        var history = _simulatedRobot.GetPositionHistory();
        var points = new PointCollection();

        foreach (var item in history) {
            points.Add(new Point(item.Position.X, _map.Size - item.Position.Y));
        }

        var polyline = new Polyline {
            Points = points,
            Stroke = Brushes.DarkBlue
        };
        _canvas.Children.Add(polyline);

        return polyline;
    }

    public override void Dispose() {
        _disposed = true;
        Running = false;
        _canvas.Children.Remove(_robotIcon);

        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            _canvas.Children.Remove(_sensorIcons[i]);
        }

        foreach (PinControl pinControl in _pinControls) {
            _pinControlsContainer.Children.Remove(pinControl.Control);
        }
    }
}

// class ParallelSimulation : Simulation {
//     // private Image _map;
//     // private RobotPosition _startingPosition;
//     public SimulatedRobot[] Robots = new SimulatedRobot[1];


// }
