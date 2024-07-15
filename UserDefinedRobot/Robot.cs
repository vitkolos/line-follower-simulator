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

    public override MotorsState MotorsMicroseconds => new(_leftMotor.Microseconds, _rightMotor.Microseconds);
    public override int FirstSensorPin => 3;
    // public override string InternalState => _leftMotor.Microseconds + " " + _rightMotor.Microseconds + " " + string.Join(",", sensorsBlack);


    private const int forwardPercentage = 100;
    private const int turnPercentage = -10;
    private const int updateMillis = 50;

    private const int turnDirectionDetectionOffMillis = 1250;  // should be at maximum 3000, finetuned to 1250
                                                               // turnDirectionDetectionOffMillis should be a little longer than the interval between a mark and a turn
    private const int preferenceChangeDelayMillis = 350;
    private const int finishHitMillis = 100;  // allows robot to stop at finish even if not aligned
    private const int rotateMillis = 500;
    private const int rotateCoooldownMillis = 100;
    private const int gapSideSensorsWaitingTime = 600;  // prevent robot from detecting line with side sensors when it should not

    private const int gapSearchStep0 = 500;   // robot realises this is not a turn
    private const int gapSearchStep1 = 1000;  // r. tries to search in one direction
    private const int gapSearchStep2 = 2000;  // r. tries to search in the other one
    private const int gapSearchStep3 = 4700;  // r. goes forward (it has to be that way), alternatives: 4700, 4500 (safer but less correct)

    private const bool preferLeft = true;
    private const bool follow = true;

    bool[] sensorsBlack = new bool[SensorsCount];
    bool ride = true; // modified
    direction_t direction = direction_t.FORWARD;
    int directionCounter = 0;
    raceState_t raceState = raceState_t.BEFORE;
    int[] hits = new int[SensorsCount];
    int turnDirectionDetectionOff = 0;
    int rotating = 0;
    int insideGap = 0;
    bool lastSBLeft = false;
    bool lastSBRight = false;
    bool nextTurnPreferLeft = preferLeft;
    bool futureNextTurnPreferLeft = preferLeft;
    long lastTime;

    public override void Setup() {
        _leftMotor.SetDirection(true);
        _rightMotor.SetDirection(false);
        _sensors.Setup(this);
        PinMode(ButtonPin, PMode.InputPullup);
        PinMode(LedPin, PMode.Output);
    }

    public override void Loop() {
        // _leftMotor.Go(100);

        // if (!_sensors.Read(3)) {
        //     _rightMotor.Go(0);
        //     // DigitalWrite(LedPin, true);
        // } else {
        //     _rightMotor.Go(100);
        // }

        // DigitalWrite(LedPin, DigitalRead(ButtonPin));
        // // Console.WriteLine("millis: " + Millis());

        bool buttonPressed = !DigitalRead(ButtonPin);

        if (buttonPressed && !ride) {
            ride = true;
            raceState = raceState_t.BEFORE;
        }

        if (ride) {
            // assign "lastSensorsBlack"
            lastSBLeft = sensorsBlack[0];
            lastSBRight = sensorsBlack[4];

            // sensor data simplification
            for (int i = 0; i < SensorsCount; i++) {
                sensorsBlack[i] = !_sensors.Read(i);
            }

            // time operations
            var currentTime = Millis();
            bool evaluateTick = currentTime - lastTime >= updateMillis;

            if (evaluateTick) {
                lastTime = currentTime;

                for (int i = 0; i < SensorsCount; i++) {
                    hits[i] = Math.Max(hits[i] - 1, 0);
                }

                turnDirectionDetectionOff = Math.Max(turnDirectionDetectionOff - 1, 0);
                rotating = Math.Max(rotating - 1, 0);
                insideGap = (insideGap > 0) ? insideGap + 1 : 0;
                directionCounter++;
            }

            for (int i = 0; i < SensorsCount; i++) {
                hits[i] = sensorsBlack[i] ? (finishHitMillis / updateMillis) : hits[i];
            }

            // leftmost and rightmost sensor
            if ((!lastSBLeft && sensorsBlack[0] && !sensorsBlack[1]) ||
                (!lastSBRight && sensorsBlack[4] && !sensorsBlack[3])) {
                if (hits[0] > 0 && hits[4] > 0 && hits[2] > 0) {
                    switch (raceState) {
                        case raceState_t.BEFORE:
                            raceState = raceState_t.RACING;
                            turnDirectionDetectionOff = 0;
                            break;

                        case raceState_t.RACING:
                            ride = false;
                            raceState = raceState_t.FINISHED;
                            break;
                    }
                } else if (!sensorsBlack[1] && !sensorsBlack[2] && !sensorsBlack[3]) {
                    // gap
                    if ((insideGap * updateMillis) > gapSideSensorsWaitingTime) {
                        direction = sensorsBlack[0] ? direction_t.LEFT : direction_t.RIGHT;
                    }

                    turnDirectionDetectionOff = 0;
                } else if (turnDirectionDetectionOff == 0) {
                    // turn left <=> (marker is on the left AND markers are followed) OR (marker is on the right AND markers are not followed)
                    futureNextTurnPreferLeft = (sensorsBlack[0] == follow);
                    turnDirectionDetectionOff = turnDirectionDetectionOffMillis / updateMillis;
                }
            }

            // delayed preferred direction setting
            int millisElapsedSinceTurnDetection = turnDirectionDetectionOffMillis - (turnDirectionDetectionOff * updateMillis);

            if (millisElapsedSinceTurnDetection > preferenceChangeDelayMillis) {
                nextTurnPreferLeft = futureNextTurnPreferLeft;
            }

            // basic ride
            bool preferredDirection = nextTurnPreferLeft ? sensorsBlack[1] : sensorsBlack[3];
            bool otherDirection = nextTurnPreferLeft ? sensorsBlack[3] : sensorsBlack[1];

            if (preferredDirection) {
                direction = nextTurnPreferLeft ? direction_t.LEFT : direction_t.RIGHT;
            } else if (sensorsBlack[2]) {
                direction = direction_t.FORWARD;
            } else if (otherDirection) {
                direction = nextTurnPreferLeft ? direction_t.RIGHT : direction_t.LEFT;
            } else {
                // keep the previous direction
            }

            // gap mode switch
            if (sensorsBlack[0] || sensorsBlack[1] || sensorsBlack[2] || sensorsBlack[3] || sensorsBlack[4]) {
                insideGap = 0;
            } else if (insideGap == 0) {
                insideGap = 1;
            }

            // gap resolution
            if (insideGap > 0) {
                int millisInsideGap = insideGap * updateMillis;

                if (millisInsideGap > gapSearchStep3) {
                    direction = direction_t.FORWARD;
                } else if (millisInsideGap > gapSearchStep2) {
                    direction = direction_t.RIGHT;
                } else if (millisInsideGap > gapSearchStep1) {
                    direction = direction_t.LEFT;
                } else if (millisInsideGap > gapSearchStep0) {
                    direction = direction_t.FORWARD;
                }
            }

            // right angle resolution
            if (hits[1] > 0 && hits[2] > 0 && hits[3] > 0) {
                rotating = rotateMillis / updateMillis;
            }

            // motor operation
            if (evaluateTick) {
                if (rotating > 0) {
                    // rotation direction must correspond to gap resolution direction
                    if (rotating <= ((rotateMillis - rotateCoooldownMillis) / updateMillis)) {
                        _leftMotor.Go(-forwardPercentage);
                    } else {
                        _leftMotor.Go(forwardPercentage);
                    }

                    _rightMotor.Go(forwardPercentage);
                } else {
                    switch (direction) {
                        case direction_t.FORWARD:
                            _leftMotor.Go(forwardPercentage);
                            _rightMotor.Go(forwardPercentage);
                            break;
                        case direction_t.LEFT:
                            _leftMotor.Go(turnPercentage);
                            _rightMotor.Go(forwardPercentage);
                            break;
                        case direction_t.RIGHT:
                            _leftMotor.Go(forwardPercentage);
                            _rightMotor.Go(turnPercentage);
                            break;
                    }
                }
            }
        } else {
            _leftMotor.Go(0);
            _rightMotor.Go(0);
        }
    }
}

enum direction_t {
    FORWARD,
    LEFT,
    RIGHT
};

enum raceState_t {
    BEFORE,
    RACING,
    FINISHED
};
