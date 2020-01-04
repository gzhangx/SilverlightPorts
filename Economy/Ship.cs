
using System;
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SilverlightPorts
{
   public interface IShip
   {
      int ShipId { get; }
      double Speed { get; }
      ShipGenerationInfo ShipType { get; }
      double CurLife { get; }
      DomainInfo Domain { get; }
      void TakeDamange(IBullet blt);
      bool IsDead();
   }
   public class Ship : IShip
   {
      private static int shipIdCnt = 0;
      private static object shipIdLock = new object();
      public int ShipId { get; private set; }
      public double Speed
      {
         get
         {
            return ShipType.Speed;
         }
      }
      //public PortInfo port { get; private set; }
      public float ConstructCostMineral { get; private set; }
      //public float ConstructCostMoney { get; private set; }
      public ShipGenerationInfo ShipType { get; private set; }
      public double CurLife { get; private set; }
      public bool IsDead() { return CurLife <= 0; }
      public DomainInfo Domain { get; private set; }
      public Ship(DomainInfo domain)
      {
         Domain = domain;
         lock (shipIdLock)
         {
            unchecked
            {
               ShipId = shipIdCnt++;
            }
         }
         //port = prt;
         ShipType = new ShipGenerationInfo(domain);
         CurLife = ShipType.MaxLife;
         ConstructCostMineral = ShipType.MaxLife * 10;
         CurLife = ShipType.MaxLife;
         //ConstructCostMoney = ShipType.MaxLife * 80;
      }

      public void ResetLife()
      {
         CurLife = ShipType.MaxLife;
      }
      public void TakeDamange(IBullet blt)
      {
         ShipGenerationInfo EneShipType = blt.Gen;
         float half = EneShipType.Damage / 2;
         int genDiff = ShipType.Generation - EneShipType.Generation;
         if (genDiff <= 0)
         {
            CurLife -= half;
         }
         else
         {
            CurLife -= (half / genDiff);
         }
      }
   }

   public class PortPresenterShip : IShip
   {
      private PortInfo _port;
      public int ShipId { get { throw new Exception(""); } }
      public PortPresenterShip(PortInfo port)
      {
         _port = port;
         ShipType = new PortShipGenInfo(port);
      }
      public double Speed
      {
         get { return 0; }
      }
      public ShipGenerationInfo ShipType
      {
         get;
         private set;
      }

      public double CurLife
      {
         get { return _port.Population; }
      }
      public bool IsDead() { return CurLife <= 0; }

      public DomainInfo Domain
      {
         get { return _port.Domain; }
      }

      public void TakeDamange(IBullet blt)
      {
         _port.DamageReducePop(120000000);
      }

   }


   public class ShipGenerationInfo
   {
      private DomainInfo domain;
      public virtual int Generation { get { return domain.Generation; } }
      public virtual double ShipSize { get { return 128; } }
      public ShipGenerationInfo(DomainInfo dom)
      {
         domain = dom;
      }
      public virtual float Speed
      {
         get
         {
            float spd = Generation * 0.1f;
            if (spd > Consts.MAX_SHIP_SPEED) spd = Consts.MAX_SHIP_SPEED;
            return spd;
         }
      }
      public double BulletSpeed
      {
         get
         {
            return 2;
         }
      }
      public double MaxBulletFlightTime
      {
         get
         {
            return 200 + (Generation / 2);
         }
      }
      public double MaxBulletRange
      {
         get
         {
            return BulletSpeed * MaxBulletFlightTime;
         }
      }
      public float Damage
      {
         get
         {
            return Generation * 2f;
         }
      }

      public float MaxLife
      {
         get
         {
            return 10 + (Generation * 1.1f);
         }
      }
   }

   public class PortShipGenInfo : ShipGenerationInfo
   {
      private PortInfo port;
      public PortShipGenInfo(PortInfo prt)
         : base(prt.Domain)
      {
         port = prt;
      }
      public override double ShipSize { get { return 128; } }
      public override float Speed
      {
         get { return 0; }
      }
      public override int Generation { get { return port.Domain.Generation; } }
   }
}
