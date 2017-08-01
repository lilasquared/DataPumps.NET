using System;
using Microsoft.Extensions.DependencyInjection;
using StructureMap;

namespace DataPumps
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();

            var container = new Container(config =>
            {
                config.Scan(scanner =>
                {
                    scanner.ConnectImplementationsToTypesClosing(typeof(IGenerator));
                });
            });

            Console.WriteLine("Hello World!");
        }
    }
}