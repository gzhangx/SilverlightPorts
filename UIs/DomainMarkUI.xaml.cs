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
   public partial class DomainMarkUI : UserControl
   {
      public int domainId { get; private set; }
      public DomainMarkUI(string txt, int domId)
      {
         InitializeComponent();
         domainId = domId;
         markText.Text = txt;
      }
      protected override Size MeasureOverride(Size availableSize)
      {
         markText.Measure(new Size(double.MaxValue, double.MaxValue));
         Canvas.SetLeft(markText, (this.Width - markText.DesiredSize.Width) / 2);
         return availableSize;
      }
   }
}
