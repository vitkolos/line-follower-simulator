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
            EvaluatePosition(pr, interval);
        }
    }

    private static void EvaluatePosition(PositionedRobot positionedRobot, int elapsedMillis) {
        float wheelDistance = 200f;
        float speedCoefficient = 0.01f;

        Position oldPosition = positionedRobot.Position;
        Position newPosition = new();

        (int leftMicroseconds, int rightMicroseconds) = positionedRobot.Robot.MotorsMicroseconds;
        Console.WriteLine(positionedRobot.Robot.MotorsMicroseconds);
        float leftSpeed = (leftMicroseconds - Servo.StopMicroseconds) * speedCoefficient;
        float rightSpeed = (-rightMicroseconds + Servo.StopMicroseconds) * speedCoefficient;

        if (leftSpeed == rightSpeed) {
            float distance = leftSpeed * elapsedMillis;
            newPosition.X = (float)(oldPosition.X + distance * Math.Cos(oldPosition.Rotation));
            newPosition.Y = (float)(oldPosition.Y + distance * Math.Sin(oldPosition.Rotation));
            newPosition.Rotation = oldPosition.Rotation;
        } else {
            float rotationChange = (rightSpeed - leftSpeed) / wheelDistance;
            float turnRadius = wheelDistance * (rightSpeed + leftSpeed) / (2 * (rightSpeed - leftSpeed));
            newPosition.X = (float)(oldPosition.X + turnRadius * (Math.Sin(rotationChange * elapsedMillis + oldPosition.Rotation) - Math.Sin(oldPosition.Rotation)));
            newPosition.Y = (float)(oldPosition.Y - turnRadius * (Math.Cos(rotationChange * elapsedMillis + oldPosition.Rotation) - Math.Cos(oldPosition.Rotation)));
            newPosition.Rotation = oldPosition.Rotation + rotationChange;
        }

        positionedRobot.Position = newPosition;
    }
}

record class PositionedRobot {
    public RobotBase Robot;
    public Position Position;

    public PositionedRobot(RobotBase robot) {
        Robot = robot;
    }
}

record struct Position(float X, float Y, float Rotation);
