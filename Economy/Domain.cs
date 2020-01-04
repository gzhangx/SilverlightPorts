using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading;

namespace SilverlightPorts
{
   public class Consts
   {
      public const int MAX_SHIP_SPEED = 1;
      public const int MaxPortSize = 6;
      public const double ShipBuildingRat = 3;
      public const double CostSPPerPort = 3;
      public const double CostSPMaintainPerPort = 5;
      public const double MaxEffDeduction = 0.5;
   }

   public class DomainDspInfo
   {
      public double TotalMoney { get; set; }
      public double MoneyThisTurn { get; set; }
      public double MoneyThisTurnPlanetCost { get; set; }
      public double MoneyThisTurnForScience { get; set; }
      public double MoneyForShipMaintenance { get; set; }
      public double MoneyForShipBuild { get; set; }
   }
   public partial class DomainInfo : INotifyPropertyChanged
   {
      public bool LetAIControlMe { get; set; }
      //private static Thread UINotifyThread;
      //private static object UINotifyLock = new object();
      //private static List<DomainInfo> UINotifyQueue = new List<DomainInfo>();
      private AIThresholds AIThing;      
      //private double _ThisTurnHaulCosts;
      private double ScienceMoney = 0;
      private double _TotalMoney = 0;
      //Money without planet cost
      private double _MoneyIncomeThisTurn;
      public double MoneyRevenueThisTurn { get; private set; }
      public double MoneyThisTurnForScience { get; private set; }
      public double MoneyForShipMaintenance { get; private set; }
      private double _MoneyForShipBuild;
      //private List<Transport> UIDoneTransports = new List<Transport>();
      //public DomainDspInfo GetDspInfo()
      //{
      //    return new DomainDspInfo
      //      {
      //          MoneyForShipBuild = _MoneyForShipBuild,
      //          MoneyForShipMaintenance = _MoneyForShipMaintenance,
      //          MoneyThisTurn = _MoneyIncomeThisTurn,
      //          MoneyThisTurnForScience = _MoneyThisTurnForScience,
      //          MoneyThisTurnPlanetCost = _MoneyRevenueThisTurn,
      //          TotalMoney = _TotalMoney
      //      };
      //}
      //static DomainInfo()
      //{
      //   UINotifyThread = new Thread(new ThreadStart(() =>
      //   {            
      //      while (true)
      //      {
      //         DomainInfo[] infos = null;
      //         lock (UINotifyLock)
      //         {
      //            Monitor.Wait(UINotifyLock);
      //            infos =UINotifyQueue.ToArray();
      //            UINotifyQueue.Clear();
      //         }
      //         foreach (DomainInfo info in infos)
      //         {
      //            info.DoUINotify();
      //         }
      //      }
      //   }));
      //   UINotifyThread.Start();
      //}
      public const string PROPNAME_Ships = "Ships";
      public Player player { get; set; }
      private List<IHual> Ships { get; set; }
      public int TotalShipCount { get { return Ships.Count;  } }
      private object shipGenLock = new object();

      public void AddShip(IHual hual)
      {
         lock (shipGenLock)
         {            
            Ships.Add(hual);
         }
         OnPropertyChange(PROPNAME_Ships);
      }
      public void RemoveShipOfGen(IHual h)
      {
         lock (shipGenLock)
         {
            IShip s = h.ShipInfo;
            int gen = s.ShipType.Generation;
            Ships.Remove(h);
         }
         OnPropertyChange(PROPNAME_Ships);
      }
      public double EstimateShipConsts()
      {
         lock (shipGenLock)
         {
            double res = 0;            
            double cost = GetShipMaintenanceCost();
            res += Ships.Count * cost;            
            return res;
         }
      }
      public double MaintainShips(double money)
      {
         lock (shipGenLock)
         {
            List<int> keys = new List<int>();                        
            {
               List<IHual> ships = Ships;
               double cost = GetShipMaintenanceCost();
               if (cost > money)
               {
                  ships.ForEach(s => { s.DeleteHaul(); });
                  Ships.Clear();
               }
               else
               {
                  double totalCost = ships.Count * cost;
                  if (totalCost >= money)                  
                  {
                     int savedCnt = (int)(money / cost);
                     totalCost = savedCnt* cost;                     
                     while (ships.Count > savedCnt)
                     {
                        ships[ships.Count - 1].DeleteHaul();
                     }
                  }
                  money -= totalCost;
               }

            }
            return money;
         }
      }

      public bool IsFriendlyDomain(DomainInfo d)
      {
         return d.DomainID == DomainID;
      }
      public bool IsFriendlyDomain(int domId)
      {
          return domId == DomainID;
      }
      
      //private List<Transport> Transports { get; set; }
      //private object TransportLock = new object();
      //public void AddTransport(Transport tran)
      //{
      //   lock (TransportLock)
      //   {
      //      Transports.Add(tran);
      //   }
      //}
      //public void RemoveTransport(Transport tran)
      //{
      //   lock (TransportLock)
      //   {
      //      Transports.Remove(tran);
      //   }
      //}
      //public void RemoveTransport(IHual h)
      //{
      //   lock (TransportLock)
      //   {
      //      foreach (Transport tran in Transports)
      //      {
      //         if (tran.Ship == h)
      //         {
      //            Transports.Remove(tran);
      //            break;
      //         }
      //      }            
      //   }
      //}
      //private List<PortInfo> AllPorts { get { return GlobalData.Ports; } }
      private static int domIdCnt = 0;
      public static void ResetDomainCount() { domIdCnt = 0; }
      public int DomainID { get; private set; }
      public bool IsHuman { get { return player != null; } }
      public double ScienceRate { get { return AIThing.PercentForScience;  } }
      private static double shipBaseCost = 400;
      public static double ShipBaseCost
      {
         get
         {
            if (shipBaseCost > 1) return shipBaseCost;
            long maxPop = PortInfo.GetMaxPopulationFromSize(Consts.MaxPortSize);
            shipBaseCost = PortInfo.EstimateTaxCollection(maxPop, 0.1) - PortInfo.EstimatePortCost(maxPop, 0.1);
            shipBaseCost /= Consts.CostSPPerPort;
            return shipBaseCost;
         }
      }

      public double GetShipCost()
      {         
         double cost = GetShipMaintenanceCost();
         cost *= 200;
         return cost;
      }      
      public double GetShipMaintenanceCost()
      {
         int gen = Generation;
         int genCnt = Ships.Count;

         //if (GenToCount.ContainsKey(gen)) genCnt = GenToCount[gen];
         double cost = ShipBaseCost;
         if (genCnt < 10)
         {
            cost += (cost * .1) * (10 - genCnt);
         }
         int TotalShipCount = 0;         
         {
            TotalShipCount += Ships.Count;
         }
         

         //double GenToTotalFact = 1;
         //if (TotalShipCount < 10)
         //{
         //    GenToTotalFact = TotalShipCount / 10;
         //}
         
         //cost -= (cost * (GenToTotalFact - 0.5));
         return cost;
      }
      public DomainInfo(string name, int Gen, double taxrate)
      {
         Ships = new List<IHual>();
         //Transports = new List<Transport>();
         //AllPorts = allPorts;
         unchecked
         {
            DomainID = domIdCnt++;
         }
         Name = name;
         Generation = Gen;
         TaxRate = taxrate;
         AIThing = new AIThresholds(this);
      }
      private double _taxrate = 0.1;
      public double TaxRate
      {
         get { return _taxrate; }
         set { _taxrate = value; }
      }
      public string Name { get; private set; }
      public int Generation { get; private set; }
      
      public double SciencePercent
      {
         get
         {
            return ScienceMoney / ResourceToNextGen;
         }
      }
      private void CommitToScience(double mny)
      {
         ScienceMoney += Math.Sqrt(mny);
         if (ScienceMoney >= ResourceToNextGen)
         {
            ScienceMoney -= ResourceToNextGen;
            Generation++;
         }
      }
      public float ResourceToNextGen
      {
         get
         {
            return (Generation + 1) * 1.5f * 1000;
         }
      }


      private List<PortInfo> ports = new List<PortInfo>();
      private object PortsLock = new object();
      public List<PortInfo> PortsSnapShot
      {
         get {
            List<PortInfo> snapshot = new List<PortInfo>();
            lock (PortsLock)
            {
               ports.ForEach(p=>snapshot.Add(p));
            }
            return snapshot; 
         }
      }
      public void AddPort(PortInfo p)
      {
         lock (PortsLock)
         {
            p.SetOwner(this);
            ports.Add(p);
         }
      }
      //private void ProcessTransports()
      //{
      //   lock (TransportLock)
      //   {
      //      List<Transport> endedTransports = new List<Transport>();
      //      foreach (Transport tran in Transports)
      //      {
      //         if (tran.NextStep())
      //         {
      //            endedTransports.Add(tran);
      //         }
      //      }
      //      foreach (Transport tran in endedTransports)
      //      {
      //         Transports.Remove(tran);
      //         tran.Ship.CurrentTransport = null;
      //         tran.ToPort.PortBattleField.AddShipToBattle(tran.Ship, false);
      //      }
      //      UIDoneTransports.AddRange(endedTransports);
      //   }
      //}

      //public void ProcessTransportUI()
      //{
      //   lock (TransportLock)
      //   {
      //      foreach (Transport tran in Transports)
      //      {
      //         tran.ShowUI();
      //      }
      //      foreach (Transport tran in UIDoneTransports)
      //      {
      //         tran.RemoveUI();
      //      }
      //      UIDoneTransports.Clear();
      //   }
      //}
      //private void ProcessBattles()
      //{
      //   foreach (PortInfo p in ports)
      //   {
      //      p.PortBattleField.Process();
      //   }
      //}
      public void DoUINotify()
      {
         GlobalData.RootMapGraphics.Dispatcher.BeginInvoke(() =>
         {
            OnPropertyChange("MoneyRevenueThisTurn");
            OnPropertyChange("MoneyThisTurnForScience");
            OnPropertyChange("MoneyForShipMaintenance");
            OnPropertyChange("Generation");
         });
      }

      
      public void DoTurn()
      {
         //ProcessBattles();
         //ProcessTransports();
         _MoneyIncomeThisTurn = 0;
         foreach (PortInfo p in ports)
         {
             if (p.DisplayPortRevenue >= 0)
             {                 
                 _MoneyIncomeThisTurn += p.DisplayPortRevenue;
             }
         }
         
         _TotalMoney = _MoneyIncomeThisTurn;
         DoAIThing();
         double tmpMark = _TotalMoney;
         List<PortInfo> portsToDelete = new List<PortInfo>();
         foreach (PortInfo p in ports)
         {
            p.CalcPortInfo();
         }
         ports.Sort((a, b) =>
         {
            if (a.DisplayPortRevenue > b.DisplayPortRevenue) return -1;
            if (a.DisplayPortRevenue < b.DisplayPortRevenue) return 1;
            return 0;
         });
         foreach (PortInfo p in ports)
         {
             if (p.DisplayPortRevenue >= 0)
             {
                 p.Grow(p.DisplayPortCost);
             }else
            _TotalMoney = p.Grow(_TotalMoney);
             if (p.Population <= 0 || (p.Domain != this))
            {
               portsToDelete.Add(p);
            }
         }
         lock (PortsLock)
         {
            portsToDelete.ForEach(p => { ports.Remove(p); });
         }
         portsToDelete.ForEach(p => { p.TryReleaseOwner(this); });         
         MoneyRevenueThisTurn = _TotalMoney;
         //_MoneyIncomeThisTurn = _TotalMoney - tmpMark;
         //OnPropertyChange("MoneyThisTurn");
         tmpMark = _TotalMoney;

         HaulBuildAI();

         double MoneySupposedForScience = _TotalMoney * ScienceRate;
         //foreach (PortInfo p in ports)
         {
            _TotalMoney = MaintainShips(_TotalMoney);
         }
         
         MoneyForShipMaintenance = tmpMark - _TotalMoney;
         //OnPropertyChange("MoneyForShipMaintenance");
         tmpMark = _TotalMoney;

         if (_TotalMoney > MoneySupposedForScience)
         {
            CommitToScience(MoneySupposedForScience);
            _TotalMoney -= MoneySupposedForScience;
         }
         else
         {
            CommitToScience(_TotalMoney);
            _TotalMoney = 0;
         }

         MoneyThisTurnForScience = tmpMark - _TotalMoney;
         //OnPropertyChange("MoneyThisTurnAfterScience");
         tmpMark = _TotalMoney;


         double totalPortShare = 0;
         foreach (PortInfo p in ports)
         {
            if (p.ShipBuildingQueue.CurrentBuildShipQueue > 0)
            {
               totalPortShare += p.MoneyCollected();
            }
         }
         _MoneyForShipBuild = 0;
         if (totalPortShare > 1)
         {
            double MoneyAvailable = _TotalMoney;
            foreach (PortInfo p in ports)
            {
               if (p.ShipBuildingQueue.CurrentBuildShipQueue > 0)
               {
                  double pct = p.MoneyCollected() / totalPortShare;
                  _TotalMoney = p.BuildShips(_TotalMoney * pct, MoneyAvailable);
               }
            }
            _MoneyForShipBuild = MoneyAvailable - _TotalMoney;
         }

         TryColPlanetsWithMoney(_TotalMoney);
         CommitToScience(_TotalMoney);
         MoneyThisTurnForScience += _TotalMoney;
         //OnPropertyChange("MoneyThisTurnForScience");
         _TotalMoney = 0;
         //OnPropertyChange("TotalMoney");
         //OnPropertyChange("Generation");
         //lock (UINotifyLock)
         //{
         //   if (!UINotifyQueue.Contains(this))
         //   {
         //      UINotifyQueue.Add(this);
         //      Monitor.Pulse(UINotifyLock);
         //   }
         //}
      }

      

      private PortInfo FindNearstPort(PortInfo p, List<PortInfo> ports)
      {
         PortInfo selected = null;
         double dist = 0;
         foreach (PortInfo prt in ports)
         {
            if (prt.ID != p.ID && !prt.Domain.IsFriendlyDomain(p.Domain))
            {
               if (selected == null)
               {
                  selected = prt;
                  dist = p.DistOf(prt);
               }
               else
               {
                  double nd = p.DistOf(prt);
                  if (nd < dist)
                  {
                     selected = prt;
                     dist = nd;
                  }
               }
            }
         }
         return selected;
      }
      #region INotifyPropertyChanged Members

      public event PropertyChangedEventHandler PropertyChanged;

      public void OnPropertyChange(string name)
      {
         PropertyChangedEventHandler proc = PropertyChanged;
         if (proc != null)
         {
            proc(this, new PropertyChangedEventArgs(name));
         }
      }
      #endregion
   }



}
