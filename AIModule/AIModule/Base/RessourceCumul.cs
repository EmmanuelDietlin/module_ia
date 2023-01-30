using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Bases
{
    public class RessourceCumul
    {
        public int amount { get; set; }

        public int taxedAmount { get; set; }       //Stock de ressources a prélever par les taxes

        public int storage { get; set; }

        public int production { get; set; }                   //Production par minute

        public int taxes { get; set; }                      //Pourcentage de ressource a mettre dans "taxedAmount" et pas dans "amount"

        public int consumption { get; set; }                   //Consommation par minute

        public string detailId { get; set; }

        //[BsonDateTimeOptions(Representation = BsonType.String)]
        public DateTime lastCalcul { get; set; }           //Dernière fois que le calcul de cette ressource a été faite


        public RessourceCumul() { }

        public RessourceCumul(int amount, int taxedAmount, int storage, int production, int taxes, int consumption, string detailId, DateTime lastCalcul)
        {
            this.amount = amount;
            this.taxedAmount = taxedAmount;
            this.storage = storage;
            this.production = production;
            this.taxes = taxes;
            this.consumption = consumption;
            this.detailId = detailId;
            this.lastCalcul = lastCalcul;
        }


        public int addAndRefresh(int toAdd)
        {
            //Refresh de la ressource
            float timeDiff = (float)(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified) - lastCalcul).TotalSeconds;
            double finalProduction = (production - consumption) * (timeDiff / 3600);
            int overload = 0;

            amount = (toAdd + amount + (int)(finalProduction * (1 - ((float)taxes / 100))));
            taxedAmount = (taxedAmount + (int)(finalProduction * ((taxes == 0) ? (float)taxes / 100 : 0)));

            //On limite à 75% du stockage max la quantité de ressources possédées par l'IA
            if (amount > 0.75 * storage)
            {
               amount = storage*(int)0.75;
            }
            else if (amount < 0)
            {
                amount = 0;
            }

            if (taxedAmount < 0)
            {
                taxedAmount = 0;
            }
           lastCalcul = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            return amount;
        }

    }
}
