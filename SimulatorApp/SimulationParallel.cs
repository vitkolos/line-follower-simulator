using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;

using CoreLibrary;

namespace SimulatorApp;

class SimulationParallel : Simulation {
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
    private const int IterationCount = 300_000;
    // private const int IterationCount = 10_000;
    private const int RobotCount = 50;

    public SimulationParallel(Canvas canvas, Map map, Type robotType, RobotSetup robotSetup) : base(canvas, map) {
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        Running = true;
        _map.BoolBitmap.PopulateCache();

        for (int i = 0; i < RobotCount; i++) {
            if (!Running || _disposed) {
                break;
            }

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

        Running = false;
    }

    public void Run() {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Running = true;
        Parallel.For(0, RobotCount, RunRobotByIndex);
        Running = false;
    }

    private void RunRobotByIndex(int index) => RunRobot(_simulatedRobots[index]);

    private void RunRobot(SimulatedRobot simulatedRobot) {
        for (int i = 0; i < IterationCount; i++) {
            if (!Running || _disposed) {
                break;
            }

            int randomIntervalDifference = RandomInterval ? RandomIntPM(simulatedRobot.Random!, IterationIntervalDifference) : 0;
            simulatedRobot.MoveNext(IterationIntervalMs + randomIntervalDifference);
        }
    }

    public override IReadOnlyList<Polyline> DrawTrajectories() {
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
        _disposed = true;
        Running = false; // race condition
    }
}
