using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverlightPorts
{
   public interface BaseAngleInfo
   {
      double ActualHeight { get; }      
      double ActualWidth { get; }
      double BaseAngle
      {
         get;
      }
      //UIElement BaseUIElement
      //{
      //   get;
      //}
      double PosX { get; }
      double PosY { get; }
   }
}
