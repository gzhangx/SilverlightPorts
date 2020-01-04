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
    public partial class ShipUI : UserControl
    {
        public ShipUI()
        {
            InitializeComponent();
        }

        public double PosX
        {
            get { return Canvas.GetLeft(this); }
            set { Canvas.SetLeft(this, value);}
        }
        public double PosY
        {
            get { return Canvas.GetTop(this); }
            set { Canvas.SetTop(this, value); }
        }
    }
}
