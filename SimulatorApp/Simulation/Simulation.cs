namespace SimulatorApp;

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
