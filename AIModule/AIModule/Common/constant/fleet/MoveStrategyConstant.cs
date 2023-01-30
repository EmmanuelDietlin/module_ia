using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Common.constant.fleet
{
    public class MoveStrategyConstant
    {
        public const string RUSH = "RUSH"; //flotte avance jusqu'à la position 200
        public const string ESCAPE = "ESCAPE"; //La flotte fuit le combat (elle se dirige vers la sortie de combat la plus proche)
        public const string CHASE = "CHASE"; //flotte avance jusqu’à ce que sa position + sa portée faible soit supérieur ou égale à la position de l’escadron le plus éloigné
        public const string DEFAULT = "DEFAULT"; //La flotte avance jusqu’à ce que sa position + sa portée faible soit supérieur ou égale à la position de l’escadron le moins éloigné

        public List<string> getAll()
        {
                List<string> allValues = new List<string>();
                allValues.Add(DEFAULT);
                allValues.Add(ESCAPE);
                allValues.Add(CHASE);
                allValues.Add(RUSH);
                return allValues;
        }

    }
}
