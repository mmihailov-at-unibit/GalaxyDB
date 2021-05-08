using System;

namespace GalaxyDB
{
    class Program
    {
        static void Main(string[] _)
        {
            (new REPL(new DB())).Run();
        }
    }
}
