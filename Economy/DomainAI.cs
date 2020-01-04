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
   public partial class DomainInfo
   {
      //domain id to domain snapshot
      private static Dictionary<int, Dictionary<int, AIDomainSnapShot>> DomainSnapShot = new Dictionary<int,Dictionary<int,AIDomainSnapShot>>();
      private static Dictionary<int, Dictionary<int, AIPortSnapShot>> PortSnapShot = new Dictionary<int,Dictionary<int,AIPortSnapShot>>();

      //reset each turn;
      private Dictionary<int, PortInfo> ThisTurnSentShips = new Dictionary<int, PortInfo>();
      public void SetPortSnapShot(PortInfo p, DomainInfo enemyDomain)
      {
         if (!DomainSnapShot.ContainsKey(DomainID))
         {
            DomainSnapShot.Add(DomainID, new Dictionary<int, AIDomainSnapShot>());
            PortSnapShot.Add(DomainID, new Dictionary<int, AIPortSnapShot>());
         }
         if (p.Domain != this)
         {
            Dictionary<int, AIPortSnapShot> ps = PortSnapShot[DomainID];
            if (!ps.ContainsKey(enemyDomain.DomainID))
            {
               ps.Add(enemyDomain.DomainID, new AIPortSnapShot
               {
                  DomainID = enemyDomain.DomainID,
                  PortPopulation = p.Population,
               });
            }
            else
            {
               AIPortSnapShot snp = ps[enemyDomain.DomainID];
               snp.PortPopulation = p.Population;
               snp.DomainID = enemyDomain.DomainID;
            }

            Dictionary<int, AIDomainSnapShot> ds = DomainSnapShot[DomainID];
            if (!ds.ContainsKey(enemyDomain.DomainID))
            {
               ds.Add(enemyDomain.DomainID, new AIDomainSnapShot
               {
                  Domain = enemyDomain,
                  DomGen = enemyDomain.Generation,
               });
            }
            else
            {
               AIDomainSnapShot snp = ds[p.Domain.DomainID];
               snp.Domain = enemyDomain;
               snp.DomGen = enemyDomain.Generation;
            }

         }
      }
      private AIPortSnapShot GetPortSnapShot(PortInfo p)
      {
         AIPortSnapShot snp = null;
         if (PortSnapShot.ContainsKey(DomainID))
         {
            Dictionary<int, AIPortSnapShot> lst = PortSnapShot[DomainID];
            if (lst.ContainsKey(p.ID))
            {
               snp = lst[p.ID];
               try
               {
                  snp.Domain = DomainSnapShot[DomainID][snp.DomainID];
               }
               catch { }
            }
         }
         return snp;
      }

      //before sci and maintain.
      private void HaulBuildAI()
      {
         if (this.IsHuman)
         {
            if (!LetAIControlMe)
               return;
         }
         double HaulMoney = _TotalMoney * AIThing.PercentForHauls;
         int curNumShips = Ships.Count;
         HaulMoney -= curNumShips * GetShipMaintenanceCost();
         double shipCost = GetShipCost();
         double maintenanceCost = GetShipMaintenanceCost();
         if (HaulMoney < maintenanceCost)
         {
            foreach (PortInfo p in PortsSnapShot)
            {
               p.ShipBuildingQueue.CurrentBuildShipQueue = 0;
            }
         }
         else
         {
            if (HaulMoney > 0)
            {
               foreach (PortInfo p in PortsSnapShot)
               {
                  if (p.ShipBuildingQueue.CurrentBuildShipQueue > 0)
                  {
                     HaulMoney -= shipCost;
                     if (HaulMoney <= 0) break;
                  }
               }
               if (HaulMoney > maintenanceCost)
               {
                  foreach (PortInfo p in PortsSnapShot)
                  {
                     if (p.ShipBuildingQueue.CurrentBuildShipQueue == 0)
                     {
                        p.ShipBuildingQueue.CurrentBuildShipQueue++;
                        HaulMoney -= shipCost;
                        if (HaulMoney <= 0) break;
                     }
                  }
               }
            }
         }
      }

      private List<BattleGroup> BattleGroups = new List<BattleGroup>();


      private int curAITime = 0;
      private void DoAIThing()
      {
         curAITime++;
         if (curAITime < AIThing.AISpeed)
         {
            return;
         }
         curAITime = 0;
         ThisTurnSentShips.Clear();         
         if (this.IsHuman)
         {
            if (!LetAIControlMe)
               return;
         }
         MakePortSnapShots();
         lock (shipGenLock)
         {
            AssignBattleGroups();
            foreach (BattleGroup b in BattleGroups)
            {
               if (b.Order != null && b.Order.order == ShipOrderType.OrderExpand)
               {
                  if (b.Order.ToPort.Domain == this)
                  {
                     b.Order = null;
                  }
               }
            }
            double PlanetMoney = _TotalMoney * AIThing.PercentForNewPorts;
            double portCosts = 0;
            foreach (PortInfo p in ports)
            {
               double portRev = p.DisplayPortRevenue;
               if (portRev < 0)
               {
                  portCosts += portRev;
               }
            }
            double availabelForNewPlanet = PlanetMoney + portCosts;
            //List<BattleGroup> usedBattleGroups = 
            AITrySaveInvadedPorts();
            
            TryColPlanetsWithMoney(availabelForNewPlanet); //usedBattleGroups
         }
         //foreach (PortInfo p in ports)
         //{
         //   DoBattleStrag(p);
         //}
      }

      private void TryColPlanetsWithMoney(double availabelForNewPlanet)
      {
         if (availabelForNewPlanet > PortInfo.MaxPortCost)
         {
            int numPlanetstoCol = (int)(availabelForNewPlanet / PortInfo.MaxPortCost);
            int colShips = 0;
            Dictionary<int, BattleGroup> PortToBattleGroupMapping = new Dictionary<int, BattleGroup>();
            if (numPlanetstoCol > 0)
            {
               {
                  foreach (BattleGroup b in BattleGroups)
                  {
                     if (b.Order != null && b.Order.order == ShipOrderType.OrderExpand)
                     {
                        colShips++;
                        if (!PortToBattleGroupMapping.ContainsKey(b.Order.ToPort.ID))
                        {
                           PortToBattleGroupMapping.Add(b.Order.ToPort.ID, b);
                        }
                     }
                  }
               }
               for (; colShips <= numPlanetstoCol; colShips++)
               {
                  AIPickShipAndPlanetForCol(PortToBattleGroupMapping);
               }
            }
         }
         DoActualShipPorting();
      }

      private void AssignBattleGroups()
      {
         foreach (BattleGroup grp in BattleGroups)
         {
            List<IHual> gone = new List<IHual>();
            foreach (IHual h in grp.Ships)
            {
               if (h.ShipInfo.IsDead())
               {
                  gone.Add(h);
               }
            }
            gone.ForEach(h => grp.Ships.Remove(h));
         }
         List<int> GroupCompos = new List<int>();
         lock (shipGenLock)
         {
            if (Ships.Count == 0)
            {
               BattleGroups.Clear();
               return;
            }
            else
            {
               double compoPct = 0.6;
               int next = Ships.Count;
               while (next > 0)
               {
                  int tmp = (int)(next * compoPct);
                  if (tmp < next) tmp++;
                  if (tmp == 0)
                  {
                     GroupCompos.Add(next);
                     break;
                  }
                  GroupCompos.Add(tmp);
                  next = next - tmp;
               }
            }
            if (BattleGroups.Count > GroupCompos.Count)
            {
               for (int j = BattleGroups.Count - 1; j >= GroupCompos.Count; j--)
               {
                  BattleGroup rmg = BattleGroups[j];
                  BattleGroups.RemoveAt(j);
                  foreach (IHual h in rmg.Ships)
                  {
                     h.TheBattleGroup = null;
                  }
               }
            }
            List<IHual> freeShips = new List<IHual>();
            foreach (IHual h in Ships)
            {
               if (h.TheBattleGroup == null)
               {
                  freeShips.Add(h);
               }
            }
            for (int i = 0; i < GroupCompos.Count; i++)
            {
               int curGrpCount = GroupCompos[i];
               if (BattleGroups.Count <= i) break;
               BattleGroup curGrp = BattleGroups[i];
               int realCnt = curGrp.Ships.Count;
               if (realCnt > curGrpCount)
               {
                  for (int j = realCnt - 1; j >= curGrpCount; j--)
                  {
                     IHual h = curGrp.Ships[j];
                     curGrp.Ships.RemoveAt(j);
                     h.TheBattleGroup = null;
                     freeShips.Add(h);
                  }
               }
            }
            for (int i = 0; i < GroupCompos.Count; i++)
            {
               int curGrpCount = GroupCompos[i];
               if (BattleGroups.Count <= i)
               {
                  BattleGroup curGrp = new BattleGroup();
                  BattleGroups.Add(curGrp);
                  for (int j = 0; j < curGrpCount; j++)
                  {
                     curGrp.Ships.Add(freeShips[j]);
                     freeShips[j].TheBattleGroup = curGrp;
                  }
                  freeShips.RemoveRange(0, curGrpCount);
               }
               else
               {
                  BattleGroup curGrp = BattleGroups[i];
                  if (curGrp.Ships.Count == curGrpCount) continue;
                  //if (curGrp.Ships.Count > curGrpCount)
                  //{
                  //  for (int j = curGrpCount; j < curGrp.Ships.Count; j++)
                  //  {
                  //     curGrp.Ships[j].TheBattleGroup = null;
                  //     freeShips.Add(curGrp.Ships[j]);
                  //  }
                  //  curGrp.Ships.RemoveRange(curGrpCount, curGrp.Ships.Count - curGrpCount);
                  //}
                  if (curGrp.Ships.Count < curGrpCount)
                  {
                     int needed = curGrpCount - curGrp.Ships.Count;
                     for (int j = 0; j < needed; j++)
                     {
                        IHual h = freeShips[j];
                        h.TheBattleGroup = curGrp;
                        curGrp.Ships.Add(h);
                     }
                     freeShips.RemoveRange(0, needed);
                  }
               }
            }
         }
      }

      private ShipOrder FindPortToGo(PortInfo from, Dictionary<int, BattleGroup> PortToBattleGroupMapping, bool attack)
      {
         foreach (PortDistInfo dst in from.PortDists)
         {
            //TODO: added a domain view so ai don't cheat
            if (dst.Port.Domain == this) continue;
            if (PortToBattleGroupMapping.ContainsKey(dst.Port.ID)) continue;
            AIPortSnapShot snap = GetPortSnapShot(dst.Port);
            if (snap == null || attack)
            {
               return new ShipOrder
               {
                  order = ShipOrderType.OrderExpand,
                  ToPort = dst.Port
               };
            }
         }
         return null;
      }

      private PortInfo GetBestBGPort(BattleGroup bg)
      {
         if (bg.Ships.Count == 0) return null;
         Dictionary<int, BGPortCount> pct = new Dictionary<int, BGPortCount>();
         foreach (IHual h in bg.Ships)
         {
            BattleField curBf = h.CurrentBattleField;
            if (curBf != null)
            {
               if (pct.ContainsKey(curBf.Port.ID))
               {
                  pct[curBf.Port.ID].Count++;
               }
               else
               {
                  pct.Add(curBf.Port.ID, new BGPortCount
                  {
                     Count = 1,
                     Port = curBf.Port
                  });
               }
            }
            else
            {
               Transport curTf = h.CurrentTransport;
               if (curTf != null)
               {
                  int toPortId = curTf.ToPort.ID;
                  if (pct.ContainsKey(toPortId))
                  {
                     pct[toPortId].Count++;
                  }
                  else
                  {
                     pct.Add(toPortId, new BGPortCount
                     {
                        Count = 1,
                        Port = curTf.ToPort
                     });
                  }
               }
            }
         }

         List<BGPortCount> lst = new List<BGPortCount>();
         foreach (int id in pct.Keys)
         {
            lst.Add(pct[id]);
         }
         lst.Sort((a, b) =>
         {
            if (a.Count > b.Count) return -1;
            if (a.Count < b.Count) return 1;
            return 0;
         });
         if (lst.Count == 0) return null;
         return lst[0].Port;
      }

      private void MakePortSnapShots()
      {
         List<PortInfo> snapshotports = new List<PortInfo>();
         lock (PortsLock)
         {
            snapshotports.AddRange(this.ports);
         }
         Dictionary<int, int> toPortShipCnt = new Dictionary<int, int>();
         foreach (PortInfo p in snapshotports)
         {
            if (!toPortShipCnt.ContainsKey(p.ID))
            {
               toPortShipCnt.Add(p.ID, 0);
            }
            Dictionary<int, Transport> snaptrans = new Dictionary<int, Transport>();
            p.PortBattleField.UIGetCurTransportSnapShot(snaptrans);
            foreach (Transport tran in snaptrans.Values)
            {
               int portId = tran.ToPort.ID;
               if (toPortShipCnt.ContainsKey(portId))
               {
                  toPortShipCnt[portId]++;
               }
               else
               {
                  toPortShipCnt.Add(portId, 1);
               }
            }
         }
         foreach (PortInfo p in snapshotports)
         {
            p.CachedEnemyShipCountThisTurn = p.PortBattleField.CountEnemyShip(this);
            p.CachedMyShipCountThisTurn = p.PortBattleField.CountMyShips(this);
            p.CachedArrivingShipCountThisTurn = toPortShipCnt[p.ID];
         }
      }
      private void AITrySaveInvadedPorts()
      {
         List<PortInfo> invadedPorts = new List<PortInfo>();

         
         foreach (PortInfo p in ports)
         {
            if (p.CachedEnemyShipCountThisTurn > p.CachedMyShipCountThisTurn + p.CachedArrivingShipCountThisTurn)
            {
               invadedPorts.Add(p);
            }
         }
         invadedPorts.Sort((p1, p2) =>
         {
            if (p1.Population > p2.Population) return -1;
            if (p1.Population < p2.Population) return 1;
            return 0;
         });
         int totalAvailableShips = 0;
         foreach (BattleGroup MainBattleGroup in BattleGroups)
         {
            totalAvailableShips += MainBattleGroup.Ships.Count;
         }
         List<BattleGroup> usedBattleGroups = new List<BattleGroup>();
         int shipAt = 0;
         foreach (PortInfo p in invadedPorts)
         {
#if DEBUG
            if (this.IsHuman)
            {
               Console.Write("TODO: Remove this");
            }
#endif
            if (p.CachedEnemyShipCountThisTurn > totalAvailableShips) continue;
            if (shipAt >= Ships.Count) break;
            int curCnt = 0;
            int neededCnt = p.CachedEnemyShipCountThisTurn - p.CachedMyShipCountThisTurn - p.CachedArrivingShipCountThisTurn;
            while (curCnt < neededCnt)
            {
               if (shipAt >= Ships.Count) break;
               IHual h = Ships[shipAt++];
               if (ThisTurnSentShips.ContainsKey(h.ShipInfo.ShipId)) continue;
               BattleField curBf = h.CurrentBattleField;
               if (curBf != null)
               {
                  if (curBf.Port == p)
                  {
                     ThisTurnSentShips[h.ShipInfo.ShipId] = p;
                     continue;
                  }
                  else
                     if (!curBf.Port.HasEnemyShip(this))
                     {
                        //h.Order = new ShipOrder { order = ShipOrderType.OrderGoToPort, ToPort = MainBattleGroup.GatherPort };
                        curBf.Port.SendShipTo(p, h);
                        ThisTurnSentShips[h.ShipInfo.ShipId] = p;
                        curCnt++;
                     }
               }
            }
         }
         //return usedBattleGroups;
      }
      //private int SendAllBattleGroupToPort(BattleGroup bg, PortInfo port)
      //{
      //   int totalSent = 0;
      //   foreach (IHual h in bg.Ships)
      //   {
      //      BattleField curBf = h.CurrentBattleField;
      //      if (curBf != null)
      //      {
      //         if (curBf.Port == port)
      //         {
      //            totalSent++;
      //         }
      //         else
      //            if (!curBf.Port.HasEnemyShip(this))
      //            {
      //               //h.Order = new ShipOrder { order = ShipOrderType.OrderGoToPort, ToPort = MainBattleGroup.GatherPort };
      //               curBf.Port.SendShipTo(bg.GatherPort, h);
      //               totalSent++;
      //            }
      //      }
      //      else
      //      {
      //         Transport curTran = h.CurrentTransport;
      //         if (curTran != null)
      //         {
      //            if (curTran.ToPort == port)
      //            {
      //               totalSent++;
      //            }
      //         }
      //      }
      //   }
      //   return totalSent;
      //}
      private void AIPickShipAndPlanetForCol(Dictionary<int, BattleGroup> PortToBattleGroupMapping) //, List<BattleGroup> usedBgs
      {
         //foreach (IHual h in Ships)
         //{
         //    if (h.Order == null && h.CurrentBattleField != null && h.TheBattleGroup == null)
         //    {                  
         //       h.Order = newOrder = FindPortToGo(h.CurrentBattleField.Port, false);
         //       if(newOrder!= null) return;
         //    }
         //}


         //didn't find a ship or all ports are mine, try again use main battle group.
         if (ports.Count > 0)
         {
            foreach (BattleGroup MainBattleGroup in BattleGroups)
            {
               //if (usedBgs.Contains(MainBattleGroup)) continue;
               if (MainBattleGroup.GatherPort == null)
               {
                  MainBattleGroup.GatherPort = GetBestBGPort(MainBattleGroup);
               }
               if (MainBattleGroup.Order != null) continue;
               if (MainBattleGroup.Ships.Count > 0)
               {
                  int sentToSaveOther = 0;
                  int goodPosCount = 0;
                  foreach (IHual h in MainBattleGroup.Ships)
                  {
                     BattleField curBf = h.CurrentBattleField;
                     if (curBf != null)
                     {
                        if (ThisTurnSentShips.ContainsKey(h.ShipInfo.ShipId))
                        {
                           sentToSaveOther++;
                        }
                        else
                        if (curBf.Port != MainBattleGroup.GatherPort
                           && !curBf.Port.HasEnemyShip(this))
                        {
                           //h.Order = new ShipOrder { order = ShipOrderType.OrderGoToPort, ToPort = MainBattleGroup.GatherPort };
                           curBf.Port.SendShipTo(MainBattleGroup.GatherPort, h);
                        }
                        else
                        {
                           goodPosCount++;
                        }
                     }
                  }
                  if ( (goodPosCount == MainBattleGroup.Ships.Count)
                     || (
                     (goodPosCount >= MainBattleGroup.Ships.Count/2)
                     && (sentToSaveOther + goodPosCount == MainBattleGroup.Ships.Count)
                     ))
                  {
                     if (MainBattleGroup.GatherPort.Domain == null
                        || !MainBattleGroup.GatherPort.Domain.IsFriendlyDomain(this))
                     {
                        MainBattleGroup.Order = new ShipOrder
                        {
                           order = ShipOrderType.OrderExpand,
                           ToPort = MainBattleGroup.GatherPort
                        };
                     }else                     
                     {
                        MainBattleGroup.Order = FindPortToGo(MainBattleGroup.GatherPort, PortToBattleGroupMapping, true);
                        if (MainBattleGroup.Order != null)
                        {
                           MainBattleGroup.GatherPort = MainBattleGroup.Order.ToPort;
                        }
                        return;
                     }
                  }
               }
            }
         }
         //DoActualShipPorting();
      }

      private void DoActualShipPorting()
      {
         foreach (BattleGroup b in BattleGroups)
         {
            ShipOrder curorder = b.Order;
            if (curorder != null)
            {
               int totalOnOrder = 0;
               foreach (IHual h in b.Ships)
               {
                  BattleField curBf = h.CurrentBattleField;
                  if (curBf != null)
                  {
                     switch (curorder.order)
                     {
                        case ShipOrderType.OrderExpand:
                        case ShipOrderType.OrderGoToPort:
                        case ShipOrderType.OrderSavePort:
                           if (curorder.ToPort != curBf.Port)
                           {
                              curBf.Port.SendShipTo(curorder.ToPort, h);
                           }
                           else
                           {
                              if (curorder.order == ShipOrderType.OrderExpand)
                              {
                                 if (ColonizePort(curBf.Port))
                                 {
                                    b.Order = null;
                                 }
                              }
                              totalOnOrder++;
                           }
                           break;
                     }
                     if (b.Order == null) break;
                  }
               }
               if (totalOnOrder == b.Ships.Count)
               {
                  b.Order = null;
               }
            }
         }
      }

      private bool IsMainBattleGroupReady()
      {
         bool res = false;
         return res;
      }

      private bool ColonizePort(PortInfo port)
      {
         long initialPop = port.PortBattleField.GetShipCount(this) * 1000;
         if (initialPop != 0)
         {
            return GlobalData.ColonizePlanet(this, port, initialPop);
         }
         return false;
      }

      public void RaiseBattleLostAlert(PortInfo p) { }
      public void RaisePortLostAlert(PortInfo p) { }
   }

   class BGPortCount
   {
      public PortInfo Port;
      public int Count;
   }
}