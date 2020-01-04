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
using SilverlightPorts.UnitControls;

namespace SilverlightPorts
{
   public partial class BasicHaulUI : UserControl, IFrameworkUIForRotateUtil
   {
      public IHual Haul { get; private set; }
      List<BasicTurretUI> Turs = new List<BasicTurretUI>();
      public BasicHaulUI(IHual h)
      {
         Haul = h;
         InitializeComponent();
         SetDomainMark(h.domain);
         h.LocInf.AttachUIElement(this);
         foreach (ITurret t in h.Turs)
         {
            BasicTurretUI tui = new BasicTurretUI(t);
            Turs.Add(tui);
            canv.Children.Add(tui);
            t.LocInf.AttachUIElement(tui);
         }
      }
    
      #region IFrameworkUIForPlacement Members

      private void SetLocation(double x, double y)
      {
         Haul.LocInf.SetRelativePosition(new Point(x, y));
      }

      public void SyncUI()
      {
         RotateUtils LocInf = Haul.LocInf;
         Point p = LocInf.GetRelativePosition();
         Canvas.SetLeft(this, p.X - (LocInf.ActualWidth / 2));
         Canvas.SetTop(this, p.Y - (LocInf.ActualHeight / 2));
         rotatTran.Angle = LocInf.DisplayUseRotation;
         foreach (BasicTurretUI t in Turs)
         {
            t.SyncUI();
         }
      }
      public void SetUiSize(double x, double y, double w, double h)
      {
         this.Width = w;
         this.Height = h;
         rotatTran.CenterX = w / 2;
         rotatTran.CenterY = h / 2;
      }
      //public void AddUIChild(IFrameworkUIForRotateUtil u)
      //{
      //   canv.Children.Add(u.UIElement);
      //}      


      private void SetDomainMark(DomainInfo dom)
      {
         DomainMarkUI mark = DomainMarks.GetDomainMark(dom);
         canv.Children.Add(mark);
         Canvas.SetLeft(mark, 56);
         Canvas.SetTop(mark, 56);
      }
      //public RotateTransform Rotation
      //{
      //   get { return rotatTran; }
      //}

      public Canvas MainCanvas { get { return canv; } }


      private int dieState = 0;
      static Random rnd = new Random();
      //Done on UI thread
      public bool ProcessSpecialShipState(Canvas screen, BasicHaulUI haului)
      {
         switch (Haul.CurHaulState)
         {
            case HaulState.StateDieing:
               return ProcessDieShipState(screen, haului);
            case HaulState.StatePorting:
               return ProcessPortingShipState(screen);
         }
         //throw new InvalidOperationException("Bad state " + Haul.CurHaulState);
         return true;
      }

      private bool ProcessDieShipState(Canvas screen, BasicHaulUI haului)
      {
         dieState++;
         Point p = Haul.LocInf.GetRelativePosition();
         //SyncUI();
         p.X++;
         p.Y++;
         SetLocation(p.X, p.Y);
         SyncUI();
         if (dieState % 7 == 0)
         {
            Point rp = new Point();
            rp.X = rnd.Next(128) - 64;
            rp.Y = rnd.Next(128) - 64;
            SmallExplosion exp = new SmallExplosion(haului.canv, rp);
         }
         if (dieState == 50)
         {
            Point rp = new Point(32, 32);
            SmallExplosion exp = new SmallExplosion(haului.canv, rp);
         }
         if (dieState > 50)
         {
            
               this.Opacity -= 0.02;
            
         }
         if (dieState >= 90)
         {
            screen.Children.Remove(this);
            return true;
         }
         return false;
      }

      private bool ProcessPortingShipState(Canvas screen)
      {
         if (dieState == 0)
         {
            if (screen != null)
            {
               WarpUI wrp = new WarpUI();
               Point loc = Haul.LocInf.GetRelativePosition();
               Canvas.SetLeft(wrp, loc.X - 64);
               Canvas.SetTop(wrp, loc.Y - 64);
               screen.Children.Add(wrp);
            }
         }
         dieState++;
         
         
            this.Opacity -= 0.02;
         
         if (dieState >= 45)
         {
            screen.Children.Remove(this);
            return true;
         }
         return false;
      }

      #endregion
   }
}
