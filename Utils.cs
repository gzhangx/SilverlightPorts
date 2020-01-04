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
using System.Windows.Data;

namespace SilverlightPorts
{
   public class MapCreationParams
   {
      public string PlayerName { get; set; }
      public int PlanetCount { get; set; }
      public int MapSize { get; set; }
      public int Opponents { get; set; }
      public int MapSeed { get; set; }
   }
   public class Utils
   {
      public static List<string> PortNames()
      {
         List<string> names = new List<string>();
         string[] nms = new string[]{
            "Earth", "Moon","Mars","Phobos","Deimos","Jupiter","Metis","Adrastea","Amalthea","Thebe",
"Io","Europa","Ganymede","Callisto","Leda","Himalia","Lysithia","Elara","Ananke"
,"Carme","Pasipha?","Sinope","Saturn","Pan","Atlas","Prometheus","Pandora","Janus","Epimetheus","Mimas","Enceladus","Tethys","Telesto","Calypso","Dione","Helene","Rhea","Titan","Hyperion","Iapetus","Phoebe","Uranus","Cordelia","Ophelia","Bianca","Cressida","Desdemona","Juliet","Portia","Rosalind","Belinda","Puck","Miranda","Ariel","Umbriel","Titania","Oberon","Caliban","Sycorax","Neptune","Naiad",
"Thalassa","Despina","Galatea","Larissa","Proteus","Triton","Nereid","Pluto","Charon"
         };
         foreach (string name in nms)
         {
            names.Add(name);
         }
         return names;
      }
      public static List<PortInfo> MakeMap(MapCreationParams creationParams)
      {
         Random rnd = new Random(creationParams.MapSeed);
         List<PortInfo> pInfs = new List<PortInfo>();
         List<string> names = PortNames();
         int unnamedCnt = 1;
         for (int i = 0; i < creationParams.PlanetCount; i++)
         {
            string name = "test";
            if (i < names.Count)
            {
               name = names[i];
            }
            else
            {
               name = "Unnamed " + (unnamedCnt++);
            }
            int xx = rnd.Next(creationParams.MapSize)+64;
            int yy = rnd.Next(creationParams.MapSize)+64;
            int size = rnd.Next(Consts.MaxPortSize);
            while (size < 3)
            {
               size = rnd.Next(Consts.MaxPortSize);
            }
            long pop = 0;
            if (i < 2)
            {
               pop = 5000000000;
               size = 6;
            }
            PortInfo pinf = new PortInfo(name, xx, yy, size, pop);           
            bool ok = true;
            for (int j = 0; j < 100; j++)
            {
               foreach (PortInfo close in pInfs)
               {
                  if (Math.Abs(close.PosX - pinf.PosX) < 64
                     && Math.Abs(close.PosY - pinf.PosY) < 64)
                  {
                     ok = false;
                     xx = rnd.Next(creationParams.MapSize)+64;
                     yy = rnd.Next(creationParams.MapSize)+64;
                     //pinf = new PortInfo(name, xx, yy, size, pop);
                     pinf.MovePos(xx, yy);
                     break;
                  }
               }
               if (ok) break;
            }
            //if (ok)
            pInfs.Add(pinf);
         }

         for (int i = 0; i < pInfs.Count; i++)
         {
            PortInfo from = pInfs[i];
            for (int j = i + 1; j < pInfs.Count; j++)
            {
               PortInfo to = pInfs[j];
               PortDistInfo toinf = new PortDistInfo(to, to.DistOf(from));
               InsertDistInOrder(from, toinf);
               PortDistInfo fromInf = new PortDistInfo(from, toinf.PortDist);
               InsertDistInOrder(to, fromInf);
            }
         }
         return pInfs;
      }

      private static void InsertDistInOrder(PortInfo p, PortDistInfo inf)
      {
         int i = 0;
         for (; i < p.PortDists.Count; i++)
         {
            if (p.PortDists[i].PortDist > inf.PortDist)
            {
               break;
            }
         }
         p.PortDists.Insert(i, inf);
      }
   }

   public class GenValueConverter : IValueConverter
   {

      #region IValueConverter Members

      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         if (parameter != null)
         {
            string formatStr = parameter.ToString();
            if (!string.IsNullOrEmpty(formatStr))
            {
               return string.Format(formatStr, value);
            }
         }
         return value.ToString();
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotImplementedException();
      }

      #endregion
   }
}
