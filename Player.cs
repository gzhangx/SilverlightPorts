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
using System.ComponentModel;

namespace SilverlightPorts
{
   public class Player : INotifyPropertyChanged
   {      
      public const string PROPNAME_CurrentSendToPort = "CurrentSendToPort";
      public Action<PortInfo> OnSendToPortChanged;
      private PortInfo _sendToPort;
      public PortInfo CurrentSendToPort { get { return _sendToPort; }
         set { _sendToPort = value; 
            OnPropertyChanged(PROPNAME_CurrentSendToPort);
            Action<PortInfo> hand = OnSendToPortChanged;
            if (hand != null)
            {
               hand(value);
            }
         }
      }
      public bool InSendToMode { get; set; }
      public PortInfo CurentSelectedPort { get; set; }
      public static Player ActivePlayer { get; private set; }
      public DomainInfo Domain { get; private set; }
      public Player(DomainInfo dom)
      {
         //if (ActivePlayer != null) throw new Exception("bbdplyr");
         ActivePlayer = this;
         Domain = dom;
         Domain.player = this;
      }

      #region INotifyPropertyChanged Members

      public event PropertyChangedEventHandler PropertyChanged;
      public void OnPropertyChanged(string name)
      {
         PropertyChangedEventHandler hand = PropertyChanged;
         if (hand != null)
         {
            hand(this, new PropertyChangedEventArgs(name));
         }
      }

      #endregion
   }
}
