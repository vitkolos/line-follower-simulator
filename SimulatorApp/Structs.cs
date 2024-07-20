namespace SimulatorApp;

public readonly record struct SensorPosition(float X, float Y);
public readonly record struct RobotPosition(float X, float Y, float Rotation);
public readonly record struct PositionHistoryItem(RobotPosition Position, int Time);
public readonly record struct RobotConfig(float Size, float SensorDistance, float Speed);
public readonly record struct RobotSetup(RobotPosition Position, RobotConfig Config);
