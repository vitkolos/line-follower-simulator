using CoreLibrary;

namespace UserDefinedRobot;

class Motor : Servo {
    private const float NumberOfSteps = 2.5f;
    private const int SpeedCoefficient = 1;

    public void Go(int targetPercentage) {
        if (targetPercentage != _targetPercantage) {
            _initialPercentage = _currentPercentage;
            _targetPercantage = targetPercentage;
            int difference = _targetPercantage - _initialPercentage;
            bool goingUp = difference > 0;
            _step = (int)(difference / NumberOfSteps);
            _step = goingUp ? Math.Max(_step, 1) : Math.Min(_step, -1);  // prevent zero step
        }

        if (_currentPercentage != _targetPercantage) {
            int newPercentage = _currentPercentage + _step;
            _currentPercentage = (_step > 0) ? Math.Min(_targetPercantage, newPercentage) : Math.Max(_targetPercantage, newPercentage);  // prevent overshooting
            ChangeMotorSpeed();
        }
    }
    public void SetDirection(bool positive) {
        _dir = positive ? 1 : -1;
    }

    private int _dir = 0;
    private int _initialPercentage = 0;
    private int _targetPercantage = 0;
    private int _currentPercentage = 0;
    private int _step = 0;

    private void ChangeMotorSpeed() {
        WriteMicroseconds(StopMicroseconds + _dir * _currentPercentage * SpeedCoefficient);
    }
};

class Sensors {
    Robot? _robot;

    public void Setup(Robot robot) {
        _robot = robot;

        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            _robot.PinMode(_robot.FirstSensorPin + i, PMode.Input);
        }
    }

    public bool Read(int sensorNumber) {
        // returns true for white, false for black
        return _robot!.DigitalRead(_robot.FirstSensorPin + sensorNumber);
    }
};


public class Robot : RobotBase {
    private readonly Motor _leftMotor = new();
    private readonly Motor _rightMotor = new();
    private readonly Sensors _sensors = new();

    private const int ButtonPin = 2;
    private const int LedPin = 11;

    public override (int, int) MotorsMicroseconds => (_leftMotor.Microseconds, _rightMotor.Microseconds);
    public override int FirstSensorPin => 3;

    public override void Setup() {
        _leftMotor.SetDirection(true);
        _rightMotor.SetDirection(false);
        _sensors.Setup(this);
        PinMode(ButtonPin, PMode.InputPullup);
        PinMode(LedPin, PMode.Output);
    }

    public override void Loop() {
        _leftMotor.Go(-100);
        _rightMotor.Go(-100);
        // Console.WriteLine("millis: " + Millis());
    }
}
