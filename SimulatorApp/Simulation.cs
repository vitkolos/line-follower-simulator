using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;

using CoreLibrary;
using System.Drawing.Drawing2D;

namespace SimulatorApp;

abstract class Simulation : IDisposable {
    public abstract void Dispose();
}
