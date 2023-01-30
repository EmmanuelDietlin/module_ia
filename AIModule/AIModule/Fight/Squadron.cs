using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Fight
{
    [BsonIgnoreExtraElements]
    public class Squadron
    {
       public string techId { get;set; }

        //Stratégies
        public string moveTactic { get; set; }
        public string shootingTactic { get; set; }
        public int formationRound { get; set; }
        public int line { get; set; }
        public int bombardTurn { get; set; }        //Nombre de tour pendant lequel l'escadron bombardera en cas d'attaque
        public bool doPillage { get; set; }

        public int restraint { get; set; }          //Dégats reçu lors d'un tour mais non appliqué
        public string fleetId { get; set; }

        public int amount { get; set; }

        //Cumulated values
        public int cumulatedPower { get; set; }
        public int cumulatedConsumption { get; set; }
        public int cumulatedStaticConsumption { get; set; }

        [BsonElement("cumulatedCapacity")]
        public int cumulatedStorageCapacity { get; set; }

        //Current Storage value
        public Dictionary<string, int> storage { get; set; }
        public int currentStorage { get; set; }
        //Fuel value
        public int cumulatedFuelTank { get; set; }      //Capacité en fuel


        //Specials values
        public Dictionary<string, int> cumulatedCapacities { get; set; }

        //endregion


        //Infos des vaisseaux (pour ne pas avoir besoin d'aller chercher le Ship en bdd lors du combat)
        //region ship
        public string shipId { get; set; }    //Id du modèle de vaisseau
        public string name { get; set; }

        //Life values
        public int hp { get; set; }
        public int armor { get; set; }
        public int shield { get; set; }
        //Battle values
        public int damage { get; set; }
        public int shoots { get; set; }
        public int range { get; set; }

        //Movement values
        public int speed { get; set; }
        public int warmupTime { get; set; }       //seconds

        public int consumption { get; set; }
        public int staticConsumption { get; set; }

        //Storage value
        public int size { get; set; }         //Usefull for later, when it will be carried by other ship
        public int storageCapacity { get; set; }      //La quantitée de ressource que le vaisseau peu stocker

        public int fuelTank { get; set; }      //Capacité en fuel
        public int fuelTime { get; set; }      //fuelTank/conso

        public Dictionary<string, int> capacities { get; set; }
        //Power values
        public int power { get; set; }
        //endregion

        

        public Squadron(Ship ship, string techId, string moveTactic, string shootingTactic, int formationRound, int line, int bombardTurn, bool doPillage, string fleetId)
        {
            //Squad data
            this.techId = techId;

            //Stratégie
            this.moveTactic = moveTactic;
            this.shootingTactic = shootingTactic;
            this.formationRound = formationRound;
            this.line = line;
            this.bombardTurn = bombardTurn;
            this.doPillage = doPillage;


            this.restraint = 0;
            this.fleetId = fleetId;
            this.amount = 0;



            //Donnée cumulée
            this.cumulatedConsumption = 0;
            this.cumulatedPower = 0;
            this.cumulatedStaticConsumption = 0;
            this.cumulatedStorageCapacity = 0;
            this.currentStorage = 0;
            this.storage = new Dictionary<string, int>();
            this.cumulatedFuelTank = 0;

            this.cumulatedCapacities = new Dictionary<string, int>();

            //Ship data
            this.shipId = ship.id;
            this.name = ship.name;
            this.hp = ship.hp;
            this.armor = ship.armor;
            this.shield = ship.shield;
            this.speed = ship.speed;
            this.shoots = ship.shoots;
            this.range = ship.range;
            this.size = ship.size;
            this.storageCapacity = ship.storageCapacity;
            this.fuelTank = ship.fuelTank;
            this.fuelTime = ship.fuelTime;

            this.damage = ship.damage;
            this.warmupTime = ship.warmupTime;
            this.capacities = ship.capacities;

            this.consumption = ship.consumption;
            this.power = ship.power;
            this.staticConsumption = ship.staticConsumption;
        }

        public Squadron(Squadron squadron)
        {
            //Squad data
            this.techId = squadron.techId;

            //Stratégie
            this.moveTactic = squadron.moveTactic;
            this.shootingTactic = squadron.shootingTactic;
            this.formationRound = squadron.formationRound;
            this.line = squadron.line;
            this.bombardTurn = squadron.bombardTurn;
            this.doPillage = squadron.doPillage;


            this.restraint = squadron.restraint;
            this.fleetId = squadron.fleetId;
            this.amount = squadron.amount;


            this.fuelTime = squadron.fuelTime;

            //Donnée cumulée
            this.cumulatedConsumption = squadron.cumulatedConsumption;
            this.cumulatedPower = squadron.cumulatedPower;
            this.cumulatedStaticConsumption = squadron.cumulatedStaticConsumption;
            this.cumulatedStorageCapacity = squadron.cumulatedStorageCapacity;
            this.currentStorage = squadron.currentStorage;
            this.storage = squadron.storage;
            this.cumulatedFuelTank = squadron.cumulatedFuelTank;

            this.cumulatedCapacities = squadron.cumulatedCapacities;

            //Ship data
            this.shipId = squadron.shipId;
            this.name = squadron.name;
            this.hp = squadron.hp;
            this.armor = squadron.armor;
            this.shield = squadron.shield;
            this.speed = squadron.speed;
            this.shoots = squadron.shoots;
            this.range = squadron.range;
            this.size = squadron.size;
            this.storageCapacity = squadron.storageCapacity;
            this.fuelTank = squadron.fuelTank;
            this.fuelTime = squadron.fuelTime;
            this.damage = squadron.damage;
            this.warmupTime = squadron.warmupTime;
            this.capacities = squadron.capacities;

            this.consumption = squadron.consumption;
            this.power = squadron.power;
            this.staticConsumption = squadron.staticConsumption;
        }

        public Squadron() { }

        public Squadron(string techId, string moveTactic, string shootingTactic, int formationRound, int line, int bombardTurn, bool doPillage, int restraint, string fleetId, int amount, int cumulatedPower, int cumulatedConsumption, int cumulatedStaticConsumption, int cumulatedStorageCapacity, Dictionary<string, int> storage, int currentStorage, int cumulatedFuelTank, Dictionary<string, int> cumulatedCapacities, string shipId, string name, int hp, int armor, int shield, int damage, int shoots, int range, int speed, int warmupTime, int consumption, int staticConsumption, int size, int storageCapacity, int fuelTank, int fuelTime, Dictionary<string, int> capacities, int power)
        {
            this.techId = techId;
            this.moveTactic = moveTactic;
            this.shootingTactic = shootingTactic;
            this.formationRound = formationRound;
            this.line = line;
            this.bombardTurn = bombardTurn;
            this.doPillage = doPillage;
            this.restraint = restraint;
            this.fleetId = fleetId;
            this.amount = amount;
            this.cumulatedPower = cumulatedPower;
            this.cumulatedConsumption = cumulatedConsumption;
            this.cumulatedStaticConsumption = cumulatedStaticConsumption;
            this.cumulatedStorageCapacity = cumulatedStorageCapacity;
            this.storage = storage;
            this.currentStorage = currentStorage;
            this.cumulatedFuelTank = cumulatedFuelTank;
            this.cumulatedCapacities = cumulatedCapacities;
            this.shipId = shipId;
            this.name = name;
            this.hp = hp;
            this.armor = armor;
            this.shield = shield;
            this.damage = damage;
            this.shoots = shoots;
            this.range = range;
            this.speed = speed;
            this.warmupTime = warmupTime;
            this.consumption = consumption;
            this.staticConsumption = staticConsumption;
            this.size = size;
            this.storageCapacity = storageCapacity;
            this.fuelTank = fuelTank;
            this.fuelTime = fuelTime;
            this.capacities = capacities;
            this.power = power;
        }
    }
}
