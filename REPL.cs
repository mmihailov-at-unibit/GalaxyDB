using System;
using System.Text.RegularExpressions;

namespace GalaxyDB
{
    interface ICommand
    {
        bool Execute(DB backend);
    }

    class NOP : ICommand
    {
        public bool Execute(DB backend)
        {
            return true;
        }
    }

    class PrintError : ICommand
    {
        public PrintError(string error)
        {
            this.error = error;
        }

        public bool Execute(DB backend)
        {
            Console.WriteLine(error);
            return true;
        }

        private readonly string error;
    }

    class Add : ICommand
    {
        public Add(CelestialBody body, CelestialBody parent)
        {
            this.body = body;
            this.parent = parent;
        }

        public bool Execute(DB backend)
        {
            backend.Add(body, parent);
            return true;
        }

        private readonly CelestialBody body;
        private readonly CelestialBody parent;
    }

    class List : ICommand
    {
        public List(Type bodyType, string bodyTypeStr)
        {
            this.bodyType = bodyType;
            this.bodyTypeStr = bodyTypeStr;
        }

        public bool Execute(DB backend)
        {
            var names = backend.List(bodyType);

            Console.Write("--- List of all researched " + bodyTypeStr + " ---");
            string sep = "\n";

            foreach (string name in names)
            {
                Console.Write(sep);
                Console.Write(name);
                sep = ", ";
            }

            Console.WriteLine("\n--- End of " + bodyTypeStr + " list ---");
            return true;
        }

        private readonly Type bodyType;
        private readonly string bodyTypeStr;
    }

    class Stats : ICommand
    {
        public bool Execute(DB backend)
        {
            Console.WriteLine("--- Stats ---");
            Console.WriteLine("Galaxies: {0}", backend.List(typeof(Galaxy)).Count);
            Console.WriteLine("Stars: {0}", backend.List(typeof(Star)).Count);
            Console.WriteLine("Planets: {0}", backend.List(typeof(Planet)).Count);
            Console.WriteLine("Moons: {0}", backend.List(typeof(Moon)).Count);
            Console.WriteLine("--- End of stats ---");

            return true;
        }
    }

    class Print : ICommand
    {
        public Print(Galaxy galaxy) => this.galaxy = galaxy;

        public bool Execute(DB backend)
        {
            Console.WriteLine("--- Data for " + galaxy.Name + " galaxy ---");
            Console.WriteLine("Type: {0}", galaxy.Kind);
            Console.WriteLine("Age: {0}{1}", galaxy.Age,
                                             galaxy.Units switch { Galaxy.AgeUnits.million => 'M',
                                                                   Galaxy.AgeUnits.billion => 'B',
                                                                   _ => '?' } );
            PrintStars(backend, galaxy, "");
            Console.WriteLine("--- End of data for " + galaxy.Name + " galaxy ---");

            return true;
        }

        private static void PrintStars(DB backend, Galaxy galaxy, string indent)
        {
            Console.WriteLine(indent + "Stars:");

            var children = backend.GetChildren(galaxy);
            if (children != null)
            { 
                foreach (CelestialBody child in children)
                {
                    Star star = (Star) child;
                    if (star != null)
                    {
                        Console.WriteLine("{0}- Name: {1}", indent, star.Name);
                        Console.WriteLine("{0}  Class: {1} ({2}, {3}, {4}, {5})", 
                                          indent, star.Class, star.Mass, star.Diameter / 2, star.Temperature, star.Luminosity);
                        PrintPlanets(backend, star, indent + "  ");
                    }
                }
            }
        }
        private static void PrintPlanets(DB backend, Star star, string indent)
        {
            Console.WriteLine(indent + "Planets:");

            var children = backend.GetChildren(star);
            if (children != null)
            {
                foreach (CelestialBody child in children)
                {
                    Planet planet = (Planet) child;
                    if (planet != null)
                    {
                        Console.WriteLine("{0}- Name: {1}", indent, planet.Name);
                        Console.WriteLine("{0}  Type: {1}", indent, planet.KindAsStr);
                        Console.WriteLine("{0}  Supports life: {1}", indent, planet.SupportsLife ? "yes" : "no");
                        PrintMoons(backend, planet, indent + "  ");
                    }
                }
            }
        }

        private static void PrintMoons(DB backend, Planet planet, string indent)
        {
            Console.WriteLine(indent + "Moons:");

            var children = backend.GetChildren(planet);
            if (children != null)
            { 
                foreach (CelestialBody childsChildChild in backend.GetChildren(planet))
                {
                    Moon moon = (Moon)childsChildChild;
                    if (moon != null)
                    {
                        Console.WriteLine("{0}- {1}", indent, moon.Name);
                    }
                }
            }
        }

        private readonly Galaxy galaxy;
    }

    class Exit : ICommand
    {
        public bool Execute(DB backend)
        {
            return false;
        }
    }

    class REPL
    {
        public REPL(DB backend) => this.backend = backend;
        // The main REPL loop
        public void Run()
        {
            while (ParseCommand(Console.ReadLine()).Execute(backend));
        }

        // Utility finctions for the parser that consume "tokens" from the string
        private static readonly Regex reWord = new Regex(@"^\s*(?<Word>\S+)\s*(?<Rest> .*)?$", RegexOptions.Compiled);
        private static bool ConsumeWord(ref string inputBuffer, out string word)
        {
            Match match = reWord.Match(inputBuffer);

            if (match.Success)
            {
                word = match.Groups["Word"].Value;
                inputBuffer = match.Groups["Rest"].Value;
                return true;
            }
            else
            {
                word = "";
                return false;
            }
        }

        private static readonly Regex reName = new Regex(@"^\s*\[(?<Name>[^\]]+)\]\s*(?<Rest> .*)?$", RegexOptions.Compiled);
        private static bool ConsumeName(ref string inputBuffer, out string name)
        {
            Match match = reName.Match(inputBuffer);

            if (match.Success)
            {
                name = match.Groups["Name"].Value;
                inputBuffer = match.Groups["Rest"].Value;
                return true;
            }
            else
            {
                name = "";
                return false;
            }
        }

        private static bool ConsumeNumber<T>(ref string inputBuffer, out T number)
        {
            Match match = reWord.Match(inputBuffer);

            if (match.Success)
            {
                try
                {
                    number = (T) Convert.ChangeType(match.Groups["Word"].Value, typeof(T));
                    inputBuffer = match.Groups["Rest"].Value;
                    return true;
                }
                catch
                {
                }
            }

            number = default;
            return false;
        }

        // The parser entry point
        private ICommand ParseCommand(string input)
        {
            ICommand cmd = ConsumeWord(ref input, out string commandName) ?
                commandName switch
                {
                    "add" => ParseAdd(ref input),
                    "list" => ParseList(ref input),
                    "stats" => new Stats(),
                    "print" => ParsePrint(ref input),
                    "exit" => new Exit(),
                    _ => new PrintError("Unknown command: " + commandName),
                }
            : // No word consumed means the line contains only whitespace
                new NOP();

            // This is quite hacky, just like most of the ad-hock parsing here
            return (input.Length == 0 || cmd is PrintError)
                ? cmd
                : new PrintError("Extra input after command: " + input);
        }

        private ICommand ParseAdd(ref string input)
        {
            return ConsumeWord(ref input, out string bodyType) ?
                bodyType switch
                {
                    "galaxy" => ParseAddGalaxy(ref input),
                    "star" => ParseAddStar(ref input),
                    "planet" => ParseAddPlanet(ref input),
                    "moon" => ParseAddMoon(ref input),
                    _ => new PrintError("Unknown celestial body type: " + bodyType),
                }
            : // No word consumed
                new PrintError("Celestial body type expected after \"add\".");
        }

        private ICommand ParseAddGalaxy(ref string input)
        {
            // Syntax: add galaxy [<name>] (elliptical|lenticular|spiral|irregular) <float age>(M|B)
            string name;
            if (!ConsumeName(ref input, out name))
            {
                return new PrintError("Galaxy name (enclosed in square brackets) expected after \"add galaxy\".");
            }

            string galaxyKindStr;
            if (!ConsumeWord(ref input, out galaxyKindStr))
            {
                return new PrintError("Galaxy type expected after the galaxy name.");
            }

            Galaxy.GalaxyKind galaxyKind;
            try
            {
                galaxyKind = (Galaxy.GalaxyKind)Enum.Parse(typeof(Galaxy.GalaxyKind), galaxyKindStr);
            }
            catch
            {
                return new PrintError("Invalid galaxy type: " + galaxyKindStr);
            };

            string ageStr;

            if (!ConsumeWord(ref input, out ageStr))
            {
                return new PrintError("Galaxy age expected after galaxy type.");
            }

            Galaxy.AgeUnits units;

            switch (ageStr[^1])
            {
                case 'M':
                    units = Galaxy.AgeUnits.million;
                    break;
                case 'B':
                    units = Galaxy.AgeUnits.billion;
                    break;
                default:
                    return new PrintError("Galaxy age must end in 'M' or 'B' to denote units.");
            }

            float age;
            try
            {
                age = float.Parse(ageStr[0..^1]);
            } catch 
            {
                return new PrintError("Galaxy age must be a float number followed by 'M' or 'B'.");
            }

            if (age == 0)
            {
                return new PrintError("Galaxy age cannot be negative");
            }

            return new Add(new Galaxy(name, galaxyKind, age, units), null);
        }

        // Parsing the parameters to the "add star" command.
        private ICommand ParseAddStar(ref string input)
        {
            // Syntax: add star [<galaxy name>] [<star name>] <float mass> <float diameter > <uint temp> <float luminosity>
            string galaxyName;
            if (!ConsumeName(ref input, out galaxyName))
            {
                return new PrintError("Galaxy name and star name (each enclosed in square brackets) expected after \"add star\".");
            }

            string name;
            if (!ConsumeName(ref input, out name))
            {
                return new PrintError("Star name (enclosed in square brackets) expected after galaxy name.");
            }

            float mass;
            if (!ConsumeNumber<float>(ref input, out mass))
            {
                return new PrintError("Star mass (a float) expected after star name.");
            }

            float diameter;
            if (!ConsumeNumber<float>(ref input, out diameter))
            {
                return new PrintError("Star diameter (a float) expected after star mass.");
            }

            if (diameter < 0)
            {
               return new PrintError("Star diamter cannot be negative.");
            }

            int temperature;
            if (!ConsumeNumber<int>(ref input, out temperature))
            {
                return new PrintError("Star temperature (an integer) expected after star diameter.");
            }

            if (temperature < 0)
            {
                return new PrintError("Star temperature in Kelvin cannot be negative.");
            }

            float luminosity;
            if (!ConsumeNumber<float>(ref input, out luminosity))
            {
                return new PrintError("Star luminosity (a float) expected after star diameter.");
            }

            CelestialBody galaxy = backend.Find(typeof(Galaxy), galaxyName);
            if (galaxy == null)
            {
                return new PrintError("Unknown galaxy: " + galaxyName);
            }

            Star.StarClass starClass = Star.Classify(mass, diameter, temperature, luminosity);
            if (starClass == Star.StarClass.invalid)
            {
                return new PrintError("Invalid combination of star characteristics.");
            }

            return new Add(new Star(name, mass, diameter, temperature, luminosity, starClass), galaxy);
        }

        private ICommand ParseAddPlanet(ref string input)
        {
            // Syntax: add planet [<star name>] [<planet name>] <planet type> <support life>
            string starName;
            if (!ConsumeName(ref input, out starName))
            {
                return new PrintError("Star name and planet name (each enclosed in square brackets) expected after \"add planet\".");
            }

            string name;
            if (!ConsumeName(ref input, out name))
            {
                return new PrintError("Planet name (enclosed in square brackets) expected after star name.");
            }

            string planetTypeStr;
            if (!ConsumeWord(ref input, out planetTypeStr))
            {
                return new PrintError("Planet type expected after planet name.");
            }

            if (planetTypeStr == "giant" || planetTypeStr == "ice")
            {
                if (ConsumeWord(ref input, out string secondWord))
                {
                    planetTypeStr += " " + secondWord;
                }
            }

            Planet.PlanetKind type = Planet.StrToKind(planetTypeStr);

            if (type == Planet.PlanetKind.invalid)
            {
                return new PrintError("Invalid planet type: " + planetTypeStr);
            }

            string supportsLifeStr;
            if (!ConsumeWord(ref input, out supportsLifeStr) || (supportsLifeStr != "yes" && supportsLifeStr != "no")) 
            {
                return new PrintError("Expected \"yes\" or \"no\" after planet type.");
            }

            bool supportsLife = (supportsLifeStr == "yes");

            CelestialBody star = backend.Find(typeof(Star), starName);
            if (star == null)
            {
                return new PrintError("Unknown star: " + starName);
            }

            return new Add(new Planet(name, type, supportsLife), star);
        }

        private ICommand ParseAddMoon(ref string input)
        {
            // Syntax: add moon [<planetname>] [<moon name>]
            string planetName;
            if (!ConsumeName(ref input, out planetName))
            {
                return new PrintError("Planet name and moon name (each enclosed in square brackets) expected after \"add moon\".");
            }

            string name;
            if (!ConsumeName(ref input, out name))
            {
                return new PrintError("Moon name (enclosed in square brackets) expected after planet name.");
            }

            CelestialBody planet = backend.Find(typeof(Planet), planetName);
            if (planet == null)
            {
                return new PrintError("Unknown planet: " + planetName);
            }

            return new Add(new Moon(name), planet);
        }
        private ICommand ParseList(ref string input)
        {
            string bodyTypeStr;

            if (!ConsumeWord(ref input, out bodyTypeStr))
            {
                return new PrintError("Celestial body type (in plural) expected after \"list\".");
            }

            Type bodyType = bodyTypeStr switch
            {
                "galaxies" => typeof(Galaxy),
                "stars" => typeof(Star),
                "planets" => typeof(Planet),
                "moons" => typeof(Moon),
                _ => null,
            };

            if (bodyType == null)
            {
                return new PrintError("Unknown celestial body type: " + bodyTypeStr);
            }

            return new List(bodyType, bodyTypeStr);
        }

        private ICommand ParsePrint(ref string input)
        {
            string name;
            if (!ConsumeName(ref input, out name))
            {
                return new PrintError("Galaxy name expected after \"print\".");
            }

            Galaxy galaxy = (Galaxy) backend.Find(typeof(Galaxy), name);
            if (galaxy == null)
            {
                return new PrintError("Unknown galaxy: " + name);
            }

            return new Print(galaxy);
        }

        private readonly DB backend;
    }
}
