using System.Reflection;
using System.Drawing;
using System.Collections.Generic;

using CoreLibrary;

namespace SimulatorApp;

class SimulatedRobot {
    public RobotBase Robot { get; }
    public RobotPosition Position { get; private set; }
    public SensorPosition[] SensorPositions = new SensorPosition[RobotBase.SensorsCount];
    private int _currentTime = 0;
    private readonly List<PositionHistoryItem> _positionHistory;
    private readonly Action<int> _addMillis;
    private readonly PMode[] _pinModes;
    private readonly bool[] _pinValues;
    private readonly Bitmap _map;
    private readonly float _mapScale;

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

    public SimulatedRobot(RobotBase robot, RobotPosition initialPosition, Image map, float mapScale) {
        Robot = robot;
        Position = initialPosition;
        _map = new Bitmap(map);
        _mapScale = mapScale;
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

        if (_currentTime == 4000) {
            // Console.WriteLine(string.Join(",", _positionHistory));
        }
    }

    public IReadOnlyList<PositionHistoryItem> GetPositionHistory() => _positionHistory;

    private void MovePosition(int elapsedMillis) {
        Position = GetRobotPosition(Position, Robot.MotorsMicroseconds, elapsedMillis);
        _positionHistory.Add(new PositionHistoryItem(Position, _currentTime));
    }

    private void CheckSensors() {
        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            var sensorPosition = new SensorPosition {
                X = (float)(Position.X + _sensorDistances[i] * Math.Cos(Position.Rotation + _sensorAngles[i])),
                Y = (float)(Position.Y + _sensorDistances[i] * Math.Sin(Position.Rotation + _sensorAngles[i]))
            };
            int pixelX = (int)(sensorPosition.X / _mapScale);
            int pixelY = (int)(-sensorPosition.Y / _mapScale);

            if (pixelX >= 0 && pixelY >= 0 && pixelX < _map.Width && pixelY < _map.Height) {
                // returns true for white, false for black
                _pinValues[Robot.FirstSensorPin + i] = Math.Round(_map.GetPixel(pixelX, pixelY).GetBrightness()) == 1;
            } else {
                // "table" is white
                _pinValues[Robot.FirstSensorPin + i] = true;
            }

            SensorPositions[i] = sensorPosition;
        }
    }

    private static RobotPosition GetRobotPosition(RobotPosition oldPosition, MotorsState motorsMicroseconds, int elapsedMillis) {
        RobotPosition newPosition;
        float elapsedSeconds = elapsedMillis / 1000f;
        float leftSpeed = (motorsMicroseconds.Left - Servo.StopMicroseconds) * SpeedCoefficient;
        float rightSpeed = (-motorsMicroseconds.Right + Servo.StopMicroseconds) * SpeedCoefficient;

        if (leftSpeed == rightSpeed) {
            float distance = leftSpeed * elapsedSeconds;
            newPosition = new RobotPosition {
                X = (float)(oldPosition.X + distance * Math.Cos(oldPosition.Rotation)),
                Y = (float)(oldPosition.Y + distance * Math.Sin(oldPosition.Rotation)),
                Rotation = oldPosition.Rotation
            };
        } else {
            float rotationChange = (rightSpeed - leftSpeed) * elapsedSeconds / WheelDistance;
            float turnRadius = WheelDistance * (rightSpeed + leftSpeed) / (2 * (rightSpeed - leftSpeed));
            newPosition = new RobotPosition {
                X = (float)(oldPosition.X + turnRadius * (Math.Sin(rotationChange + oldPosition.Rotation) - Math.Sin(oldPosition.Rotation))),
                Y = (float)(oldPosition.Y - turnRadius * (Math.Cos(rotationChange + oldPosition.Rotation) - Math.Cos(oldPosition.Rotation))),
                Rotation = oldPosition.Rotation + rotationChange
            };
        }

        return newPosition;
    }
}
