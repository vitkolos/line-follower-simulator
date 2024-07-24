using CoreLibrary;

namespace SimulatorApp;

/// <summary>
/// Parallel simulation allows to test the stability of the robot (its inner logic)
/// by letting multiple robots to drive over the same track with a subtle level of randomness.
/// Supported actions: Prepare, Run, DrawTrajectories, Cancel
/// </summary>
class SimulationParallel {
    private const int MinPointDistanceMs = 200; // to prevent UI from lagging
    private const int IterationCount = 10_000;
    private const int RobotCount = 50;
    private const int IterationIntervalMs = 6;

    // randomness settings
    private const int IterationIntervalDifference = 3;
    private const float PositionDifference = 10f;
    private const float RotationDifference = 0.25f;
    public const double SensorErrorLikelihood = 0.000_01;
    public const int MotorDifference = 20;

    // randomness flags
    private const bool RandomInterval = true;
    private const bool RandomPosition = true;
    public const bool RandomSensors = true;
    public const bool RandomMotors = true;

    private readonly Canvas _canvas;
    private readonly Map _map;
    private readonly Random _random;
    private readonly Type _robotType;
    private readonly RobotSetup _robotSetup;
    private readonly SimulatedRobot[] _simulatedRobots = new SimulatedRobot[RobotCount];
    private bool _canceled = false;
    public bool AnyRobotMoved => _simulatedRobots.Any(simulatedRobot => simulatedRobot.RobotMoved);

    public SimulationParallel(Canvas canvas, Map map, Type robotType, RobotSetup robotSetup) {
        _canvas = canvas;
        _map = map;
        int seed = new Random().Next();
        _random = new Random(seed);
        _robotType = robotType;
        _robotSetup = robotSetup;
    }

    private static float RandomFloatPM(Random random) {
        return random.NextSingle() * 2 - 1;
    }

    public static int RandomIntPM(Random random, int value) {
        return random.Next(-value, value + 1);
    }

    public void Prepare() {
        if (_canceled) { return; }

        _map.BoolBitmap.PopulateCache();

        for (int i = 0; i < RobotCount; i++) {
            if (_canceled) { break; }

            RobotBase robot = SimulatedRobot.SafelyGetNewRobot(_robotType);
            var robotRng = new Random(_random.Next());
            var modifiedSetup = RandomPosition ? new RobotSetup {
                Config = _robotSetup.Config,
                Position = new RobotPosition {
                    X = _robotSetup.Position.X + RandomFloatPM(robotRng) * PositionDifference,
                    Y = _robotSetup.Position.Y + RandomFloatPM(robotRng) * PositionDifference,
                    Rotation = _robotSetup.Position.Rotation + RandomFloatPM(robotRng) * RotationDifference
                }
            } : _robotSetup;
            var boolBitmap = new BoolBitmap(_map.BoolBitmap);
            _simulatedRobots[i] = new SimulatedRobot(robot, modifiedSetup, boolBitmap, _map.Scale, robotRng);
        }
    }

    public void Run() {
        try {
            Parallel.For(0, RobotCount, RunRobotByIndex);
        } catch (AggregateException exception) {
            if (exception.InnerExceptions.All(innerException => innerException is RobotException)) {
                throw exception.InnerExceptions.First();
            } else {
                throw;
            }
        }
    }

    private void RunRobotByIndex(int index) => RunRobot(_simulatedRobots[index]);

    private void RunRobot(SimulatedRobot simulatedRobot) {
        for (int i = 0; i < IterationCount; i++) {
            if (_canceled) { break; }

            int randomIntervalDifference = RandomInterval ? RandomIntPM(simulatedRobot.Random!, IterationIntervalDifference) : 0;
            simulatedRobot.MoveNext(IterationIntervalMs + randomIntervalDifference);
        }
    }

    public IReadOnlyList<Polyline> DrawTrajectories() {
        var polylines = new Polyline[RobotCount];

        for (int i = 0; i < RobotCount; i++) {
            var history = _simulatedRobots[i].GetPositionHistory();
            var points = new List<Point>();
            int lastVisibleTime = -MinPointDistanceMs;

            foreach (PositionHistoryItem item in history) {
                if (item.Time - lastVisibleTime >= MinPointDistanceMs) {
                    lastVisibleTime = item.Time;
                    // #coordinates
                    points.Add(new Point(item.Position.X, _map.Size - item.Position.Y));
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

    public void Cancel() {
        _canceled = true;
    }
}
