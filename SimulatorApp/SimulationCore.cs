using System.Reflection;

using CoreLibrary;

namespace SimulatorApp;


class SimulationCore {
    // private object _loadedMap = new object();
    // private Position _startingPosition = new Position();
    public SimulatedRobot[] Robots = new SimulatedRobot[1];


}

class SimulatedRobot {
    public RobotBase Robot { get; }
    public Position Position { get; private set; }
    public Point[] SensorPositions = new Point[RobotBase.SensorsCount];
    private int _currentTime = 0;
    private readonly List<PositionHistoryItem> _positionHistory;
    private readonly Action<int> _addMillis;
    private readonly PMode[] _pinModes;
    private readonly bool[] _pinValues;

    private const float WheelDistance = 20f; // 20f => 20 px
    private const float SpeedCoefficient = 0.5f; // 1f means that 1600 (1500+100) microseconds equals 100 px/s; 2f & 1600 us => 200 px/s etc.
    private const float SensorDistanceX = 15f;
    private static readonly float[] _sensorDistancesY = { 10f, 3f, 0f, -3f, -10f };
    private static readonly float[] _sensorAngles = new float[RobotBase.SensorsCount];
    private static readonly float[] _sensorDistances = new float[RobotBase.SensorsCount];

    static SimulatedRobot() {
        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            _sensorDistances[i] = (float)Math.Sqrt(SensorDistanceX * SensorDistanceX + _sensorDistancesY[i] * _sensorDistancesY[i]);
            _sensorAngles[i] = (float)Math.Atan(_sensorDistancesY[i] / SensorDistanceX);
        }
    }

    public SimulatedRobot(RobotBase robot, Position initialPosition) {
        Robot = robot;
        Position = initialPosition;
        _positionHistory = [new PositionHistoryItem(initialPosition, 0)];

        MethodInfo addMillis = typeof(RobotBase).GetMethod("AddMillis", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _addMillis = addMillis.CreateDelegate<Action<int>>(Robot);
        FieldInfo pinModes = typeof(RobotBase).GetField("_pinModes", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _pinModes = (PMode[])pinModes.GetValue(Robot)!;
        FieldInfo pinValues = typeof(RobotBase).GetField("_pinValues", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _pinValues = (bool[])pinValues.GetValue(Robot)!;

        Robot.Setup();
        CheckSensors();
        Robot.Loop();
    }

    public void MoveNext(int elapsedMillis) {
        // timekeeping
        _currentTime += elapsedMillis;
        _addMillis(elapsedMillis);

        // input & output
        MovePosition(elapsedMillis);
        CheckSensors();

        // loop
        Robot.Loop();
    }

    private void MovePosition(int elapsedMillis) {
        Position = GetRobotPosition(Position, Robot.MotorsMicroseconds, elapsedMillis);
        _positionHistory.Add(new PositionHistoryItem(Position, _currentTime));
    }

    private void CheckSensors() {
        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            var sensorPosition = new Point {
                X = (float)(Position.X + _sensorDistances[i] * Math.Cos(Position.Rotation + _sensorAngles[i])),
                Y = (float)(Position.Y + _sensorDistances[i] * Math.Sin(Position.Rotation + _sensorAngles[i]))
            };
            // _pinValues[Robot.FirstSensorPin + i] = FIXME;
            SensorPositions[i] = sensorPosition;
        }
    }

    private static Position GetRobotPosition(Position oldPosition, MotorsState motorsMicroseconds, int elapsedMillis) {
        Position newPosition;
        float elapsedSeconds = elapsedMillis / 1000f;
        float leftSpeed = (motorsMicroseconds.Left - Servo.StopMicroseconds) * SpeedCoefficient;
        float rightSpeed = (-motorsMicroseconds.Right + Servo.StopMicroseconds) * SpeedCoefficient;

        if (leftSpeed == rightSpeed) {
            float distance = leftSpeed * elapsedSeconds;
            newPosition = new Position {
                X = (float)(oldPosition.X + distance * Math.Cos(oldPosition.Rotation)),
                Y = (float)(oldPosition.Y + distance * Math.Sin(oldPosition.Rotation)),
                Rotation = oldPosition.Rotation
            };
        } else {
            float rotationChange = (rightSpeed - leftSpeed) * elapsedSeconds / WheelDistance;
            float turnRadius = WheelDistance * (rightSpeed + leftSpeed) / (2 * (rightSpeed - leftSpeed));
            newPosition = new Position {
                X = (float)(oldPosition.X + turnRadius * (Math.Sin(rotationChange + oldPosition.Rotation) - Math.Sin(oldPosition.Rotation))),
                Y = (float)(oldPosition.Y - turnRadius * (Math.Cos(rotationChange + oldPosition.Rotation) - Math.Cos(oldPosition.Rotation))),
                Rotation = oldPosition.Rotation + rotationChange
            };
        }

        return newPosition;
    }
}

readonly record struct Point(float X, float Y);
readonly record struct Position(float X, float Y, float Rotation);
readonly record struct PositionHistoryItem(Position Position, int Time);
