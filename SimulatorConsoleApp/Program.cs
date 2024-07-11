using CoreLibrary;

using UserDefinedRobot; // fixme

namespace SimulatorConsoleApp;

class Program {
    public static void Main() {
        int interval = 10;

        var pr = new PositionedRobot(new Robot());
        pr.Position.X = 5;
        pr.Position.Y = 5;
        pr.Robot.Setup();

        for (int i = 0; i < 20; i++) {
            pr.Robot.AddMillis(interval);
            pr.Robot.Loop();
            Console.WriteLine(pr);
            SimulationCore.EvaluatePosition(pr, interval);
        }
    }
}

public class SimulationCore {
    public static void EvaluatePosition(PositionedRobot positionedRobot, int elapsedMillis) {
        float wheelDistance = 20f;
        float speedCoefficient = 1f; // 1f means that 1600 (1500+100) microseconds equals 100 px/s
        float elapsedSeconds = elapsedMillis / 1000f;

        Position oldPosition = positionedRobot.Position;
        Position newPosition = new();

        (int leftMicroseconds, int rightMicroseconds) = positionedRobot.Robot.MotorsMicroseconds;
        // Console.WriteLine(positionedRobot.Robot.MotorsMicroseconds);
        float leftSpeed = (leftMicroseconds - Servo.StopMicroseconds) * speedCoefficient;
        float rightSpeed = (-rightMicroseconds + Servo.StopMicroseconds) * speedCoefficient;

        if (leftSpeed == rightSpeed) {
            float distance = leftSpeed * elapsedSeconds;
            newPosition.X = (float)(oldPosition.X + distance * Math.Cos(oldPosition.Rotation));
            newPosition.Y = (float)(oldPosition.Y + distance * Math.Sin(oldPosition.Rotation));
            newPosition.Rotation = oldPosition.Rotation;
        } else {
            float rotationChange = (rightSpeed - leftSpeed) * elapsedSeconds / wheelDistance;
            float turnRadius = wheelDistance * (rightSpeed + leftSpeed) / (2 * (rightSpeed - leftSpeed));
            newPosition.X = (float)(oldPosition.X + turnRadius * (Math.Sin(rotationChange + oldPosition.Rotation) - Math.Sin(oldPosition.Rotation)));
            newPosition.Y = (float)(oldPosition.Y - turnRadius * (Math.Cos(rotationChange + oldPosition.Rotation) - Math.Cos(oldPosition.Rotation)));
            newPosition.Rotation = oldPosition.Rotation + rotationChange;
        }

        positionedRobot.Position = newPosition;
    }
}

public record class PositionedRobot {
    public RobotBase Robot;
    public Position Position;

    public PositionedRobot(RobotBase robot) {
        Robot = robot;
    }
}

public record struct Position(float X, float Y, float Rotation);
