using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using Bitmap = System.Drawing.Bitmap;

using CoreLibrary;
using System.Drawing.Drawing2D;

namespace SimulatorApp;

class ParallelSimulation : Simulation {
    // private Image _map;
    // private RobotPosition _startingPosition;
    // public SimulatedRobot[] Robots = new SimulatedRobot[1];
    
    public override void Dispose() {
        throw new NotImplementedException();
    }
}
