using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Quest
{
    public class QuestPassiveObjective
    {
        public string type { get; set; }
        public string description { get; set; }
        public Dictionary<string, int> objectives { get; set; } = new Dictionary<string, int>();
        public int amountNeeded { get; set; }
        public bool isOptional { get; set; } = false;

        public QuestPassiveObjective() { }

        public QuestPassiveObjective(string type, string description, Dictionary<string,int> objectives, 
            int amountNeeded, bool isOptional)
        {
            this.type = type;
            this.description = description;
            this.objectives = objectives;  
            this.amountNeeded = amountNeeded;
            this.isOptional = isOptional;
        }   
    }
}
