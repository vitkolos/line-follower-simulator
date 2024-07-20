using CoreLibrary;

namespace SimulatorApp;

public class DummyRobot : RobotBase {
    public override MotorsState MotorsMicroseconds => new(Servo.StopMicroseconds, Servo.StopMicroseconds);
    public override int FirstSensorPin => 3;
    public override void Loop() { }
    public override void Setup() { }
}
