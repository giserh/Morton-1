using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortonGeo
{
    class Program
    {
        static void Main(string[] args)
        {

            var geologic = new GeoLogic();
            geologic.GenerateTestData();
            Console.WriteLine("--------finished-------");
            Console.ReadLine();
        }
    }
}
