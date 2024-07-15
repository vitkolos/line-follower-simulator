namespace CoreLibrary;

public abstract class Servo {
    public const int StopMicroseconds = 1500;

    public int Microseconds { get; private set; } = StopMicroseconds;

    public void WriteMicroseconds(int value) {
        Microseconds = value;
    }
}
