using System;
using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;

namespace GalaxyDB
{
    class CelestialBody
    {
        protected CelestialBody(string name)
        {
            Name = name;
        }
        public string Name
        {
            get;
        }
    }
    class Galaxy : CelestialBody
    {
        public Galaxy(string name, GalaxyKind kind, float age, AgeUnits units) : base(name)
        {
            Kind = kind;
            Age = age;
            Units = units;
        }

        public enum GalaxyKind
        {
            elliptical, lenticular, spiral, irregular
        }

        public enum AgeUnits
        {
            million, billion
        }

        public GalaxyKind Kind
        {
            get;
        }
        public float Age
        {
            get;
        }

        public AgeUnits Units
        {
            get;
        }
    }
    class Star : CelestialBody
    {
        /* Star classification table 
         * --------------------------------------------------------------
         * Class   Temperature    Luminosity     Mass           Radius
         * 
         * O       ≥ 30,000       ≥ 30000        ≥ 16           ≥ 6.6
         * B       10000–30000    25–30000       2.1–16         1.8–6.6
         * A       7500–10000     5–25           1.4–2.1        1.4–1.8
         * F       6000–7500      1.5–5          1.04–1.4       1.15–1.4
         * G       5200–6000      0.6–1.5        0.8–1.04       0.96–1.15
         * K       3700–5200      0.08–0.6       0.45–0.8       0.7–0.96
         * M       2400–3700      ≤ 0.08         0.08–0.45      ≤ 0.7
         */

        public enum StarClass
        {
            O, B, A, F, G, K, M, invalid
        };

        public static StarClass Classify(float mass, float diameter, int temperature, float luminosity)
        {
            if (temperature < 2400 || mass < 0.08) {
                return StarClass.invalid;
            } else if (temperature < 3700)
            {
                if (luminosity > 0.08 || mass > 0.45 || diameter / 2  > 0.7)
                {
                    return StarClass.invalid;
                }

                return StarClass.M;
            } else if (temperature < 5200) {
                if (luminosity > 0.6 || mass > 0.8 || diameter / 2 > 0.96)
                {
                    return StarClass.invalid;
                }

                return StarClass.K;
            }
            else if (temperature < 6000) {
                if (luminosity > 1.5 || mass > 1.04 || diameter / 2 > 1.15)
                {
                    return StarClass.invalid;
                }

                return StarClass.G;
            }
            else if (temperature < 7500) {
                if (luminosity > 5 || mass > 1.4 || diameter / 2 > 1.4)
                {
                    return StarClass.invalid;
                }

                return StarClass.F;
            }
            else if (temperature < 10000) {
                if (luminosity > 25 || mass > 2.1 || diameter / 2 > 1.8)
                {
                    return StarClass.invalid;
                }

                return StarClass.A;
            }
            else if (temperature < 30000) {
                if (luminosity > 30000 || mass > 16 || diameter / 2 > 6.6)
                {
                    return StarClass.invalid;
                }

                return StarClass.B;
            }
            else {
                return StarClass.O;
            }
        }

        public Star(string name, float mass, float diameter, int temperature, float luminosity, StarClass starClass) : base(name)
        {
            Mass = mass;
            Diameter = diameter;
            Temperature = temperature;
            Luminosity = luminosity;
            Class = starClass;
        }


        public float Mass
        {
            get;
        }
        public float Diameter
        {
            get;
        }

        public int Temperature
        {
            get;
        }

        public float Luminosity
        {
            get;
        }

        public StarClass Class
        {
            get;
        }
    }

    class Planet : CelestialBody
    {
        public enum PlanetKind
        {
            terrestrial,
            giant_planet,
            ice_giant, 
            mesoplanet,
            mini_neptune,
            planetar,
            super_earth,
            super_jupiter,
            sub_earth,
            invalid
        };

        public static Planet.PlanetKind StrToKind(string str)
        {
            return str switch
            {
                "terrestrial" => Planet.PlanetKind.terrestrial,
                "giant planet" => Planet.PlanetKind.giant_planet,
                "ice giant" => Planet.PlanetKind.ice_giant,
                "mesoplanet" => Planet.PlanetKind.mesoplanet,
                "mini-neptune" => Planet.PlanetKind.mini_neptune,
                "planetar" => Planet.PlanetKind.planetar,
                "super-earth" => Planet.PlanetKind.super_earth,
                "super-jupiter" => Planet.PlanetKind.super_jupiter,
                "sub-earth" => Planet.PlanetKind.sub_earth,
                _ => Planet.PlanetKind.invalid
            };
        }

        public Planet(string name, PlanetKind kind, bool supportsLife) : base(name) 
        {
            Kind = kind;
            SupportsLife = supportsLife;
        }

        public PlanetKind Kind
        {
            get;
        }

        public string KindAsStr
        {
            get => Kind switch
            {
                PlanetKind.terrestrial => "terrestrial",
                PlanetKind.giant_planet => "giant planet",
                PlanetKind.ice_giant => "ice giant",
                PlanetKind.mesoplanet => "mesoplanet",
                PlanetKind.mini_neptune => "mini-neptune",
                PlanetKind.planetar => "planetar",
                PlanetKind.super_earth => "super-earth",
                PlanetKind.super_jupiter => "super-jupiter",
                PlanetKind.sub_earth => "sub-earth",
                _ => "unknown"
            };
        }

        public bool SupportsLife
        {
            get;
        }
    }

    class Moon : CelestialBody
    {
        public Moon(string name) : base(name) { }

    }
    
    class DB
    {
        public DB()
        {
            globalIndex = new Dictionary<Type, SortedDictionary<string, CelestialBody>>();
            childrenIndex = new Dictionary<CelestialBody, List<CelestialBody>>();
        }

        public void Add(CelestialBody body, CelestialBody parent)
        {
            Type bodyType = body.GetType();

            var index = GetIndex(bodyType);
            
            if (!index.ContainsKey(body.Name))
            {
                index[body.Name] = body;

                if (parent != null)
                {
                    if (!childrenIndex.ContainsKey(parent))
                    {
                        childrenIndex[parent] = new List<CelestialBody>();
                    }
                    childrenIndex[parent].Add(body);
                }
            }
            else
            {
                // TODO: Throw and let the frontend communicate the error
                Console.WriteLine("{0} {1} already exists.", bodyType.Name, body.Name);
            }
        }

        public IReadOnlyCollection<string> List(Type bodyType)
        {
            Debug.WriteLine("List() called with " + bodyType.Name);
            return GetIndex(bodyType).Keys;
        }

        public CelestialBody Find(Type bodyType, string name)
        {
            return GetIndex(bodyType).GetValueOrDefault(name);
        }

        public IReadOnlyCollection<CelestialBody> GetChildren(CelestialBody parent)
        {
            return childrenIndex.GetValueOrDefault(parent);
        }

        private SortedDictionary<string, CelestialBody> GetIndex(Type bodyType)
        {
            if (!globalIndex.ContainsKey(bodyType))
            {
                globalIndex[bodyType] = new SortedDictionary<string, CelestialBody>();
            }
            return globalIndex[bodyType];
        }

        private readonly Dictionary<Type, SortedDictionary<string, CelestialBody>> globalIndex;
        private readonly Dictionary<CelestialBody, List<CelestialBody>> childrenIndex;
    }
}
