namespace CoreLibrary;

public abstract class RobotBase {
    public const int PinCount = 32;
    public const int SensorsCount = 5;

    private long _millis = 0;
    private readonly PMode[] _pinModes = new PMode[PinCount];
    private readonly bool[] _pinValues = new bool[PinCount];

    public long Millis() => _millis;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:RemoveUnusedPrivateMember", Justification = "called using reflection from the simulation app")]
    private void AddMillis(int toBeAdded) {
        _millis += toBeAdded;
    }

    public abstract void Setup();
    public abstract void Loop();
    public abstract MotorsState MotorsMicroseconds { get; }
    public abstract int FirstSensorPin { get; }
    public virtual string InternalState => "";

    public void PinMode(int pin, PMode mode) {
        _pinModes[pin] = mode;

        if (mode == PMode.InputPullup) {
            _pinValues[pin] = true;
        }
    }

    public bool DigitalRead(int pin) {
        if (_pinModes[pin] == PMode.Input || _pinModes[pin] == PMode.InputPullup) {
            return _pinValues[pin];
        } else {
            throw new InvalidOperationException("this pin should not be read");
        }
    }

    public void DigitalWrite(int pin, bool value) {
        if (_pinModes[pin] == PMode.Output) {
            _pinValues[pin] = value;
        } else {
            throw new InvalidOperationException("this pin should not be set");
        }
    }
}

public enum PMode {
    Input,
    Output,
    InputPullup
}

public abstract class Servo {
    public const int StopMicroseconds = 1500;

    public int Microseconds { get; private set; } = StopMicroseconds;

    public void WriteMicroseconds(int value) {
        Microseconds = value;
    }
}

public readonly record struct MotorsState(int Left, int Right);
