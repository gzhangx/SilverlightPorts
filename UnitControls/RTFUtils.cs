using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace SilverlightPorts
{
   public class BattleField
   {
      private object bfLockObj = new object();
      public double FieldWidth { get { return 1200; } }
      public double FieldHeight { get { return 1200; } }
      public Canvas DrawingCanvas { get; set; }
      public PortInfo Port { get; private set; }
      private List<IHual> Hauls { get; set; }
      public List<IBullet> Bullets { get; private set; }
      public List<IBullet> ToBeRemovedBullets { get; private set; }
      private List<IHual> DieingHauls { get; set; }
      private object DomainShipCountLock = new object();
      private Dictionary<int, List<IHual>> DomainShipCounts = new Dictionary<int, List<IHual>>();
      public BattleFieldMap bfMap { get; private set; }
      public PlanetHaul PlanetHit { get; private set; }
      //public List<UIActionEvent> UIActionList { get; private set; }
      //private List<IHual> UIUpdateAddedHuals = new List<IHual>();
      //private List<IHual> UIUpdateRemovedHuals = new List<IHual>();
      private object TransportRequestLock = new object();
      private Dictionary<int,Transport> _transports = new Dictionary<int,Transport>();
      private object TransportLock = new object();
      private Dictionary<int, TransportRequest> TransportRequests = new Dictionary<int, TransportRequest>();
      private object AddHaulLock = new object();
      private List<AddHaulRequest> HaulAddRequests = new List<AddHaulRequest>();
      public void RequestAddHaulToBF(AddHaulRequest req)
      {
         lock (AddHaulLock)
         {
            HaulAddRequests.Add(req);
         }
      }
      public void AddTransportRequest(TransportRequest req)
      {
         lock (TransportRequestLock)
         {
            if (TransportRequests.ContainsKey(req.Ship.ShipInfo.ShipId))
            {
               TransportRequests[req.Ship.ShipInfo.ShipId] = req;
            }
            else
            {
               TransportRequests.Add(req.Ship.ShipInfo.ShipId, req);
            }
         }
      }
      private void ProcessTransports()
      {
         List<TransportRequest> TMPTRRequests = new List<TransportRequest>();
         lock (TransportRequestLock)
         {
            foreach (TransportRequest req in TransportRequests.Values)
            {
               TMPTRRequests.Add(req);               
            }
            TransportRequests.Clear();
         }

         lock (TransportLock)
         {
            foreach (TransportRequest req in TMPTRRequests)
            {
               int shipId = req.Ship.ShipInfo.ShipId;
               //if (!_transports.ContainsKey(shipId))
               if (RemoveShipFromBattle(req.Ship))
               {
                  _transports.Add(shipId, new Transport(req.Ship, this.Port, req.ToPort));
                  req.Ship.CurHaulState = HaulState.StatePorting;
                  
               }
            }
         }

         List<Transport> endedTransports = new List<Transport>();
         lock (TransportLock)
         {
            List<int> endedTransportsIds = new List<int>();
            foreach (int id in _transports.Keys)
            {
               Transport tran = _transports[id];
               if (tran.NextStep())
               {
                  endedTransportsIds.Add(id);
                  endedTransports.Add(tran);
               }
            }
            endedTransportsIds.ForEach(id =>
            {
               _transports.Remove(id);
            });
         }
         
         foreach (Transport tran in endedTransports)
         {            
            tran.Ship.CurrentTransport = null;
            tran.ToPort.PortBattleField.RequestAddHaulToBF(new AddHaulRequest { Haul = tran.Ship, FromPlanet = false });
         }
      }
      public void UIGetCurTransportSnapShot(Dictionary<int, Transport> snap)
      {
         lock (TransportLock)
         {
            foreach (Transport t in _transports.Values)
            {
               int id = t.Ship.ShipInfo.ShipId;
               if (!snap.ContainsKey(id))
               {
                  snap.Add(id, t);
               }
            }
         }
      }
      
      public List<IHual> GetMyShips(DomainInfo dom)
      {
         lock (DomainShipCountLock)
         {
            List<IHual> my = new List<IHual>();
            //Hauls.ForEach(h =>
            //{
            //   if (h.domain == dom)
            //   {
            //      my.Add(h);
            //   }
            //});
            if (DomainShipCounts.ContainsKey(dom.DomainID))
            {
               my.AddRange(DomainShipCounts[dom.DomainID]);
            }
            return my;
         }
      }
      public int GetShipCount(DomainInfo dom)
      {
         int total = 0;
         lock (DomainShipCountLock)
         {
            if (DomainShipCounts.ContainsKey(dom.DomainID))
            {
               return DomainShipCounts[dom.DomainID].Count;
            }
            return total;
         }
      }
      //public void AddShipSpecialStateAni(IHual h)
      //{
      //   DieingHauls.Add(h);
      //}
      public bool IsShipInPortZone(IHual ship)
      {
         return ship.LocInf.PosY <= 20;
      }
      public void SetShipMovetoPortZone(IHual ship)
      {
         Point p = new Point(ship.LocInf.PosX, 10);
         ship.SetMoveTo(p);
      }
      public bool SetShipMovetoBombardZone(IHual ship)
      {
         Point shipLoc = ship.GetLocation();
         double toY = FieldHeight - ship.ShipInfo.ShipType.MaxBulletRange - 20;
         if (shipLoc.Y >= toY)
         {
            ship.SetMoveTo(null);
            return true;
         }
         else
         {
            Point p = new Point(ship.LocInf.PosX, toY+40);
            ship.SetMoveTo(p);
         }
         return false;
      }
      public BattleField(PortInfo p)
      {
         Port = p;
         //UIActionList = new List<UIActionEvent>();
         PlanetHit = new PlanetHaul(p);
         DieingHauls = new List<IHual>();
         Hauls = new List<IHual>();
         Bullets = new List<IBullet>();
         ToBeRemovedBullets = new List<IBullet>();
         bfMap = new BattleFieldMap(FieldWidth, FieldHeight);
         //DrawingCanvas = new Canvas();
      }

      public bool HasEnemyShip(DomainInfo Domain)
      {
         foreach (DomainInfo dom in GlobalData.Domains)
         {
            if (!Domain.IsFriendlyDomain(dom))
            {
               int cnt = GetShipCount(dom);
               if (cnt > 0) return true;
            }
         }
         return false;
      }
      public int CountEnemyShip(DomainInfo Domain)
      {
         int total = 0;
         foreach (DomainInfo dom in GlobalData.Domains)
         {
            if (!Domain.IsFriendlyDomain(dom))
            {
               total += GetShipCount(dom);
            }
         }
         return total;
         //lock (bfLockObj)
         //{
         //   int count = 0;
         //   foreach (IHual h in Hauls)
         //   {
         //      if (!h.ShipInfo.IsDead())
         //      {
         //         if (!Domain.IsFriendlyDomain(h.domain)) count++;
         //      }
         //   }
         //   return count;
         //}
      }
      public int CountMyShips(DomainInfo Domain)
      {
         return GetShipCount(Domain);
      }

      public void UIThreadUpdatePortInfo()
      {
         int count = 0;
         lock (bfLockObj)
         {
            count = Hauls.Count;
         }
         Port.DomainFleetInfo.Count = count;
      }
      public void UILoopShowAI(Dictionary<int, BasicHaulUI> shipIdToUIShips, Dictionary<int, BasicHaulUI> specialUIShips,
         Action<List<IBullet>> bulletCheck, Canvas screen)
      {
         UIThreadUpdatePortInfo();
         Dictionary<int, IHual> usedHauls = new Dictionary<int,IHual>();
         lock (bfLockObj)
         {
            Hauls.ForEach(h => { usedHauls.Add(h.ShipInfo.ShipId, h); });
         }
         
         List<IHual> needAdds = new List<IHual>();
         foreach (IHual h in usedHauls.Values)
         {
            if (!shipIdToUIShips.ContainsKey(h.ShipInfo.ShipId)) needAdds.Add(h);
         }
         foreach (IHual h in needAdds)
         {
            
            BasicHaulUI shipui = new BasicHaulUI(h);
            shipIdToUIShips.Add(h.ShipInfo.ShipId, shipui);
            screen.Children.Add(shipui);
         }
         foreach (BasicHaulUI uh in shipIdToUIShips.Values)
         {
            uh.SyncUI();
         }
         
         lock (bfLockObj)
         {
            foreach (IHual h in DieingHauls)
            {
               if (shipIdToUIShips.ContainsKey(h.ShipInfo.ShipId))
               {
                  if (!specialUIShips.ContainsKey(h.ShipInfo.ShipId))
                  {
                     specialUIShips.Add(h.ShipInfo.ShipId, shipIdToUIShips[h.ShipInfo.ShipId]);
                  }
               }
            }
            DieingHauls.Clear();
         }

         List<int> doneDieHauls = new List<int>();
         foreach (BasicHaulUI uh in specialUIShips.Values)
         {
            if (uh.ProcessSpecialShipState(screen, uh))
            {
               doneDieHauls.Add(uh.Haul.ShipInfo.ShipId);
            }
         }
         doneDieHauls.ForEach(h =>{
            screen.Children.Remove(specialUIShips[h]);
            specialUIShips.Remove(h);
         });

         List<int> needDeletes = new List<int>();
         foreach (BasicHaulUI uh in shipIdToUIShips.Values)
         {
            int shipId = uh.Haul.ShipInfo.ShipId;
            if (!usedHauls.ContainsKey(shipId)
               && !specialUIShips.ContainsKey(shipId)) needDeletes.Add(uh.Haul.ShipInfo.ShipId);
         }
         needDeletes.ForEach(key => { 
            shipIdToUIShips.Remove(key); 
         });

         List<IBullet> bullets = new List<IBullet>();
         lock (bfLockObj)
         {
            bullets.AddRange(Bullets);
         }
         bulletCheck(bullets);
      }
      
      private static Random entranceRand = new Random(DateTime.Now.Millisecond);
      private void AddShipToBattle(IHual ch, bool fromThisPlanet)
      {
         lock (bfLockObj)
         {
#if DEBUG
            if (Hauls.Contains(ch))
            {
               throw new Exception("Ship already exists");
            }
#endif
            if (ch.CurHaulState == HaulState.StatePorting)
            {
               ch.CurHaulState = HaulState.StateNormal;
            }
            if (ch.ShipInfo.IsDead())
            {
               ch.CurHaulState = HaulState.StateDieing;
            }
            //CombinedHaul ch = CombinedHaul.CreateH(s, this);
            ch.SetBattleField(this);
            double xPos = entranceRand.Next((int)(FieldWidth / 2));
            ch.SetLocation(new Point(xPos, fromThisPlanet ? FieldHeight : 0));
            //ch.SyncUI();
            Hauls.Add(ch);
            bfMap.MoveToLocation(ch);
         }

         lock (DomainShipCountLock)
         {
            if (!DomainShipCounts.ContainsKey(ch.domain.DomainID))
            {
               DomainShipCounts.Add(ch.domain.DomainID, new List<IHual>());
            }
            DomainShipCounts[ch.domain.DomainID].Add(ch);
         }
      }

      //private void UiUpdateAddPortFleetInfoLoop()
      //{
      //   foreach (IHual ch in UIUpdateAddedHuals)
      //   {
      //      Port.DomainFleetInfo.Count++;
      //   }
      //   UIUpdateAddedHuals.Clear();
      //}

      //private void UiUpdateHualRemoveLoop()
      //{
      //   foreach (IHual s in UIUpdateRemovedHuals)
      //   {
      //      Port.DomainFleetInfo.Count--;
      //   }
      //   UIUpdateRemovedHuals.Clear();
      //}
      private bool RemoveShipFromBattle(IHual s)
      {

         if (Hauls.Remove(s))
         {
            if (s.CurCords != null)
            {
               s.CurCords.RemoveObj(s);
            }
            s.SetBattleField(null);

            lock (DomainShipCountLock)
            {
               if (DomainShipCounts.ContainsKey(s.domain.DomainID))
               {
                  DomainShipCounts[s.domain.DomainID].Remove(s);
                  if (DomainShipCounts[s.domain.DomainID].Count == 0)
                  {
                     DomainShipCounts.Remove(s.domain.DomainID);
                  }
               }
            }

            if (DrawingCanvas != null)
            {
               CombinedHaul hh = CombinedHaul.CreateH(s.ShipInfo);
               s.LocInf.SetCloneTransferUiElement(hh.LocInf);
               for (int i = 0; i < hh.Turs.Count; i++)
               {
                  s.Turs[i].LocInf.SetCloneTransferUiElement(hh.Turs[i].LocInf);
               }
               hh.SetBattleField(this);
               hh.CurHaulState = s.CurHaulState;
               DieingHauls.Add(s);
            }
            //UIUpdateRemovedHuals.Add(s);
            return true;
         }
         return false;
      }

      private Thread workerThread = null;
      private bool RunThread = true;
      private int SleepTime = 10;
      public void StartProcessThread()
      {
         if (workerThread == null)
         {
            workerThread = new Thread(new ThreadStart(() =>
            {
               while (RunThread)
               {
                  Thread.Sleep(SleepTime);
                  Process();
               }
            }));
            workerThread.Start();
         }
      }
      public void StoppingThread()
      {
         RunThread = false;         
      }
      public void WaitThreadStop()
      {
         if (workerThread != null)
         {
            workerThread.Join();
            workerThread = null;
         }
      }
      private void Process()
      {
         lock (AddHaulLock)
         {
            HaulAddRequests.ForEach(addreq => { AddShipToBattle(addreq.Haul, addreq.FromPlanet); });
            HaulAddRequests.Clear();
         }
         lock (bfLockObj)
         {            
            List<IHual> DeleteRequestedHauls = new List<IHual>();
            foreach (IHual h1 in Hauls)
            {
               if (h1.CurHaulState == HaulState.StateDieing)
               {
                  DeleteRequestedHauls.Add(h1);
                  continue;
               }
               h1.SetMoveTo(null);
               h1.XSqueezeForce = 0;
               h1.YSqueezeForce = 0;
               double nearstDist;
               IHual nearst = FindNearstOFAC(h1, out nearstDist);
               foreach (ITurret t in h1.Turs)
               {
                  t.FireRateTimer++;
                  t.Target = nearst;
               }
               if (nearst != null)
               {
                  h1.LocInf.Turnto(nearst.GetLocation());
                  foreach (ITurret t in h1.Turs)
                  {
                     t.Target = nearst;
                     t.TargetDist = nearstDist;
                     t.LocInf.Turnto(nearst.GetLocation());
                  }
               }
               ProcessHaulMoveToEnemyAI(h1, nearst, nearstDist);
               ProcessHaulMove(h1);
            }

            DeleteRequestedHauls.ForEach(h =>
            {
               RemoveShipFromBattle(h);               
            });

            foreach (IBullet b in Bullets)
            {
               b.ProcessShot(this);
            }
            foreach (IBullet b in ToBeRemovedBullets)
            {
               //if (DrawingCanvas != null && b.UIElement != null)
               //{
               //   //DrawingCanvas.Children.Remove(b.UIElement.UIElement);
               //   this.UIActionList.Add(new UIActionEvent
               //   {
               //      Action = UIActions.UIActionRemove,
               //      //CanvasField = DrawingCanvas,
               //      UIElement = b.UIElement
               //   });
               //}
               Bullets.Remove(b);
            }
            ToBeRemovedBullets.Clear();
            
         }
         ProcessTransports();         
      }
      public static double DistOf(Point p1, Point p2)
      {
         double dx = p1.X - p2.X;
         double dy = p1.Y - p2.Y;
         return Math.Sqrt((dx * dx) + (dy * dy));
      }
      private IHual FindNearstOFAC(IHual me, out double odist)
      {
         IHual nearst = null;
         double dist = 999999999999;
         odist = dist;

          //fast enemy check
         lock (DomainShipCountLock)
         {
            foreach (int domId in DomainShipCounts.Keys)
            {
               if (!me.domain.IsFriendlyDomain(domId))
               {
                  nearst = DomainShipCounts[domId][0];
                  break;
               }
            }
         }
         if (nearst == null) return null;
         nearst = null;
         Point pme = me.GetLocation();
         Func<List<IHual>, bool> SearchFunc = (hauls)=>{
            foreach (IHual h in hauls)
            {
               if (h.domain.Name != me.domain.Name)
               {
                  Point p1 = h.GetLocation();
                  if (nearst == null)
                  {
                     nearst = h;
                     dist = DistOf(pme, p1);
                  }
                  else
                  {
                     double newdist = DistOf(pme, p1);
                     if (newdist < dist)
                     {
                        dist = newdist;
                        nearst = h;
                     }
                  }
                  if (nearst != null)
                  {
                     if (dist < me.ShipInfo.ShipType.MaxBulletRange) return true;
                  }
               }
            }
            return false;
         };
         for (int rng = 0; rng < 4; rng++)
         {
            bfMap.SearchLocations(pme, rng, SearchFunc);
            if (nearst != null) break;            
         }

         Func<List<IHual>, bool> SqueezeFunc = (hauls) =>
         {
            const double DShipSize = 128;
            foreach (IHual h in hauls)
            {
                if (h == me) continue;
               Point hisPos = h.GetLocation();
               double ddd = DistOf(hisPos, pme);
               if (ddd == 0)
               {
                  ddd = 0.5;
                  hisPos.X += 0.5;
               }
               if (ddd < DShipSize)
               {
                  double force = (DShipSize / ddd);
                  double xForce = force * (pme.X - hisPos.X);
                  double yForce = force * (pme.Y - hisPos.Y);
                  me.XSqueezeForce += xForce;
                  me.YSqueezeForce += yForce;
               }
            }
            return false;
         };
         bfMap.SearchLocations(pme, 0, SqueezeFunc);
         bfMap.SearchLocations(pme, 1, SqueezeFunc);
         //TODO: this will gurrant we find the nearst, but takes time.
         if (nearst == null && Hauls.Count < 5)
         {
            SearchFunc(Hauls);
         }
         if (nearst == null)
         {
            lock (DomainShipCountLock)
            {
               foreach (int domId in DomainShipCounts.Keys)
               {
                  if (!me.domain.IsFriendlyDomain(domId))
                  {
                     nearst = DomainShipCounts[domId][0];
                     dist = DistOf(pme, nearst.GetLocation());
                     break;
                  }
               }
            }
            //foreach (IHual h in Hauls)
            //{
            //   if (h.domain.Name != me.domain.Name)
            //   {
            //      nearst = h;
            //      dist = DistOf(pme, h.GetLocation());
            //      break;
            //   }
            //}
         }
         odist = dist;
         return nearst;
      }

      private void ProcessHaulMoveToEnemyAI(IHual h, IHual nearstEnemy, double nearstDist)
      {
          if (nearstEnemy != null && nearstDist > h.ShipInfo.ShipType.MaxBulletRange)
          {
              //Point p = h.LocInf.GetRelativePosition();
              Point toPos = nearstEnemy.LocInf.GetRelativePosition();
             Point myPos = h.GetLocation();
             double xDiff = toPos.X - myPos.X;
             if (Math.Abs(xDiff) < h.ShipInfo.ShipType.ShipSize/2)
             {
                toPos.X = myPos.X;
             }
             double yDiff = toPos.Y - myPos.Y;
             if (Math.Abs(yDiff) < h.ShipInfo.ShipType.ShipSize / 2)
             {
                toPos.Y = myPos.Y;
             }
              h.SetMoveTo(toPos);
          }
          else
          {
              if (h.CurrentBattleField.Port.Domain==null || h.CurrentBattleField.Port.Domain.IsFriendlyDomain(h.domain))
             {
                h.SetMoveTo(null);
             }
             else
             {
                if (h.CurrentBattleField.Port.Population != 0)
                {
                   if (h.CurrentBattleField.SetShipMovetoBombardZone(h))
                   {
                      Point nearstLoc = h.GetLocation();
                      nearstDist = h.CurrentBattleField.FieldHeight - nearstLoc.Y;
                      nearstLoc.Y = h.CurrentBattleField.FieldHeight;
                      h.LocInf.Turnto(nearstLoc);
                      foreach (ITurret t in h.Turs)
                      {
                         t.Target = this.PlanetHit;
                         t.TargetDist = nearstDist;
                         t.LocInf.Turnto(nearstLoc);
                      }
                   }
                }
             }
          }
      }

      private void ProcessHaulMove(IHual h)
      {
         bool hasXinc = false;
         bool hasYinc = false;
         double yInc = 0;
         double xInc = 0;

         if (h.GetMoveTo() != null)
         {
            xInc = h.ShipInfo.Speed;
            yInc = h.ShipInfo.Speed;
            Point p = h.LocInf.GetRelativePosition();
            Point toPos = h.GetMoveTo().Value;
            if (p.X > toPos.X) xInc = -xInc;
            if (p.Y > toPos.Y) yInc = -yInc;
            if (Math.Abs(p.X - toPos.X) > xInc)
            {
               hasXinc = true;
            }
            else
            {
               xInc = 0;
            }
            if (Math.Abs(p.Y - toPos.Y) > yInc)
            {
               hasYinc = true;
            }
            else
            {
               yInc = 0;
            }
         }

         if (Math.Abs(h.XSqueezeForce) > 1)
         {
            xInc = Math.Sign(h.XSqueezeForce) * h.ShipInfo.Speed;
            hasXinc = true;
         }
         if (Math.Abs(h.YSqueezeForce) > 1)
         {
            yInc = Math.Sign(h.YSqueezeForce) * h.ShipInfo.Speed;
            hasYinc = true;
         }

         if (h.LocInf.PosY < 0)
         {
            hasYinc = true;
            yInc = h.ShipInfo.Speed;
         }
         else if (h.LocInf.PosY +64 > FieldHeight)
         {
            hasYinc = true;
            yInc = -h.ShipInfo.Speed;
         }

         if (h.LocInf.PosX < 64)
         {
            hasXinc = true;
            xInc = h.ShipInfo.Speed;
         }
         else if (h.LocInf.PosX + 64 > FieldWidth)
         {
            hasXinc = true;
            xInc = -h.ShipInfo.Speed;
         }

         
         if (hasXinc || hasYinc)
         {
            Point p = h.LocInf.GetRelativePosition();
            p.X += xInc;
            p.Y += yInc;
            h.LocInf.Turnto(p);
            h.SetLocation(p);
            return;
         }
      }
   }

   public class AddHaulRequest
   {
      public IHual Haul { get; set; }
      public bool FromPlanet { get; set; }
   }
   //public enum UIActions
   //{
   //   //UIActionAdd,
   //   //UIActionCreateWithActionAndAdd,
   //   //UIActionRemove,
   //   //UIActionAction,
   //   UIActionAddToShipUI,
   //}
   //public class UIActionEvent
   //{
   //   public int ShipId; //only used when action is UIActionAddToShipUI
   //   public UIActions Action;
   //   //public Canvas CanvasField { get; set; }
   //   public IFrameworkUIForRotateUtil UIElement { get; set; }
   //   public Action UIAction { get; set; }
   //   public Action<Canvas> ShipUiAction { get; set; }
   //   //public Func<IFrameworkUIForRotateUtil> UiAddAction { get; set; }
   //}
   public enum HaulState
   {
       StateNormal = 0,
       StateDieing,
       StatePorting
   }
   public class BattleFieldMap
   {
      public const double MapSqureSize = 128;
      private int MapIntCrdX;
      private int MapIntCrdY;
      public MapCordObj[,] map;
      public BattleFieldMap(double Totalwidth, double Totalheight)
      {
         unchecked
         {
            MapIntCrdX = (int)(Totalwidth / MapSqureSize) + 1;
            MapIntCrdY = (int)(Totalheight / MapSqureSize) + 1;
            map = new MapCordObj[MapIntCrdX + 1, MapIntCrdY + 1];
         }
      }
      public void MoveToLocation(IHual obj)
      {
         if (obj.CurCords != null)
         {
            obj.CurCords.RemoveObj(obj);
         }

         MapCordObj mapCord = GetCordObj(obj.LocInf.GetRelativePosition());
         mapCord.AddObj(obj);
      }
      private MapCordObj GetCordObj(Point loc)
      {
         double newLocXFrac = loc.X / MapSqureSize;
         int newLocX = (int)(newLocXFrac);
         if (newLocX < -1) newLocX = -1;
         //int xTrend = (int)((newLocXFrac - newLocX - 0.5) * MapSqureSize);

         double newLocYFrac = loc.Y / MapSqureSize;
         int newLocY = (int)(newLocYFrac);
         //int yTrend = (int)((newLocYFrac - newLocY - 0.5) * MapSqureSize);
         if (newLocY < -1) newLocY = -1;
         newLocX++;
         newLocY++;
         if (newLocX > MapIntCrdX) newLocX = MapIntCrdX;
         if (newLocY > MapIntCrdY) newLocY = MapIntCrdY;
         MapCordObj mapCord = GetCordObjInt(newLocX, newLocY);
         return mapCord;
      }
      private MapCordObj GetCordObjInt(int x, int y)
      {
         MapCordObj mapCord = map[x, y];
         if (mapCord == null)
         {
            mapCord = new MapCordObj(x, y);
            map[x, y] = mapCord;
         }
         return mapCord;
      }

      public void SearchLocationsInRange(Point loc, int range, Func<List<IHual>, bool> searchFunc)
      {
         for (int i = 0; i <= range; i++)
         {
            if (SearchLocations(loc, i, searchFunc))
            {
               return;
            }
         }
      }
      public bool SearchLocations(Point loc, int range, Func<List<IHual>, bool> searchFunc)
      {
         if (range == 0)
         {
            MapCordObj cordObj = GetCordObj(loc);
            return searchFunc(cordObj.Items);
         }
         else
         {
            MapCordObj centerCord = GetCordObj(loc);
            int rxlow = centerCord.X - range;
            if (rxlow < 0) rxlow = 0;
            int rxhigh = centerCord.X + range;
            if (rxhigh > MapIntCrdX) rxhigh = MapIntCrdX;
            int rylow = centerCord.Y - range;
            if (rylow < 0) rylow = 0;
            int ryhigh = centerCord.Y + range;
            if (ryhigh > MapIntCrdY) ryhigh = MapIntCrdY;
            for (int ix = rxlow; ix <= rxhigh; ix++)
            {
               if (searchFunc(GetCordObjInt(ix, rylow).Items)) return true;
               if (searchFunc(GetCordObjInt(ix, ryhigh).Items)) return true;
            }
            for (int iy = rylow + 1; iy <= ryhigh - 1; iy++)
            {
               if (searchFunc(GetCordObjInt(rxlow, iy).Items)) return true;
               if (searchFunc(GetCordObjInt(rxhigh, iy).Items)) return true;
            }
         }
         return false;
      }
   }
   public class MapCordObj
   {
      public int X { get; private set; }
      public int Y { get; private set; }
      public List<IHual> Items { get; private set; }
      public MapCordObj(int x, int y)
      {
         X = x;
         Y = y;
         Items = new List<IHual>();
      }
      public void AddObj(IHual obj)
      {
         obj.CurCords = this;
         Items.Add(obj);
      }
      public void RemoveObj(IHual obj)
      {
         Items.Remove(obj);
      }
   }

   public interface IMappableObject
   {
      MapCordObj CurCords { get; set; }
   }
}
