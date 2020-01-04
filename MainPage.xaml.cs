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
   public partial class MainPage : UserControl
   {
      private MapCreationParams creationParams = new MapCreationParams
      {
         MapSeed = 4,
         MapSize = 800,
         PlanetCount = 10,
         Opponents = 1,
         PlayerName = "PlayerName"
      };
      private DispatcherTimer timer;
      //PortInfo port;
      DomainInfo mydomain;

      private Dictionary<int, UITransportPresenter> AlllTransports = new Dictionary<int, UITransportPresenter>();
      //DomainInfo otherdomain;

      private Dictionary<int, PortUserControl> PortUIs = new Dictionary<int, PortUserControl>();
      
      private List<PortInfo> portsOwnerChanged = new List<PortInfo>();
      private object PortsOwnerChangeLock = new object();
      public PortInfo SelectedPort
      {
         get
         {
            return Player.ActivePlayer.CurentSelectedPort;
         }
      }

      private void ShowVer()
      {
         string ver = System.Reflection.Assembly.GetCallingAssembly().FullName;
         ver = ver.Substring(ver.IndexOf(",") + 1).Trim();
         ver = ver.Substring(0, ver.IndexOf(",")).Replace("=", " ");
         versiontxt.Text = ver;
      }
      public MainPage()
      {
         //port = new PortInfo("port1", 220, 80, 4, 1000000);
         //port.Population = 1000000;

         InitializeComponent();
         ShowVer();
         Loaded +=new RoutedEventHandler(MainPage_Loaded);
      }

      void MainPage_Loaded(object sender, RoutedEventArgs e)
      {
         doStartPrompt();
      }

      private void doStartPrompt()
      {
         UIAdminStartParamsChildWindow startPrm = new UIAdminStartParamsChildWindow(creationParams);
         startPrm.Closed += new EventHandler((snd, eve) =>
         {
            if (startPrm.DialogResult == true)
            {
               StartThis(creationParams);
            }
         });
         startPrm.Show();
      }
      private void StartThis(MapCreationParams creationParams)
      {
         foreach (PortUserControl u in PortUIs.Values)
         {
            groot.Children.Remove(u);
         }
         PortUIs.Clear();
         DomainInfo.ResetDomainCount();
         List<DomainInfo> domains = new List<DomainInfo>();
         //while (groot.Children.Count > 1)
         //{
         //   groot.Children.RemoveAt(1);
         //}
         //int mapSize = 800;
         groot.Width = creationParams.MapSize + 128;
         groot.Height = creationParams.MapSize + 128;
         GlobalData.RootMapGraphics = groot;
         List<PortInfo> ports = Utils.MakeMap(creationParams);
         if (ports.Count <= creationParams.Opponents + 1)
         {
            creationParams.Opponents = ports.Count - 1;
         }
         GlobalData.Ports = ports;
         GlobalData.Domains = domains;
         mydomain = new DomainInfo(creationParams.PlayerName, 1, 0.1);         
         mydomain.player = new Player(mydomain);
         mydomain.AddPort(ports[0]);
         domains.Add(mydomain);

         for (int i = 0; i < creationParams.Opponents; i++)
         {
            string ainame = ((char)('B' + i)).ToString();
            DomainInfo otherdomain = new DomainInfo(ainame, 1, 0.1);
            otherdomain.AddPort(ports[1]);
            domains.Add(otherdomain);
         }
         
         


         foreach (PortInfo p in ports)
         {
            PortUserControl PortUI = new PortUserControl(p);
            PortUIs.Add(p.ID, PortUI);
            Canvas.SetLeft(PortUI, p.PosX - (PortUI.Width/2));
            Canvas.SetTop(PortUI, p.PosY - (PortUI.Height / 2));
            GlobalData.RootMapGraphics.Children.Add(PortUI);
            //p.DoUIPlacement();
            PortUI.OnSelected = (pinf) =>
            {
               Player thePlayer = Player.ActivePlayer;
               if (thePlayer.InSendToMode)
               {
                  thePlayer.CurrentSendToPort = pinf;
                  thePlayer.InSendToMode = false;
                  return;
               }
               thePlayer.CurentSelectedPort = pinf;
               portInfo.SetData(pinf);
               Canvas.SetLeft(ShowDetailPan, pinf.PosX + 32);
               Canvas.SetTop(ShowDetailPan, pinf.PosY);
            };
            p.RaiseOwnerChangeEvent = (thep) =>
            {
               lock (PortsOwnerChangeLock)
               {
                  portsOwnerChanged.Add(thep);
               }
            };
         }

         domainInfo.DataContext = mydomain;
         portInfo.SetData(ports[0]);

         mainScrollViewer.UpdateLayout();
         mainScrollViewer.ScrollToHorizontalOffset(ports[0].PosX);
         mainScrollViewer.ScrollToVerticalOffset(ports[0].PosY);
         //shipSender.SetData(ports[0].FleetInfo);
         timer = new DispatcherTimer();
         timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
         timer.Tick += new EventHandler(timer_Tick);
         timer.Start();

         //for (int i = 0; i < 3; i++)
         //{
         //   fakeShip(mydomain, ports[1].PortBattleField).SetLocation(new Point(100*i, 100));
         //}

         //for (int i = 0; i < 1; i++)
         //{
         //   fakeShip(otherdomain, ports[0].PortBattleField).SetLocation(new Point(100 * i, 800));
         //}
         //fakeShip(mydomain, ports[0].PortBattleField);
         //fakeShip(mydomain, ports[0].PortBattleField);
         //fakeShip(mydomain, ports[0].PortBattleField);
         //fakeShip(mydomain, ports[0].PortBattleField);

         //fakeShip(otherdomain, ports[1].PortBattleField).SetLocation(new Point(100, 800));

         GlobalData.StartDomainThread();
      }


      public CombinedHaul fakeShip(DomainInfo domain, BattleField bf)
      {
         PortInfo port = domain.PortsSnapShot.First();
         Ship ship = new Ship(domain);
         CombinedHaul ch = CombinedHaul.CreateH(ship);

         bf.RequestAddHaulToBF(new AddHaulRequest { Haul = ch, FromPlanet = true });
         return ch;
      }

      private int CheckWinTimer = 0;
      void timer_Tick(object sender, EventArgs e)
      {
         GlobalData.DoTurn(PortUIs);

         CheckWinTimer++;
         if (CheckWinTimer > 100)
         {
            CheckWinTimer = 0;
            int winState = 0; //-1 lost, 1 win
            if (Player.ActivePlayer.Domain.PortsSnapShot.Count == 0)
            {
               timer.Stop();
               GlobalData.StopGame();
               winState = -1;               
            }
            else
            {
               int otherDomainPortCount = 0;
               foreach (DomainInfo dom in GlobalData.Domains)
               {
                  if (mydomain.IsFriendlyDomain(dom)) continue;
                  otherDomainPortCount += dom.PortsSnapShot.Count;
                  if (otherDomainPortCount > 0) break;
               }
               if (otherDomainPortCount == 0)
               {
                  timer.Stop();
                  GlobalData.StopGame();
                  winState = 1;                  
               }
            }
            if (winState != 0)
            {
               MessageBoxResult res = MessageBox.Show((winState > 0 ?"You Win!":"You Lose")+", want to play again?", "", MessageBoxButton.OKCancel);
               if (res == MessageBoxResult.OK)
               {
                  doStartPrompt();
               }
            }
         }
         List<PortInfo> ocports = new List<PortInfo>();
         lock (PortsOwnerChangeLock)
         {
            ocports.AddRange(portsOwnerChanged);
            portsOwnerChanged.Clear();
         }
         
         foreach (PortInfo p in ocports)
         {
            if (p.Domain == null)
            {
               PortUIs[p.ID].SetUiPortDomainMark(null);
            }
         }

         foreach (PortInfo p in GlobalData.Ports)
         {
            if (p.Domain == mydomain)
            {
               if (p.HasEnemyShip(mydomain))
               {
                  PortUIs[p.ID].StartInvationAlert();
               }
               else
               {
                  //PortUIs[p.ID].StopInvationAlert();
               }
            }
            else
            {
               //PortUIs[p.ID].StopInvationAlert();
            }
         }
         
         UITransportPresenter.ProcessShowTransportUI(AlllTransports);

         mydomain.DoUINotify();
         if (Player.ActivePlayer.CurentSelectedPort != null)
         {
            Player.ActivePlayer.CurentSelectedPort.PortBattleField.UIThreadUpdatePortInfo();
         }
         //double curMoney = port.MoneyCollected();
         //double scienceMoney = curMoney * mydomain.ScienceRate;
         //mydomain.CommitToScience(scienceMoney);
         //double left = curMoney - scienceMoney;

         //totalMoney += left;
         //IEnumerator<PortInfo> enu = mydomain.PortsSnapShot.GetEnumerator();
         //enu.MoveNext();

         //plate2.Text = "gen0 " + (int)mydomain.GetShipCost() + " " + (int)mydomain.GetShipMaintenanceCost() + "gen1 " + (int)mydomain.GetShipCost() + " " + (int)mydomain.GetShipMaintenanceCost();
         //PortInfo port = enu.Current;
         //if (port != null)
         //{
         //   plate.Text = "sc=" + port.GetShips().Count + " pop: " + port.Population + " money " + port.MoneyCollected().ToString("0.0") + " total " + mydomain.GetDspInfo().TotalMoney.ToString("0.0") + " SciPct " + mydomain.SciencePercent.ToString("0.00") + " gen= " + mydomain.Generation;
         //}
         //else
         //{
         //   plate.Text = "";
         //}

         bool setToVisible = false;
         if (SelectedPort != null)
         {
            if (!SelectedPort.PortBattleField.HasEnemyShip(Player.ActivePlayer.Domain))
            {
               if (SelectedPort.HasMyShip(mydomain))
               {
                  if (SelectedPort.Domain == null)
                  {
                     setToVisible = true;
                  }
                  else if (SelectedPort.Population == 0)
                  {
                     setToVisible = true;
                  }
               }
            }
         }
         if (setToVisible)
         {
            if (btnColonize.Visibility != Visibility.Visible) btnColonize.Visibility = Visibility.Visible;
         }
         else if (btnColonize.Visibility == Visibility.Visible)
         {
            btnColonize.Visibility = Visibility.Collapsed;
         }
      }

      private void btnShowDetail_Click(object sender, RoutedEventArgs e)
      {
         PortInfo port = SelectedPort;
         //port.PortBattleField.DrawingCanvas = new Canvas();
         BattleFieldChildWindow wnd = new BattleFieldChildWindow(SelectedPort.PortBattleField);
         wnd.Width = this.ActualWidth;
         wnd.Height = this.ActualHeight;
         //port.PortBattleField.AttachUI();
         wnd.Show();
      }

      
      private void Colonize_Click(object sender, RoutedEventArgs e)
      {
         long initialPop = SelectedPort.PortBattleField.GetMyShips(Player.ActivePlayer.Domain).Count * 1000;
         if (initialPop != 0)
         {
            GlobalData.ColonizePlanet(Player.ActivePlayer.Domain, SelectedPort, initialPop);
         }
      }

      private bool MouseDown = false;
      private PortInfo LastMouseOverPort { get; set; }

      private void groot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
      {
         MouseDown = true;
      }

      private void groot_MouseMove(object sender, MouseEventArgs e)
      {         
         if (MouseDown){
            LastMouseOverPort = null;
            PortInfo curPort = Player.ActivePlayer.CurentSelectedPort;
            if (curPort != null)
            {
               if (curPort.PortBattleField.CountMyShips(mydomain) != 0)
               {
                  shipSendBand.X1 = curPort.PosX;
                  shipSendBand.Y1 = curPort.PosY;
                  Point mousePoint = e.GetPosition(groot);
                  shipSendBand.X2 = mousePoint.X;
                  shipSendBand.Y2 = mousePoint.Y;
                  LastMouseOverPort = GetMouseOverPort(mousePoint);
                  if (LastMouseOverPort != null)
                  {
                     shipSendBand.X2 = LastMouseOverPort.PosX;
                     shipSendBand.Y2 = LastMouseOverPort.PosY;
                  }
               }
            }
         }
      }

      private void groot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
      {
         if (MouseDown)
         {
            shipSendBand.X1 = -1;
            shipSendBand.Y1 = -1;
            shipSendBand.X2 = -1;
            shipSendBand.Y2 = -1;
            PortInfo curPort = Player.ActivePlayer.CurentSelectedPort;
            if (LastMouseOverPort != null && LastMouseOverPort != curPort)
            {               
               curPort.SendShipsOfGenTo(mydomain, LastMouseOverPort, 1);
               portInfo.acbSendTo.Text = LastMouseOverPort.Name;
               LastMouseOverPort = null;
            }
         }
         MouseDown = false;
      }

      private PortInfo GetMouseOverPort(Point p)
      {
         foreach (PortInfo port in GlobalData.Ports)
         {
            double xdiff = Math.Abs(p.X - port.PosX);
            double ydiff = Math.Abs(p.Y - port.PosY);
            if (xdiff <= 32 && ydiff <= 32)
            {
               return port;
            }
         }
         return null;
      }
   }
}
