using CoreLibrary;

namespace SimulatorApp;

/// <summary>
/// If the user did not provide an assembly with a valid RobotBase child, this robot is used instead to improve user experience
/// (e.g. it enables the user to set robot position and size before loading an assembly)
/// </summary>
public class DummyRobot : RobotBase {
    public override MotorsState MotorsMicroseconds => new(Servo.StopMicroseconds, Servo.StopMicroseconds);
    public override int FirstSensorPin => 3;
    public override void Loop() { }
    public override void Setup() { }
}
