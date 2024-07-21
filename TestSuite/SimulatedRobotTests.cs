using SkiaSharp;
using SimulatorApp;

namespace TestSuite;

public class SimulatedRobotTests : IDisposable {
    private readonly BoolBitmap _boolBitmap;
    private readonly RobotSetup _setup;

    public SimulatedRobotTests() {
        // runs for each test
        var bitmap = new SKBitmap();
        _boolBitmap = new BoolBitmap(bitmap);
        _setup = new RobotSetup(new RobotPosition(0, 0, 0), new RobotConfig(1, 1, 1));
    }

    public void Dispose() {
        _boolBitmap.Dispose();
    }

    [Fact]
    public void SetupCalled() {
        var robot = new FakeRobot();
        Assert.Equal(0, robot.SetupCalled);

        var simulatedRobot = new SimulatedRobot(robot, _setup, _boolBitmap, 1);
        Assert.Equal(1, robot.SetupCalled);

        simulatedRobot.MoveNext(10);
        Assert.Equal(1, robot.SetupCalled);
    }

    [Fact]
    public void LoopCalled() {
        var robot = new FakeRobot();
        Assert.Equal(0, robot.LoopCalled);

        var simulatedRobot = new SimulatedRobot(robot, _setup, _boolBitmap, 1);
        Assert.Equal(1, robot.LoopCalled);

        simulatedRobot.MoveNext(10);
        Assert.Equal(2, robot.LoopCalled);

        simulatedRobot.MoveNext(10);
        Assert.Equal(3, robot.LoopCalled);
    }

    [Fact]
    public void ButtonsLeds() {
        var robot = new FakeRobot();
        var simulatedRobot = new SimulatedRobot(robot, _setup, _boolBitmap, 1);

        List<int> buttons = simulatedRobot.GetButtons().ToList();
        List<int> leds = simulatedRobot.GetLeds().ToList();

        Assert.Equal(2, leds.Count);
        Assert.Equal(1, leds[0]);
        Assert.Equal(2, leds[1]);

        Assert.Equal(2, buttons.Count);
        Assert.Equal(3, buttons[0]);
        Assert.Equal(4, buttons[1]);
    }

    [Fact]
    public void ButtonsFunction() {
        var robot = new FakeRobot();
        var simulatedRobot = new SimulatedRobot(robot, _setup, _boolBitmap, 1);
        List<int> buttons = simulatedRobot.GetButtons().ToList();

        Assert.Equal(2, buttons.Count);

        Assert.True(simulatedRobot.GetPinStatus(buttons[0]));
        Assert.True(simulatedRobot.GetPinStatus(buttons[1]));

        simulatedRobot.SetButton(buttons[0], true);
        Assert.False(simulatedRobot.GetPinStatus(buttons[0]));
        Assert.True(simulatedRobot.GetPinStatus(buttons[1]));

        simulatedRobot.SetButton(buttons[0], false);
        Assert.True(simulatedRobot.GetPinStatus(buttons[0]));
        Assert.True(simulatedRobot.GetPinStatus(buttons[1]));
    }

    [Fact]
    public void PositionHistoryTiming() {
        var robot = new FakeRobot();
        var simulatedRobot = new SimulatedRobot(robot, _setup, _boolBitmap, 1);
        int[] times = { 5, 6, 4, 15, 25, 3 };

        for (int i = 0; i < times.Length; i++) {
            simulatedRobot.MoveNext(times[i]);
        }

        IReadOnlyList<PositionHistoryItem> positionHistory = simulatedRobot.GetPositionHistory();
        Assert.Equal(times.Length + 1, positionHistory.Count);
        int total = 0;
        Assert.Equal(total, positionHistory[0].Time);

        for (int i = 1; i < positionHistory.Count; i++) {
            total += times[i - 1];
            Assert.Equal(total, positionHistory[i].Time);
        }
    }

    private (RobotPosition, RobotPosition) MoveRobot(int angleDeg) {
        var robot = new FakeRobot();
        float angle = (float)(angleDeg / 180f * Math.PI);
        var customSetup = new RobotSetup(new RobotPosition(0, 0, angle), new RobotConfig(1, 1, 1));
        var simulatedRobot = new SimulatedRobot(robot, customSetup, _boolBitmap, 1);
        robot.LeftSpeed = 50;
        robot.RightSpeed = 50;
        simulatedRobot.MoveNext(1000);
        IReadOnlyList<PositionHistoryItem> positionHistory = simulatedRobot.GetPositionHistory();
        Assert.Equal(2, positionHistory.Count);
        return (positionHistory[0].Position, positionHistory[1].Position);
    }

    [Fact]
    public void DiagonalDrive() {
        var (pos1, pos2) = MoveRobot(45);
        Assert.True(pos1.X < pos2.X);
        Assert.True(pos1.Y < pos2.Y);
        Assert.Equal(pos2.X, pos2.Y, 3);
    }

    [Fact]
    public void HorizontalDrive() {
        var (pos1, pos2) = MoveRobot(0);
        Assert.True(pos1.X < pos2.X);
        Assert.Equal(pos1.Y, pos2.Y, 3);
    }

    [Fact]
    public void VerticalDrive() {
        var (pos1, pos2) = MoveRobot(90);
        Assert.Equal(pos1.X, pos2.X, 3);
        Assert.True(pos1.Y < pos2.Y);
    }

    [Fact]
    public void MiddleSensor() {
        var robot = new FakeRobot();
        var bitmap = new SKBitmap(5, 5);
        var boolBitmap = new BoolBitmap(bitmap);
        var customSetup = new RobotSetup(new RobotPosition(0, 0, 0), new RobotConfig(1, 1, 1));
        var simulatedRobot = new SimulatedRobot(robot, customSetup, boolBitmap, 1);

        // X = 1, Y = 0 in "standard" coordinates corresponds to X = 1, Y = 4 in "bitmap" coordinates
        bitmap.SetPixel(1, 4, SKColors.White);
        simulatedRobot.MoveNext(10);
        Assert.True(simulatedRobot.GetPinStatus(12));
        bitmap.SetPixel(1, 4, SKColors.Black);
        simulatedRobot.MoveNext(10);
        Assert.False(simulatedRobot.GetPinStatus(12));
    }
}
