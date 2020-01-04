using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverlightPorts
{
   public partial class WarpUI : UserControl
   {
      public WarpUI()
      {
         InitializeComponent();
         (LayoutRoot.Resources["story1"] as Storyboard).Begin();
      }

      private void story1_Completed(object sender, EventArgs e)
      {
         (this.Parent as Canvas).Children.Remove(this);
      }
   }
}
