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

namespace SilverlightPorts
{
   public class AIThresholds
   {
      public int AISpeed = 500;
      public double PercentForNewPorts { get; private set; }
      public double PercentForScience { get; private set; }
      public double PercentForHauls { get { return 1 - PercentForScience - PercentForNewPorts; } }
      public double MainBattleGroupdPercent { get { return 0.8; } }
      public AIThresholds(DomainInfo dom)
      {
         PercentForNewPorts = 0.2;
         PercentForScience = 0.1;
      }
      public void UpdateThreadholds()
      {
      }
   }

   public enum ShipOrderType {
      OrderExpand,
      OrderGoToPort,
      OrderSavePort,
   }
   public class ShipOrder
   {
      public ShipOrderType order { get; set; }
      public PortInfo ToPort { get; set; }
   }
}
