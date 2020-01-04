using System.Collections.Generic;
using System.Windows.Controls;
namespace SilverlightPorts
{
   public class UITransportPresenter
   {
      private ShipUI UIElement;
      public Transport Tran { get; set; }
      public UITransportPresenter(Transport tran)
      {
         UIElement = new ShipUI();
         Tran = tran;
         UIElement.Width = 10;
         UIElement.Height = 10;
         GlobalData.RootMapGraphics.Children.Add(UIElement);
      }
      private void ShowUI()
      {
         Canvas.SetLeft(UIElement, Tran.TPosX);
         Canvas.SetTop(UIElement, Tran.TPosY);
      }
      private void RemoveUI()
      {
         GlobalData.RootMapGraphics.Children.Remove(UIElement);
      }
      public static void ProcessShowTransportUI(Dictionary<int, UITransportPresenter> AlllTransports)
      {
         Dictionary<int, Transport> curTransports = new Dictionary<int, Transport>();
         foreach (PortInfo p in GlobalData.Ports)
         {
            p.PortBattleField.UIGetCurTransportSnapShot(curTransports);
         }

         foreach (int shipId in curTransports.Keys)
         {
            Transport tran = curTransports[shipId];
            if (!AlllTransports.ContainsKey(shipId))
            {
               AlllTransports.Add(shipId, new UITransportPresenter(tran));
            }
            else
            {
               AlllTransports[shipId].Tran = curTransports[shipId];
            }
         }

         List<int> ToBeDeleted = new List<int>();
         foreach (int shipId in AlllTransports.Keys)
         {
            UITransportPresenter pr = AlllTransports[shipId];
            pr.ShowUI();
            if (!curTransports.ContainsKey(shipId))
            {
               ToBeDeleted.Add(shipId);
            }
         }
         ToBeDeleted.ForEach(id =>
         {
            UITransportPresenter pr = AlllTransports[id];
            pr.RemoveUI();
            AlllTransports.Remove(id);
         });
      }
   }
}