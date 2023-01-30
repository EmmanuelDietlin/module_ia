using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIModule.Common.constant;
using AIModule.Common.constant.Base;

namespace AIModule.Events
{
    public class FleetMoveEvent : Event
    {
        public GlobalPosition origine { get; set; }

        //Objectif de la flotte, participe à la détermination de l'event de résolution
        public string objectif { get; set; }

        public string fleetId { get; set; }
        public FleetMoveEvent()
        {
            this.type = EventTypeConstant.MOVE;
            this.creation = DateTime.Now;
            this.userIds = new HashSet<string>();
            this._class = "fr.need_a_name.bdd_module.event.entity.FleetMoveEvent";
        }

        
    }
}
