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
   public class DomainMarks
   {
      private static string[] Marks;
      static DomainMarks()
      {
         int total = 26;
         Marks = new string[total];
         for (int i = 0; i < total; i++)
         {
            Marks[i] = ((char)('A' + i)).ToString();
         }
      }
      public static DomainMarkUI GetDomainMark(DomainInfo dom)
      {
         return new DomainMarkUI(Marks[dom.DomainID % Marks.Length], dom.DomainID);
      }
   }
}
