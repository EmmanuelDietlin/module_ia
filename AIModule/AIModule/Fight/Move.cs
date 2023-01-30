using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Fight
{
    public class Move
    {
        public int fleetNewPos { get; set; }
        public Boolean joinFight { get; set; }
        public Dictionary<string, int> newSquadPos { get; set; }

        public Move() { }   
}
}
