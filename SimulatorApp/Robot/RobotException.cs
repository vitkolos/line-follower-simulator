namespace SimulatorApp;

class RobotException : ApplicationException {
    public RobotException(string message, Exception inner) : base(message, inner) { }
}
