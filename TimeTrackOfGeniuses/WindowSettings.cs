using System;
using System.Windows;

namespace TimeTrackOfGeniuses
{
  [Serializable]
  public class WindowSettings
  {
    public double Width { get; set; }
    public double Height { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public WindowState WindowState { get; set; }
  }
}
