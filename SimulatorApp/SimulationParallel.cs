using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;

using CoreLibrary;
using System.Drawing.Drawing2D;

namespace SimulatorApp;

// inconsistency 1: robotType
class ParallelSimulation : Simulation {
    private readonly Canvas _canvas;
    private readonly Map _map;
    private readonly int _seed;
    private readonly Random _random;
    private readonly SimulatedRobot[] _simulatedRobots = new SimulatedRobot[RobotCount];

    private const int IterationCount = 20000;
    private const int AverageIterationIntervalMs = 5;
    private const int RobotCount = 200;
    private const int MinPointDistanceMs = 100;

    public ParallelSimulation(Canvas canvas, Type robotType, RobotSetup robotSetup, Map map) {
        _canvas = canvas;
        _map = map;
        var rng = new Random();
        _seed = rng.Next();
        _random = new Random(_seed);

        PrepareRobots(robotType, robotSetup);

        var sw = new Stopwatch();
        sw.Start();

        for (int i = 0; i < RobotCount; i++) {
            RunRobot(i);
        }

        sw.Stop();
        Console.WriteLine("running: " + sw.Elapsed);
    }

    private static float RandomFloatPM(Random random) {
        return random.NextSingle() * 2 - 1;
    }

    private void PrepareRobots(Type robotType, RobotSetup robotSetup) {
        var sw = new Stopwatch();
        sw.Start();

        _map.BoolBitmap.PopulateCache();
        Console.WriteLine(_map.BoolBitmap.Cached);

        for (int i = 0; i < RobotCount; i++) {
            var robot = (RobotBase)Activator.CreateInstance(robotType)!;
            var robotRng = new Random(_random.Next());
            var modifiedSetup = new RobotSetup {
                Config = robotSetup.Config,
                Position = new RobotPosition {
                    X = robotSetup.Position.X + RandomFloatPM(robotRng) * 10,
                    Y = robotSetup.Position.Y + RandomFloatPM(robotRng) * 10,
                    Rotation = robotSetup.Position.Rotation + RandomFloatPM(robotRng) / 4
                }
            };
            var boolBitmap = new BoolBitmap(_map.BoolBitmap);
            
            _simulatedRobots[i] = new SimulatedRobot(robot, modifiedSetup, boolBitmap, _map.Scale, robotRng);
        }

        sw.Stop();
        Console.WriteLine("preparation: " + sw.Elapsed);
    }

    private void RunRobot(int index) {
        for (int i = 0; i < IterationCount; i++) {
            _simulatedRobots[index].MoveNext(AverageIterationIntervalMs + 0); // fixme
        }
    }

    public IReadOnlyList<Polyline> DrawTrajectories() {
        var sw = new Stopwatch();
        sw.Start();
        var polylines = new Polyline[RobotCount];

        for (int i = 0; i < RobotCount; i++) {
            var history = _simulatedRobots[i].GetPositionHistory();
            var points = new PointCollection();
            int lastVisibleTime = -MinPointDistanceMs;

            foreach (PositionHistoryItem item in history) {
                if (item.Time - lastVisibleTime >= MinPointDistanceMs) {
                    lastVisibleTime = item.Time;
                    // #coordinates
                    points.Add(new System.Windows.Point(item.Position.X, _map.Size - item.Position.Y));
                }
            }

            polylines[i] = new Polyline {
                Points = points,
                Stroke = Brushes.Red
            };
            _canvas.Children.Add(polylines[i]);
        }

        sw.Stop();
        Console.WriteLine("drawing: " + sw.Elapsed);
        return polylines;
    }

    public override void Dispose() {
        throw new NotImplementedException();
    }
}
