namespace SimulatorApp;

readonly record struct SensorPosition(float X, float Y);
readonly record struct RobotPosition(float X, float Y, float Rotation);
readonly record struct PositionHistoryItem(RobotPosition Position, int Time);