namespace CoreLibrary;

/// <summary>
/// It is not neccessary to use Servo class but it may be useful. It can act as a replacement for a class of the same name in the Servo library
/// </summary>
public class Servo {
    public const int StopMicroseconds = 1500;
    public int Microseconds { get; private set; } = StopMicroseconds;

    public void WriteMicroseconds(int value) {
        Microseconds = value;
    }
}
