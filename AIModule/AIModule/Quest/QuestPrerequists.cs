using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Quest
{
    public class QuestPrerequists
    {
        public string type { get; set; }
        public string description { get; set; }
        public Dictionary<string, int> objectives { get; set; } = new Dictionary<string, int>();
        public int amountNeeded { get; set; }
        public bool isOptional = false;
    }
}
