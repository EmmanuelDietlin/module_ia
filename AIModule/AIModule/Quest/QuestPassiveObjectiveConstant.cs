using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Quest
{
    public static class QuestPassiveObjectiveConstant
    {
        public const string BUILDING = "BUILDING";
        public const string SHIP = "SHIP";
        public const string AGENT = "AGENT";
    
        public const string RESEARCH = "RESEARCH";
    
        //Resource
        public const string RESOURCE_PROD = "RESOURCE_PROD";
        public const string RESOURCE_STORAGE = "RESOURCE_STORAGE";
        public const string RESOURCE_AMOUNT = "RESOURCE_AMOUNT";
    
        //Quetes
        public const string QUEST_SUCCESS = "QUEST_SUCCESS";
        public const string QUEST_FAILURE = "QUEST_FAILURE";
        public const string QUEST_EXPIRED = "QUEST_EXPIRED";
        public const string QUEST_REJECTED = "QUEST_REJECTED";
        public const string NEW_QUEST = "NEW_QUEST";
    
        //Alliance
        public const string ALLIANCE_JOINED = "ALLIANCE_JOINED";
        public const string ALLIANCE_CREATED = "ALLIANCE_CREATED";
    
        //Réputation
        public const string MINIMAL_FAME = "MINIMAL_FAME";
        public const string MAXIMAL_FAME = "MAXIMAL_FAME";
    
        //Gouvernement
        public const string GOVERNMENT = "GOVERNMENT";
    }
}
