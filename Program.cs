using System;

namespace GalaxyDB
{
    class Program
    {
        static void Main(string[] _)
        {
            DB backend = new DB();
            REPL frontend = new REPL(backend);
            frontend.Run();
        }
    }
}
