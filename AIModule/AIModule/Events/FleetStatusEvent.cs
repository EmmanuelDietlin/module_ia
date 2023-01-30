using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIModule.Common.constant;

namespace AIModule.Events
{
    public class FleetStatusEvent : Event
    {
        public string fleetId { get; set; }
        //Statut a la fin de l'event
        public string status { get; set; }

        public FleetStatusEvent() : base()
        {
            this.type = EventTypeConstant.FLEET_STATUS;
            this.creation = DateTime.Now;
            this.userIds = new HashSet<string>();
        }
        /*
         *public String launch(Fleet fleet, String status){
        FleetStatusEvent fleetStatusEvent = new FleetStatusEvent();
        fleetStatusEvent.setStatus(status);
        fleetStatusEvent.setResolution(LocalDateTime.now().plusSeconds(1+fleet.getWarmupTime()));
        fleetStatusEvent.setDescription("Passage de la flotte " + fleet.getName() + " au statut " + status);
        fleetStatusEvent.setFleetId(fleet.getId());
        fleetStatusEvent.getUserIds().add(fleet.getOwnerId());
        fleetStatusEvent.setLocation(fleet.getPosition());
        fleet.setStatus(FleetStatusConstant.TRANSITING);
        fleet.setTravelTime(fleetStatusEvent.getResolution());

        fleetDao.save(fleet);
        eventDao.save(fleetStatusEvent);
        eventRabbitService.sendEvent(fleetStatusEvent);
        return null;
    } 
         */
    }
}
