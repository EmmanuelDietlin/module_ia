using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Common.constant.fleet
{
    public class ShootStrategyConstant
    {
        public const string CLOSER_RANDOM = "CLOSER_RANDOM"; //Priorisant un groupe aléatoire
        public const string CLOSER_HP_MAX = "CLOSER_HP_MAX"; //Priorisant les groupes par HP (non cumulé) décroissant
        public const string CLOSER_HP_MIN = "CLOSER_HP_MIN"; //Priorisant les groupes par HP (non cumulé) croissant

        public List<string> getAll()
            {
                List<string> allValues = new List<string>();
                allValues.Add(CLOSER_RANDOM);
                return allValues;
            }

    }
}
