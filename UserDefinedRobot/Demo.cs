using CoreLibrary;

namespace Demo;

class Motor(bool positiveDirection) : Servo {
    private const int SpeedCoefficient = 1;
    private int DirectionCoefficient => positiveDirection ? 1 : -1;
    public int Speed { get; private set; }

    public void Go(int percentage) {
        Speed = percentage * SpeedCoefficient;
        WriteMicroseconds(StopMicroseconds + DirectionCoefficient * Speed);
    }
};

class Sensors(Robot robot) {
    public void Setup() {
        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            robot.PinMode(robot.FirstSensorPin + i, PMode.Input);
        }
    }

    public bool this[int sensorNumber] => !robot.DigitalRead(robot.FirstSensorPin + sensorNumber);
    // returns true <=> black detected
};

enum Direction { Forward, Left, Right };

public class Robot : RobotBase {
    private const int ButtonPin = 2;
    private const int LedPin = 11;
    private const int ForwardPercentage = 100;
    private const int TurnCorrectionPercentage = -10;
    private const int BlinkMillis = 1000;

    private readonly Motor _leftMotor;
    private readonly Motor _rightMotor;
    private readonly Sensors _sensors;
    private bool _ride = true; // needs to be true for parallel simulation
    private Direction _direction = Direction.Forward;
    private long _lastTime;
    private bool _ledState = false;

    public override MotorsState MotorsMicroseconds => new MotorsState(_leftMotor.Microseconds, _rightMotor.Microseconds);
    public override int FirstSensorPin => 3;
    public override string InternalState => $"left motor speed: {_leftMotor.Speed}";

    public Robot() {
        _sensors = new Sensors(this);
        _leftMotor = new Motor(true);
        _rightMotor = new Motor(false);
    }

    public override void Setup() {
        _sensors.Setup();
        PinMode(ButtonPin, PMode.InputPullup);
        PinMode(LedPin, PMode.Output);
    }

    public override void Loop() {
        bool buttonPressed = !DigitalRead(ButtonPin);

        if (buttonPressed) {
            _ride = true;
        }

        if (_ride) {
            // time operations
            var currentTime = Millis();
            bool toggleLed = currentTime - _lastTime >= BlinkMillis;

            if (toggleLed) {
                _lastTime = currentTime;
                _ledState = !_ledState;
                DigitalWrite(LedPin, _ledState);
            }

            // direction update
            if (_sensors[2]) {
                _direction = Direction.Forward;
            } else if (_sensors[1]) {
                _direction = Direction.Left;
            } else if (_sensors[3]) {
                _direction = Direction.Right;
            } else {
                // keep the previous direction
            }

            // motor operation
            if (buttonPressed) {
                _leftMotor.Go(0);
                _rightMotor.Go(0);
            } else {
                switch (_direction) {
                    case Direction.Forward:
                        _leftMotor.Go(ForwardPercentage);
                        _rightMotor.Go(ForwardPercentage);
                        break;
                    case Direction.Left:
                        _leftMotor.Go(TurnCorrectionPercentage);
                        _rightMotor.Go(ForwardPercentage);
                        break;
                    case Direction.Right:
                        _leftMotor.Go(ForwardPercentage);
                        _rightMotor.Go(TurnCorrectionPercentage);
                        break;
                }
            }
        } else {
            _leftMotor.Go(0);
            _rightMotor.Go(0);
        }
    }
}
