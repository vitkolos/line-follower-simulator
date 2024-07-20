
using CoreLibrary;

using SimulatorApp;

namespace TestSuite;

class FakeRobot : RobotBase {
    public override MotorsState MotorsMicroseconds => new(Servo.StopMicroseconds + LeftSpeed, Servo.StopMicroseconds - RightSpeed);

    public override int FirstSensorPin => 3;

    public int LeftSpeed = 0;
    public int RightSpeed = 0;
    public int LoopCalled = 0;
    public int SetupCalled = 0;

    public override void Loop() {
        LoopCalled++;
    }

    public override void Setup() {
        SetupCalled++;
    }
}
