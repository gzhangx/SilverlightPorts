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
   public class Transport
   {
      public IHual Ship { get; private set; }
      public PortInfo ToPort { get; private set; }
      public Point FromPoint = new Point();
      public double Angle { get; private set; }
      public double Speed { get { return 0.5; } }
      public int Steps { get; set; }
      public int TotalSteps { get; private set; }
      //public ShipUI UIElement = null;
      public Transport(IHual s, PortInfo fromp, PortInfo toPort)
      {
         s.CurrentTransport = this;
         //s.SetBattleField(null);
         FromPoint.X = fromp.PosX;
         FromPoint.Y = fromp.PosY;
         Angle = RotateUtils.GetAngle(toPort.PosX, toPort.PosY, fromp.PosX, fromp.PosY);         
         Ship = s;
         ToPort = toPort;
         TotalSteps = (int)(fromp.DistOf(toPort) / Speed);
         //Canvas.SetLeft(UIElement, fromp.PosX);
         //Canvas.SetTop(UIElement, fromp.PosY);         
      }

      public double TPosX { get { return FromPoint.X + (Steps * Speed * (Math.Cos(Angle))); } }
      public double TPosY { get { return FromPoint.Y + (Steps * Speed * (Math.Sin(Angle))); } }

      //public void ShowUI()
      //{
      //   if (UIElement == null)
      //   {
      //      UIElement = new ShipUI();
      //      UIElement.Width = 10;
      //      UIElement.Height = 10;
      //      GlobalData.RootMapGraphics.Children.Add(UIElement);
      //   }
      //   Canvas.SetLeft(UIElement, FromPoint.X + (Steps * Speed * (Math.Cos(Angle))));
      //   Canvas.SetTop(UIElement, FromPoint.Y + (Steps * Speed * (Math.Sin(Angle))));
      //}
      //public void RemoveUI()
      //{
      //   if (UIElement != null)
      //   {
      //      GlobalData.RootMapGraphics.Children.Remove(UIElement);
      //   }
      //}
      public bool NextStep()
      {
         Steps++;
         if (Steps >= TotalSteps)
         {            
            return true;
         }         
         return false;
      }
   }

   public class TransportRequest
   {
      public IHual Ship { get; set; }
      public PortInfo ToPort { get; set; }
   }
}
