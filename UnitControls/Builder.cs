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
using SilverlightPorts.UnitControls;

namespace SilverlightPorts
{
   public class BattleGroup
   {
      public ShipOrder Order { get; set; }
      public PortInfo GatherPort { get; set; }
      public List<IHual> Ships = new List<IHual>();
      public void ClearLostOnes(List<IHual> okones)
      {
         Dictionary<int, IHual> oks = new Dictionary<int, IHual>();
         foreach (IHual h in okones)
         {
            oks.Add(h.ShipInfo.ShipId, h);
         }
         List<IHual> deletes = new List<IHual>();
         foreach (IHual h in Ships)
         {
            if (!oks.ContainsKey(h.ShipInfo.ShipId)) deletes.Add(h);
         }
         foreach (IHual h in deletes)
         {
            Ships.Remove(h);
         }
      }
   }
   public enum TurretTypes
   {
      Turret
   };
   public enum HualTypes
   {
      Haul
   };
   public enum BulletTypes
   {
      Bullet
   }
   public interface IBullet
   {
      int BulletId { get; }
      IHual HitOn { get; }
      //IFrameworkUIForRotateUtil UIElement { get; set; }
      BulletTypes Type { get; }
      ShipGenerationInfo Gen { get; }
      DomainInfo domain { get; }
      IHual Target { get; }
      double Speed { get; }
      double Angle { get; }
      double FlightTime { get; }
      double MaxFlightTime { get; }
      double MaxRange { get; }
      Point GetPosition();
      void SetPosition(Point p);
      void ProcessShot(BattleField bf);
      //void SyncUI();
   }
   public interface ITurret
   {
      BattleField Field { get; }
      IHual Target { get; set; }
      double TargetDist { get; set; }
      TurretTypes Type { get; }
      RotateUtils LocInf { get; }
      int FireRateTimer { get; set; }
      int FireRate { get; }
      bool IsFiring { get; set; }
   }
   public interface IHual : IMappableObject
   {
      BattleGroup TheBattleGroup { get; set; }
      //void RemoveShipFromBattle();
      HaulState CurHaulState { get; set; }
      double XSqueezeForce { get; set; }
      double YSqueezeForce { get; set; }
      Point? GetMoveTo();
      void SetMoveTo(Point? p);
      BattleField CurrentBattleField { get; }
      Transport CurrentTransport { get; set; }
      IShip ShipInfo { get; }
      DomainInfo domain { get; }
      List<ITurret> Turs { get; }
      HualTypes Type { get; }
      RotateUtils LocInf { get; }
      bool IsHit(IBullet blt);
      void SetLocation(Point p);
      Point GetLocation();
      void DeleteHaul();
      void SetBattleField(BattleField bf);
      Point TranslateWorldLocaionToShipCord(Point wloc);
      //bool ProcessSpecialShipState();
      //void SyncUI();
   }


   public class BasicBullet : IBullet
   {
      private static int _bulletId = 0;
      public int BulletId { get; private set; }
      public ShipGenerationInfo Gen { get; set; }
      //private bool Moved = false;
      private bool Expired = false;
      public IHual HitOn { get; private set; }
      public Point GetPosition()
      {
         return Position;
      }
      public void SetPosition(Point p)
      {
         Position = p;       
      }

      //public IFrameworkUIForRotateUtil UIElement { get; set; }
      private Action<IBullet> ExpireAction;
      public BasicBullet(DomainInfo dom, ShipGenerationInfo gen, BulletTypes type, double ang, Action<IBullet> expAct)
      {
         unchecked
         {
            BulletId = _bulletId++;
         }
         Gen = gen;
         ExpireAction = expAct;
         domain = dom;
         Type = type;
         FlightTime = 0;
         Position = new Point();
         Angle = ang;
         switch (type)
         {
            case BulletTypes.Bullet:
               Speed = gen.BulletSpeed;
               MaxFlightTime = gen.MaxBulletFlightTime;
               break;
         }
      }
      public DomainInfo domain { get; private set; }

      public BulletTypes Type { get; private set; }

      public IHual Target { get; set; }

      public double Speed { get; private set; }

      public double Angle { get; private set; }
      public double MaxRange { get { return Gen.MaxBulletRange; } }
      public double FlightTime { get; private set; }
      public double MaxFlightTime { get; private set; }
      private Point Position;
      private void Expire()
      {
         Expired = true;
         ExpireAction(this);
      }
      private void Move()
      {
         FlightTime++;
         if (FlightTime > MaxFlightTime)
         {
            Expire();
         }

         Position.X += Speed * Math.Cos(Angle);
         Position.Y += Speed * Math.Sin(Angle);
         SetPosition(Position);
      }
      public void ProcessShot(BattleField field)
      {
         if (Expired) return;
         Move();
         if (FlightTime > MaxFlightTime)
         {
            Expire();
            return;
         }

         field.bfMap.SearchLocationsInRange(Position, 1, (hauls) =>
         {
            foreach (IHual h in hauls)
            {
               if (!domain.IsFriendlyDomain(h.domain))
               {
                  if (h.IsHit(this))
                  {
                     this.HitOn = h;
                     Expire();
                     return true;
                  }
               }
            }
            return false;
         });
         if (!Expired)
         {
            if (field.PlanetHit.IsHit(this))
            {
               this.HitOn = field.PlanetHit;
               Expire();
            }
         }
      }
   }
   public class BaseTurret : ITurret
   {
      public BattleField Field
      {
         get
         {
            return MyHaul.CurrentBattleField;
         }
      }
      private IHual MyHaul { get; set; }
      public int FireRateTimer { get; set; }
      public int FireRate { get; private set; }
      public bool IsFiring { get; set; }
      private Point FirePoint = new Point();
      public void InitBaseInfo(BaseAngleInfo binf, double x, double y, double aw, double ah)
      {
         LocInf.InitBaseInf(binf, x, y, aw, ah);
         FirePoint = new Point { X = 0, Y = 10 };
      }
      public static BaseTurret Create(IHual haul, ShipGenerationInfo gen, DomainInfo dom)
      {
         BaseTurret t = new BaseTurret
         {
            MyHaul = haul,
            LocInf = new RotateUtils(),
            Type = TurretTypes.Turret,
            FireRate = 30,
         };
         t.LocInf.AimDone = (rt) =>
         {
            if (haul.CurrentBattleField == null) return;
            if (t.Target == null) return;
            if (t.TargetDist > gen.MaxBulletRange + t.Target.ShipInfo.ShipType.ShipSize) return;
            if (t.FireRateTimer > t.FireRate)
            {
               t.FireRateTimer = 0;
               BattleField bulletBattlefield = haul.CurrentBattleField;
               BasicBullet b = new BasicBullet(dom, gen, BulletTypes.Bullet, rt.BaseAngle, me =>
               {
                  //if (haul.CurrentBattleField == null) return;
                  //field.Bullets.Remove(me);
                  bulletBattlefield.ToBeRemovedBullets.Add(me);
                  //if (bulletBattlefield.DrawingCanvas != null && me.UIElement != null)
                  //{
                  //   //haul.CurrentBattleField.DrawingCanvas.Children.Remove(me.UIElement.UIElement);
                  //   bulletBattlefield.UIActionList.Add(new UIActionEvent
                  //   {
                  //       Action = UIActions.UIActionRemove,
                  //       //CanvasField = bulletBattlefield.DrawingCanvas,
                  //       UIElement = me.UIElement
                  //   });
                  //}
               });
               haul.CurrentBattleField.Bullets.Add(b);
               //if (haul.CurrentBattleField.DrawingCanvas != null)
               //{                  
               //   //haul.CurrentBattleField.DrawingCanvas.Children.Add(b.UIElement.UIElement);
               //   haul.CurrentBattleField.UIActionList.Add(new UIActionEvent
               //   {
               //      Action = UIActions.UIActionCreateWithActionAndAdd,
               //     // CanvasField = bulletBattlefield.DrawingCanvas,
               //      UiAddAction = () =>
               //      {                        
               //         b.UIElement = new BulletCtrl();
               //         return b.UIElement;
               //      }
               //      //UIElement = b.UIElement
               //   });
               //}
               b.SetPosition(t.LocInf.TranslatePoint(t.FirePoint));
               if (haul.CurrentBattleField.DrawingCanvas != null)
               {
                  t.IsFiring = true;
               }
               //if (t.LocInf.uiElement != null)
               //{
               //   IGenFireableUI ui = t.LocInf.uiElement.UIElement as IGenFireableUI;
               //   //ui.FireAni();
               //   haul.CurrentBattleField.UIActionList.Add(new UIActionEvent
               //   {
               //      Action = UIActions.UIActionAction,
               //      UIAction = ui.FireAni
               //   });
               //}
            }
         };
         return t;
      }
      public IHual Target { get; set; }
      public double TargetDist { get; set; }
      public TurretTypes Type
      {
         get;
         private set;
      }

      public RotateUtils LocInf
      {
         get;
         private set;
      }
   }
   public class CombinedHaul : IHual
   {
      public BattleGroup TheBattleGroup { get; set; }
      //private bool Moved = false;
      public HaulState CurHaulState { get; set; }
      public double XSqueezeForce { get; set; }
      public double YSqueezeForce { get; set; }
      public MapCordObj CurCords { get; set; }
      private Point? moveTo;
      public Point? GetMoveTo()
      {
         return moveTo;
      }
      public void SetMoveTo(Point? p)
      {
         moveTo = p;
      }
      public BattleField CurrentBattleField { get; private set; }
      public Transport CurrentTransport { get; set; }
      public IShip ShipInfo { get; private set; }
      public DomainInfo domain { get { return ShipInfo.Domain; } }
      //public Fleet MyFleet { get; private set; }
      private CombinedHaul(IShip s)
      {
         CurHaulState = HaulState.StateNormal;
         //MyFleet = fleet;
         ShipInfo = s;
         Turs = new List<ITurret>();
         LocInf = new RotateUtils();
      }
      public void SetBattleField(BattleField bf)
      {
         if (bf == null)
         {
            //DetachUI();
            CurrentBattleField = null;
         }
         else
         {
            CurrentBattleField = bf;
            //AttachUI();
         }
      }
      public void SetLocation(Point p)
      {
         LocInf.SetRelativePosition(p);
         if (ShipInfo.CurLife > 0)
         {
            this.CurrentBattleField.bfMap.MoveToLocation(this);
         }
         //Moved = true;
         //if (LocInf.uiElement != null)
         //{            
         //   Canvas.SetLeft(LocInf.uiElement.UIElement, p.X - (LocInf.ActualWidth / 2));
         //   Canvas.SetTop(LocInf.uiElement.UIElement, p.Y - (LocInf.ActualHeight / 2));
         //}
      }
      //public void SyncUI()
      //{
      //   if (LocInf.uiElement == null) return;
      //   if (Moved)
      //   {
      //      Point p = LocInf.GetRelativePosition();
      //      Canvas.SetLeft(LocInf.uiElement.UIElement, p.X - (LocInf.ActualWidth / 2));
      //      Canvas.SetTop(LocInf.uiElement.UIElement, p.Y - (LocInf.ActualHeight / 2));            
      //   }
      //   LocInf.uiElement.SyncUI();
      //   foreach (ITurret t in Turs)
      //   {
      //       if (t.LocInf.uiElement != null)
      //       {
      //           t.LocInf.uiElement.SyncUI();
      //       }
      //   }
      //}
      public Point GetLocation()
      {
         return LocInf.GetRelativePosition();
      }

      public void DetachUI()
      {
         //if (CurrentBattleField.DrawingCanvas != null)
         //{
         //    if (LocInf.uiElement != null)
         //    {
         //        CurrentBattleField.DrawingCanvas.Children.Remove(LocInf.uiElement.UIElement);
         //    }
         //}
         LocInf.AttachUIElement(null);
      }
      public static CombinedHaul CreateH(IShip dom)
      {
         CombinedHaul h = new CombinedHaul(dom);
         //field.Hauls.Add(h);

         h.LocInf.InitBaseInf(null, 0, 0, 128, 128);
         BaseTurret tur = BaseTurret.Create(h, dom.ShipType, h.domain);
         tur.LocInf.TurnRate = 0.1;
         tur.InitBaseInfo(h.LocInf, 128 - 12, 64 - 20, 24, 24);
         h.Turs.Add(tur);
         return h;
      }


      public void DeleteHaul()
      {
         ShipInfo.Domain.RemoveShipOfGen(this);
         //MyFleet.DeleteShip(this);

         this.CurHaulState = HaulState.StateDieing;
         //RemoveShipFromBattle();
         //if (CurrentTransport != null)
         //{
         //   ShipInfo.Domain.RemoveTransport(CurrentTransport);
         //}
      }

      //private void RemoveShipFromBattle()
      //{

      //   {
      //      switch (this.CurHaulState)
      //      {
      //         case HaulState.StateDieing:
      //         case HaulState.StatePorting:
      //            CombinedHaul hh = CombinedHaul.CreateH(this.ShipInfo);
      //            this.LocInf.SetCloneTransferUiElement(hh.LocInf);
      //            for (int i = 0; i < hh.Turs.Count; i++)
      //            {
      //               Turs[i].LocInf.SetCloneTransferUiElement(hh.Turs[i].LocInf);
      //            }
      //            hh.CurrentBattleField = this.CurrentBattleField;
      //            hh.CurHaulState = this.CurHaulState;
      //            if (CurrentBattleField != null && CurrentBattleField.DrawingCanvas != null)
      //            {
      //               CurrentBattleField.AddShipSpecialStateAni(hh);
      //            }
      //            break;
      //         default:
      //            throw new InvalidOperationException("bad state " + CurHaulState);
      //      }
      //   }
      //}


      #region IHual Members

      public List<ITurret> Turs
      {
         get;
         private set;
      }

      public HualTypes Type
      {
         get;
         private set;
      }

      public RotateUtils LocInf
      {
         get;
         private set;
      }

      public bool IsHit(IBullet blt)
      {
         double dist = BattleField.DistOf(blt.GetPosition(), LocInf.GetRelativePosition());
         if (dist < (LocInf.ActualWidth / 2) && dist < (LocInf.ActualHeight / 2))
         {
            ShipInfo.TakeDamange(blt);
            //ShowBulletHit(blt);
            if (ShipInfo.IsDead())
            {
               if (CurrentBattleField.GetShipCount(domain) == 0 && CurrentBattleField.Port.Domain != domain)
               {
                  domain.RaiseBattleLostAlert(CurrentBattleField.Port);
                  ShipInfo.Domain.SetPortSnapShot(CurrentBattleField.Port, blt.domain);
               }
               DeleteHaul();
            }
            return true;
         }
         return false;
      }

      public Point TranslateWorldLocaionToShipCord(Point wloc)
      {
         Point my = LocInf.GetRelativePosition();
         double dist = BattleField.DistOf(my, wloc);
         double ang = RotateUtils.GetAngle(wloc.X, wloc.Y, my.X, my.Y);
         ang -= LocInf.BaseAngle;
         Point ret = new Point(LocInf.ActualWidth / 2, LocInf.ActualHeight / 2);
         ret.X += dist * Math.Cos(ang);
         ret.Y += dist * Math.Sin(ang);
         return ret;
      }
      //private void ShowBulletHit(IBullet blt)
      //{
      //   if (CurrentBattleField.DrawingCanvas != null)
      //   {
      //      Point p = TranslateWorldLocaionToShipCord(blt.GetPosition());
      //      if (this.CurrentBattleField != null)
      //      {
      //         CurrentBattleField.UIActionList.Add(new UIActionEvent
      //         {
      //            Action = UIActions.UIActionAddToShipUI,
      //            ShipId = ShipInfo.ShipId,
      //            ShipUiAction = (shipCanvas) =>
      //            {
      //               SilverlightPorts.UnitControls.SmallExplosion exp = new SilverlightPorts.UnitControls.SmallExplosion(shipCanvas, p);
      //            }
      //         });
      //      }
      //   }
      //}

      #endregion
   }

   public class PlanetHaul : IHual
   {
      public BattleGroup TheBattleGroup { get { return null; } set { throw new Exception("Bad op o-"); } }
      private PortInfo curPort;
      public void SyncUI() { }
      public PlanetHaul(PortInfo port)
      {
         curPort = port;
         ShipInfo = new PortPresenterShip(port);
      }
      #region IHual Members

      public Point TranslateWorldLocaionToShipCord(Point wloc)
      {
         return wloc;
      }

      public HaulState CurHaulState
      {
         get
         {
            throw new NotImplementedException();
         }
         set
         {
            throw new NotImplementedException();
         }
      }

      public double XSqueezeForce
      {
         get
         {
            throw new NotImplementedException();
         }
         set
         {
            throw new NotImplementedException();
         }
      }

      public double YSqueezeForce
      {
         get
         {
            throw new NotImplementedException();
         }
         set
         {
            throw new NotImplementedException();
         }
      }

      public Point? GetMoveTo()
      {
         throw new NotImplementedException();
      }

      public void SetMoveTo(Point? p)
      {
         throw new NotImplementedException();
      }

      public BattleField CurrentBattleField
      {
         get { return curPort.PortBattleField; }
      }

      public Transport CurrentTransport
      {
         get
         {
            throw new NotImplementedException();
         }
         set
         {
            throw new NotImplementedException();
         }
      }

      public IShip ShipInfo
      {
         get;
         private set;
      }

      public DomainInfo domain
      {
         get { return ShipInfo.Domain; }
      }

      public List<ITurret> Turs
      {
         get { throw new NotImplementedException(); }
      }

      public HualTypes Type
      {
         get { throw new NotImplementedException(); }
      }

      public RotateUtils LocInf
      {
         get { throw new NotImplementedException(); }
      }

      public bool IsHit(IBullet blt)
      {
         if (ShipInfo.Domain == null) return false;
         if (!blt.domain.IsFriendlyDomain(ShipInfo.Domain))
         {
            if (blt.GetPosition().Y >= curPort.PortBattleField.FieldHeight - 10)
            {
               ShipInfo.TakeDamange(blt);
               //ShowBulletHit(blt);
               if (curPort.Population <= 0  && CurrentBattleField.GetShipCount(domain) == 0)
               {
                  domain.RaisePortLostAlert(CurrentBattleField.Port);
                  ShipInfo.Domain.SetPortSnapShot(CurrentBattleField.Port, blt.domain);
               }
               return true;
            }
         }
         return false;
      }

      //private void ShowBulletHit(IBullet blt)
      //{
      //   if (CurrentBattleField.DrawingCanvas != null)
      //   {
      //      Point p = blt.GetPosition();
      //      if (this.CurrentBattleField != null)
      //      {
      //         CurrentBattleField.UIActionList.Add(new UIActionEvent
      //         {
      //            Action = UIActions.UIActionAction,
      //            UIAction = () =>
      //            {
      //               if (CurrentBattleField.DrawingCanvas != null)
      //               {
      //                  SilverlightPorts.UnitControls.SmallExplosion exp = new SilverlightPorts.UnitControls.SmallExplosion(CurrentBattleField.DrawingCanvas, p);
      //               }
      //            }
      //         });
      //      }
      //   }
      //}

      public void DeleteHaul()
      {
         throw new NotImplementedException();
      }

      public void SetLocation(Point p)
      {
         throw new NotImplementedException();
      }

      public Point GetLocation()
      {
         throw new NotImplementedException();
      }

      public void SetBattleField(BattleField bf)
      {
         throw new NotImplementedException();
      }

      public bool ProcessSpecialShipState()
      {
         throw new NotImplementedException();
      }

      #endregion

      #region IMappableObject Members

      public MapCordObj CurCords
      {
         get
         {
            throw new NotImplementedException();
         }
         set
         {
            throw new NotImplementedException();
         }
      }

      #endregion
   }
   //public class FrameworkUIForRotateUtilSample : IFrameworkUIForRotateUtil
   //{
   //   #region IFrameworkUIForRotateUtil Members

   //   public FrameworkElement UIElement
   //   {
   //      get;
   //      set;
   //   }

   //   public RotateTransform Rotation
   //   {
   //      get { throw new NotImplementedException(); }
   //   }

   //   public void SetUiSize(double w, double h)
   //   {
   //   }

   //   #endregion
   //}
}
