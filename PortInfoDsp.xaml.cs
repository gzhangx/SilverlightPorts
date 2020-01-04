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
using System.Collections.ObjectModel;

namespace SilverlightPorts
{
   public partial class PortInfoDsp : UserControl
   {
      private PortShipInfo FleetInfo;
      public PortInfoDsp()
      {
         InitializeComponent();
      }
      private PortInfo ThisPort;
      public void SetData(PortInfo pinf)
      {
         ThisPort = pinf;
         BuildQueueGrid.DataContext = pinf.ShipBuildingQueue;
         this.DataContext = pinf;
         int playerDomainId = Player.ActivePlayer.Domain.DomainID;
         
         FleetInfo = pinf.DomainFleetInfo;
         List<PortShipInfo> dsp = new List<PortShipInfo>();
         dsp.Add(pinf.DomainFleetInfo);
         grid.ItemsSource = dsp;         
         acbSendTo.ItemsSource = GlobalData.Ports;
      }
      private void DeleteButton_Click(object sender, RoutedEventArgs e)
      {
         //TestData dat = (sender as FrameworkElement).DataContext as TestData;
         //data.Remove(dat);
      }

      private void SendButton_Click(object sender, RoutedEventArgs e)
      {
         Player.ActivePlayer.InSendToMode = true;
         Player.ActivePlayer.OnSendToPortChanged = (prt) =>
         {
            SelectSendToCover.Visibility = Visibility.Collapsed;
            if (Player.ActivePlayer.InSendToMode)
            {
               acbSendTo.SelectedItem = prt;
               //acbSendTo.Text = prt.Name;
               if (Player.ActivePlayer.CurrentSendToPort == null)
               {
                  MessageBox.Show("Please select a planet to send ships to");
                  return;
               }

               if (Player.ActivePlayer.CurrentSendToPort == ThisPort)
               {
                  MessageBox.Show("To and from ports must be different");
                  return;
               }

               PortShipInfo data = FleetInfo;
               if (data.SendCount != 0)
               {
                  data.Port.SendShipsOfGenTo(Player.ActivePlayer.Domain, Player.ActivePlayer.CurrentSendToPort, data.SendCount);
                  //Player.ActivePlayer.CurentSelectedPort
                  data.SendCount = 0;
               }
            }
         };
         if (Player.ActivePlayer.CurrentSendToPort != null)
         {            
            Player.ActivePlayer.OnSendToPortChanged(Player.ActivePlayer.CurrentSendToPort);
            Player.ActivePlayer.InSendToMode = false;
         }
         else
         {
            SelectSendToCover.Visibility = Visibility.Visible;
         }
      }

      private void ShipQueueTextBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         TextBox tb = (sender as TextBox);
         ShipBuildInfo inf = tb.DataContext as ShipBuildInfo;
         try
         {
            inf.CurrentBuildShipQueue = Convert.ToInt32(tb.Text);
         }
         catch { }

      }

      private void acbSendTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         AutoCompleteBox atb = sender as AutoCompleteBox;
         Player.ActivePlayer.CurrentSendToPort = atb.SelectedItem as PortInfo;
      }


      private void CancelSelectButton_Click(object sender, RoutedEventArgs e)
      {
         SelectSendToCover.Visibility = Visibility.Collapsed;
      }

      private void SendCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         TextBox tb = (sender as TextBox);
         PortShipInfo psinf = tb.DataContext as PortShipInfo;
         try
         {
            psinf.SendCount = Convert.ToInt32(tb.Text);
         }
         catch { };
      }
   }
}
