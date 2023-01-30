using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Bases
{
    public class Built
    {
        public int level { get; set; }

        public int HP { get; set; }

        public BuildingRef building { get; set; }

        public bool active { get; set; }

        //Skin du batiment (configurable par le joueur)
        public string picture { get; set; }

        public Built() { }


        public Built(int level, int hP, BuildingRef building, bool active, string picture)
        {
            this.level = level;
            HP = hP;
            this.building = building;
            this.active = active;
            this.picture = picture;
        }
    }
}
