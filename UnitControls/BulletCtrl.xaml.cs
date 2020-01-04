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
   public partial class BulletCtrl : UserControl
   {
      public IBullet TheBullet { get; private set; }
      public BulletCtrl(IBullet b)
      {
         TheBullet = b;
         InitializeComponent();
      }

      public void SyncUI()
      {
         Point p = TheBullet.GetPosition();
         Canvas.SetLeft(this, p.X);
         Canvas.SetTop(this, p.Y);
      }

   }
}
