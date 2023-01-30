using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Quest
{
    public class WaitingQuest : Quest
    {
        public List<string> userId { get; set; }

        public WaitingQuest(Quest quest)
        {
            this.id = quest.id;
            this.name = quest.name;
            this.description = quest.description;
            this.passiveObjectives = quest.passiveObjectives;
            this.activesObjectives = quest.activesObjectives;
            this.maxTime = quest.maxTime;
            this.minimalOptional = quest.minimalOptional;
            this.isRejectable= quest.isRejectable;
            this.sponsorId = quest.sponsorId;
            this.questRewards= quest.questRewards;
            this.templateUuid= quest.templateUuid;
            this.userId = new List<string>();
        }

        public WaitingQuest() { }
    }
}
