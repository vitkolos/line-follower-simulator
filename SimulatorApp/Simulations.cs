using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using Bitmap = System.Drawing.Bitmap;

using CoreLibrary;

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
    private const int IterationLimit = 100000;
    private const int IterationIntervalMs = 10;
    private bool _disposed = false;

    public RealTimeSimulation(Canvas canvas, RobotBase robot, RobotPosition initialPosition, Map map) {
        _canvas = canvas;
        _map = map;
        _simulatedRobot = new SimulatedRobot(robot, initialPosition, _map.Bitmap, _map.Scale);
        DrawRobot();
    }

    public async void Run() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(RealTimeSimulation));
        }

        for (int i = 0; i < IterationLimit; i++) {
            await Task.Delay(IterationIntervalMs);

            if (_disposed) {
                break;
            }

            _simulatedRobot.MoveNext(IterationIntervalMs);
            DrawRobot();
        }
    }

    private void DrawRobot() {
        var robotIcon = (Path)_canvas.FindName("cursor");
        var rotation = (RotateTransform)robotIcon.FindName("rotation");
        Canvas.SetLeft(robotIcon, _simulatedRobot.Position.X);
        Canvas.SetTop(robotIcon, -_simulatedRobot.Position.Y);
        rotation.Angle = -_simulatedRobot.Position.Rotation / Math.PI * 180;

        for (int j = 0; j < RobotBase.SensorsCount; j++) {
            Path sensor = (Path)_canvas.FindName("sensor" + j);
            Canvas.SetLeft(sensor, _simulatedRobot.SensorPositions[j].X);
            Canvas.SetTop(sensor, -_simulatedRobot.SensorPositions[j].Y);
            sensor.Stroke = _simulatedRobot.Robot.DigitalRead(_simulatedRobot.Robot.FirstSensorPin + j) ? Brushes.LightGreen : Brushes.Red;
        }
    }

    public void DrawTrajectory() {
        var history = _simulatedRobot.GetPositionHistory();
        var points = new PointCollection();

        foreach (var item in history) {
            points.Add(new Point(item.Position.X, -item.Position.Y));
        }

        var polyline = (Polyline)_canvas.FindName("polyline");
        polyline.Points = points;
    }

    public override void Dispose() {
        _disposed = true;
    }
}

// class ParallelSimulation : Simulation {
//     // private Image _map;
//     // private RobotPosition _startingPosition;
//     public SimulatedRobot[] Robots = new SimulatedRobot[1];


// }
