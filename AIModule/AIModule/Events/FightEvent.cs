using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIModule.Bases;
using AIModule.Fight;
using AIModule.Common.constant;
using AIModule.Report;
using AIModule.Common.constant.fleet;
using AIModule.Owners;
using AIModule.Common.constant.Base;

namespace AIModule.Events
{
    public class FightEvent : Event 
    {
       
        public int turn { get; set; }

        public List<string> defenseOwnerId { get; set; }
        public List<string> attackOwnerId { get; set; }
        public string initiatorOwnerId { get; set; }

        public HashSet<string> fleetIds { get; set; }

        [BsonElement("base")]
        public Base Base { get; set; }

        public string mainReportId { get; set; }
        public Dictionary<string, string> reportIds { get; set; }       //Un rapport par utilisateur impliqué
        public FightRound round { get; set; }

        public FightEvent(GlobalPosition position)
        {
            this.type = EventTypeConstant.FIGHT;

            this.round = new FightRound();

            this.location = position;

            this.fleetIds = new HashSet<string>();
            this.creation = DateTime.SpecifyKind(creation, DateTimeKind.Unspecified);
            this.turn = 0;
            //Changer ça pour réduire ou augmenter le temps entre 2 tours de combat.
            this.resolution = DateTime.SpecifyKind(creation.AddSeconds(10), DateTimeKind.Unspecified);

            this.attackOwnerId = new List<string>();
            this.defenseOwnerId = new List<string>();

            this.userIds = new HashSet<string>();

            this.description = "Combat";

            this.reportIds = new Dictionary<string, string>();
        }



    }
}
