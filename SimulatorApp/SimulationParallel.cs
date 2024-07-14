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

namespace SimulatorApp;

// inconsistency 1: robotType
class ParallelSimulation : Simulation {
    private readonly Canvas _canvas;
    private readonly Map _map;
    private readonly int _seed;
    private readonly Random _random;
    private readonly Type _robotType;
    private readonly RobotSetup _robotSetup;
    private readonly SimulatedRobot[] _simulatedRobots = new SimulatedRobot[RobotCount];

    // random flags
    private const bool RandomInterval = true;
    private const bool RandomPosition = true;
    public const bool RandomSensors = true;
    public const bool RandomMotors = true;

    private const int IterationIntervalMs = 6;
    private const int IterationIntervalDifference = 3;
    public const int MotorDifference = 20;
    public const double SensorErrorLikelihood = 0.000_01;
    private const int MinPointDistanceMs = 200; // to prevent UI from lagging
    private const int IterationCount = 10000;
    private const int RobotCount = 50;

    public ParallelSimulation(Canvas canvas, Type robotType, RobotSetup robotSetup, Map map) {
        _canvas = canvas;
        _map = map;
        var rng = new Random();
        _seed = rng.Next();
        _random = new Random(_seed);
        _robotType = robotType;
        _robotSetup = robotSetup;
        Console.WriteLine("seed: " + _seed);
    }

    private static float RandomFloatPM(Random random) {
        return random.NextSingle() * 2 - 1;
    }

    public static int RandomIntPM(Random random, int value) {
        return random.Next(-value, value + 1);
    }

    public void Prepare() {
        var sw = new Stopwatch();
        sw.Start();

        _map.BoolBitmap.PopulateCache();

        for (int i = 0; i < RobotCount; i++) {
            var robot = (RobotBase)Activator.CreateInstance(_robotType)!;
            var robotRng = new Random(_random.Next());
            var modifiedSetup = RandomPosition ? new RobotSetup {
                Config = _robotSetup.Config,
                Position = new RobotPosition {
                    X = _robotSetup.Position.X + RandomFloatPM(robotRng) * 10,
                    Y = _robotSetup.Position.Y + RandomFloatPM(robotRng) * 10,
                    Rotation = _robotSetup.Position.Rotation + RandomFloatPM(robotRng) / 4
                }
            } : _robotSetup;
            var boolBitmap = new BoolBitmap(_map.BoolBitmap);
            _simulatedRobots[i] = new SimulatedRobot(robot, modifiedSetup, boolBitmap, _map.Scale, robotRng);
        }

        sw.Stop();
        Console.WriteLine("preparation: " + sw.Elapsed);
    }

    public void Run() {
        var sw = new Stopwatch();
        sw.Start();

        Parallel.For(0, RobotCount, RunRobotByIndex);

        sw.Stop();
        Console.WriteLine("running: " + sw.Elapsed);
    }

    private void RunRobotByIndex(int index) => RunRobot(_simulatedRobots[index]);

    public static void RunRobot(SimulatedRobot simulatedRobot) {
        for (int i = 0; i < IterationCount; i++) {
            int randomIntervalDifference = RandomInterval ? RandomIntPM(simulatedRobot.Random!, IterationIntervalDifference) : 0;
            simulatedRobot.MoveNext(IterationIntervalMs + randomIntervalDifference);
        }
    }

    public IReadOnlyList<Polyline> DrawTrajectories() {
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

        return polylines;
    }

    public override void Dispose() {
        // fixme
        throw new NotImplementedException();
    }
}
