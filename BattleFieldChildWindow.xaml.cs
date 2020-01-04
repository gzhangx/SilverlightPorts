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
using System.Windows.Threading;

namespace SilverlightPorts
{
   public partial class BattleFieldChildWindow : ChildWindow
   {
      private Dictionary<int,BasicHaulUI> shipIdToUIShips = new Dictionary<int,BasicHaulUI>();
      private Dictionary<int, BasicHaulUI> specialUIShips = new Dictionary<int, BasicHaulUI>();
      private Dictionary<int, BulletCtrl> bulletUis = new Dictionary<int, BulletCtrl>();
      private DispatcherTimer uiTimer;
      private Canvas _content;
      public BattleFieldChildWindow(BattleField PortBattleField)
      {
         _content = new Canvas();
         PortBattleField.DrawingCanvas = _content;
         double canW = PortBattleField.FieldWidth;
         double canH = PortBattleField.FieldHeight;
         InitializeComponent();
         contentGrid.Width = canW;
         contentGrid.Height = canH;
         contentGrid.Children.Add(_content);
         uiTimer = new DispatcherTimer();
         uiTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
         uiTimer.Tick += new EventHandler((snd, tice) =>
         {
            PortBattleField.UILoopShowAI(shipIdToUIShips, specialUIShips, ProcessBullets, _content);
         });
         uiTimer.Start();
         this.Closing += new EventHandler<System.ComponentModel.CancelEventArgs>((cls, cle) =>
         {
            uiTimer.Stop();
         });
      }

      private void ProcessBullets(List<IBullet> bullets)
      {
         Dictionary<int, bool> hasControl = new Dictionary<int, bool>();
         foreach (IBullet b in bullets)
         {
            hasControl.Add(b.BulletId, true);
            if (!bulletUis.ContainsKey(b.BulletId))
            {
               BulletCtrl bui = new BulletCtrl(b);
               bulletUis.Add(bui.TheBullet.BulletId, bui);
               _content.Children.Add(bui);               
            }
         }
         List<int> removed = new List<int>();
         foreach (BulletCtrl bui in bulletUis.Values)
         {
            if (hasControl.ContainsKey(bui.TheBullet.BulletId))
            {
               bui.SyncUI();
            }
            else
            {
               removed.Add(bui.TheBullet.BulletId);
               _content.Children.Remove(bui);
               if (bui.TheBullet.HitOn != null)
               {
                  if (bui.TheBullet.HitOn is PlanetHaul)
                  {
                     Point p = bui.TheBullet.GetPosition();
                     SilverlightPorts.UnitControls.SmallExplosion exp = new SilverlightPorts.UnitControls.SmallExplosion(_content, p);
                  }
                  else if (bui.TheBullet.HitOn is CombinedHaul)
                  {
                     CombinedHaul ship = bui.TheBullet.HitOn as CombinedHaul;
                     if (shipIdToUIShips.ContainsKey(ship.ShipInfo.ShipId))
                     {
                        BasicHaulUI ui = shipIdToUIShips[ship.ShipInfo.ShipId];
                        Point p = ui.Haul.TranslateWorldLocaionToShipCord(bui.TheBullet.GetPosition());
                        SilverlightPorts.UnitControls.SmallExplosion exp = new SilverlightPorts.UnitControls.SmallExplosion(ui.canv, p);
                     }
                  }
               }
            }
         }
         removed.ForEach(key => bulletUis.Remove(key));
      }
      private void OKButton_Click(object sender, RoutedEventArgs e)
      {
         this.DialogResult = true;
      }

      private void CancelButton_Click(object sender, RoutedEventArgs e)
      {
         this.DialogResult = false;
      }
   }
}

