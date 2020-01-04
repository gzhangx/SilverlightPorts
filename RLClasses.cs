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
using System.Threading;

namespace SilverlightPorts
{
   public class GlobalData
   {
      public static int CheatLevel = 10;
      public static Canvas RootMapGraphics { get; set; }
      private static object lockObj = new object();
      public static List<PortInfo> Ports;
      public static List<DomainInfo> Domains;
      private static Thread domainThread;
      private static bool domainThreadRunning = true;
      private static void ProcessBattles()
      {
         foreach (PortInfo p in Ports)
         {
            p.PortBattleField.StartProcessThread();
         }
      }

      public static void DoTurn(Dictionary<int, PortUserControl> PortUIs)
      {
         foreach (DomainInfo dom in Domains)
         {
            DoUITurn(dom, PortUIs);
         }
      }

      private static void DoUITurn(DomainInfo dom, Dictionary<int, PortUserControl> PortUIs)
      {
         List<PortInfo> snapshot = dom.PortsSnapShot;
         foreach (PortInfo p in snapshot)
         {
            p.SetIncomeDisplayForUIUpdate();
            if (GlobalData.CheatLevel > 5)
            {
               if (PortUIs[p.ID].NeedSetUiPortDomainMark(dom.DomainID))
               {
                  PortUIs[p.ID].SetUiPortDomainMark(DomainMarks.GetDomainMark(dom));
               }
            }
         }
         //ProcessTransportUI();
      }

      public static void StartDomainThread()
      {
         ProcessBattles();
         domainThreadRunning = true;
         domainThread = new Thread(new ThreadStart(()=>{
            while (domainThreadRunning)
            {
               Thread.Sleep(100);
               foreach (DomainInfo dom in Domains)
               {
                  dom.DoTurn();
               } 
            }
         }));
         domainThread.Start();
      }

      public static void StopGame()
      {
         if (domainThread == null) return;
         domainThreadRunning = false;
         foreach (PortInfo p in Ports)
         {
            p.PortBattleField.StoppingThread();
         }
         domainThread.Join();
         domainThread = null;
         foreach (PortInfo p in Ports)
         {
            p.PortBattleField.StoppingThread();
         }
         foreach (PortInfo p in Ports)
         {
            p.PortBattleField.WaitThreadStop();
         }
      }

      public static bool ColonizePlanet(DomainInfo dom, PortInfo port, long pop)
      {
         lock (lockObj)
         {
            if (port.Population > 0) return false;
            dom.AddPort(port);            
            port.InitColonizeAddPop(pop);
         }
         return true;
      }
   }
}
