using AIModule.Common.constant;
using AIModule.Common.constant.Base;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Events
{
    public static class EventResultTypeConstants
    {
        //Combat
        public const string PILLED_RESOURCES = "PILLED_RESOURCES";       //Map des ressources pillées (attaque)
        public const string LOST_RESOURCES = "LOST_RESOURCES";           //Map des ressources perdues (défenseur)
        public const string DESTROYED_SHIP = "DESTROYED_SHIP";           //Map des vaisseaux détruit (attaque)
        public const string DESTROYED_POWER = "DESTROYED_POWER";
        public const string LOST_SHIP = "LOST_SHIP";                     //Map des vaisseaux perdus (défense)
        public const string LOST_POWER = "LOST_POWER";
        public const string BOMBED_SHIP = "BOMBED_SHIP";                 //Map des vaisseaux bombardés (bombardement des flottes inactives)
        public const string DESTROYED_BASE = "DESTROYED_BASE";           //Id du joueur qui à détruit la base (l'initiateur du combat)
        public const string CAPTURED_BASE = "CAPTURED_BASE";             //Id du nouveau proprio
        public const string ENEMIES_ID = "ENEMIES_ID";                   //Liste des ownerId des enemis
        public const string ALLIED_ID = "ALLIED_ID";                     //Liste des ownerId des alliés
        public const string DEFENDERS_ID = "DEFENDERS_ID";
        public const string ATTACKERS_ID = "ATTACKERS_ID";
        public const string INITIAL_POWER = "INITIAL_POWER";             //Force initiale de la flotte envoyée par l'owner dans le combat


        //Déplacement
        public const string ATTACK = "ATTACK";                                   //booleen
        public const string FIGHT = "FIGHT";                                     //booleen
        public const string SUPPORT = "SUPPORT";                                 //booleen
        public const string BUILD_BASE = "BUILD_BASE";                           //booleen
        public const string DELIVERED_RESOURCES = "DELIVERED_RESOURCES";         //Liste des ressources livrées
        public const string ESCAPE = "ESCAPE";                                   //booleen
        public const string CANCELED_BASE_CONSTRUCT = "CANCELED_BASE_CONSTRUCT"; //booleen
        public const string MOVE_ERROR = "MOVE_ERROR"; //booleen
        public const string MOVED_FLEET = "MOVED_FLEET";

        //Infilration
        public const string INFILTRATION_SUCCESS = "INFILTRATION_SUCCESS";
        public const string AGENT_CAUGHT = "AGENT_CAUGHT";

        //Loot
        public const string SEARCH_LOOT = "SEARCH_LOOT";
        public const string SEARCH_SIZE = "SEARCH_SIZE";

        //Construction de vaisseaux
        public const string BUILT_SHIP = "BUILT_SHIP";

        //Construction
        public const string BUILT_BUILDING = "BUILT_BUILDING";

        //Recherche
        public const string RESEARCHED = "RESEARCHED";

        //Agent
        public const string MISSON_SUCCESS = "MISSION_SUCCESS";
        public const string MISSION_DIFFICULTY = "MISSION_DIFFICULTY";

        //Quete
        public const string QUEST_ID = "QUEST_ID";
        public const string QUEST_TEMPLATE = "QUEST_TEMPLATE";
        public const string QUEST_STATUS = "QUEST_STATUS";

    }



    [BsonIgnoreExtraElements]
    public class EventResult
    {
        public string id { get; set; }

        public string eventType { get; set; }

        public GlobalPosition globalPosition { get; set; }

        public DateTime? eventEnd { get; set; }

        public string userId { get; set; }  

        public string impactedOwnerId { get; set; }

        public Dictionary<string, object> results { get; set; }


        public EventResult() { }

        


    }




}
