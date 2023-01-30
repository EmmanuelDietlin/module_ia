using AI_Init;
using AIModule.AI;
using AIModule.Bases;
using AIModule.Common;
using AIModule.Common.constant.Base;
using AIModule.Common.constant.fleet;
using AIModule.Common.constant.owner;
using AIModule.Fight;
using AIModule.Owners;
using AIModule.Planet;
using AIModule.Quest;
using AIModule.Reputation;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using SharpCompress.Common;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using YamlDotNet.Core.Tokens;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Initialisation de toutes les IAs en BDD
/// </summary>
class Init {
    public static void Main()
    {
        MongoClient client;
        IMongoDatabase database;
        //Emplacement du fichier de configuration des IAs
        string aiInfosPath = Path.Combine(Directory.GetCurrentDirectory(),"AIConfig.json");
        //Emplacement du fichier de configuration de la connection
        string connectionInfos = Path.Combine(Directory.GetCurrentDirectory(), "connectionInfos.json");

        //Lecture du fichier de configuration de la connection MongoDB et accès à la base de donnée du jeu
        using (var reader = new StreamReader(connectionInfos))
        {
            string json = reader.ReadToEnd();
            var infos = JsonSerializer.Deserialize<ConnectionInfos>(json);
            client = new MongoClient(infos.mongodb.uri);
            database = client.GetDatabase(infos.mongodb.database);
            Console.WriteLine(json);

        }

        //Lecture du fichier de configuration des IAs et désérialisation des informations des IAs dans une liste
        var aiInfos = new List<AIConfig>();
        using (StreamReader r = new StreamReader(aiInfosPath))
        {
            string json = r.ReadToEnd();
            aiInfos = JsonSerializer.Deserialize<List<AIConfig>>(json);
        }

        var aiDeltaCollection = database.GetCollection<AIDelta>("owners");
        var aiGammaCollection = database.GetCollection<AIGamma>("owners");
        var fleetsCollection = database.GetCollection<Fleet>("fleets");
        var basesCollection = database.GetCollection<Base>("bases");
        var planetsCollection = database.GetCollection<Planet>("planets");


        //Code pour nettoyer les anciennes IAs de owners, fleets, bases
        foreach (var fleet in fleetsCollection.AsQueryable().Where(x => x.type == "NPC"))
        {
            fleetsCollection.DeleteOne(Builders<Fleet>.Filter.Eq(x => x.id, fleet.id));
        }
        foreach (var Base in basesCollection.AsQueryable().Where(x => x.type == "NPC_MAIN"))
        {
            basesCollection.DeleteOne(Builders<Base>.Filter.Eq(x => x.id, Base.id));
        }
        foreach (var aidelta in aiDeltaCollection.AsQueryable().Where(x => x.type == "AIDELTA"))
        {
            aiDeltaCollection.DeleteOne(Builders<AIDelta>.Filter.Eq(x => x.id, aidelta.id));
        }
        foreach (var aigamma in aiGammaCollection.AsQueryable().Where(x => x.type == "AIGAMMA"))
        {
            aiGammaCollection.DeleteOne(Builders<AIGamma>.Filter.Eq(x => x.id, aigamma.id));
        }

        //Code pour nettoyer les anciennes bases IA de la carte
        var planets = planetsCollection.Find(_ => true).ToList();
        foreach (var planet in planets)
        {
            foreach (var key in planet.tiles.Keys)
            {
                if (planet.tiles[key].Base != null && planet.tiles[key].Base.type == "NPC_MAIN")
                {
                    planet.tiles.Remove(key);
                }
            }
            var planetUupdate = Builders<Planet>.Update.Set(x => x.tiles, planet.tiles);
            planetsCollection.UpdateOne(Builders<Planet>.Filter.Eq(x => x.id, planet.id), planetUupdate);
        }

        Console.WriteLine("Starting IA initialization...");

        for (int i = 0; i < aiInfos.Count; i++)
        {
            Console.WriteLine("Creating AI {0}", aiInfos[i].name);

            var aiBasePower = 0;
            switch(aiInfos[i].baseTemplate)
            {
                case "NPC_TEMPLATE_1k":
                    aiBasePower = (int)(3.5 * 1);
                    break;
                case "NPC_TEMPLATE_10k":
                    aiBasePower = (int)(3.5 * 7);
                    break;
                case "NPC_TEMPLATE_100k":
                    aiBasePower = (int)(3.5 * 13);
                    break;
                case "NPC_TEMPLATE_1m":
                    aiBasePower = (int)(3.5 * 17);
                    break;
            }
            var planetFilter = Builders<Planet>.Filter.Eq(x => x.name, aiInfos[i].planetName);

            var baseFilter = Builders<Base>.Filter.Eq("name", aiInfos[i].baseTemplate);
            var baseTemplate = basesCollection.Find(baseFilter).First();

            //Création de la base IA à partir du template spécifié dans le fichier de configuration
            var newBase = baseTemplate;
            newBase.id = null;
            newBase.name = "AI base";
            newBase.type = BaseTypeConstant.NPC_MAIN;
            newBase.position = new GlobalPosition();
            newBase.position.coordinates = string.Format("{0};{1}", aiInfos[i].x_coord, aiInfos[i].y_coord);
            newBase.position.planetId = planetsCollection.Find(planetFilter).First().id;
            newBase.version = 1;
            newBase.isAttackable = true;
            //Insertion de la base dans la base de donnée afin de récupérer un nouvel id
            basesCollection.InsertOne(newBase);

            //Si l'IA est une IA de type Delta
            if (aiInfos[i].type.Equals(OwnerTypeConstant.AI_DELTA))
            {
                //Création d'une nouvelle IA Delta
                AIDelta d = new AIDelta();
                d.name = aiInfos[i].name;
                d.description = aiInfos[i].description;
                d.type = OwnerTypeConstant.AI_DELTA;
                d.mainBaseId = newBase.id;
                d.enemyAllianceIds = aiInfos[i].enemyAllianceIds;
                d.allyAllianceIds= aiInfos[i].allyAllianceIds;
                d.version = 1;
                d.personality= aiInfos[i].personality;
                d.baseListId.Add(d.mainBaseId, BaseTypeConstant.NPC_MAIN);
                d.aiBasePower = aiBasePower;

                d.allianceId = aiInfos[i].allianceId;
                d.allianceName = aiInfos[i].allianceName;
                //Insertion en base de donnée afin de récupérer un id
                aiDeltaCollection.InsertOne(d);

                //Ajout de la nouvelle IA en tant que propriétaire de la base
                newBase.ownerId = d.id;
                newBase.ownerName = d.name;

                //Mise à jour des données concernant le propriétaire de la base en base de donnée
                var updateBase = Builders<Base>.Update.Set("ownerId", newBase.ownerId).Set("ownerName", newBase.ownerName);
                var newBaseFilter = Builders<Base>.Filter.Eq(x => x.id, newBase.id);
                basesCollection.UpdateOne(newBaseFilter, updateBase);

                //Ajout de l'IA sur la carte du jeu
                var aiTile = new Tile();
                aiTile.Base = new BaseRef(newBase);
                aiTile.buildable = true;
                var planetUpdate = Builders<Planet>.Update.Set(x => x.tiles[newBase.position.coordinates], aiTile);
                planetsCollection.UpdateOne(planetFilter, planetUpdate);

                //On créée les différentes types de flottes de l'IA
                createFleets(d, aiInfos[i], fleetsCollection, newBase, database);

                //mise à jour de la liste des flottes de l'IA en base de donnée
                var aiUpdate = Builders<AIDelta>.Update.Set(x => x.fleetTemplates, d.fleetTemplates).Set(x => x.fleetIdsGroupedByType, d.fleetIdsGroupedByType).Set(x => x.fleetListId, d.fleetListId);
                aiDeltaCollection.UpdateOne(Builders<AIDelta>.Filter.Eq(x => x.id, d.id), aiUpdate);

            }
            else
            {
                AIGamma d = new AIGamma();
                d.name = aiInfos[i].name;
                d.description = aiInfos[i].description;
                d.type = OwnerTypeConstant.AI_GAMMA;
                d.mainBaseId = newBase.id;
                d.enemyAllianceIds = aiInfos[i].enemyAllianceIds;
                d.allyAllianceIds = aiInfos[i].allyAllianceIds;
                d.version = 1;
                d.personality = aiInfos[i].personality;
                d.baseListId.Add(d.mainBaseId, BaseTypeConstant.NPC_MAIN);
                d.aiBasePower = aiBasePower;

                d.allianceId = aiInfos[i].allianceId;
                d.allianceName = aiInfos[i].allianceName;
                aiGammaCollection.InsertOne(d);

                newBase.ownerId = d.id;
                newBase.ownerName = d.name;

                var updateBase = Builders<Base>.Update.Set("ownerId", newBase.ownerId).Set("ownerName", newBase.ownerName);
                var newBaseFilter = Builders<Base>.Filter.Eq(x => x.id, newBase.id);
                basesCollection.UpdateOne(newBaseFilter, updateBase);

                var aiTile = new Tile();
                aiTile.Base = new BaseRef(newBase);
                aiTile.buildable = true;
                var planetUpdate = Builders<Planet>.Update.Set(x => x.tiles[newBase.position.coordinates], aiTile);
                planetsCollection.UpdateOne(planetFilter, planetUpdate);

                //On créée les différentes types de flottes de l'IA
                createFleets(d, aiInfos[i], fleetsCollection, newBase, database);

                //mise à jour de la liste des flottes de l'IA en base de donnée
                var aiUpdate = Builders<AIGamma>.Update.Set(x => x.fleetTemplates, d.fleetTemplates).Set(x => x.fleetIdsGroupedByType, d.fleetIdsGroupedByType).Set(x => x.fleetListId, d.fleetListId);
                aiGammaCollection.UpdateOne(Builders<AIGamma>.Filter.Eq(x => x.id, d.id), aiUpdate);

            }
        }

        var aiDeltaFilter = Builders<AIDelta>.Filter.Eq(x => x.type, OwnerTypeConstant.AI_DELTA);
        var aiGammaFilter = Builders<AIGamma>.Filter.Eq(x => x.type, OwnerTypeConstant.AI_GAMMA);
        var aisDelta = aiDeltaCollection.Find(aiDeltaFilter).ToList();
        var aisGamma = aiGammaCollection.Find(aiGammaFilter).ToList();
        
        //On ajoute pour chaque IA delta la liste de ses ennemis et de ses alliés
        for (int i = 0; i < aisDelta.Count; i++)
        {
            //Liste des id des alliés de l'IA
            var allies = (from ai in aisDelta
                         where ai.allianceId == aisDelta[i].allianceId && ai.id != aisDelta[i].id && ai.allianceId != ""
                         select ai.id).Concat(from ai in aisGamma
                                              where ai.allianceId == aisDelta[i].allianceId && ai.id != aisDelta[i].id && ai.allianceId != ""
                                              select ai.id);
            allies = allies.Concat((from ai in aisDelta
                                   where aisDelta[i].allyAllianceIds.Contains(ai.allianceId) && ai.allianceId != ""
                                    select ai.id).Concat(from ai in aisGamma
                                                        where ai.allianceId != "" && aisDelta[i].allyAllianceIds.Contains(ai.allianceId)
                                                        select ai.id));
            aisDelta[i].allyIds = allies.ToList();

            //Liste des id des ennemis de l'IA
            var enemies = (from ai in aisDelta
                           where aisDelta[i].enemyAllianceIds.Contains(ai.allianceId) && ai.allianceId != ""
                           select ai.id).Concat(from ai in aisGamma
                                                where ai.allianceId != "" && aisDelta[i].enemyAllianceIds.Contains(ai.allianceId)
                                                select ai.id);
            aisDelta[i].enemyIds= enemies.ToList();

            //Mise à jour de ces listes en base de donnée
            var filter = Builders<AIDelta>.Filter.Eq(x => x.id, aisDelta[i].id);
            var update = Builders<AIDelta>.Update.Set(x => x.allyIds, aisDelta[i].allyIds).Set(x => x.enemyIds, aisDelta[i].enemyIds);
            aiDeltaCollection.UpdateOne(filter, update);
        }
        //On ajout pour chaque IA Gamma la liste de ses alliés et de ses ennemis
        for (int i = 0; i < aisGamma.Count; i++)
        {
            //Liste des id des alliés de l'IA
            var allies = (from ai in aisGamma
                          where ai.allianceId == aisGamma[i].allianceId && ai.id != aisGamma[i].id && ai.allianceId != ""
                          select ai.id).Concat(from ai in aisDelta
                                               where ai.allianceId == aisGamma[i].allianceId && ai.id != aisGamma[i].id && ai.allianceId != ""
                                               select ai.id);
            allies = allies.Concat((from ai in aisGamma
                                    where aisGamma[i].allyAllianceIds.Contains(ai.allianceId) && ai.allianceId != ""
                                    select ai.id).Concat(from ai in aisDelta
                                                         where aisGamma[i].allyAllianceIds.Contains(ai.allianceId) && ai.allianceId != ""
                                                         select ai.id));

            aisGamma[i].allyIds = allies.ToList();

            //Liste des id des ennemis de l'IA
            var enemies = (from ai in aisGamma
                           where aisGamma[i].enemyAllianceIds.Contains(ai.allianceId) && ai.allianceId != ""
                           select ai.id).Concat(from ai in aisDelta
                                                where aisGamma[i].enemyAllianceIds.Contains(ai.allianceId) && ai.allianceId != ""
                                                select ai.id);
            aisGamma[i].enemyIds = enemies.ToList();

            //Mise à jour de ces listes en base de donnée
            var filter = Builders<AIGamma>.Filter.Eq(x => x.id, aisGamma[i].id);
            var update = Builders<AIGamma>.Update.Set(x => x.allyIds, aisGamma[i].allyIds).Set(x => x.enemyIds, aisGamma[i].enemyIds);
            aiGammaCollection.UpdateOne(filter, update);
        }
        Console.WriteLine("Finished IA initialization");
        
        Console.WriteLine("Starting reputation initialization...");
        var reputationCollection = database.GetCollection<ReputationObject>("reputation");

        //On récupère tous les objets dans la collection "reputation"
        var reputationObjects = reputationCollection.Find(_ => true).ToList();
        var reputation = new Dictionary<string, float>();

        //On initialise pour tous les joueurs un dictionnaire vide pour la réputation
        for (int i = 0; i < reputationObjects.Count; i++)
        {
            var update = Builders<ReputationObject>.Update.Set(x => x.reputationValues, reputation);
            var filter = Builders<ReputationObject>.Filter.Eq(x => x.id, reputationObjects[i].id);
            reputationCollection.UpdateOne(filter, update);

        }
        Console.WriteLine("Finished reputation initialization");

    }


    public static void createFleets(AI ai, AIConfig aiInfo, IMongoCollection<Fleet> fleetsCollection, Base newBase, IMongoDatabase database)
    {
        //On créée les différentes types de flottes de l'IA
        foreach (var key in aiInfo.fleetTemplates.Keys)
        {
            //Récupération du template de la flotte
            var builder = Builders<Fleet>.Filter;
            var fleetFilter = builder.Eq("name", aiInfo.fleetTemplates[key]) & builder.Eq("type", FleetTypeConstant.TEMPLATE);
            var fleetTemplate = fleetsCollection.Find(fleetFilter).First();

            //Stockage du template dans le champ dédié de l'IA
            ai.fleetTemplates[key] = new TemplateElement(aiInfo.fleetTemplates[key], aiInfo.numberOfFleets[key], fleetTemplate.squadrons.Sum(x => x.Value.cumulatedPower));
            ai.fleetIdsGroupedByType[key] = new List<string>();

            //On créée un certain nombre de chaque type de flotte
            for (int j = 0; j < aiInfo.numberOfFleets[key]; j++)
            {
                //Création de la flotte à partir du template
                var newFleet = fleetTemplate;
                newFleet.name = String.Format("{0}'s fleet", ai.name);
                createFleet(newFleet, database, newBase, ai.personality, key);
                //On met à jour la flotte en BDD
                fleetFilter = Builders<Fleet>.Filter.Eq(x => x.id, newFleet.id);
                var newFleetUpdate = Builders<Fleet>.Update.Set(x => x.squadrons, newFleet.squadrons).Set(x => x.capacities, newFleet.capacities).
                    Set(x => x.power, newFleet.power).Set(x => x.speed, newFleet.speed).Set(x => x.staticConsumption, newFleet.staticConsumption).
                    Set(x => x.minRange, newFleet.minRange).Set(x => x.maxRange, newFleet.maxRange).Set(x => x.consumption, newFleet.consumption).
                    Set(x => x.amount, newFleet.amount).Set(x => x.cumulatedFuelTank, newFleet.cumulatedFuelTank).Set(x => x.currentFuel, newFleet.currentFuel).
                    Set(x => x.fuelTime, newFleet.fuelTime);
                fleetsCollection.UpdateOne(fleetFilter, newFleetUpdate);
                ai.fleetListId.Add(newFleet.id);
                ai.fleetIdsGroupedByType[key].Add(newFleet.id);
            }
            for (int j = 0; j < ai.fleetListId.Count; j++)
            {
                //Ajout pour les flottes du nom et id du propriétaire
                var updateFleet = Builders<Fleet>.Update.Set("ownerId", ai.id).Set("ownerName", ai.name).Set("currentBaseId", newBase.id);
                var newFleetFilter = Builders<Fleet>.Filter.Eq(x => x.id, ai.fleetListId[j]);
                fleetsCollection.UpdateOne(newFleetFilter, updateFleet);
            }
        }
    }

    public static void createFleet(Fleet fleet, IMongoDatabase database, Base Base, string personality, string fleetType)
    {
        var fleetsCollection = database.GetCollection<Fleet>("fleets");
        fleet.version = 1;
        fleet.id = null;

        //Si la flotte est une flotte de défense ou une flotte de tourelles, alors elle est en status de patrouille
        if (fleetType == AIFleetTypes.defenseFleet || fleetType == AIFleetTypes.turretFleet)
        {
            fleet.status = FleetStatusConstant.PATROLLING;
        }
        //Sinon, le status de la flotte dépend de la personalité de l'IA
        else
        {
            switch (personality)
            {
                case Personality.Pacifist:
                    fleet.status = FleetStatusConstant.INACTIVE;
                    break;
                case Personality.Warmonger:
                    fleet.status = FleetStatusConstant.PATROLLING;
                    break;
                case Personality.Trader:
                    fleet.status = FleetStatusConstant.STANDBY;
                    break;
            }
        }
        
        //Ajout de la position, du type de base et l'id de la base auquelle la flotte est rattachée
        fleet.position = Base.position;
        fleet.type = FleetTypeConstant.NPC;
        fleet.linkedBaseId = Base.id;

        //Ajout du ratio de pillage pour la flotte
        fleet.pillageRate = new Dictionary<string, int>
        {
            { RessourcesConstant.CRISTAL, 25 },
            { RessourcesConstant.ORGANIC, 25 },
            { RessourcesConstant.METAL, 25 },
            { RessourcesConstant.ENERGY, 25 }
        };
        //On insère la flotte pour récupérer un id
        fleetsCollection.InsertOne(fleet);

        Dictionary<string, Squadron> newSquad = new Dictionary<string, Squadron>();
        //On met à jour les Squadrons de la flotte
        foreach (var squadron in fleet.squadrons)
        {
            string newTechId = Guid.NewGuid().ToString();
            squadron.Value.fleetId = fleet.id;
            squadron.Value.techId = newTechId;
            newSquad[newTechId] = squadron.Value;
        }
        fleet.squadrons = newSquad;

        //on met une consommation pour la flotte IA afin que celle-ci puisse bien se déplacer
        int power = 0;
        int speed = 1000;
        int minRange = 1000;
        int maxRange = 0;
        int consummation = 1;
        int staticConsummation = 0;
        int amount = 0;
        int cumulatedFuelTank = 1000000;

        //On met à jour les Squadrons
        Dictionary<string, int> capacities = new Dictionary<string, int>();
        Dictionary<string, Squadron> squadronMap = new Dictionary<string, Squadron>();
        foreach (var entry in fleet.squadrons)
        {
            Squadron squadron = entry.Value;
            squadronMap[entry.Key] = squadron;

            power += squadron.cumulatedPower;
            if (squadron.speed < speed)
            {
                speed = squadron.speed;
            }
            if (squadron.range < minRange)
            {
                minRange = squadron.range;
            }
            if (squadron.range > maxRange)
            {
                maxRange = squadron.range;
            }
            foreach (var capacity in squadron.cumulatedCapacities)
            {
                if (capacities.ContainsKey(capacity.Key))
                {
                    capacities[capacity.Key] = capacities[capacity.Key] + capacity.Value;
                }
                else
                {
                    capacities[capacity.Key] = capacity.Value;
                }
            }
            amount += squadron.amount;
        }

        fleet.capacities = (capacities);
        fleet.power = (power);
        fleet.speed = ((speed == 1000) ? 0 : speed);
        fleet.staticConsumption = (staticConsummation);
        fleet.minRange = ((minRange == 1000) ? 0 : minRange);
        fleet.maxRange = (maxRange);
        fleet.consumption = (consummation);
        fleet.amount = (amount);
        fleet.cumulatedFuelTank = (cumulatedFuelTank);
        fleet.squadrons = (squadronMap);
        fleet.currentFuel = 1000000;
        fleet.fuelTime = 1000000;

    }
}




