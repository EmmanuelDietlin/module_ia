using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_Init
{
    /// <summary>
    /// Classe servant à récupérer les informations de configuration des IAs stockées dans un fichier au format .json
    /// </summary>
    public class AIConfig
    {
        public string name { get; set; }
        public string description { get; set; }
        public string personality { get; set; } 
        public string baseTemplate { get; set; }
        public Dictionary<string, string> fleetTemplates { get; set; }
        public Dictionary<string, int> numberOfFleets { get; set; }
        public string allianceName { get; set; }
        public string allianceId { get; set; }
        public int x_coord { get; set; }
        public int y_coord { get; set; }
        public List<string> enemyAllianceIds { get; set; }
        public List<string> allyAllianceIds { get; set; }
        public string type { get; set; }
        public string planetName { get; set; }

        public AIConfig() { }
    }
}
