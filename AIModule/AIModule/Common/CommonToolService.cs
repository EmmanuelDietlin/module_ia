using AIModule.Common.constant.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Common
{
    public static class CommonToolService
    {
        public static double distance(GlobalPosition origin, GlobalPosition destination)
        {
            string[] originCoord = origin.coordinates.Split(";");
            string[] destCoord = destination.coordinates.Split(";");
            return 1000 * Math.Sqrt((int.Parse(destCoord[1]) - int.Parse(originCoord[1])) * (int.Parse(destCoord[1]) - int.Parse(originCoord[1]))
                    + (int.Parse(destCoord[0]) - int.Parse(originCoord[0])) * (int.Parse(destCoord[0]) - int.Parse(originCoord[0])));
        }

        //Celle là retourne une erreur si quelque chose manque
        public static Dictionary<string, int> checkMaps(Dictionary<string, int> needed, Dictionary<string, int> possessed)
        {
            Dictionary<string, int> missing = new Dictionary<string, int>();
            foreach (var n in needed)
            {
                if (!possessed.ContainsKey(n.Key))
                {
                    throw new Exception("L'un des prérequis requis n'existe pas");
                }
                if (possessed[n.Key] < n.Value)
                {
                    missing[n.Key] = n.Value - possessed[n.Key];
                }
            }
            /*
            needed.forEach((s, integer)-> {
                if (possessed.get(s) == null)
                {
                    throw new ResourceNotFoundException("L'un des prérequis requis n'existe pas", s);
                }
                if (possessed.get(s) < integer)
                {
                    missing.put(s, integer - possessed.get(s));
                }
            });*/
            return missing;
        }

        //Celle là ne retourne pas d'erreur, mais a la place
        //Compare 2 maps de type string, Integer,
        // vérifie que toutes les strings incluent dans "needed" sont bien présente dans "possessed", en quantité suffisante
        public static Dictionary<string, int> returnMissing(Dictionary<string, int> needed, Dictionary<string, int> possessed)
        {
            Dictionary<string, int> missing = new Dictionary<string, int>();
            foreach (var n in needed)
            {
                if (!possessed.ContainsKey(n.Key))
                {
                    missing[n.Key] = n.Value;
                }
                if (possessed[n.Key] < n.Value)
                {
                    missing[n.Key] = n.Value - possessed[n.Key];
                }
            }
            /*
            needed.forEach((s, integer)-> {
                if (possessed.get(s) == null)
                {
                    missing.put(s, integer);
                }
                else if (possessed.get(s) < integer)
                {
                    missing.put(s, integer - possessed.get(s));
                }
            });*/
            return missing;
        }
    }
}
