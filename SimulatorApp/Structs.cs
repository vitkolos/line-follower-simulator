using System.Windows.Controls;

namespace SimulatorApp;

readonly record struct SensorPosition(float X, float Y);
readonly record struct RobotPosition(float X, float Y, float Rotation);
readonly record struct PositionHistoryItem(RobotPosition Position, int Time);
readonly record struct RobotConfig(float Size, float SensorDistance, float Speed);
readonly record struct RobotSetup(RobotPosition Position, RobotConfig Config);
readonly record struct PinControl(int Pin, bool IsLed, Control Control);
