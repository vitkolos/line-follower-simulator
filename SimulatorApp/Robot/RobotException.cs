namespace SimulatorApp;

/// <summary>
/// Wraps an exception thrown inside the robot inner logic to be processed at an upper level
/// </summary>
class RobotException : ApplicationException {
    public RobotException(string message, Exception inner) : base(message, inner) { }
}
