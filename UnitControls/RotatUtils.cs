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
   public partial class RotateUtils : BaseAngleInfo
   {
      public Action<RotateUtils> AimDone;
      //public IFrameworkUIForRotateUtil uiElement { get; private set; }
      private BaseAngleInfo baseInfo;
      private double _posX;
      private double _posY;
      private double _actualWidth;
      private double _actualHeight;
      private double _CurRotate = 0;
      private double _posToCenterLenCst = 0;
      private double _initAngleCst = 0;
      private double _DisplayUseRotation;
      public double DisplayUseRotation
      {
         get { return _DisplayUseRotation; }
      }
      public void SetCloneTransferUiElement(RotateUtils rot)
      {
         rot._DisplayUseRotation = _DisplayUseRotation;
         rot._posX = _posX;
         rot._posY = _posY;
         //IFrameworkUIForRotateUtil oldUiElenent = uiElement;
         //uiElement = null;
         //return new RotateUtils
         //{
         //   _actualHeight = this._actualHeight,
         //   _actualWidth = this._actualWidth,
         //   _CurRotate = this._CurRotate,
         //   _initAngleCst = this._initAngleCst,
         //   _posToCenterLenCst = this._posToCenterLenCst,
         //   _posX = this._posX,
         //   _posY = this._posY,
         //   uiElement = oldUiElenent,
         //};
      }
      protected double CurRotate
      {
         get
         {
            if (baseInfo == null) return TreatAngle(_CurRotate);
            return TreatAngle(BaseAngle);
         }
         set
         {
            if (baseInfo == null)
            {
               _CurRotate = value;
            }
            else
            {
               _CurRotate = value - baseInfo.BaseAngle;
            }
            _CurRotate = TreatAngle(_CurRotate);
         }
      }
      public double TurnRate = 0.01;

      public Point TranslatePoint(Point p)
      {
         double l = Math.Sqrt((p.X * p.X) + (p.Y * p.Y));
         p.X = PosX + (l * Math.Cos(BaseAngle));
         p.Y = PosY + (l * Math.Sin(BaseAngle));
         return p;
      }
      public void InitBaseInf(BaseAngleInfo binf, double x, double y, double aw, double ah)
      {
         baseInfo = binf;
         _posX = x;
         _posY = y;
         if (baseInfo != null)
         {
            x -= baseInfo.ActualWidth / 2;
            y -= baseInfo.ActualHeight / 2;
            _initAngleCst = GetAngle(_posX, _posY, baseInfo.ActualWidth / 2, baseInfo.ActualHeight / 2);
         }
         _posToCenterLenCst = Math.Sqrt((x * x) + (y * y));
         _actualWidth = aw;
         _actualHeight = ah;
      }

      /// <summary>
      /// Location of ship on battle field
      /// </summary>
      /// <param name="p"></param>
      public void SetRelativePosition(Point p)
      {
         _posX = p.X;
         _posY = p.Y;
      }
      public Point GetRelativePosition()
      {
         return new Point
         {
            X = _posX,
            Y = _posY
         };
      }
      public void AttachUIElement(IFrameworkUIForRotateUtil u)
      {
         //uiElement = u;
         if (u != null)
         {
            //uiElement.UIElement.Width = _actualWidth;
            //uiElement.UIElement.Height = _actualHeight;
            //uiElement.Rotation.CenterX = this.ActualWidth / 2;
            //uiElement.Rotation.CenterY = this.ActualHeight / 2;
            //Canvas.SetLeft(uiElement.UIElement, _posX - (_actualWidth / 2));
            //Canvas.SetTop(uiElement.UIElement, _posY - (_actualHeight / 2));
            //u.SetLocation(_posX, _posY);
            u.SetUiSize(_posX, _posY,_actualWidth, _actualHeight);
         }
      }
      public double PosX
      {
         get
         {
            double x = _posX;
            //if (uiElement != null)
            //{
            //   x = Canvas.GetLeft(uiElement);
            //}
            if (baseInfo == null)
               return x;

            return baseInfo.PosX + (_posToCenterLenCst * Math.Cos(baseInfo.BaseAngle + _initAngleCst));
         }
      }
      public double PosY
      {
         get
         {
            double y = _posY;
            //if (uiElement != null)
            //{
            //   y = Canvas.GetTop(uiElement);
            //}
            if (baseInfo == null) return y;
            return baseInfo.PosY + (_posToCenterLenCst * Math.Sin(baseInfo.BaseAngle + _initAngleCst));
         }
      }
      public double ActualWidth
      {
         get
         {
            return _actualWidth;
         }
      }
      public double ActualHeight
      {
         get
         {
            return _actualHeight;
         }
      }
      public static double TreatAngle(double ang)
      {
         while (ang > Math.PI)
         {
            ang -= Math.PI * 2;
         }
         while (ang < -Math.PI)
         {
            ang += Math.PI * 2;
         }
         return ang;
      }

      public static double GetAngle(double x, double y, double fromX, double fromY)
      {
         double dy = y - fromY;
         double dx = x - fromX;
         if (dx == 0) dx = 0.0001;
         double ang = Math.Atan(dy / dx);
         if (dx < 0)
         {

            ang = Math.PI + ang;

         }
         ang = TreatAngle(ang);
         return ang;
      }
      public void Turnto(Point p)
      {
         Turnto(p.X, p.Y);
      }
      public void Turnto(double x, double y)
      {
         //double dy = y - PosY;
         //double dx = x - PosX;
         //if (dx == 0) dx = 0.0001;
         //double ang = Math.Atan(dy / dx);
         //if (dx < 0)
         //{

         //      ang = Math.PI + ang;

         //}         
         //ang = TreatAngle(ang);
         double ang = GetAngle(x, y, PosX, PosY);
         double diff = Math.Abs(ang - CurRotate);
         diff = TreatAngle(diff);
         double absdiff = Math.Abs(diff);
         if (absdiff < TurnRate)
         {
            CurRotate = ang;
            ang = (CurRotate * 180 / Math.PI);
            if (AimDone != null)
            {
               AimDone(this);
            }
         }
         else
         {
            bool add = true;
            if ((ang > (Math.PI / 2)) && (CurRotate < (ang - Math.PI)))
            {
               add = false;
            }
            else if ((ang < (Math.PI / 2)) && (CurRotate > (ang + Math.PI)))
            {
               add = true;
            }
            else
               if (absdiff < Math.PI)
               {
                  add = ang > CurRotate;
               }
               else
               {
                  add = ang < CurRotate;
               }

            if (add)
               CurRotate += TurnRate;
            else
               CurRotate -= TurnRate;


            ang = CurRotate;
            ang = (ang * 180 / Math.PI);
         }
         if (baseInfo != null)
         {
            ang -= baseInfo.BaseAngle * 180 / Math.PI;
         }
         _DisplayUseRotation = ang;
         
         //turs.ForEach(a => a.Turnto(x, y));
      }

      #region BaseAngleInfo Members

      public double BaseAngle
      {
         get
         {
            if (baseInfo == null)
               return CurRotate;
            return TreatAngle(_CurRotate + baseInfo.BaseAngle);
         }
      }

      #endregion
   }


   public interface ISyncableUI
   {
       void SyncUI();
   }
   public interface IFrameworkUIForRotateUtil : ISyncableUI
   {
      //FrameworkElement UIElement { get; }
      //RotateTransform Rotation { get; }
      void SetUiSize(double x, double y, double w, double h);
      //void SetLocation(double x, double y);
      //void SetRotation(double r);
      //Canvas MainCanvas { get; }
   }

   //public interface IFrameworkUIForPlacement : IFrameworkUIForRotateUtil
   //{
   //   void AddUIChild(IFrameworkUIForRotateUtil u);
   //}
   //public class FrameworkUIForRotateUtil : IFrameworkUIForRotateUtil
   //{

   //   public FrameworkElement UIElement
   //   {
   //      get;
   //      set;
   //   }

   //   public RotateTransform Rotation
   //   {
   //      get;
   //      set;
   //   }


   //   public void SetUiSize(double w, double h)
   //   {
   //      throw new NotImplementedException();
   //   }      
   //}
}
