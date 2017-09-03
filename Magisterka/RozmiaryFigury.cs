using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace main
{
    public class RozmiaryFigury
    {
       public int dlugosc { get; set; }
       public int szerokosc { get; set; }
        public int dluzszyBok { get; set; }
        public  RozmiaryFigury(int a, int b)
        {
            dlugosc = a;
            szerokosc = b;
            if (dlugosc > szerokosc)
                dluzszyBok = dlugosc;
            else
            { dluzszyBok = szerokosc; }
            try
            {
                Monitorowanie.liczniki[dluzszyBok] = Monitorowanie.liczniki[dluzszyBok] + 1;
            }
            catch(Exception)
            { }
          
            

        }
       
    }
}
