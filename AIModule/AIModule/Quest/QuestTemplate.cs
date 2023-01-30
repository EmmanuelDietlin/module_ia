using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Quest
{
    [BsonIgnoreExtraElements]
    public class QuestTemplate : Quest
    {
        public List<QuestPrerequists> questPrerequists { get; set; } = new List<QuestPrerequists>();
    }
}
