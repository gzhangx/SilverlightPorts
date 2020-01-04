using System;
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace SilverlightPorts
{
   public class PortInfo : INotifyPropertyChanged
   {
      public object PortLock = new object();
      public List<PortDistInfo> PortDists { get; private set; }
      //public PortUserControl PortUI { get; private set; }
      public Action<PortInfo> RaiseOwnerChangeEvent;
      public int CachedEnemyShipCountThisTurn = 0;
      public int CachedMyShipCountThisTurn = 0;
      public int CachedArrivingShipCountThisTurn = 0;
      public override string ToString()
      {
         return Name;
      }
      public double DistOf(PortInfo p)
      {
         double dx = p.PosX - PosX;
         double dy = p.PosY - PosY;
         dx *= dx;
         dy *= dy;
         return Math.Sqrt(dx + dy);
      }
      public bool HasEnemyShip(DomainInfo dom)
      {
         return PortBattleField.HasEnemyShip(dom);
      }
      private static int idgen = 0;
      public int ID { get; private set; }
      public DomainInfo Domain { get; private set; }
      public BattleField PortBattleField { get; private set; }
      public PortShipInfo DomainFleetInfo { get; private set; }
      public List<IHual> GetShips(DomainInfo dom)
      {
         return PortBattleField.GetMyShips(dom);
      }
      public bool HasMyShip(DomainInfo dom)
      {
         return PortBattleField.GetShipCount(dom) > 0;
      }

      public ShipBuildInfo ShipBuildingQueue { get; private set; }
      public void SetBuildShipQueue(int num)
      {
         //ShipBuildingQueue.CurrentBuildShipCost = Domain.GetShipCost();
         ShipBuildingQueue.CurrentBuildShipQueue = num;
      }

      public int PortSize { get; private set; }
      private Dictionary<string, bool> Visits = new Dictionary<string, bool>();
      public void Visit(DomainInfo domain)
      {
         if (HasVisitedBy(domain)) return;
         Visits[domain.Name] = true;
      }
      public bool HasVisitedBy(DomainInfo domain)
      {
         return Visits.ContainsKey(domain.Name);
      }
      public void SetOwner(DomainInfo domain)
      {
         lock (PortLock)
         {
            Domain = domain;
            ShipBuildingQueue.SetDomain(domain);
         }
         Action<PortInfo> handl = RaiseOwnerChangeEvent;
         if (handl != null) handl(this);
      }
      public void TryReleaseOwner(DomainInfo owner)
      {
         lock (PortLock)
         {
            if (Domain == owner)
            {
               Domain = null;
               ShipBuildingQueue.SetDomain(null);
            }
         }
         Action<PortInfo> handl = RaiseOwnerChangeEvent;
         if (handl != null) handl(this);

      }
      public PortInfo(string name, int x, int y, int size, long pop)
      {
         PortDists = new List<PortDistInfo>();
         unchecked
         {
            ID = idgen++;
         }
         PortBattleField = new BattleField(this);
         DomainFleetInfo = new PortShipInfo(this);

         Name = name;
         PosX = x;
         PosY = y;
         PortSize = size;
         Population = pop;
         ShipBuildingQueue = new ShipBuildInfo(this);
      }
      //public void DoUIPlacement()
      //{
      //   PortUI = new PortUserControl(this);

      //   Canvas.SetLeft(PortUI, PosX);
      //   Canvas.SetTop(PortUI, PosY);
      //   GlobalData.RootMapGraphics.Children.Add(PortUI);
      //}

      
      

      public string Name { get; private set; }
      public int PosX { get; private set; }
      public int PosY { get; private set; }
      public void MovePos(int x, int y)
      {
         PosX = x;
         PosY = y;
         //Canvas.SetLeft(PortUI, PosX);
         //Canvas.SetTop(PortUI, PosY);
      }
      public long MaxPopulation
      {
         get
         {
            return GetMaxPopulationFromSize(PortSize);
         }
      }
      private long _internalPopulation;
      public long Population
      {
         get { return _internalPopulation; }
         private set
         {
            _internalPopulation = value;
            //if (PortUI != null)
            //{
            //   PortUI.Dispatcher.BeginInvoke(() =>
            //   {
            //      OnPropertyChange("Population");
            //   });
            //}
         }
      }
      public void InitColonizeAddPop(long pop)
      {
         Population = pop;
      }
      public void DamageReducePop(long amount)
      {
         if (Population > amount)
         {
            Population -= amount;
         }
         else
            Population = 0;
      }
      public double TaxRate
      {
         get {
            if (Domain == null) return 0;
            return Domain.TaxRate; 
         }
      }

      public const double BaseGrowthRate = 0.01;
      //private ObservableCollection<Fleet> Fleets = new ObservableCollection<Fleet>();
      const int PortBaseCost = 400;
      public static double MaxPortCost { get { return PortBaseCost; } }
      private double InternalPortCost
      {
         get
         {
            if (Domain == null) return 0;
            return EstimatePortCost(Population, TaxRate);
         }
      }
      public double DisplayPortIncome { get; private set; }
      public double DisplayPortRevenue { get; private set; }
      public double DisplayPortCost { get; private set; }
      public void SetIncomeDisplayForUIUpdate()
      {
         OnPropertyChange("Population");
         OnPropertyChange("DisplayPortIncome");
         OnPropertyChange("DisplayPortCost");
         OnPropertyChange("DisplayPortRevenue");
         ShipBuildingQueue.DoUIUpdate();
      }
      public void CalcPortInfo()
      {
         DisplayPortIncome = MoneyCollected();
         DisplayPortCost = InternalPortCost;
         DisplayPortRevenue = DisplayPortIncome - DisplayPortCost;
      }
      public double Grow(double Money)
      {
         Money += DisplayPortIncome;
         if (Money >= DisplayPortCost)
         {
            Money -= DisplayPortCost;
            Population += PopGrowth();
            if (Population > MaxPopulation)
            {
               Population = MaxPopulation;
            }
            return Money;
         }
         else
         {

            Population = (long)(Population * (1 - (0.01 * ((DisplayPortCost - Money) / DisplayPortCost))) * (1 - TaxRate));

            return 0;
         }
      }
      public long PopGrowth()
      {
         if (Population > (MaxPopulation * .8))
         {
            return (long)(Population * BaseGrowthRate * .001 * (0.5 - TaxRate));
         }
         else if (Population < MaxPopulation / 16)
         {
            return (long)(Population * BaseGrowthRate * (0.5 - TaxRate)) * 4;
         }
         return (long)(Population * BaseGrowthRate * (0.5 - TaxRate));
      }
      public double MoneyCollected()
      {
         return EstimateTaxCollection(Population, TaxRate);
      }

      public static long GetMaxPopulationFromSize(int size)
      {
         return 1000000000 * (long)size;
      }
      public static double EstimateTaxCollection(long pop, double TaxRate)
      {
         return Math.Sqrt(pop) * CalcEffectTaxRate(TaxRate);
      }
      public static double EstimatePortCost(long pop, double TaxRate)
      {
         double ppp = Math.Sqrt(pop);
         return PortBaseCost + (ppp * 0.5 * TaxRate);
      }
      private static double CalcEffectTaxRate(double TaxRate)
      {
         if (TaxRate < 0) return 0;
         double eff = 0;
         double left = TaxRate;
         for (int i = 0; i < 20; i++)
         {
            double curBrac = 1;
            if (i != 0)
            {
               curBrac = 1.5 / (i + 1);
            }
            if (left > 0.05)
            {
               eff += 0.05 * curBrac;
               left -= 0.05;
            }
            else
            {
               eff += left * curBrac;
               break;
            }
         }
         return eff;
      }


      public double BuildShips(double money, double MoneyAvailable)
      {
         double maxAllowed = Consts.ShipBuildingRat * MoneyCollected();
         double excess = 0;
         if (maxAllowed < money)
         {
            excess = money - excess;
         }
         else
         {
            maxAllowed = money;
         }
         double maintainCost = -1;
         while (maxAllowed > 0 && ShipBuildingQueue.CurrentBuildShipQueue > 0)
         {
            double toFinish = Domain.GetShipCost() - ShipBuildingQueue.CurrentBuildProgress;
            if (maxAllowed < toFinish)
            {
               ShipBuildingQueue.CurrentBuildProgress += maxAllowed;
               maxAllowed = 0;
               break;
            }
            else
            {
               if (maintainCost < 0) maintainCost = Domain.GetShipMaintenanceCost();
               if (MoneyAvailable < maintainCost && Domain.TotalShipCount != 0)
               {
                  //TODO: declear ship can't be build due to low maintain cost.
                  break;
               }
               maxAllowed -= toFinish;
               ShipBuildingQueue.CurrentBuildShipQueue--;
               Ship ship = new Ship(Domain);
               CombinedHaul ch = CombinedHaul.CreateH(ship);
               Domain.AddShip(ch);

               PortBattleField.RequestAddHaulToBF(new AddHaulRequest{ Haul = ch, FromPlanet = true});
               //PortDefault.Ships.Add(ch);
               ShipBuildingQueue.CurrentBuildProgress = 0;
               //ShipBuildingQueue.CurrentBuildShipCost = Domain.GetShipCost();
            }
         }
         return maxAllowed + excess;
      }
      //public void DeleteShip(IHual h)
      //{
      //   Domain.RemoveShipOfGen(h);
      //}

      public void SendShipTo(PortInfo p, IHual ship)
      {
         //PortInfo fromp = this;         
         //ship.CurHaulState = HaulState.StatePorting;
         //ship.RemoveShipFromBattle();

         //Transport tran = new Transport(ship, fromp, p);      
         //ship.domain.AddTransport(tran);

         BattleField curBf = ship.CurrentBattleField;
         if (curBf != null)
         {
            curBf.AddTransportRequest(new TransportRequest
            {
               Ship = ship,
               ToPort = p
            });
         }
      }

      public int SendShipsOfGenTo(DomainInfo domain, PortInfo toP, int count)
      {
         int total = 0;
         foreach (IHual h in GetShips(domain))
         {
            if (h.domain == Player.ActivePlayer.Domain)
            {
               total++;
               SendShipTo(toP, h);
               if (total >= count) break;
            }
         }
         return total;
      }

      #region INotifyPropertyChanged Members

      public event PropertyChangedEventHandler PropertyChanged;
      public void OnPropertyChange(string name)
      {
         PropertyChangedEventHandler handle = PropertyChanged;
         if (handle != null)
         {
            handle(this, new PropertyChangedEventArgs(name));
         }
      }
      #endregion
   }

   public class PortDistInfo
   {
       public double PortDist { get; private set; }
       public PortInfo Port { get; private set; }
       public PortDistInfo(PortInfo port, double dist)
       {
           Port = port;
           PortDist = dist;
       }
   }

   public class ShipBuildInfo : INotifyPropertyChanged
   {
      private PortInfo Port;
      private DomainInfo Domain;
      //private double _CurrentBuildShipCost;
      //public double CurrentBuildShipCost
      //{
      //   get { return _CurrentBuildShipCost; }
      //   set { _CurrentBuildShipCost = value; OnPropertyChange("CurrentBuildShipCost"); }
      //}
      private bool HasUIChanges = false;
      private double _mnyProg;
      public ShipBuildInfo(PortInfo p)
      {
         Port = p;
      }
      public void SetDomain(DomainInfo dom) { Domain = dom; }
      public double CurrentBuildProgress
      {
         get { return _mnyProg; }
         set
         {
            _mnyProg = value; HasUIChanges = true;
         }
      }

      public string FinishPercent
      {
         get
         {
            lock (Port.PortLock)
            {
               if (Domain == null) return "0";
               return (CurrentBuildProgress / Domain.GetShipCost()).ToString("0.00");
            }
         }
      }

      private int _count;
      public int CurrentBuildShipQueue
      {
         get { return _count; }
         set { _count = value; HasUIChanges = true; }
      }

      #region INotifyPropertyChanged Members

      public event PropertyChangedEventHandler PropertyChanged;
      public void OnPropertyChange(string name)
      {
         PropertyChangedEventHandler hand = PropertyChanged;
         if (hand != null)
         {
            hand(this, new PropertyChangedEventArgs(name));
         }
      }
      #endregion

      public void DoUIUpdate()
      {
         lock (Port.PortLock)
         {
            if (HasUIChanges)
            {
               HasUIChanges = false;
            }
            else return;
         }
         OnPropertyChange("CurrentBuildShipQueue");
         OnPropertyChange("CurrentBuildProgress");
         OnPropertyChange("FinishPercent");
      }
   }

   public class PortShipInfo : INotifyPropertyChanged
   {
      public PortInfo Port { get; private set; }
      public PortShipInfo(PortInfo p)
      {
         Port = p;
      }

      public int Generation
      {
         get { if (Port.Domain != null) return Port.Domain.Generation; return 0; }
         //set
         //{
         //   _generation = value;
         //   OnPropertyChange("Generation");
         //}
      }
      private int _count;

      public int Count
      {
         get { return _count; }
         set
         {
            _count = value;
            OnPropertyChange("Count");
         }
      }

      private int _sendCount = 0;
      public int SendCount
      {
         get { return _sendCount; }
         set
         {
            _sendCount = value;
            OnPropertyChange("SendCount");
         }
      }

      #region INotifyPropertyChanged Members

      public event PropertyChangedEventHandler PropertyChanged;

      public void OnPropertyChange(string name)
      {
         PropertyChangedEventHandler hand = PropertyChanged;
         if (hand != null)
         {
            hand(this, new PropertyChangedEventArgs(name));
         }
      }
      #endregion
   }

   public class AIDomainSnapShot
   {
      public DomainInfo Domain { get; set; }
      public int DomGen { get; set; }
   }
   public class AIPortSnapShot
   {
      public AIDomainSnapShot Domain { get; set; }
      public int DomainID { get; set; }
      public long PortPopulation { get; set; }      
   }
}
