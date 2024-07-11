namespace CoreLibrary;

public abstract class RobotBase {
    private const int PinCount = 32;
    public const int SensorsCount = 5;

    private long _millis = 0;
    private readonly PMode[] _pinModes = new PMode[PinCount];
    private readonly bool[] _pinValues = new bool[PinCount];

    public long Millis() => _millis;

    public void AddMillis(int toBeAdded) { // fixme
        _millis += toBeAdded;
    }

    public abstract void Setup();

    public abstract void Loop();

    public abstract (int, int) MotorsMicroseconds { get; }

    public abstract int FirstSensorPin { get; }


    public void PinMode(int pin, PMode mode) {
        _pinModes[pin] = mode;
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
