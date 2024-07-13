using System.Reflection;
using System.Windows;
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
    private readonly Map _map;
    private readonly Path _robotIcon = new();
    private readonly Path[] _sensorIcons = new Path[RobotBase.SensorsCount];
    private readonly RotateTransform _rotation = new();
    private const int IterationLimit = 100000;
    private const int IterationIntervalMs = 10;
    private bool _disposed = false;

    public RealTimeSimulation(Canvas canvas, RobotBase robot, RobotPosition initialPosition, Map map, float scaleIcons, float scaleSpeed, float sensorOffset) {
        _canvas = canvas;
        _map = map;
        _simulatedRobot = new SimulatedRobot(robot, initialPosition, _map.Bitmap, _map.Scale, scaleIcons, scaleSpeed, sensorOffset);
        PrepareIcons(scaleIcons, sensorOffset);
        RedrawRobot();
    }

    public async void Run() {
        ObjectDisposedException.ThrowIf(_disposed, this);

        for (int i = 0; i < IterationLimit; i++) {
            await Task.Delay(IterationIntervalMs);

            if (_disposed) {
                break;
            }

            _simulatedRobot.MoveNext(IterationIntervalMs);
            RedrawRobot();
        }
    }

    private void PrepareIcons(float scale, float sensorOffset) {
        // https://yqnn.github.io/svg-path-editor/
        // M 15 -10 V 10 M 10 -10 V 10 H -20 V -10 H 10 M 0 -10 V 10 M -2 0 H 2
        // M 10 -10 V 10 M 8 -10 V 10 H -14 V -10 H 8 M 0 -10 V 10 M -2 0 H 2
        _robotIcon.Data = Geometry.Parse("M " + sensorOffset.ToString() + " -10 V 10 M 8 -10 V 10 H -14 V -10 H 8 M 0 -10 V 10 M -2 0 H 2");
        _robotIcon.Stroke = Brushes.Black;
        _robotIcon.Fill = Brushes.Gray;
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
        Canvas.SetTop(_robotIcon, -_simulatedRobot.Position.Y);
        _rotation.Angle = -_simulatedRobot.Position.Rotation / Math.PI * 180;

        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            Canvas.SetLeft(_sensorIcons[i], _simulatedRobot.SensorPositions[i].X);
            Canvas.SetTop(_sensorIcons[i], -_simulatedRobot.SensorPositions[i].Y);
            _sensorIcons[i].Stroke = _simulatedRobot.Robot.DigitalRead(_simulatedRobot.Robot.FirstSensorPin + i) ? Brushes.Green : Brushes.Red;
        }
    }

    public Polyline DrawTrajectory() {
        var history = _simulatedRobot.GetPositionHistory();
        var points = new PointCollection();

        foreach (var item in history) {
            points.Add(new Point(item.Position.X, -item.Position.Y));
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
        _canvas.Children.Remove(_robotIcon);

        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            _canvas.Children.Remove(_sensorIcons[i]);
        }
    }
}

// class ParallelSimulation : Simulation {
//     // private Image _map;
//     // private RobotPosition _startingPosition;
//     public SimulatedRobot[] Robots = new SimulatedRobot[1];


// }
