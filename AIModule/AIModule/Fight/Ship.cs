using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Fight
{
    public class Ship
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id;

        public string name;
        public string description;
        public string techType;

        //Life values
        public int hp;
        public int armor;        //Réduction de dégats, en flat. 10 d'armure = réduction de 10 dégats sur chaque tirs. 10% des dégats minimum
        public int shield;
        //Battle values
        public int damage;
        public int shoots;           //Nombre de tirs par tour
        public int range;

        //Movement values
        public int speed;
        public int warmupTime;       //seconds

        public int consumption;
        public int staticConsumption;

        //Stockage
        public int size;         //Usefull for later, when it will be carried by other ship
        public int storageCapacity;        //La quantitée de ressource que le vaisseau peu stocker

        public int fuelTank;      //Capacité en fuel
        public int fuelTime;

        //Capacitées spéciale des vaisseaux
        public Dictionary<string, int> capacities;

        //Power values
        public int power;

        //Build values
        public Dictionary<string, int> cost;
        public int buildingTime;        //secondes
                                           //Prerequis
        public Dictionary<string, int> buildingReq;
        public Dictionary<string, int> researchReq;
        public Dictionary<string, int> effectReq;

        public Ship() { }

        public Ship(string id, string name, string description, string techType, int hp, int armor, int shield, int damage, int shoots, int range, int speed, int warmupTime, int consumption, int staticConsumption, int size, int storageCapacity, int fuelTank, int fuelTime, Dictionary<string, int> capacities, int power, Dictionary<string, int> cost, int buildingTime, Dictionary<string, int> buildingReq, Dictionary<string, int> researchReq, Dictionary<string, int> effectReq)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.techType = techType;
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
            this.cost = cost;
            this.buildingTime = buildingTime;
            this.buildingReq = buildingReq;
            this.researchReq = researchReq;
            this.effectReq = effectReq;
        }
    }
}
