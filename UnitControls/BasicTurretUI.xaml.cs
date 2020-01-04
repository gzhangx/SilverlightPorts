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

namespace SilverlightPorts
{
   public partial class BasicTurretUI : UserControl, IFrameworkUIForRotateUtil
   {
      private RotateUtils locInf;
      private ITurret tur;
      public BasicTurretUI(ITurret t)
      {
         this.tur = t;
         locInf = t.LocInf;
         InitializeComponent();         
      }


      #region IFrameworkUIForRotateUtil Members

      //public RotateTransform Rotation
      //{
      //   get { return rotatTran; }
      //}

       //should be on ui thread
      //public void SetLocation(double x, double y)
      //{
      //    Canvas.SetLeft(this, x);
      //    Canvas.SetTop(this, y);
      //}

      public void SyncUI()
      {
         rotatTran.Angle = locInf.DisplayUseRotation;
         if (tur.IsFiring)
         {
            tur.IsFiring = false;
            FireAni();
         }
      }

      public void SetUiSize(double x, double y, double w, double h)
      {        
         rotatTran.CenterX = w / 2;
         rotatTran.CenterY = h / 2;
         Canvas.SetLeft(this, x - rotatTran.CenterX);
         Canvas.SetTop(this, y - rotatTran.CenterY);
         this.Width = w;
         this.Height = h;
         mainOvl.Width = w;
         mainOvl.Height = h;
         //pointOvl.Width = 4;
         //pointOvl.Height = 4;
         //Canvas.SetLeft(pointOvl, x + 4);
         //Canvas.SetTop(pointOvl, (x / 2) - 2);
         //Canvas.SetLeft(barrel, x - 10);
         //Canvas.SetTop(barrel, (x / 2) - 2);
         rotatTran.CenterX = w / 2;
         rotatTran.CenterY = h / 2;
         rotatTran.Angle = 0;
         //mainOvl.Fill = new SolidColorBrush(Colors.White);
      }  
      #endregion

      #region IGenFireableUI Members

      public void FireAni()
      {
          firestory.Pause();
          firestory.Begin();
      }

      #endregion
   }
}
