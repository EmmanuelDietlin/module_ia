using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Ressource
{
    public class RessourceDetail
    {
        public string id { get; set; }

        public int totalProduction { get; set; }

        public int totalMultiplier { get; set; }

        public int calculatedProduction { get; set; }

        public int totalConsumption { get; set; }

        public int totalTaxe { get; set; }

        public int totalStorage { get; set; }

        public int totalStorageMultiplier { get; set; }

        public int calculatedStorage { get; set; }

        public Dictionary<string, RessourceSource> productions { get; set; }

        public Dictionary<string, RessourceSource> multipliers { get; set; }

        public Dictionary<int, RessourceSource> buildingConsumptions { get; set; }

        public Dictionary<string, RessourceSource> fleetConsumptions { get; set; }

        public Dictionary<string, RessourceSource> externalConsumptions { get; set; }

        public Dictionary<string, RessourceSource> taxes { get; set; }

        public Dictionary<string, RessourceSource> storages { get; set; }

        public Dictionary<string, RessourceSource> storageMultipliers { get; set; }

        public RessourceDetail()
        {
            totalProduction = 0;
            totalMultiplier = 100;
            calculatedProduction = 0;
            totalConsumption = 0;
            totalTaxe = 0;
            totalStorage = 0;
            totalStorageMultiplier = 100;
            calculatedStorage = 0;
            productions = new Dictionary<string, RessourceSource>();
            multipliers = new Dictionary<string, RessourceSource>();
            buildingConsumptions = new Dictionary<int, RessourceSource>();
            fleetConsumptions = new Dictionary<string, RessourceSource>();
            externalConsumptions = new Dictionary<string, RessourceSource>();
            taxes = new Dictionary<string, RessourceSource>();
            storages = new Dictionary<string, RessourceSource>();
            storageMultipliers = new Dictionary<string, RessourceSource>();
        }

        public int updateFleetConsumption(string key, int amount, string libelle)
        {
            if (amount > 0)
            {
                //Maj de la conso totale
                if (fleetConsumptions.ContainsKey(key))
                {
                    totalConsumption = totalConsumption + amount - fleetConsumptions[key].amount;
                }
                else
                {
                    totalConsumption = totalConsumption + amount;
                }
                fleetConsumptions[key] = new RessourceSource(libelle, amount);
            }
            else
            {
                if (fleetConsumptions.ContainsKey(key))
                {
                    totalConsumption = totalConsumption - fleetConsumptions[key].amount;
                    fleetConsumptions.Remove(key);

                }
            }

            return totalConsumption;
        }

        public int updateExternalConsumption(string key, int amount, string libelle)
        {
            if (amount > 0)
            {
                //Maj de la conso totale
                if (externalConsumptions.ContainsKey(key))
                {
                    totalConsumption = totalConsumption + amount - externalConsumptions[key].amount;
                }
                else
                {
                    totalConsumption = totalConsumption + amount;
                }
                externalConsumptions[key] = new RessourceSource(libelle, amount);
            }
            else
            {
                if (externalConsumptions.ContainsKey(key))
                {
                    externalConsumptions.Remove(key);
                }
            }

            return totalConsumption;
        }

        public int removeFleetConsumption(string key)
        {
            if (fleetConsumptions.ContainsKey(key))
            {
                totalConsumption = totalConsumption - fleetConsumptions[key].amount;
                fleetConsumptions.Remove(key);
            }
            return totalConsumption;
        }

        public int removeExternalConsumption(string key)
        {
            if (externalConsumptions.ContainsKey(key))
            {
                totalConsumption = totalConsumption - externalConsumptions[key].amount;
                externalConsumptions.Remove(key);
            }
            return totalConsumption;
        }
    }


}
