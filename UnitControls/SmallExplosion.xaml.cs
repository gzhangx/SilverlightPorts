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
using System.Windows.Threading;

namespace SilverlightPorts.UnitControls
{
    public partial class SmallExplosion : UserControl
    {
        private Canvas _parent;
        private Point _pos;
        private int count;
        public SmallExplosion(Canvas parent, Point pos)
        {
            _parent = parent;
            _pos = pos;
            InitializeComponent();
            _parent.Children.Add(this);
            Canvas.SetLeft(this, pos.X - 16);
            Canvas.SetTop(this, pos.Y - 16);
            tmr.Interval = new TimeSpan(0, 0, 0, 0, 100);
            tmr.Tick += new EventHandler(tmr_Tick);
            tmr.Start();
        }

        DispatcherTimer tmr = new DispatcherTimer();

        void tmr_Tick(object sender, EventArgs e)
        {
            count++;
            if (count < 10){
                yellow.Width++;
                yellow.Height++;
                red.Width += 2;
                red.Height += 2;
            }else
            if (count < 15)
            {
                red.Width ++;
                red.Height ++;
            }
            else if (count < 30)
            {
                this.Opacity -= 0.1;
            }
            else
            {
                tmr.Stop();
                _parent.Children.Remove(this);
            }
        }
    }
}
