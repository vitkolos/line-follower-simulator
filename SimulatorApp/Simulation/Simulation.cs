namespace SimulatorApp;

/// <summary>
/// Base class for various simulation types
/// </summary>
/// <param name="canvas">The canvas is used to display the simulation progress/results</param>
/// <param name="map">Map the robot is driving on</param>
abstract class Simulation(Canvas canvas, Map map) : IDisposable {
    protected Canvas _canvas = canvas;
    protected Map _map = map;
    protected bool _disposed = false;
    public bool Running {
        get => _running;
        protected set {
            _running = value;
            StateChange(value);
        }
    }
    protected bool _running = false;
    public event Action<bool> StateChange = _ => { };

    public abstract void Dispose();
}
