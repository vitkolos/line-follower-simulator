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
    private readonly float _robotScale;
    private readonly float _speedScale;

    private const float WheelDistance = 20f; // 20f => 20 px
    private const float SpeedCoefficient = 0.5f; // 1f means that 1600 (1500+100) microseconds equals 100 px/s; 2f & 1600 us => 200 px/s etc.
    private static readonly float[] SensorDistancesY = { 10f, 3f, 0f, -3f, -10f };
    private readonly float[] _sensorAngles = new float[RobotBase.SensorsCount];
    private readonly float[] _sensorDistances = new float[RobotBase.SensorsCount];

    public SimulatedRobot(RobotBase robot, RobotPosition initialPosition, Image map, float mapScale, float robotScale, float speedScale, float sensorOffset) {
        Robot = robot;
        Position = initialPosition;
        _map = new Bitmap(map);
        _mapScale = mapScale;
        _robotScale = robotScale;
        _speedScale = speedScale;
        _positionHistory = [new PositionHistoryItem(initialPosition, 0)];

        MethodInfo addMillis = typeof(RobotBase).GetMethod("AddMillis", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _addMillis = addMillis.CreateDelegate<Action<int>>(Robot);
        FieldInfo pinModes = typeof(RobotBase).GetField("_pinModes", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _pinModes = (PMode[])pinModes.GetValue(Robot)!;
        FieldInfo pinValues = typeof(RobotBase).GetField("_pinValues", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _pinValues = (bool[])pinValues.GetValue(Robot)!;

        PrepareSensorPositions(sensorOffset);

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

    public IReadOnlyList<PositionHistoryItem> GetPositionHistory() => _positionHistory;

    public IEnumerable<int> GetButtons() {
        for (int i = 0; i < RobotBase.PinCount; i++) {
            if (_pinModes[i] == PMode.InputPullup) {
                yield return i;
            }
        }
    }

    public IEnumerable<int> GetLeds() {
        for (int i = 0; i < RobotBase.PinCount; i++) {
            if (_pinModes[i] == PMode.Output) {
                yield return i;
            }
        }
    }

    public bool LedStatus(int pin) {
        if (_pinModes[pin] == PMode.Output) {
            return _pinValues[pin];
        } else {
            throw new InvalidOperationException("this pin is not an Output");
        }
    }

    public void SetButton(int pin, bool down) {
        if (_pinModes[pin] == PMode.InputPullup) {
            // pin is true <=> button is released
            _pinValues[pin] = !down;
        } else {
            throw new InvalidOperationException("this pin is not an InputPullup");
        }
    }

    private void MovePosition(int elapsedMillis) {
        Position = GetRobotPosition(Position, Robot.MotorsMicroseconds, elapsedMillis, _robotScale, _speedScale);
        _positionHistory.Add(new PositionHistoryItem(Position, _currentTime));
    }

    private void PrepareSensorPositions(float sensorDistanceX) {
        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            _sensorDistances[i] = (float)Math.Sqrt(sensorDistanceX * sensorDistanceX + SensorDistancesY[i] * SensorDistancesY[i]);
            _sensorAngles[i] = (float)Math.Atan(SensorDistancesY[i] / sensorDistanceX);
        }
    }

    private void CheckSensors() {
        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            var sensorPosition = new SensorPosition {
                X = (float)(Position.X + _robotScale * _sensorDistances[i] * Math.Cos(Position.Rotation + _sensorAngles[i])),
                Y = (float)(Position.Y + _robotScale * _sensorDistances[i] * Math.Sin(Position.Rotation + _sensorAngles[i]))
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

    private static RobotPosition GetRobotPosition(RobotPosition oldPosition, MotorsState motorsMicroseconds, int elapsedMillis, float robotScale, float speedScale) {
        RobotPosition newPosition;
        float elapsedSeconds = elapsedMillis / 1000f;
        float leftSpeed = (motorsMicroseconds.Left - Servo.StopMicroseconds) * SpeedCoefficient * speedScale;
        float rightSpeed = (-motorsMicroseconds.Right + Servo.StopMicroseconds) * SpeedCoefficient * speedScale;

        if (leftSpeed == rightSpeed) {
            float distance = leftSpeed * elapsedSeconds;
            newPosition = new RobotPosition {
                X = (float)(oldPosition.X + distance * Math.Cos(oldPosition.Rotation)),
                Y = (float)(oldPosition.Y + distance * Math.Sin(oldPosition.Rotation)),
                Rotation = oldPosition.Rotation
            };
        } else {
            float modifiedWheelDistance = robotScale * WheelDistance;
            float rotationChange = (rightSpeed - leftSpeed) * elapsedSeconds / modifiedWheelDistance;
            float turnRadius = modifiedWheelDistance * (rightSpeed + leftSpeed) / (2 * (rightSpeed - leftSpeed));
            newPosition = new RobotPosition {
                X = (float)(oldPosition.X + turnRadius * (Math.Sin(rotationChange + oldPosition.Rotation) - Math.Sin(oldPosition.Rotation))),
                Y = (float)(oldPosition.Y - turnRadius * (Math.Cos(rotationChange + oldPosition.Rotation) - Math.Cos(oldPosition.Rotation))),
                Rotation = oldPosition.Rotation + rotationChange
            };
        }

        return newPosition;
    }
}
