using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Quest
{
    [BsonIgnoreExtraElements]
    public class Quest
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public List<QuestPassiveObjective> passiveObjectives { get; set; }
        public List<QuestActiveObjective> activesObjectives { get; set; }
        public int maxTime { get; set; } = 0;
        public int minimalOptional { get; set; } = 0;
        public bool isRejectable { get; set; } = true;
        public string sponsorId { get; set; }
        public bool isFromAi { get; set; }
        public List<QuestReward> questRewards { get; set; } = new List<QuestReward>();
        public string templateUuid { get;set; }

        public Quest() { }

    }
}
