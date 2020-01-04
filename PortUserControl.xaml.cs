using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace SilverlightPorts
{
   public partial class PortUserControl : UserControl
   {
      PortInfo _PortInfo;
      public Action<PortInfo> OnSelected;
      public PortUserControl(PortInfo pinf)
      {
         _PortInfo = pinf;
         InitializeComponent();
         this.DataContext = _PortInfo;
         this.Width = _PortInfo.PortSize * 64 / 6;
         this.Height = _PortInfo.PortSize * 64 / 6;
         alertStory.Completed += new EventHandler((tt, yy) =>
         {
            alertWarn.Visibility = Visibility.Collapsed;
         });
      }

      private DomainMarkUI curDomainUIMark = null;
      public bool NeedSetUiPortDomainMark(int domId)
      {
         if (curDomainUIMark == null || curDomainUIMark.domainId != domId)
            return true;
         return false;
      }
      public void SetUiPortDomainMark(DomainMarkUI mark)
      {
         if (curDomainUIMark == null || mark == null || curDomainUIMark.domainId != mark.domainId)
         {
            symbolPlate.Children.Clear();
            curDomainUIMark = mark;
            if (mark != null)
            {
               symbolPlate.Children.Add(mark);
            }
         }
      }

      public void StartInvationAlert()
      {
         if (alertWarn.Visibility == Visibility.Collapsed)
         {
            alertStory.Begin();
            alertWarn.Visibility = Visibility.Visible;
         }
      }
      //public void StopInvationAlert()
      //{
      //   alertStory.Stop();
      //   alertWarn.Visibility = Visibility.Collapsed;
      //   alertWarn.Opacity = 0;
      //}
      protected override Size MeasureOverride(Size availableSize)
      {
          plate.Measure(new Size(double.MaxValue, double.MaxValue));
          Canvas.SetLeft(plate,(this.Width - plate.DesiredSize.Width)/2);
          return availableSize;
      }

      private void LayoutRoot_MouseEnter(object sender, MouseEventArgs e)
      {
         SetSelected(true);
      }

      private void LayoutRoot_MouseLeave(object sender, MouseEventArgs e)
      {
         SetSelected(false);
      }

      private void LayoutRoot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
      {
         //MessageBox.Show("click");
         if (IsMouseDown)
         {
            SetSelected(true);
            OnSelected(_PortInfo);
         }
         IsMouseDown = false;
      }

      private void SetSelected(bool sel)
      {
         (surface.Fill as GradientBrush).GradientStops[0].Offset = sel?0.2:0.06;         
      }

      private bool IsMouseDown = false;
      private void LayoutRoot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
      {
         IsMouseDown = true;
      }
   }
}
