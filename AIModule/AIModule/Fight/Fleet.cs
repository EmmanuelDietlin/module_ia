using AIModule.Common.constant.Base;
using AIModule.Common.constant.fleet;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Fight
{
    [BsonIgnoreExtraElements]
    public class Fleet
    {
        //Base data
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }
        [BsonElement("name")]
        public string name { get; set; }

        [BsonElement("status")]
        public string status { get; set; }

        [BsonElement("type")]
        public string type { get; set; }

        public int power { get; set; }

        public int amount { get; set; }

        [BsonElement("ownerId")]
        public string ownerId { get; set; }

        [BsonElement("ownerName")]
        public string ownerName { get; set; }
        [BsonElement("position")]
        public GlobalPosition position { get; set; }
        [BsonElement("currentBaseId")]
        public string currentBaseId { get; set; }         //peut être null
        [BsonElement("linkedBaseId")]
        public string linkedBaseId { get; set; }        //peut être null
        [BsonElement("speed")]
        //Movement values
        public int speed { get; set; }
        [BsonElement("consummation")]
        public int consumption { get; set; }       //Conso par heure
        public int instantConsumption { get; set; }

        //Temporaire : fight value
        [BsonElement("maxRange")]
        public int maxRange { get; set; }       //Portée la plus élevée de la flotte
        [BsonElement("minRange")]
        public int minRange { get; set; }          //Portée la plus faible de la flotte
        [BsonElement("capacities")]
        public Dictionary<string, int> capacities { get; set; }     //Id tech (uuid) du groupe (x;y), battleGroup

        //Conso
        [BsonElement("staticConsummation")]
        public int staticConsumption { get; set; }
        [BsonElement("squadrons")]
        public Dictionary<string, Squadron> squadrons { get; set; }    //Id tech (uuid) du groupe (x;y), battleGroup

        //Portée de la flotte, dépends de la consommation des vaisseaux en mouvement et de leur stock de fuel
        public int fuelTime { get; set; }          //Quantité de temps d'autonomie restant a la flotte en consommation maximale, en seconde
        public int cumulatedFuelTank { get; set; }  //Capacité totale de carburant
        public int currentFuel { get; set; }        //Carburant en stock (tout escadron cumulé)

        public int warmupTime { get; set; }         //Temps de décollage de la flotte

        //Stratégies
        [BsonElement("movementStrategy")]
        public string strategy { get; set; }        //Stratégie de déplacement
        [BsonElement("travelTime")]
        public DateTime travelTime { get; set; }
        [BsonElement("destination")]
        public GlobalPosition destination { get; set; }

        public Dictionary<string, int> pillageRate { get; set; }      //Ratio de pillage en %. La somme doit être de 100

        public List<int> bombardTargets { get; set; }      //Liste des emplacements a bombarder

        public int bombardRound { get; set; }             //Nombre de rounds de bombardement

        public string unexpectedStrategy { get; set; }          //Stratégie en cas de combat imprévu

        public GlobalPosition returnPosition { get; set; }      //Position de retraite


        //En cas de vol
        public string moveEventId { get; set; }


        /*Null si la flotte est approvisionnée en fuel par une entitée externe (comme une base).*/
        public DateTime? lastFuelCalcul { get; set; }

        public long version { get; set; }

        public string _class { get; set; } = "fr.need_a_name.bdd_module.fleet.entity.Fleet";

        public Fleet()
        {
            staticConsumption = 0;
            speed = 0;
            minRange = 0;
            maxRange = 0;
            warmupTime = 0;
            fuelTime = 0;
            instantConsumption = 0;
            cumulatedFuelTank = 0;
            currentFuel = 0;
            consumption = 0;
            status = FleetStatusConstant.INACTIVE;
            squadrons = new Dictionary<string, Squadron>();
            strategy = MoveStrategyConstant.DEFAULT;
            type = FleetTypeConstant.DEFAULT;
            amount = 0;
            power = 0;
            capacities = new Dictionary<string, int>();
        }

        public Fleet(string name, string ownerId, string ownerName, GlobalPosition position, string currentBaseId, string linkedBaseId)
        {
            this.name = name;
            this.ownerId = ownerId;
            this.ownerName = ownerName;
            this.position = position;
            this.currentBaseId = currentBaseId;
            this.linkedBaseId = linkedBaseId;

            instantConsumption = 0;
            staticConsumption = 0;
            speed = 0;
            minRange = 0;
            maxRange = 0;
            warmupTime = 0;
            fuelTime = 0;
            cumulatedFuelTank = 0;
            currentFuel = 0;
            consumption = 0;
            status = FleetStatusConstant.INACTIVE;
            squadrons = new Dictionary<string, Squadron>();
            strategy = MoveStrategyConstant.DEFAULT;
            type = FleetTypeConstant.DEFAULT;
            amount = 0;
            power = 0;
            capacities = new Dictionary<string, int>();

            pillageRate = new Dictionary<string, int>();
            bombardTargets = new List<int>();
        }

        public Fleet(Fleet fleet)
        {
            id = fleet.id;
            name = fleet.name;
            type = fleet.type;
            power = fleet.power;
            amount = fleet.amount;
            ownerId = fleet.ownerId;
            ownerName= fleet.ownerName;
            status = fleet.status;
            speed = fleet.speed;
            consumption= fleet.consumption;
            maxRange= fleet.maxRange;
            minRange= fleet.minRange;
            capacities = fleet.capacities;
            staticConsumption= fleet.staticConsumption;
            cumulatedFuelTank = fleet.cumulatedFuelTank;
            warmupTime= fleet.warmupTime;
            position= fleet.position;
            currentBaseId= fleet.currentBaseId;
            linkedBaseId= fleet.linkedBaseId;
            instantConsumption= fleet.instantConsumption;
            fuelTime= fleet.fuelTime;
            currentFuel = fleet.currentFuel;
            strategy= fleet.strategy;
            travelTime= fleet.travelTime;
            destination= fleet.destination;
            pillageRate= fleet.pillageRate;
            bombardTargets= fleet.bombardTargets;
            bombardRound = fleet.bombardRound;
            unexpectedStrategy= fleet.unexpectedStrategy;
            returnPosition= fleet.returnPosition;
            moveEventId= fleet.moveEventId;
            lastFuelCalcul = fleet.lastFuelCalcul;
            version = 0;

            squadrons = new Dictionary<string, Squadron>();
            foreach (var squadron in fleet.squadrons)
            {
                var newSquadron = new Squadron(squadron.Value);
                //newSquadron.techId = Guid.NewGuid().ToString();
                squadrons[newSquadron.techId] = newSquadron;
            }
        }
    }
}
