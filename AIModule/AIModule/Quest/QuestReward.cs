using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Quest
{
    public class QuestReward
    {
        public string type { get; set; }
        public string entityId { get; set; }
        public int value { get; set; }

        public QuestReward() { }    

        public QuestReward(string type, string entityId, int value)
        {
            this.type = type;
            this.entityId = entityId;
            this.value = value;
        }
    }
}
