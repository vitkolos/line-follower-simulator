
using CoreLibrary;

namespace TestSuite;

class FakeRobot : RobotBase {
    public override MotorsState MotorsMicroseconds => new(Servo.StopMicroseconds + LeftSpeed, Servo.StopMicroseconds - RightSpeed);

    public override int FirstSensorPin => 10;

    public int LeftSpeed = 0;
    public int RightSpeed = 0;
    public int LoopCalled = 0;
    public int SetupCalled = 0;

    public override void Loop() {
        LoopCalled++;
    }

    public override void Setup() {
        PinMode(1, PMode.Output);
        PinMode(2, PMode.Output);
        PinMode(3, PMode.InputPullup);
        PinMode(4, PMode.InputPullup);
        SetupCalled++;
    }
}
