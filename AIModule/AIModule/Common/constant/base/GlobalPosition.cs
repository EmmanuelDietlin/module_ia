using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Common.constant.Base
{
    public class GlobalPosition
    {
        public string planetId { get; set; }
        public string coordinates { get; set; }

        public override bool Equals(object o)
        {
            // If the object is compared with itself then return true
            if (o == this)
            {
                return true;
            }
            /* Check if o is an instance of GlobalPosition or not
              "null instanceof [type]" also returns false */
            if (!GetType().Equals(o.GetType()))
            {
                return false;
            }
            //Cast de l'objet en GlobalPosition
            GlobalPosition position = (GlobalPosition)o;

            //Comparaison
            return position.coordinates.Equals(coordinates) && position.planetId.Equals(planetId);
        }

        public GlobalPosition(string planetId, string coordinates)
        {
            this.planetId = planetId;
            this.coordinates = coordinates;
        }

        public GlobalPosition()
        {
        }
    }
}
