using System.Threading.Tasks;
using Avalonia.Interactivity;

using CoreLibrary;

namespace SimulatorApp;

class SimulationLive : Simulation {
    // can be started, paused (freezed) and disposed
    // trajectory can be drawed

    private readonly SimulatedRobot _simulatedRobot;
    private readonly Panel _internalStateContainer;
    private readonly List<PinControl> _pinControls = new();
    private readonly ContentControl _internalStateControl = new Label();
    private readonly Path _robotIcon = new();
    private readonly Path[] _sensorIcons = new Path[RobotBase.SensorsCount];
    private readonly RotateTransform _rotation = new();
    private const int IterationLimit = 100_000;
    private const int IterationIntervalMs = 6;
    public RobotPosition RobotPosition => _simulatedRobot.Position;

    public SimulationLive(Canvas canvas, Map map, Type robotType, RobotSetup robotSetup, Panel internalStateContainer) : base(canvas, map) {
        _internalStateContainer = internalStateContainer;
        var robot = (RobotBase)Activator.CreateInstance(robotType)!;
        _simulatedRobot = new SimulatedRobot(robot, robotSetup, _map.BoolBitmap, _map.Scale);
        PrepareIcons(robotSetup.Config.Size, robotSetup.Config.SensorDistance);
        RedrawRobot();
        SetupStateControls();
        ShowInternalState();
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
            ShowInternalState();
            RedrawRobot();
        }

        Running = false;
    }

    public void Pause() {
        Running = false;
    }

    private void SetupStateControls() {
        foreach (int pin in _simulatedRobot.GetLeds()) {
            var control = new Label {
                MinWidth = 100,
                Padding = new Thickness(5),
                Margin = new Thickness(5, 4),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };
            var pc = new PinControl(pin, true, control);
            control.Tag = pc;
            _internalStateContainer.Children.Add(control);
            _pinControls.Add(pc);
        }

        foreach (int pin in _simulatedRobot.GetButtons()) {
            var control = new Button {
                MinWidth = 100,
                Margin = new Thickness(5, 0)
            };
            var pc = new PinControl(pin, false, control);
            control.AddHandler(Button.PointerPressedEvent, ButtonPress, RoutingStrategies.Tunnel);
            control.AddHandler(Button.PointerReleasedEvent, ButtonRelease, RoutingStrategies.Tunnel);
            control.Tag = pc;
            _internalStateContainer.Children.Add(control);
            _pinControls.Add(pc);
        }

        _internalStateControl.Padding = new Thickness(5);
        _internalStateControl.Margin = new Thickness(5);
        _internalStateControl.HorizontalContentAlignment = HorizontalAlignment.Center;
        _internalStateContainer.Children.Add(_internalStateControl);
    }

    private void PrepareIcons(float scale, float sensorOffset) {
        // https://yqnn.github.io/svg-path-editor/
        // M 15 -10 V 10 M 10 -10 V 10 H -20 V -10 H 10 M 0 -10 V 10 M -2 0 H 2
        // M 10 -10 V 10 M 8 -10 V 10 H -14 V -10 H 8 M 0 -10 V 10 M -2 0 H 2
        _robotIcon.Data = PathGeometry.Parse("M " + sensorOffset.ToString() + " -10 V 10 M 8 -10 V 10 H -14 V -10 H 8 M 0 -10 V 10 M -2 0 H 2");
        _robotIcon.Stroke = Brushes.Black;
        _robotIcon.Fill = new SolidColorBrush(Color.Parse("#99cccccc"));
        _robotIcon.StrokeThickness = 1 / scale;
        _robotIcon.RenderTransform = new TransformGroup {
            Children = [_rotation, new ScaleTransform(scale, scale)]
        };
        _robotIcon.RenderTransformOrigin = RelativePoint.TopLeft;

        _canvas.Children.Add(_robotIcon);

        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            _sensorIcons[i] = new Path {
                Data = PathGeometry.Parse("M 0 0 m 1 0 a 1 1 90 1 0 -2 0 a 1 1 90 1 0 2 0"),
                RenderTransform = new ScaleTransform(scale, scale),
                RenderTransformOrigin = RelativePoint.TopLeft,
                StrokeThickness = 1 / scale
            };
            _canvas.Children.Add(_sensorIcons[i]);
        }
    }

    private void RedrawRobot() {
        // #coordinates
        Canvas.SetLeft(_robotIcon, _simulatedRobot.Position.X);
        Canvas.SetTop(_robotIcon, _map.Size - _simulatedRobot.Position.Y);
        _rotation.Angle = -_simulatedRobot.Position.Rotation / Math.PI * 180;

        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            // #coordinates
            Canvas.SetLeft(_sensorIcons[i], _simulatedRobot.SensorPositions[i].X);
            Canvas.SetTop(_sensorIcons[i], _map.Size - _simulatedRobot.SensorPositions[i].Y);
            _sensorIcons[i].Stroke = _simulatedRobot.Robot.DigitalRead(_simulatedRobot.Robot.FirstSensorPin + i) ? Brushes.Green : Brushes.Red;
        }
    }

    private void ShowInternalState() {
        foreach (PinControl pinControl in _pinControls) {
            var control = (ContentControl)pinControl.Control;
            bool status = _simulatedRobot.PinStatus(pinControl.Pin);
            string statusText = status ? "HIGH" : "LOW";
            control.Content = $"pin {pinControl.Pin} {statusText}";

            if (pinControl.IsLed) {
                control.Background = status ? Brushes.Pink : Brushes.White;
            }
        }

        _internalStateControl.Content = _simulatedRobot.Robot.InternalState;
    }

    private void ButtonPress(object? sender, PointerPressedEventArgs e) => ButtonAction(sender!, true);
    private void ButtonRelease(object? sender, PointerReleasedEventArgs e) => ButtonAction(sender!, false);

    private void ButtonAction(object sender, bool press) {
        var btn = (Button)sender;
        var pc = (PinControl)btn.Tag!;
        _simulatedRobot.SetButton(pc.Pin, press);
    }

    public Polyline DrawTrajectory() {
        var history = _simulatedRobot.GetPositionHistory();
        var points = new List<Point>();

        foreach (var item in history) {
            // #coordinates
            points.Add(new Point(item.Position.X, _map.Size - item.Position.Y));
        }

        var polyline = new Polyline {
            Points = points,
            Stroke = Brushes.Red
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

        _internalStateContainer.Children.Clear();
    }
}
