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
   public partial class UIAdminStartParamsChildWindow : ChildWindow
   {
      private MapCreationParams prms;

      public UIAdminStartParamsChildWindow(MapCreationParams p)
      {
         InitializeComponent();
         prms = p;
         txtMapNumber.Text = p.MapSeed.ToString();
         txtMapSize.Text = p.MapSize.ToString();
         txtOpponents.Text = p.Opponents.ToString();
         txtName.Text = p.PlayerName;
      }

      private void OKButton_Click(object sender, RoutedEventArgs e)
      {
         prms.PlayerName = txtName.Text;
         try
         {
            prms.MapSeed = Convert.ToInt32(txtMapNumber.Text);
         }
         catch
         {
            prms.MapSeed = new Random(DateTime.Now.Millisecond).Next(DateTime.Now.Millisecond);
         }
         prms.PlanetCount = Convert.ToInt32(txtNumPlanets.Text);
         prms.Opponents = Convert.ToInt32(txtOpponents.Text);
         prms.MapSize = Convert.ToInt32(txtMapSize.Text);
         this.DialogResult = true;
      }

      private void CancelButton_Click(object sender, RoutedEventArgs e)
      {
         this.DialogResult = false;
      }
   }
}

