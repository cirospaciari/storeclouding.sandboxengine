using StoreClouding.SandBoxEngine.Terrain.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            
            string error;
            Console.WriteLine("Iniciando servidor");
            
            if (!GameApplication.Current.Start(out error))
                Console.WriteLine(error);
            
            Console.WriteLine("Iniciando cliente");

            if (!Client.GameApplication.Current.Start(out error))
                Console.WriteLine(error);

            Console.WriteLine("Cliente iniciado");

            string command = null;
            do
            {
                command = Console.ReadLine();
                if (command == "generate")
                {
                    Console.WriteLine("Generating map...");
                    var generator = new TerrainGenerator(GameApplication.Current.Terrain);
                    generator.GenerateWorldMap();
                    Console.WriteLine("Generated ended");
                }
            } while (command != "exit");

            if (!GameApplication.Current.Stop(out error))
                Console.WriteLine(error);
            if (!Client.GameApplication.Current.Stop(out error))
                Console.WriteLine(error);
            Console.WriteLine("Fim!");
            Console.Read();
        }
    }
}
