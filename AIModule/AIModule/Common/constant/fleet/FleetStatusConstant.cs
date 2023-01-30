using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Common.constant.fleet
{
    public class FleetStatusConstant
    {
        public const string TRANSITING = "TRANSITING";
        //STANDING STATE
        public const string PATROLLING = "PATROLLING";       //En patrouille, prete au combat/décplaement
        public const string STANDBY = "STANDBY";             //En standby, prete a décoller
        public const string INACTIVE = "INACTIVE";           //Dans les hangar, nécessite une préparation pour le décollage
        //ACTION STATE
        public const string MOVING = "MOVING";
        public const string FIGHTING = "FIGHTING";
        public const string COMMERCING = "COMMERCING";
        public const string SUPPORTING = "SUPPORTING";
        public const string EMBUSH = "EMBUSH";
        //LOCKED STATE
        public const string OUT_OF_GAS = "OUT_OF_GAS";       //La flotte n'a plus d'essence, elle doit être réaprovisionnée pour pouvoir décoller

    }
}
