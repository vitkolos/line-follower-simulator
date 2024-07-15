using CoreLibrary;

/*/

class Motor : Servo {
    const int speedCoefficient = 2; // this should be 2 for maximal reasonable speed
    const int servoMin = 500;
    const int servoStop = 1500;
    const int servoMax = 2500;
    const float P = 0.8f; // the lower the coefficient, the bigger the turning radius
    const float I = 0;
    const float D = 0;

    public void go(int percentage) {
        if (percentage != _currentPercentage) {
            // PID control (percentage … setpoint)
            int error = percentage - _currentPercentage;
            _integral = _integral + (error * Robot.updateMillis);
            int derivative = (error - _previousError) / Robot.updateMillis;
            int control = (int)((P * error) + (I * _integral) + (D * derivative));
            _previousError = error;

            _currentPercentage += control;

            if (((_currentPercentage - percentage) == -1) || ((_currentPercentage - percentage) == 1)) {
                _currentPercentage = percentage;
            }

            // Serial.print(_dir);
            // Serial.print(' ');
            // Serial.print(_currentPercentage);
            // Serial.print('\n');
            changeMotorSpeed();
        }
    }
    public void setDirection(bool positive) {
        _dir = positive ? 1 : -1;
    }

    int _dir = 0;
    int _currentPercentage = 0;
    int _previousError = 0;
    int _integral = 0;
    void changeMotorSpeed() {
        WriteMicroseconds(servoStop + _dir * _currentPercentage * speedCoefficient);
    }
};

class Sensors(Robot robot) {
    public void setup() {
        for (int i = 0; i < RobotBase.SensorsCount; i++) {
            robot.PinMode(robot.FirstSensorPin + i, PMode.Input);
        }
    }
    public bool read(int sensorNumber) {
        // returns true for white, false for black
        return robot.DigitalRead(robot.FirstSensorPin + sensorNumber);
    }
};

enum direction_t {
    FORWARD,
    LEFT,
    RIGHT
};

enum splitState_t {
    TRACK,
    BEFORE,
    INSIDE
};

class Robot : RobotBase {
    const int buttonPin = 2;
    const int numberOfSensors = 5;
    const int ledPin = 11;
    const int leftMotorPin = 12;
    const int rightMotorPin = 13;


    const int forwardPercentage = 100;
    const int turnCorrectionPercentage = -10;
    public const int updateMillis = 50;


    const int splitDoubleHitMillis = 150;  // this should probably be somewhere between 100 and 200
                                           // if splitDoubleHitMillis is too large, the robot might make random circles at turns or splits

    const bool preferLeft = false;
    const bool follow = true;

    Motor leftMotor = new();
    Motor rightMotor = new();
    Sensors sensors;
    bool[] sensorsBlack = new bool[numberOfSensors];
    bool[] lastSensorsBlack = new bool[numberOfSensors];
    bool ride = true;
    direction_t direction = direction_t.FORWARD;
    splitState_t splitState = splitState_t.TRACK;
    int hitLeft = 0;
    int hitRight = 0;
    bool nextTurnPreferLeft = preferLeft;
    long lastTime;

    public override MotorsState MotorsMicroseconds => new MotorsState(leftMotor.Microseconds, rightMotor.Microseconds);

    public override int FirstSensorPin => 3;

    public override void Setup() {
        sensors = new Sensors(this);
        leftMotor.setDirection(true);
        rightMotor.setDirection(false);
        sensors.setup();
        PinMode(buttonPin, PMode.InputPullup);
        PinMode(ledPin, PMode.Output);
        // Serial.begin(9600);
    }

    public override void Loop() {
        bool buttonPressed = !DigitalRead(buttonPin);

        if (buttonPressed) {
            ride = true;
        }

        if (ride) {
            // sensor data simplification
            for (int i = 0; i < numberOfSensors; i++) {
                lastSensorsBlack[i] = sensorsBlack[i];
                sensorsBlack[i] = !sensors.read(i);
            }

            // time operations
            var currentTime = Millis();
            bool evaluateTick = currentTime - lastTime >= updateMillis;

            if (evaluateTick) {
                lastTime = currentTime;

                hitLeft = Math.Max(hitLeft - 1, 0);
                hitRight = Math.Max(hitRight - 1, 0);
            }

            // leftmost and rightmost sensor
            //
            if ((!lastSensorsBlack[0] && sensorsBlack[0] && !sensorsBlack[1]) ||
                (!lastSensorsBlack[4] && sensorsBlack[4] && !sensorsBlack[3])) {
                // Serial.println('a');
                if (sensorsBlack[0] && sensorsBlack[4] && sensorsBlack[2]) {
                    // finish line
                    ride = false;
                    splitState = splitState_t.TRACK;
                } else if (!sensorsBlack[1] && !sensorsBlack[2] && !sensorsBlack[3]) {
                    // gap
                    direction = sensorsBlack[0] ? direction_t.LEFT : direction_t.RIGHT;
                    splitState = splitState_t.TRACK;
                } else {
                    // Serial.println('x');
                    switch (splitState) {
                        case splitState_t.TRACK:
                            // turn left <=> (marker is on the left AND markers are followed) OR (marker is on the right AND markers are not followed)
                            nextTurnPreferLeft = (sensorsBlack[0] == follow);
                            // splitState = splitState_t.BEFORE;
                            splitState = splitState_t.INSIDE;
                            break;
                        // case splitState_t.BEFORE:
                        //     splitState = splitState_t.INSIDE;
                        //     break;
                        case splitState_t.INSIDE:
                            // can't reset next turn preference here
                            splitState = splitState_t.TRACK;
                            break;
                    }
                }
            }

            DigitalWrite(ledPin, splitState == splitState_t.TRACK);

            // if (evaluateTick) {
            //     Serial.print(sensorsBlack[0]);
            //     Serial.print(sensorsBlack[1]);
            //     Serial.print(sensorsBlack[2]);
            //     Serial.print(sensorsBlack[3]);
            //     Serial.println(sensorsBlack[4]);
            // }
            //

            // basic ride
            if (sensorsBlack[2]) {
                direction = direction_t.FORWARD;
            } else if (sensorsBlack[1]) {
                direction = direction_t.LEFT;
            } else if (sensorsBlack[3]) {
                direction = direction_t.RIGHT;
            } else {
                // keep the previous direction
            }

            hitLeft = sensorsBlack[1] ? (splitDoubleHitMillis / updateMillis) : hitLeft;
            hitRight = sensorsBlack[3] ? (splitDoubleHitMillis / updateMillis) : hitRight;

            // split evaluation
            if ((hitLeft > 0 && hitRight > 0) ||
                sensorsBlack[2] && (sensorsBlack[1] || sensorsBlack[3])) {
                direction = nextTurnPreferLeft ? direction_t.LEFT : direction_t.RIGHT;
            }

            // motor operation
            if (evaluateTick) {
                switch (direction) {
                    case direction_t.FORWARD:
                        leftMotor.go(forwardPercentage);
                        rightMotor.go(forwardPercentage);
                        break;
                    case direction_t.LEFT:
                        leftMotor.go(turnCorrectionPercentage);
                        rightMotor.go(forwardPercentage);
                        break;
                    case direction_t.RIGHT:
                        leftMotor.go(forwardPercentage);
                        rightMotor.go(turnCorrectionPercentage);
                        break;
                }
            }
        } else {
            leftMotor.go(0);
            rightMotor.go(0);
        }
    }
}

/**/
