using AIModule.Bases;
using AIModule.Common.constant.fleet;
using AIModule.Common.constant;
using AIModule.Events;
using AIModule.Fight;
using AIModule.Owners;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIModule.Ressource;
using AIModule.Common.constant.owner;
using AIModule.Common.constant.Base;

namespace AIModule.Common
{
    /// <summary>
    /// Gestionnaire de déplacements pour la création de FleetMoveEvent pour l'IA.
    /// Le code est repris en partie de celui des autres modules
    /// </summary>
    public sealed class MoveManager
    {
        private static MoveManager _instance;

        public FleetMoveEvent launchMove(Fleet fleet, GlobalPosition destination, GlobalPosition origin, FleetObjectives objectif, string description, int timeShift)
        {
            if (destination.Equals(origin))
            {
                //log.debug("Can't have the same origin and destination " + destination.coordinates);
                //return "Can't have the same origin and destination " + destination.coordinates;
            }

            //Calcul du temps de trajet
            double distance = CommonToolService.distance(origin, destination);
            if (fleet.speed == 0)
            {
                //log.debug("This flotte has a speed of 0, so it can't move " + fleet.getId());
                //return "This flotte has a speed of 0, so it can't move " + fleet.id;
            }
            long travelTime = timeShift + (long)Math.Round(distance / fleet.speed);


            //Création du déplacement
            FleetMoveEvent moveEvent = new FleetMoveEvent();
            moveEvent.location = destination;
            moveEvent.objectif = objectif.ToString();
            moveEvent.origine = origin;
            moveEvent.type = EventTypeConstant.MOVE;
            moveEvent.fleetId = fleet.id;
            moveEvent.creation = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            moveEvent.userIds = new HashSet<string> { fleet.ownerId };
            moveEvent.description = description;

            if (objectif == FleetObjectives.ATTACK)
            {
                moveEvent.isHostile = true;
            }

            //Récupération de la base cible (si il y a une base cible)
            var baseFilter = Builders<Base>.Filter.Eq(x => x.position, destination);
            var tmp = MongoDBSingleton.Instance().basesCollection.Find(baseFilter).CountDocuments();
            if (MongoDBSingleton.Instance().basesCollection.Find(baseFilter).CountDocuments() > 0)
            {       //Si on a trouvé une base a la destination, alors on rajoute l'user correspondant dans la liste
                var baseCible = MongoDBSingleton.Instance().basesCollection.Find(baseFilter).First(); //Récupérer la base en BDD !
                moveEvent.userIds.Add(baseCible.ownerId);
                //Si la base cible est une base d'alliance, on prévient tout le monde
                if (baseCible.type == BaseTypeConstant.CAPTURABLE)
                {
                    var ownerFilter = Builders<User>.Filter.Eq(x => x.id, baseCible.ownerId);
                    Owner? baseOwner = MongoDBSingleton.Instance().usersCollection.Find(ownerFilter).First();
                    if (baseOwner == null) throw new InvalidBodyException(string.Format("TARGET OWNER {0} : La base cible n'a pas d'owner valide", baseCible.ownerId));

                    if (baseOwner.type == OwnerTypeConstant.PLAYER)
                    {
                        User user = (User)baseOwner;
                        if (user.allianceId != null)
                        {
                            var allianceFilter = Builders<Alliance>.Filter.Eq(x => x.id, user.allianceId);
                            Alliance? alliance = MongoDBSingleton.Instance().alliancesCollection.Find(allianceFilter).First();
                            if (alliance == null) throw new ResourceNotFoundException(string.Format("TARGET_ALLIANCE {0} was not found", user.allianceId));

                            moveEvent.userIds = (HashSet<string>)moveEvent.userIds.Concat(alliance.members.Keys);
                        }
                    }
                }
            }

            moveEvent.resolution = DateTime.SpecifyKind(DateTime.Now.AddSeconds(travelTime), DateTimeKind.Unspecified);

            //Maj de la flotte
            fleet.destination = moveEvent.location;
            fleet.travelTime = moveEvent.resolution;
            fleet.status = FleetStatusConstant.MOVING;
            fleet.position = null;
            fleet.currentBaseId = null;
            fleet.lastFuelCalcul = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            fleet.instantConsumption = fleet.consumption;

            //Sauvegarde de l'event et de la flotte
            MongoDBSingleton.Instance().moveEventsCollection.InsertOne(moveEvent);
            fleet.moveEventId = moveEvent.id;

            var fleetFilter = Builders<Fleet>.Filter.Eq(x => x.id, fleet.id);
            var fleetUpdate = Builders<Fleet>.Update.Set(x => x.destination, fleet.destination).Set(x => x.travelTime, fleet.travelTime).
                Set(x => x.status, fleet.status).Set(x => x.position, fleet.position).Set(x => x.currentBaseId, fleet.currentBaseId).
                Set(x => x.lastFuelCalcul, fleet.lastFuelCalcul).Set(x => x.instantConsumption, fleet.instantConsumption).
                Set(x => x.moveEventId, fleet.moveEventId);

            MongoDBSingleton.Instance().fleetsCollection.UpdateOne(fleetFilter, fleetUpdate);
            return moveEvent;
        }


        private MoveManager()
        {
        }

        public static MoveManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new MoveManager();
            }
            return _instance;
        }
    }
}
