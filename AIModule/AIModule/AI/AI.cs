using AIModule.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Bson;
using AIModule.Common.constant;
using System.Diagnostics;
using System.Threading.Channels;
using System.IO;
using AIModule.Fight;
using AIModule.Report;
using AIModule.Owners;
using AIModule.Bases;
using AIModule.Common.constant.fleet;
using System;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using AIModule.Quest;
using System.Numerics;
using AIModule.Reputation;
using AIModule.Common;
using System.Net.Mime;
using YamlDotNet.Core.Tokens;
using static MongoDB.Driver.WriteConcern;
using AIModule.Common.constant.owner;
using AIModule.Common.constant.Base;
using static AIModule.Common.constant.owner.Personality;
using System.Linq;
using MongoDB.Driver.Core.Bindings;
using YamlDotNet.Core.Events;
using AIModule.Planet;
using SharpCompress.Common;
using System.Collections.Immutable;
using static System.Net.WebRequestMethods;
using System.Collections;

namespace AIModule.AI
{
    [BsonIgnoreExtraElements]
    public abstract class AI : Owner
    {
        #region Properties

        /// <summary>
        /// Personalité de l'IA.
        /// Types de personalité définis dans la classe statique Personality dans le fichier Common/constant/owner/Personality.cs
        /// </summary>
        public string personality { get; set; }
        /// <summary>
        /// Identifiant d'alliance de l'IA
        /// </summary>
        public string allianceId { get; set; }
        /// <summary>
        /// Nom de l'alliance de l'IA
        /// </summary>
        public string allianceName { get; set; }
        /// <summary>
        /// Dictionnaire des templates de flotte de l'IA.
        /// Les types de clé sont définis dans la classe statique AIFleetType dans le fichier Common/constant/fleet/AIFleetTypes.cs
        /// </summary>
        public Dictionary<string, TemplateElement> fleetTemplates { get; set; } = new Dictionary<string, TemplateElement>();
        /// <summary>
        /// Dictionnaire des flottes actuelles de l'IA.
        /// Les types de clé sont définis dans la classe statique AIFleetType dans le fichier Common/constant/fleet/AIFleetTypes.cs
        /// </summary>
        public Dictionary<string, List<string>> fleetIdsGroupedByType { get; set; } = new Dictionary<string, List<string>>();
        /// <summary>
        /// Liste des identifiant d'alliances ennemies de l'IA
        /// </summary>
        public List<string> enemyAllianceIds { get; set; } = new List<string>();
        /// <summary>
        /// Liste des identifiants d'alliances alliées de l'IA
        /// </summary>
        public List<string> allyAllianceIds { get; set; } = new List<string>();
        /// <summary>
        /// Dictionnaire des flottes séparées de l'IA. Les flottes sont séparées afin de moduler la puissance d'attaque de la flotte IA.
        /// Les clés sont les identifiants des flottes parties en attaque, les valeurs sont les identifiants de la partie restante de la flotte resté à la base.
        /// </summary>
        public Dictionary<string, string> splitFleets { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// Liste des id des templates de quête de l'IA.
        /// Non utilisé pour l'instant
        /// </summary>
        public List<string> questTemplates { get; set; } = new List<string>();
        /// <summary>
        /// Puissance de la base IA
        /// </summary>
        public int aiBasePower { get; set; }

        #endregion

        #region Champs
        /// <summary>
        /// Date de dernière construction de flotte
        /// </summary>
        private DateTime lastFleetBuildTime;
        /// <summary>
        /// Date de dernier remplissage de flotte incomplète
        /// </summary>
        private DateTime lastFleetReplenishTime;
        /// <summary>
        /// Ressource préférée de l'IA. Non utilisé pour l'instant
        /// </summary>
        private string preferedResource;
        /// <summary>
        /// Date de dernier sommeil de l'IA
        /// </summary>
        private DateTime lastSleepTime;
        /// <summary>
        /// Dictionnaire des joueurs que l'IA considère, et qui contient notamment les différentes probabilités d'interactions de l'IA avec ce joueur.
        /// </summary>
        private Dictionary<string, Dictionary<PlayerElement, Object>> playersDictionary = new Dictionary<string, Dictionary<PlayerElement, object>>();

        private int maxPowerAttackFleet = 0;
        private int maxPowerDefenseFleet = 0;
        private int maxPowerDeliveryFleet = 0;
        private int maxPowerTurretFleet = 0;

        private Dictionary<string, int> storageCapacities = new Dictionary<string, int>();
        #endregion

        protected AI()
        {
            lastFleetBuildTime = DateTime.Now;
            lastFleetReplenishTime = DateTime.Now;
            var rand = new Random();
            preferedResource = Values.ressources[rand.Next(4)];
            _class = "ai";
        }



        /// <summary>
        /// Méthode permettant d'exécuter le comportement de l'IA.
        /// </summary>
        /// <param name="factory">
        /// ConnectionFactory du RabbitMQ du jeu
        /// </param>
        public virtual void runAI(ConnectionFactory factory)
        {
            lastSleepTime = DateTime.Now;

            Console.WriteLine(name);

            //Initialisation liste joueurs avec lesquels interagir
            foreach (var player in MongoDBSingleton.Instance().usersCollection.AsQueryable().Where(x => x.type == OwnerTypeConstant.PLAYER).Select(x => x.id))
            {
                TryAddPlayerToConsideredPlayers(player);
            }


            maxPowerAttackFleet = fleetTemplates[AIFleetTypes.attackFleet].maxFleetNumber * fleetTemplates[AIFleetTypes.attackFleet].fleetPower;

            maxPowerDefenseFleet = fleetTemplates[AIFleetTypes.defenseFleet].maxFleetNumber * fleetTemplates[AIFleetTypes.defenseFleet].fleetPower;

            maxPowerTurretFleet = fleetTemplates[AIFleetTypes.turretFleet].maxFleetNumber * fleetTemplates[AIFleetTypes.turretFleet].fleetPower;

            maxPowerDeliveryFleet = fleetTemplates[AIFleetTypes.deliveryFleet].maxFleetNumber * fleetTemplates[AIFleetTypes.deliveryFleet].fleetPower;


            var aifilter = Builders<Base>.Filter.Eq(x => x.id, mainBaseId);
            var aibase = MongoDBSingleton.Instance().basesCollection.Find(aifilter).First();
            foreach (KeyValuePair<string, RessourceCumul> keyValuePair in aibase.ressources)
            {
                storageCapacities.Add(keyValuePair.Key, keyValuePair.Value.storage);
            }

            Random rand = new Random();

            Event? receivedEvent;
            EventResult? eventResult;
            int eventCount = 0;
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "aiEventsExchange",
                                        durable: true,
                                        type: ExchangeType.Direct);

                var queueName = channel.QueueDeclare().QueueName;

                channel.QueueDeclare(queue: "toSchedule",
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

                channel.QueueBind(queue: queueName,
                    exchange: "aiEventsExchange",
                    routingKey: "event");

                var queueName2 = channel.QueueDeclare().QueueName;

                channel.QueueBind(queue: queueName2,
                    exchange: "aiEventsExchange",
                    routingKey: "eventResult");

                var properties = channel.CreateBasicProperties();
                properties.ContentType = "application/json";
                properties.Headers = new Dictionary<string, object>();
                properties.Headers.Add("__TypeId__", "fr.need_a_name.bdd_module.event.entity.Event");
                properties.DeliveryMode = 2;
                properties.Priority = 0;
                properties.ContentEncoding = "UTF-8";

                var eventConsumer = new EventingBasicConsumer(channel);
                eventConsumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    receivedEvent = JsonSerializer.Deserialize<Event>(message);
                    if (receivedEvent != null)
                    {
                        //Console.WriteLine(message);
                        // Console.WriteLine("received event of type {0}", receivedEvent.type);

                        //Console.WriteLine(" [x] Received {0}", message);


                        //Traitement des events reçus ici
                        switch (receivedEvent.type)
                        {
                            case EventTypeConstant.MOVE:
                                {
                                    try
                                    {
                                        //Il est possible que le temps qu'on aie l'information que l'Event est en base de donnée, celui-ci soit déjà terminé. Il faut donc tenir compte de cette
                                        //possibilité
                                        var moveEvent = MongoDBSingleton.Instance().moveEventsCollection.Find(Builders<FleetMoveEvent>.Filter.Eq(x => x.id, receivedEvent.id)).FirstOrDefault();
                                        if (moveEvent != null && Enum.Parse<FleetObjectives>(moveEvent.objectif) == FleetObjectives.ATTACK)
                                        {
                                            var aiBasePosition = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.id, mainBaseId)).First().position;
                                            var interventionProb = rand.Next(1001) / 1000f;
                                            //On cherche un allié ou un ennemi dans la liste des concernés
                                            var allies = (from p in moveEvent.userIds
                                                          where allyIds.Contains(p)
                                                          select p).ToList();
                                            //Si on trouve un allié de chaque côté, l'IA n'intervient pas
                                            if (allies.Count == 1)
                                            {
                                                var defenderBase = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.position, moveEvent.location)).First();
                                                var attackerBase = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.position, moveEvent.origine)).First();
                                                if (interventionProb < Values.helpDefendProbability && allies[0] == defenderBase.ownerId)
                                                {
                                                    //Chance d'aider le défenseur en envoyant une flotte en soutien
                                                    //Voir avec Mikrowd si après le combat en soutien, flotte revient
                                                    var attackerId = (from i in moveEvent.userIds where i != allies[0] select i).ToList()[0];
                                                    var attacker = MongoDBSingleton.Instance().usersCollection.Find(Builders<User>.Filter.Eq(x => x.id, attackerId)).First();
                                                    var defender = MongoDBSingleton.Instance().usersCollection.Find(Builders<User>.Filter.Eq(x => x.id, allies[0])).First();
                                                    TryLaunchDefendFleet(aiBasePosition, moveEvent.fleetId, attacker, defender, defenderBase, channel, properties);
                                                    goto AckEvent;
                                                }
                                                else if (interventionProb < Values.helpAttackProbability && allies[0] == attackerBase.ownerId)
                                                {
                                                    //Chance d'aider l'attaquant en envoyant une flotte d'attaque
                                                    var targetId = (from i in moveEvent.userIds where i != allies[0] select i).ToList()[0];
                                                    var target = MongoDBSingleton.Instance().usersCollection.Find(Builders<User>.Filter.Eq(x => x.id, targetId)).First();
                                                    TryLaunchAttackFleet(aiBasePosition, target, rand, channel, properties, true);
                                                    goto AckEvent;
                                                }
                                            }
                                            //Si on trouve un ennemi de chaque côté, l'IA n'intervient pas
                                            var enemies = (from p in moveEvent.userIds
                                                           where enemyIds.Contains(p)
                                                           select p).ToList();
                                            if (enemies.Count == 1)
                                            {
                                                var defenderBase = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.position, moveEvent.location)).First();
                                                var attackerBase = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.position, moveEvent.origine)).First();
                                                if (interventionProb < Values.helpAttackProbability && enemies[0] == defenderBase.ownerId)
                                                {
                                                    //Chance d'attaquer le défenseur
                                                    //Voir avec Mikrowd si après le combat en soutien, flotte revient
                                                    var target = MongoDBSingleton.Instance().usersCollection.Find(Builders<User>.Filter.Eq(x => x.id, enemies[0])).First();
                                                    TryLaunchAttackFleet(aiBasePosition, target, rand, channel, properties, true);
                                                    goto AckEvent;
                                                }
                                                else if (interventionProb < Values.helpDefendProbability && enemies[0] == attackerBase.ownerId)
                                                {
                                                    //Chance d'aider le défenseur
                                                    var defenderId = (from i in moveEvent.userIds where i != enemies[0] select i).ToList()[0];
                                                    var defender = MongoDBSingleton.Instance().usersCollection.Find(Builders<User>.Filter.Eq(x => x.id, defenderId)).First();
                                                    var attacker = MongoDBSingleton.Instance().usersCollection.Find(Builders<User>.Filter.Eq(x => x.id, enemies[0])).First();
                                                    TryLaunchDefendFleet(aiBasePosition, moveEvent.fleetId, attacker, defender, defenderBase, channel, properties);
                                                    goto AckEvent;
                                                }
                                            }

                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Utilies.WriteErrorLog(e, this);
                                    }
                                    break;
                                }



                            //Traiter les échanges, demande de troupes, pillage de convois (?)
                            default:
                                break;
                        }
                    }
                AckEvent:
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                var eventResultConsumer = new EventingBasicConsumer(channel);
                eventResultConsumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var eventResult = JsonSerializer.Deserialize<EventResult>(message);

                    try
                    {
                        //Traitement des events reçus ici
                        switch (eventResult.eventType)
                        {

                            case EventTypeConstant.MOVE:
                                {
                                    Utilies.WriteEventResultReceivedLog(eventResult, this);
                                    ManageMoveEventResult(eventResult);
                                    break;
                                }

                            case EventTypeConstant.FIGHT:
                                {
                                    Utilies.WriteEventResultReceivedLog(eventResult, this);
                                    ManageFightEventResult(eventResult, rand, channel, properties);
                                    break;
                                }

                            case EventTypeConstant.QUEST:
                                {
                                    Utilies.WriteEventResultReceivedLog(eventResult, this);
                                    ManageQuestEventResult(eventResult);
                                    break;
                                }

                            case EventTypeConstant.NEW_USER:
                                {
                                    Utilies.WriteEventResultReceivedLog(eventResult, this);
                                    ManageNewUserEventResult(eventResult);
                                    break;
                                }
                            //Traiter les échanges, demande de troupes, pillage de convois (?)
                            default:
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Utilies.WriteErrorLog(e, this);
                    }

                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: eventConsumer);

                channel.BasicConsume(queue: queueName2,
                                 autoAck: false,
                                 consumer: eventResultConsumer);



                while (true)
                {
                    AIBehaviorLoop(rand, channel, properties);
                }
            }
        }

        /// <summary>
        /// Pour un joueur donné, regarde si peut effectuer une actions avec lui ou non. Met ensuite à jour la liste des joueurs à considérer et met à jour les
        /// flottes de l'IA
        /// </summary>
        /// <param name="rand"> Random </param>
        /// <param name="player"> identifiant du joueur </param>
        /// <param name="channel"> Channel pour publier un Event RabbitMQ </param>
        /// <param name="properties"> propriétés pour le formatage correct du message RabbitMQ </param>
        /// <param name="reputation"> valeur de la réputation du joueur </param>
        /// <param name="mainBase"> base principale de l'IA </param>
        public void ChooseAndExecuteAction(Random rand, string player, IModel channel, IBasicProperties properties, float reputation, Base mainBase)
        {
            //On met à jour les flottes de l'IA
            updateFleets();
            //On récupère le statut du joueur vis-à-vis de l'IA
            ReputationStatus playerReputationStatus = Utilies.getPlayerReputationStatus(reputation);

            var playerFilter = Builders<User>.Filter.Eq(x => x.id, player);
            var playerOwner = MongoDBSingleton.Instance().usersCollection.Find(playerFilter).First();
            var playerBaseFilter = Builders<Base>.Filter.Eq(x => x.id, playerOwner.mainBaseId);
            var playerMainBase = MongoDBSingleton.Instance().basesCollection.Find(playerBaseFilter).First();

            //On regarde si on peut attaquer le joueur ou non
            ChooseAndExecuteAttack(rand, player, channel, properties, mainBase, playerOwner, playerReputationStatus);
            //On regarde si on donner une quête au joueur
            ChooseAndExecuteQuest(rand, player, playerReputationStatus);
            //On regarde si on peut donner un cadeau au joueur
            ChooseAndExecuteGift(rand, player, channel, properties, mainBase, playerOwner, playerReputationStatus, reputation);

            //On met à jour la liste des joueurs à considérer
            UpdateAlliesEnemies(player, reputation);
            //On met à jour les flottes de l'IA
            updateFleets();
        }

        /// <summary>
        /// Vérifie si on peut attaquer le joueur ou non. Si oui, alors on créée un FleetMoveEvent d'attaque. Si non, on augmente la probabilité de l'attaquer.
        /// </summary>
        /// <param name="rand"> Random</param>
        /// <param name="player"> Identifiant du joueur </param>
        /// <param name="channel"> Channel pour publier un Event RabbitMQ </param>
        /// <param name="properties"> propriétés pour le formatage correct du message RabbitMQ </param>
        /// <param name="mainBase"> base principale de l'IA</param>
        /// <param name="playerOwner"> Owner ayant pour id player</param>
        /// <param name="playerReputationStatus"> Statut du joueur vis à vis de l'IA </param>
        private void ChooseAndExecuteAttack(Random rand, string player, IModel channel, IBasicProperties properties, Base mainBase, User playerOwner, ReputationStatus playerReputationStatus)
        {
            //On fait un tirage aléatoire sur une distribution uniforme
            float attack_prob = rand.Next(1001) / 1000f;

            //Si on peut attaquer le joueur, alors on essaye de lancer une flotte d'attaque
            if (attack_prob < Convert.ToDouble(playersDictionary[player][PlayerElement.AttackProbabilityBonus]) &&
                fleetListId.Count > 0 &&
                DateTime.Now.CompareTo(((DateTime)playersDictionary[player][PlayerElement.LastAttackTime]).AddSeconds(Values.attackCooldown)) > 0)
            {
                TryLaunchAttackFleet(mainBase.position, playerOwner, rand, channel, properties, false);
            }
            //Sinon, on augmente la probabilité d'attaque du joueur, sans dépasser un certain seuil
            else
            {
                playersDictionary[player][PlayerElement.AttackProbabilityBonus] = Math.Min(Math.Max(0,
                    Convert.ToDouble(playersDictionary[player][PlayerElement.AttackProbabilityBonus]) +
                    Values.InteractionsProbabilitiesInformations[(personality, InteractionType.Attack, InteractionInformation.ProbabilityBonus, playerReputationStatus)]),
                    Values.InteractionsProbabilitiesInformations[(personality, InteractionType.Attack, InteractionInformation.MaximumProbability, playerReputationStatus)]);
            }
        }

        /// <summary>
        /// Vérifie si l'on peut donner une quête au joueur ou non. Si oui, alors on créé une nouvelle quête et on la donne au joueur. Sinon, on augmente la probabilité de 
        /// donner une quête sans dépasser un seuil.
        /// </summary>
        /// <param name="rand"> Random </param>
        /// <param name="player"> identifiant du joueur </param>
        /// <param name="playerReputationStatus"> statut du joueur vis-à-vis de l'IA </param>
        private void ChooseAndExecuteQuest(Random rand, string player, ReputationStatus playerReputationStatus)
        {
            //On fait un tirage aléatoire sur une distribution uniforme
            float quest_prob = rand.Next(1001) / 1000f;
            var user = MongoDBSingleton.Instance().usersCollection.Find(Builders<User>.Filter.Eq(x => x.id, player)).First();
            //Si on peut donner une quête au joueur
            if (quest_prob < Convert.ToDouble(playersDictionary[player][PlayerElement.QuestProbabilityBonus]) && user.aiQuestsQuantity < Values.playerMaxAiQuestNumber)
            {
                var playerMainBaseId = MongoDBSingleton.Instance().usersCollection.Find(Builders<User>.Filter.Eq(x => x.id, player)).First().mainBaseId;
                var playerMainBase = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.id, playerMainBaseId)).First();


                var objective = rand.Next(1, 4);
                //On créé une nouvelle quête
                var waitingQuest = new WaitingQuest();
                waitingQuest.id = null;
                waitingQuest.passiveObjectives = null;
                waitingQuest.name = string.Format("AI {0} quest", name);
                waitingQuest.isFromAi = true;
                var activeObjectives = new QuestActiveObjective();
                waitingQuest.activesObjectives = new List<QuestActiveObjective>();
                //Selon la personalité de l'IA, les objectifs de la quête sont différents
                switch (personality)
                {
                    //On créé une quête de fouille
                    case Pacifist:
                        waitingQuest.description = string.Format("Le NPC {0} vous demande de faire des fouilles.", name);
                        activeObjectives.type = EventResultTypeConstants.SEARCH_SIZE;
                        activeObjectives.description = string.Format("Fouillez {0} cases", objective);
                        //Comment définir les objectifs actifs ? => voir avec Mikrowd
                        activeObjectives.objectives = new Dictionary<string, int>()
                        {
                            { "",objective },
                        };
                        waitingQuest.activesObjectives.Add(activeObjectives);
                        activeObjectives.isOptional = false;
                        break;
                    //On créé une quête de livraison
                    case Trader:
                        int amount = (int)(playerMainBase.ressources.Average(x => x.Value.production) * 0.05f);
                        waitingQuest.description = string.Format("Le NPC {0} vous demande de livrer des ressources.", name);
                        activeObjectives.type = EventResultTypeConstants.DELIVERED_RESOURCES;
                        activeObjectives.description = string.Format("Livrez {0} de chaque ressource", objective * amount);
                        //Comment définir les objectifs actifs ? => voir avec Mikrowd
                        activeObjectives.objectives = new Dictionary<string, int>()
                        {
                            { RessourcesConstant.METAL,objective * amount },
                            { RessourcesConstant.ENERGY,objective * amount },
                            { RessourcesConstant.ORGANIC,objective * amount },
                            { RessourcesConstant.CRISTAL,objective * amount },
                        };
                        waitingQuest.activesObjectives.Add(activeObjectives);
                        activeObjectives.isOptional = false;
                        break;
                    //On créé une quête de pillages
                    case Warmonger:
                        int power = 0;
                        foreach (var key in playerMainBase.slots.Keys)
                        {
                            if (playerMainBase.slots[key].built != null)
                            {
                                switch (playerMainBase.slots[key].built.building.name)
                                {
                                    case "Chantier spatial":
                                        power += 2 * playerMainBase.slots[key].built.level;
                                        break;
                                    case "HQ":
                                        power += (int)Math.Ceiling(playerMainBase.slots[key].built.level / (float)2);
                                        break;
                                    case "Laboratoire":
                                        power += playerMainBase.slots[key].built.level;
                                        break;
                                }
                            }
                        }
                        waitingQuest.description = string.Format("Le NPC {0} vous demande de piller des ressources", name);
                        activeObjectives.type = EventResultTypeConstants.PILLED_RESOURCES;
                        activeObjectives.description = string.Format("Pillez {0} de chaque ressource.", objective * power * 500);
                        //Comment définir les objectifs actifs ? => voir avec Mikrowd
                        activeObjectives.objectives = new Dictionary<string, int>()
                        {
                            { RessourcesConstant.METAL,objective * power * 500 },
                            { RessourcesConstant.ENERGY,objective * power * 500 },
                            { RessourcesConstant.ORGANIC,objective * power * 500 },
                            { RessourcesConstant.CRISTAL,objective * power * 500 },
                        };
                        waitingQuest.activesObjectives.Add(activeObjectives);
                        activeObjectives.isOptional = false;
                        break;

                }
                waitingQuest.userId = new List<string>()
                {
                    player
                };
                waitingQuest.sponsorId = id;
                waitingQuest.isRejectable = true;
                waitingQuest.templateUuid = Guid.NewGuid().ToString();
                waitingQuest.questRewards = new List<QuestReward>();
                //On ajoute de la réputation dans les récompenses
                var questReward = new QuestReward();
                questReward.entityId = id;
                questReward.type = QuestRewardConstant.FAME;
                questReward.value = 5;
                waitingQuest.questRewards.Add(questReward);
                int quantity = 0;
                //On ajoute des ressources en récompense, proportionelles à la production du joueur
                foreach (var resource in playerMainBase.ressources.Keys)
                {
                    quantity = (int)(playerMainBase.ressources[resource].production * 0.1f);
                    waitingQuest.questRewards.Add(new QuestReward(QuestRewardConstant.RESOURCE, resource, quantity));
                }
                //On ajoute la quête en BDD
                if (user.aiQuestsQuantity < Values.playerMaxAiQuestNumber)
                {
                    MongoDBSingleton.Instance().waitingQuestsCollection.InsertOne(waitingQuest);

                    //On met à jour la quantité de quêtes IA que possède le joueur
                    var playerUpdate = Builders<User>.Update.Set(x => x.aiQuestsQuantity, user.aiQuestsQuantity + 1);
                    MongoDBSingleton.Instance().usersCollection.UpdateOne(Builders<User>.Filter.Eq(x => x.id, player), playerUpdate);


                    Utilies.WriteEventCreationLog("QuestEvent", waitingQuest.id, this);
                    //On réinitialise le bonus d'interaction pour les quêtes pour le joueur donné
                    playersDictionary[player][PlayerElement.QuestProbabilityBonus] = 0;
                }
                
            }
            //Sinon, on augmente la probabilité d'interaction pour les quêtes pour ce joueur, sans dépasser un seuil donné
            else
            {
                playersDictionary[player][PlayerElement.QuestProbabilityBonus] = Math.Min(Math.Max(0,
                    Convert.ToDouble(playersDictionary[player][PlayerElement.QuestProbabilityBonus]) +
                    Values.InteractionsProbabilitiesInformations[(personality, InteractionType.Quest, InteractionInformation.ProbabilityBonus, playerReputationStatus)]),
                    Values.InteractionsProbabilitiesInformations[(personality, InteractionType.Quest, InteractionInformation.MaximumProbability, playerReputationStatus)]);
            }
        }

        /// <summary>
        ///  Vérifie si l'on peut donner un cadeau au joueur ou non. Si oui, alors on envoie un FleetMoveEvent de livraison. Sinon, on augmente la probabilité de donner
        ///  un cadeau au joueur, sans dépasser un seuil donné.
        /// </summary>
        /// <param name="rand"> Random </param>
        /// <param name="player"> Identifiant du joueur </param>
        /// <param name="channel"> Channel pour publier un Event RabbitMQ </param>
        /// <param name="properties"> propriétés pour le formatage correct du message RabbitMQ </param>
        /// <param name="mainBase"> Base principale de l'IA </param>
        /// <param name="playerOwner"> Owner ayant pour id player </param>
        /// <param name="playerReputationStatus"> statut du joueur vis à vis de l'IA </param>
        /// <param name="reputation"> valeur de la réputation du joueur pour cette IA </param>
        private void ChooseAndExecuteGift(Random rand, string player, IModel channel, IBasicProperties properties, Base mainBase, User playerOwner, ReputationStatus playerReputationStatus, float reputation)
        {
            var playerBaseFilter = Builders<Base>.Filter.Eq(x => x.id, playerOwner.mainBaseId);
            var playerMainBase = MongoDBSingleton.Instance().basesCollection.Find(playerBaseFilter).First();
            FleetMoveEvent? moveEvent = null;

            //On fait un tirage aléatoire sur une distribution uniforme
            float gift_prob = rand.Next(1001) / 1000f;

            //Si on peut donner un cadeau au joueur
            if (gift_prob < Convert.ToDouble(playersDictionary[player][PlayerElement.GiftProbabilityBonus]))
            {
                //La valeur du cadeau dépend de la réputation du joueur avec l'IA et de la capacité de stockage max de l'IA
                var gift = new Dictionary<string, int>()
                    {
                        {RessourcesConstant.CRISTAL, (int)(0.1*(Math.Max(Utilies.getReputationRatio(reputation) - Values.hostileThresholdFactor,0)) * mainBase.ressources[RessourcesConstant.CRISTAL].storage) },
                        {RessourcesConstant.METAL, (int)(0.1*(Math.Max(Utilies.getReputationRatio(reputation) - Values.hostileThresholdFactor,0)) * mainBase.ressources[RessourcesConstant.METAL].storage) },
                        {RessourcesConstant.ORGANIC, (int)(0.1*(Math.Max(Utilies.getReputationRatio(reputation) - Values.hostileThresholdFactor,0)) * mainBase.ressources[RessourcesConstant.ORGANIC].storage) },
                        {RessourcesConstant.ENERGY, (int)(0.1*(Math.Max(Utilies.getReputationRatio(reputation) - Values.hostileThresholdFactor,0)) * mainBase.ressources[RessourcesConstant.ENERGY].storage) }
                    };

                //On parcourt les flottes de livraison jusqu'à en trouver une disponible
                for (int i = 0; i < fleetIdsGroupedByType[AIFleetTypes.deliveryFleet].Count; i++)
                {
                    var fleetFilter = Builders<Fleet>.Filter.Eq(x => x.id, fleetIdsGroupedByType[AIFleetTypes.deliveryFleet][i]);
                    //Si la flotte n'existe plus, on la supprime de la liste des flottes
                    if (MongoDBSingleton.Instance().fleetsCollection.Find(fleetFilter).CountDocuments() == 0)
                    {
                        fleetListId.Remove(fleetIdsGroupedByType[AIFleetTypes.deliveryFleet][i]);
                        //On met ensuite à jour la liste des flottes de l'IA en BDD
                        if (type == OwnerTypeConstant.AI_DELTA)
                        {
                            var updateOwnerFleets = Builders<AIDelta>.Update.Set(x => x.fleetListId, fleetListId);
                            MongoDBSingleton.Instance().aiDeltaCollection.UpdateOne(Builders<AIDelta>.Filter.Eq(x => x.id, id), updateOwnerFleets);

                        }
                        else
                        {
                            var updateOwnerFleets = Builders<AIGamma>.Update.Set(x => x.fleetListId, fleetListId);
                            MongoDBSingleton.Instance().aiGammaCollection.UpdateOne(Builders<AIGamma>.Filter.Eq(x => x.id, id), updateOwnerFleets);
                        }
                    }
                    else
                    {
                        //On récupère la flotte en BDD
                        var fleet = MongoDBSingleton.Instance().fleetsCollection.Find(fleetFilter).First();
                        bool fleetUsable = fleet.status != FleetStatusConstant.MOVING && fleet.status != FleetStatusConstant.FIGHTING && fleet.status != FleetStatusConstant.TRANSITING;
                        //Si la flotte n'est pas en déplacement, pas en combat et pas en transition, alors elle est utilisable
                        //Remarque : comme les flottes rentrent automatiquement après une livraison, on est assuré que les flottes de livraison qui ne se déplacent pas sont sur la base IA
                        if (fleetUsable)
                        {
                            //On remplit les soutes de chaque Squadron tant qu'il reste des ressources à mettre
                            foreach (var key in fleet.squadrons.Keys)
                            {
                                //La quantité de ressources que l'on met dans l'escouade est le minimum entre la valeur de stockage max de l'escouad et la valeur à donner
                                int quantity = Math.Min(fleet.squadrons[key].cumulatedStorageCapacity, gift.Values.Sum());
                                fleet.squadrons[key].currentStorage = quantity;
                                //On remplit d'une quantité égale des 4 ressources
                                fleet.squadrons[key].storage = new Dictionary<string, int>()
                                    {
                                        {RessourcesConstant.ORGANIC, quantity/4 },
                                        {RessourcesConstant.METAL, quantity/4 },
                                        {RessourcesConstant.CRISTAL, quantity/4 },
                                        {RessourcesConstant.ENERGY, quantity/4 }
                                    };
                                //On met ensuite à jour les ressources de la base IA
                                foreach (var res in gift.Keys)
                                {
                                    gift[res] -= quantity / 4;
                                    mainBase.ressources[res].addAndRefresh(-1 * quantity / 4);
                                }
                                //Mettre à jour les squadrons en BDD
                                var fleetUpdate = Builders<Fleet>.Update.Set(x => x.squadrons[key], fleet.squadrons[key]);
                                MongoDBSingleton.Instance().fleetsCollection.UpdateOne(fleetFilter, fleetUpdate);
                                //Pour éviter de se retrouver bloquer dans des erreurs d'arrondi
                                if (gift.Values.Sum() < 4)
                                    break;
                            }
                            //On créé le FleetMoveEvent de livraison
                            moveEvent = MoveManager.GetInstance().launchMove(fleet, playerMainBase.position, mainBase.position, FleetObjectives.DELIVERY, "AI " + name + " sends resources to player", 0);
                            Utilies.SendEvent(moveEvent, channel, properties);
                            break;
                        }
                    }
                }
                //Si on a réussi à envoyer une flotte de livraison
                if (moveEvent != null)
                {
                    Utilies.WriteEventCreationLog("MoveEvent", moveEvent.id, this);
                    //On remet à 0 le bonus de probabilité de cadeau
                    playersDictionary[player][PlayerElement.GiftProbabilityBonus] = 0;
                    moveEvent = null;
                }
            }
            //Sinon, on augmente la probabilité de faire un cadeau au joueur, sans dépasser un certain seuil.
            else
            {
                playersDictionary[player][PlayerElement.GiftProbabilityBonus] = Math.Min(Math.Max(0,
                    Convert.ToDouble(playersDictionary[player][PlayerElement.GiftProbabilityBonus]) +
                    Values.InteractionsProbabilitiesInformations[(personality, InteractionType.Gift, InteractionInformation.ProbabilityBonus, playerReputationStatus)]),
                    Values.InteractionsProbabilitiesInformations[(personality, InteractionType.Gift, InteractionInformation.MaximumProbability, playerReputationStatus)]);
            }
            var baseUpdate = Builders<Base>.Update.Set(x => x.ressources, mainBase.ressources);
            MongoDBSingleton.Instance().basesCollection.UpdateOne(Builders<Base>.Filter.Eq(x => x.id, mainBaseId), baseUpdate);
        }

        /// <summary>
        /// Prix minimum à payer pour une transaction; dépend de la réputation avec l'IA; varie entre 2 fois et 1/2 fois le prix du marché.
        ///Si le prix total de l'échange proposé par le joueur est inférieur au prix demandé par l'IA, alors l'échange doit être refusé.
        ///A l'inverse, si le prix proposé par le joueur est > au prix demandé, alors l'échange est accepté et la différence de valeur est convertie en
        ///gain de réputation.
        ///Note : cette méthode n'est pas utilisée pour l'instant, mais sera utile dans le futur lorsque le commerce sera implémenté
        /// </summary>
        /// <param name="cristal"> Quantité de cristal offerte par le joueur pour la transaction </param>
        /// <param name="organic"> Quantité d'organic offerte par le joueur pour la transaction </param>
        /// <param name="metal"> Quantité de metal offerte par le joueur pour la transaction</param>
        /// <param name="energy"> Quantité d'energy offerte par le joueur pour la transaction </param>
        /// <param name="reputation"></param>
        /// <returns></returns>
        private float PriceForTransaction(int cristal, int organic, int metal, int energy, float reputation)
        {
            var baseFilter = Builders<Base>.Filter.Eq(x => x.id, mainBaseId);
            var Base = MongoDBSingleton.Instance().basesCollection.Find(baseFilter).First();
            var totalValue = Base.ressources[RessourcesConstant.METAL].amount * Values.marketPrices[RessourcesConstant.METAL] + Base.ressources[RessourcesConstant.ORGANIC].amount * Values.marketPrices[RessourcesConstant.ORGANIC] +
                Base.ressources[RessourcesConstant.CRISTAL].amount * Values.marketPrices[RessourcesConstant.CRISTAL] + Base.ressources[RessourcesConstant.ENERGY].amount * Values.marketPrices[RessourcesConstant.ENERGY];

            var preferedFactor = preferedResource.Equals(RessourcesConstant.METAL) ? Values.ressourcePreferenceFactor : 1f;
            var metalPrice = Values.marketPrices[RessourcesConstant.METAL] * preferedFactor * (float)Math.Exp(-1 * Values.ressourceSaturationFactor * Base.ressources[RessourcesConstant.METAL].amount / totalValue);

            preferedFactor = preferedResource.Equals(RessourcesConstant.CRISTAL) ? Values.ressourcePreferenceFactor : 1f;
            var cristalPrice = Values.marketPrices[RessourcesConstant.CRISTAL] * preferedFactor * (float)Math.Exp(-1 * Values.ressourceSaturationFactor * Base.ressources[RessourcesConstant.CRISTAL].amount / totalValue);

            preferedFactor = preferedResource.Equals(RessourcesConstant.ENERGY) ? Values.ressourcePreferenceFactor : 1f;
            var energyPrice = Values.marketPrices[RessourcesConstant.ENERGY] * preferedFactor * (float)Math.Exp(-1 * Values.ressourceSaturationFactor * Base.ressources[RessourcesConstant.ENERGY].amount / totalValue);

            preferedFactor = preferedResource.Equals(RessourcesConstant.ORGANIC) ? Values.ressourcePreferenceFactor : 1f;
            var organicPrice = Values.marketPrices[RessourcesConstant.ORGANIC] * preferedFactor * (float)Math.Exp(-1 * Values.ressourceSaturationFactor * Base.ressources[RessourcesConstant.ORGANIC].amount / totalValue);

            float totalPrice = metalPrice * metal + organicPrice * organic + energyPrice * energy + cristalPrice * cristal;
            float rep = (Values.maxReputation - reputation) / (Values.maxReputation - Values.minReputation);
            totalPrice = (4 / 3 - rep) * 3 / 2 * totalPrice;
            return totalPrice;
        }

        /// <summary>
        /// Remplis une flotte lorsque celle-ci tombe en dessous d'un certain pourcentage de sa puissance maximale.
        /// </summary>
        /// <param name="fleet"> Flotte </param>
        /// <param name="template"> Template de la flotte </param>
        /// <param name="fleetType"> Type de flotte </param>
        private void replenishFleet(Fleet fleet, Fleet template, string fleetType)
        {
            fleet.power = 0;
            fleet.squadrons = new Dictionary<string, Squadron>();
            //On remplace toutes les escouades par les escouades du template
            foreach (var (squadron, newTechId) in from squadron in template.squadrons
                                                  let newTechId = Guid.NewGuid().ToString()
                                                  select (squadron, newTechId))
            {
                squadron.Value.fleetId = fleet.id;
                squadron.Value.techId = newTechId;
                fleet.squadrons[newTechId] = squadron.Value;
                fleet.power += squadron.Value.cumulatedPower;
            }
            //Dans le cas où la flotte n'est pas une flotte de livraison et pas une flotte de défense
            //On remplis la flotte jusqu'à un certain pourcentage de sa puissance maximale, qui dépend de la personalité
            if (fleetType != AIFleetTypes.deliveryFleet && fleetType != AIFleetTypes.turretFleet)
            {
                
                float threshold = Values.fleetReplenishThreshold[(int)Enum.Parse<PersonalityEnum>(personality)];
                //On vide les escouades pour garder un pourcentage des vaisseaux
                for (int i = 0; i < fleet.squadrons.Count; i++)
                {
                    var key = fleet.squadrons.Keys.ToList()[i];
                    fleet.squadrons[key].amount = (int)Math.Ceiling(threshold * fleet.squadrons[key].amount);
                    fleet.squadrons[key].cumulatedPower = (int)Math.Ceiling(threshold * fleet.squadrons[key].cumulatedPower);
                }
            }
            //On met à jour la flotte en BDD
            var update = Builders<Fleet>.Update.Set(x => x.squadrons, fleet.squadrons);
            MongoDBSingleton.Instance().fleetsCollection.UpdateOne(Builders<Fleet>.Filter.Eq(x => x.id, fleet.id), update);

            Utilies.WriteEventCreationLog("ReplenishFleet", fleet.id, this);
        }

        /// <summary>
        /// Création d'une flotte pour l'IA
        /// </summary>
        /// <param name="fleetTemplate"> Nom du template de la flotte à créer</param>
        /// <param name="fleetType"> Type de la flotte </param>
        /// <returns></returns>
        private Fleet createFleet(string fleetTemplate, string fleetType)
        {
            //On récupère le template en BDD
            var builder = Builders<Fleet>.Filter;
            var fleetFilter = builder.Eq("name", fleetTemplate) & builder.Eq("type", FleetTypeConstant.TEMPLATE);
            Fleet templ = MongoDBSingleton.Instance().fleetsCollection.Find(fleetFilter).First();
            Fleet fleet = templ;
            fleet.id = null;
            fleet.name = string.Format("{0}'s fleet", name);
            //Si la flotte est une flotte de défense ou de tourelles, alors elle patrouille
            if (fleetType == AIFleetTypes.defenseFleet || fleetType == AIFleetTypes.turretFleet)
            {
                fleet.status = FleetStatusConstant.PATROLLING;
            }
            //Si la flotte est une flotte de livraison, alors est reste en standby
            if (fleetType == AIFleetTypes.deliveryFleet)
            {
                fleet.status = FleetStatusConstant.STANDBY;
            }
            //Sinon, son statut dépend de la personalité de l'IA
            else
            {
                switch (personality)
                {
                    case Pacifist:
                        fleet.status = FleetStatusConstant.INACTIVE;
                        break;
                    case Warmonger:
                        fleet.status = FleetStatusConstant.PATROLLING;
                        break;
                    case Trader:
                        fleet.status = FleetStatusConstant.STANDBY;
                        break;
                }
            }

            var baseFilter = Builders<Base>.Filter.Eq(x => x.id, mainBaseId);

            fleet.position = MongoDBSingleton.Instance().basesCollection.Find(baseFilter).First().position;
            fleet.type = FleetTypeConstant.NPC;
            fleet.ownerName = name;
            fleet.ownerId = id;
            fleet.linkedBaseId = mainBaseId;

            //On ajoute un taux de pillage pour la flotte IA
            fleet.pillageRate = new Dictionary<string, int>
            {
                { RessourcesConstant.CRISTAL, 25 },
                { RessourcesConstant.ORGANIC, 25 },
                { RessourcesConstant.METAL, 25 },
                { RessourcesConstant.ENERGY, 25 }
            };
            //On insère la flotte en BDD afin de récupérer un id
            MongoDBSingleton.Instance().fleetsCollection.InsertOne(fleet);

            Dictionary<string, Squadron> newSquad = new Dictionary<string, Squadron>();
            //On créé la flotte de manière similaire à ce qui est faite dans les autres modules
            foreach (var squadron in fleet.squadrons)
            {
                string newTechId = Guid.NewGuid().ToString();
                squadron.Value.fleetId = fleet.id;
                squadron.Value.techId = newTechId;
                newSquad[newTechId] = squadron.Value;
            }
            fleet.squadrons = newSquad;

            //On ajoute une consommation et une quantité d'essence afin de permettre le bon fonctionnement du jeu
            int power = 0;
            int speed = 1000;
            int minRange = 1000;
            int maxRange = 0;
            int consumption = 1;
            int staticConsummation = 0;
            int amount = 0;
            int cumulatedFuelTank = 1000000;

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
            fleet.consumption = (consumption);
            fleet.amount = (amount);
            fleet.cumulatedFuelTank = (cumulatedFuelTank);
            fleet.squadrons = (squadronMap);
            fleet.currentFuel = 1000000;
            fleet.fuelTime = 1000000;

            //On met à jour les paramètres de la flotte en BDD
            var newFleetFilter = Builders<Fleet>.Filter.Eq(x => x.id, fleet.id);
            var newFleetUpdate = Builders<Fleet>.Update.Set(x => x.squadrons, fleet.squadrons).Set(x => x.capacities, fleet.capacities).
                        Set(x => x.power, fleet.power).Set(x => x.speed, fleet.speed).Set(x => x.staticConsumption, fleet.staticConsumption).
                        Set(x => x.minRange, fleet.minRange).Set(x => x.maxRange, fleet.maxRange).Set(x => x.consumption, fleet.consumption).
                        Set(x => x.amount, fleet.amount).Set(x => x.cumulatedFuelTank, fleet.cumulatedFuelTank).Set(x => x.currentFuel, fleet.currentFuel).
                        Set(x => x.fuelTime, fleet.fuelTime);
            MongoDBSingleton.Instance().fleetsCollection.UpdateOne(newFleetFilter, newFleetUpdate);
            //On ajoute la flotte dans la liste des flottes de l'IA
            fleetListId.Add(fleet.id);
            fleetIdsGroupedByType[fleetType].Add(fleet.id);
            //On met ensuite à jour la liste des flottes de l'IA en BDD
            if (type == OwnerTypeConstant.AI_DELTA)
            {
                var updateOwnerFleets = Builders<AIDelta>.Update.Set(x => x.fleetListId, fleetListId).Set(x => x.fleetIdsGroupedByType, fleetIdsGroupedByType);
                MongoDBSingleton.Instance().aiDeltaCollection.UpdateOne(Builders<AIDelta>.Filter.Eq(x => x.id, id), updateOwnerFleets);

            }
            else
            {
                var updateOwnerFleets = Builders<AIGamma>.Update.Set(x => x.fleetListId, fleetListId).Set(x => x.fleetIdsGroupedByType, fleetIdsGroupedByType);
                MongoDBSingleton.Instance().aiGammaCollection.UpdateOne(Builders<AIGamma>.Filter.Eq(x => x.id, id), updateOwnerFleets);
            }
            Utilies.WriteEventCreationLog("build_fleet", fleet.id, this);
            return fleet;

        }

        /// <summary>
        /// Met à jour les flottes IA d'un certain type.
        /// </summary>
        /// <param name="fleetType"> Type de flotte </param>
        private void updateFleetsSquadrons(string fleetType)
        {
            for (int i = fleetIdsGroupedByType[fleetType].Count - 1; i >= 0; i--)
            {
                //Si la flotte n"existe plus
                if (MongoDBSingleton.Instance().fleetsCollection.Find(Builders<Fleet>.Filter.Eq(x => x.id, fleetIdsGroupedByType[fleetType][i])).CountDocuments() == 0)
                {
                    //On la retire des listes des flottes et on met à jour ces listes en BDD
                    fleetListId.Remove(fleetIdsGroupedByType[fleetType][i]);
                    fleetIdsGroupedByType[fleetType].RemoveAt(i);
                    if (type == OwnerTypeConstant.AI_DELTA)
                    {
                        var updateOwnerFleets = Builders<AIDelta>.Update.Set(x => x.fleetListId, fleetListId).Set(x => x.fleetIdsGroupedByType, fleetIdsGroupedByType);
                        MongoDBSingleton.Instance().aiDeltaCollection.UpdateOne(Builders<AIDelta>.Filter.Eq(x => x.id, id), updateOwnerFleets);

                    }
                    else
                    {
                        var updateOwnerFleets = Builders<AIGamma>.Update.Set(x => x.fleetListId, fleetListId).Set(x => x.fleetIdsGroupedByType, fleetIdsGroupedByType);
                        MongoDBSingleton.Instance().aiGammaCollection.UpdateOne(Builders<AIGamma>.Filter.Eq(x => x.id, id), updateOwnerFleets);
                    }
                }
                else
                {
                    //On récupère la flotte en BDD
                    var fleet = MongoDBSingleton.Instance().fleetsCollection.Find(Builders<Fleet>.Filter.Eq(x => x.id, fleetIdsGroupedByType[fleetType][i])).First();
                    var fleetTemplate = MongoDBSingleton.Instance().fleetsCollection.Find(Builders<Fleet>.Filter.Eq(x => x.name, fleetTemplates[fleetType].fleetName)).First();
                    var basePosition = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.id, mainBaseId)).First().position;
                    //Si la flotte passe en dessous d'un certain pourcentage de sa puissance max, et qu'on peut la remplir
                    if (fleet.power < fleetTemplate.power * Values.fleetDepletionThreshold && (fleetType == AIFleetTypes.deliveryFleet || DateTime.Now.CompareTo(lastFleetReplenishTime.AddSeconds(Values.fleetsReplenishCooldown)) > 0)
                        && fleet.status != FleetStatusConstant.MOVING && fleet.status != FleetStatusConstant.FIGHTING && fleet.status != FleetStatusConstant.TRANSITING
                        && basePosition.Equals(fleet.position))
                    {   
                        //On remplit la flotte
                        replenishFleet(fleet, fleetTemplate, fleetType);
                        //On met à jour le timer
                        lastFleetReplenishTime = DateTime.Now;
                    }
                    //Si la flotte n'est pas sur la base et en patrouille, on la ramène à la base
                    if (fleet.status == FleetStatusConstant.PATROLLING && !basePosition.Equals(fleet.position))
                    {
                        var evnt = MoveManager.GetInstance().launchMove(fleet, basePosition, fleet.position, FleetObjectives.MOVE, string.Format("{0} returns to base", fleet.name), 0);
                        Utilies.WriteEventCreationLog("MoveEvent", evnt.id, this);
                    }
                }
            }
            //Si l'IA à moins de flottes d'un certain type que la quantité maximale qu'elle peut posséder
            if (fleetIdsGroupedByType[fleetType].Count < fleetTemplates[fleetType].maxFleetNumber)
            {
                //On créé une nouvelle flotte de ce type
                //Pour les flottes de livraison, on n'ajoute pas de timer pour sa création
                if (fleetType == AIFleetTypes.deliveryFleet)
                {
                    createFleet(fleetTemplates[fleetType].fleetName, fleetType);
                }
                //Pour les autres flottes, on vérifie le timer
                else if (DateTime.Now.CompareTo(lastFleetBuildTime.AddSeconds(Values.fleetsBuildCooldown)) > 0)
                {
                    createFleet(fleetTemplates[fleetType].fleetName, fleetType);
                    lastFleetBuildTime = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Met à jour les différents types de flottes de l'IA
        /// </summary>
        private void updateFleets()
        {
            //On met à jour les différents types de flottes de l'IA
            updateFleetsSquadrons(AIFleetTypes.defenseFleet);
            updateFleetsSquadrons(AIFleetTypes.attackFleet);
            updateFleetsSquadrons(AIFleetTypes.deliveryFleet);
            updateFleetsSquadrons(AIFleetTypes.turretFleet);

            //On met à jour le stockage en carburant des flottes IA afin d'éviter qu'elles tombent en panne
            var updateFleetsFuel = Builders<Fleet>.Update.Set(x => x.consumption, 1).Set(x => x.currentFuel, 1000000).Set(x => x.cumulatedFuelTank, 1000000).
                Set(x => x.fuelTime, 1000000);
            MongoDBSingleton.Instance().fleetsCollection.UpdateMany(Builders<Fleet>.Filter.Eq(x => x.ownerId, id), updateFleetsFuel);

            //Pour chacun des flottes divisées
            for (int i = splitFleets.Count - 1; i >= 0; i--)
            {
                var tuple = splitFleets.ElementAt(i);
                //Si la partie de la flotte restée à la base n'existe plus, on supprime le tuple qui contient son id de flotte
                if (MongoDBSingleton.Instance().fleetsCollection.Find(Builders<Fleet>.Filter.Eq(x => x.id, tuple.Value)).CountDocuments() == 0)
                {
                    splitFleets.Remove(tuple.Key);
                    //On met à jour le dictionnaire des flottes divisées de l'IA en BDD
                    if (type == OwnerTypeConstant.AI_DELTA)
                    {
                        var updateOwnerFleets = Builders<AIDelta>.Update.Set(x => x.splitFleets, splitFleets);
                        MongoDBSingleton.Instance().aiDeltaCollection.UpdateOne(Builders<AIDelta>.Filter.Eq(x => x.id, id), updateOwnerFleets);

                    }
                    else
                    {
                        var updateOwnerFleets = Builders<AIGamma>.Update.Set(x => x.splitFleets, splitFleets);
                        MongoDBSingleton.Instance().aiGammaCollection.UpdateOne(Builders<AIGamma>.Filter.Eq(x => x.id, id), updateOwnerFleets);
                    }
                }
                //Si la partie de la flotte partie en attaque n'existe plus
                else if (MongoDBSingleton.Instance().fleetsCollection.Find(Builders<Fleet>.Filter.Eq(x => x.id, tuple.Key)).CountDocuments() == 0)
                {
                    //On ajoute la partie de la flotte restée à la base dans la liste des flottes d'attaque
                    fleetIdsGroupedByType[AIFleetTypes.attackFleet].Add(splitFleets[tuple.Key]);
                    //On supprime le tuple qui contient la flotte disparue du dictionnaire des flottes séparées
                    splitFleets.Remove(tuple.Key);
                    //On met à jour le dictionnaire des flottes séparées de l'IA en BDD
                    if (type == OwnerTypeConstant.AI_DELTA)
                    {
                        var updateOwnerFleets = Builders<AIDelta>.Update.Set(x => x.splitFleets, splitFleets).Set(x => x.fleetIdsGroupedByType, fleetIdsGroupedByType);
                        MongoDBSingleton.Instance().aiDeltaCollection.UpdateOne(Builders<AIDelta>.Filter.Eq(x => x.id, id), updateOwnerFleets);

                    }
                    else
                    {
                        var updateOwnerFleets = Builders<AIGamma>.Update.Set(x => x.splitFleets, splitFleets).Set(x => x.fleetIdsGroupedByType, fleetIdsGroupedByType);
                        MongoDBSingleton.Instance().aiGammaCollection.UpdateOne(Builders<AIGamma>.Filter.Eq(x => x.id, id), updateOwnerFleets);
                    }
                }
            }
            //Mise à jour du statut des flottes dans le cas des IAs non Warmonger
            if (personality != Warmonger)
            {
                var position = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.id, mainBaseId)).First().position;


                var fleetsCollection = MongoDBSingleton.Instance().database.GetCollection<Fleet>("fleets");
                var fleetsFilter = Builders<Fleet>.Filter.Eq(x => x.ownerId, id);
                var fleets = fleetsCollection.Find(fleetsFilter).ToList();
                //Pour chacune des flottes
                for (int i = 0; i < fleets.Count - 1; i++)
                {
                    //Si la flotte n'est pas une flotte de défense, de tourelles ou de livraison, et qu'elle se trouve à la base et qu'elle n'est pas en déplacement, en combat ou en transition
                    if ((fleets[i].status == FleetStatusConstant.INACTIVE || fleets[i].status == FleetStatusConstant.PATROLLING) &&
                        fleets[i].status != FleetStatusConstant.MOVING && fleets[i].status != FleetStatusConstant.FIGHTING && fleets[i].status != FleetStatusConstant.TRANSITING &&
                        !fleetIdsGroupedByType[AIFleetTypes.turretFleet].Contains(fleets[i].id) && !fleetIdsGroupedByType[AIFleetTypes.defenseFleet].Contains(fleets[i].id)
                        && !fleetIdsGroupedByType[AIFleetTypes.deliveryFleet].Contains(fleets[i].id) && fleets[i].position.Equals(position))
                    {
                        //On change le statut de la flotte pour qu'elle passe en standby
                        fleets[i].status = FleetStatusConstant.STANDBY;
                        var fleetUpdate = Builders<Fleet>.Update.Set(x => x.status, fleets[i].status);
                        fleetsCollection.UpdateOne(Builders<Fleet>.Filter.Eq(x => x.id, fleets[i].id), fleetUpdate);
                    }
                }
            }


        }

        /// <summary>
        /// Essaie de lancer un FleetMoveEvent d'attaque.
        /// </summary>
        /// <param name="currentPosition">Position actuelle de l'IA</param>
        /// <param name="target"> Owner cible</param>
        /// <param name="rand"> Random</param>
        /// <param name="channel">Channel pour publier un Event RabbitMQ </param>
        /// <param name="properties"> propriétés pour le formatage correct du message RabbitMQ </param>
        /// <param name="helpingPlayer"> booléen indiquant si on aide un joueur ou non </param>
        private void TryLaunchAttackFleet(GlobalPosition currentPosition, Owner target, Random rand, IModel channel, IBasicProperties properties, bool helpingPlayer)
        {
            FleetMoveEvent? moveEvent = null;
            Fleet? firstAvailableFleet = null;
            //Pour chacune des flottes d'attaque
            for (int i = 0; i < fleetIdsGroupedByType[AIFleetTypes.attackFleet].Count; i++)
            {
                var fleetFilter = Builders<Fleet>.Filter.Eq(x => x.id, fleetIdsGroupedByType[AIFleetTypes.attackFleet][i]);
                //Si la flotte n'existe plus
                if (MongoDBSingleton.Instance().fleetsCollection.Find(fleetFilter).CountDocuments() == 0)
                {
                    //On la supprime des flottes de l'IA et on met à jour les données en BDD
                    fleetListId.Remove(fleetIdsGroupedByType[AIFleetTypes.attackFleet][i]);
                    fleetIdsGroupedByType[AIFleetTypes.attackFleet].Remove(fleetIdsGroupedByType[AIFleetTypes.attackFleet][i]);
                    if (type == OwnerTypeConstant.AI_DELTA)
                    {
                        var updateOwnerFleets = Builders<AIDelta>.Update.Set(x => x.fleetListId, fleetListId).Set(x => x.fleetIdsGroupedByType, fleetIdsGroupedByType);
                        MongoDBSingleton.Instance().aiDeltaCollection.UpdateOne(Builders<AIDelta>.Filter.Eq(x => x.id, id), updateOwnerFleets);

                    }
                    else
                    {
                        var updateOwnerFleets = Builders<AIGamma>.Update.Set(x => x.fleetListId, fleetListId).Set(x => x.fleetIdsGroupedByType, fleetIdsGroupedByType);
                        MongoDBSingleton.Instance().aiGammaCollection.UpdateOne(Builders<AIGamma>.Filter.Eq(x => x.id, id), updateOwnerFleets);
                    }
                }
                //Si la flotte existe
                else
                {
                    var fleet = MongoDBSingleton.Instance().fleetsCollection.Find(fleetFilter).First();
                    //On vérifie qu'on peut l'envoyer au combat
                    if (fleet.status != FleetStatusConstant.MOVING && fleet.status != FleetStatusConstant.FIGHTING && fleet.status != FleetStatusConstant.TRANSITING)
                    {
                        firstAvailableFleet = fleet;
                        break;
                    }

                }

            }
            //Si on a réussi à trouver une flotte pour le combat
            if (firstAvailableFleet != null)
            {
                //On choisit une base au hasard à attaquer parmi les bases de la cible
                var baseKey = target.baseListId.Keys.ToList()[rand.Next(target.baseListId.Count)];
                var baseToAttack = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.id, baseKey)).First();

                //On calcule la puissance de la flotte de défense de la cible
                var targetDefensePower = MongoDBSingleton.Instance().fleetsCollection.Find(Builders<Fleet>.Filter.Eq(x => x.ownerId, target.id) & Builders<Fleet>.Filter.Eq(x => x.position, baseToAttack.position)).ToList().Sum(x => x.power);
                //Si l'écart de puissance est trop grand, on n'attaque pas
                if (firstAvailableFleet.power > Values.maxPowerGapFactor * targetDefensePower)
                    return;
                
                float power = 0;
                if (target.type == OwnerTypeConstant.AI_DELTA)
                {
                    power += MongoDBSingleton.Instance().aiDeltaCollection.Find(Builders<AIDelta>.Filter.Eq(x => x.id, target.id)).First().aiBasePower;
                }
                else if (target.type == OwnerTypeConstant.AI_GAMMA)
                {
                    power += MongoDBSingleton.Instance().aiGammaCollection.Find(Builders<AIGamma>.Filter.Eq(x => x.id, target.id)).First().aiBasePower;
                }
                else
                {
                    var basesList = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.ownerId, target.id)).ToList();
                    //On calcule un pourcentage de la flotte à envoyer, qui dépend du niveau de certains bâtiments ennemis
                    foreach (var b in basesList)
                    {
                        float f = b.id == target.mainBaseId ? 1 : 0.5f;
                        foreach (var key in b.slots.Keys)
                        {
                            if (b.slots[key].built != null)
                            {
                                switch (b.slots[key].built.building.name)
                                {
                                    case "Chantier spatial":
                                        power += f * b.slots[key].built.level;
                                        break;
                                    case "Plateforme de defense":
                                        power += f * b.slots[key].built.level;
                                        break;
                                    case "HQ":
                                        power += f * (int)Math.Ceiling(b.slots[key].built.level / (float)2);
                                        break;
                                    case "Laboratoire":
                                        power += f * b.slots[key].built.level;
                                        break;
                                }
                            }
                        }
                    }
                }
                if (power < aiBasePower + Values.minAttackPower)
                {
                    return;
                }
                power += Values.minAttackPower;
                //Si on doit envoyer un pourcentage de la flotte en attaque
                if (power < 100)
                {
                    power *= .01f;
                    //On créé une deuxième flotte, qui contiendra les vaisseaux qui ne feront pas partie de la flotte d'attaque
                    var splitFleet = new Fleet(firstAvailableFleet);
                    splitFleet.id = null;
                    splitFleet.status = FleetStatusConstant.INACTIVE;
                    MongoDBSingleton.Instance().fleetsCollection.InsertOne(splitFleet);

                    firstAvailableFleet.power = 0;
                    splitFleet.power = 0;
                    firstAvailableFleet.amount = 0;
                    splitFleet.amount = 0;
                    //On divise ensuite la flotte en deux, une partie restant à la base et l'autre partie attaquant la cible
                    //La répartition des vaisseaux se fait selon le pourcentage calculé au-dessus
                    foreach (var key in firstAvailableFleet.squadrons.Keys)
                    {
                        int shipTotal = firstAvailableFleet.squadrons[key].amount;
                        firstAvailableFleet.squadrons[key].amount = (int)Math.Ceiling(power * firstAvailableFleet.squadrons[key].amount);
                        firstAvailableFleet.squadrons[key].cumulatedPower = firstAvailableFleet.squadrons[key].amount * firstAvailableFleet.squadrons[key].power;
                        shipTotal -= firstAvailableFleet.squadrons[key].amount;
                        firstAvailableFleet.power += firstAvailableFleet.squadrons[key].cumulatedPower;
                        firstAvailableFleet.amount += firstAvailableFleet.squadrons[key].amount;

                        splitFleet.squadrons[key].amount = shipTotal;
                        splitFleet.squadrons[key].cumulatedPower = splitFleet.squadrons[key].amount * splitFleet.squadrons[key].power;
                        splitFleet.squadrons[key].fleetId = splitFleet.id;
                        splitFleet.power += splitFleet.squadrons[key].cumulatedPower;
                        splitFleet.amount += splitFleet.squadrons[key].amount;
                    }
                    //On met les informations des deux flottes à jour en BDD
                    var fleetUpdate = Builders<Fleet>.Update.Set(x => x.squadrons, firstAvailableFleet.squadrons).Set(x => x.power, firstAvailableFleet.power).Set(x => x.power, firstAvailableFleet.power).
                                Set(x => x.amount, firstAvailableFleet.amount);
                    MongoDBSingleton.Instance().fleetsCollection.UpdateOne(Builders<Fleet>.Filter.Eq(x => x.id, firstAvailableFleet.id), fleetUpdate);
                    var splitFleetUpdate = Builders<Fleet>.Update.Set(x => x.squadrons, splitFleet.squadrons).Set(x => x.power, splitFleet.power).Set(x => x.power, splitFleet.power).
                                Set(x => x.amount, splitFleet.amount);
                    MongoDBSingleton.Instance().fleetsCollection.UpdateOne(Builders<Fleet>.Filter.Eq(x => x.id, splitFleet.id), splitFleetUpdate);

                    //On met à jour la liste des flottes de l'IA en BDD
                    splitFleets[firstAvailableFleet.id] = splitFleet.id;
                    fleetListId.Add(splitFleet.id);
                    if (type == OwnerTypeConstant.AI_DELTA)
                    {
                        var updateOwnerFleets = Builders<AIDelta>.Update.Set(x => x.splitFleets, splitFleets).Set(x => x.fleetListId, fleetListId);
                        MongoDBSingleton.Instance().aiDeltaCollection.UpdateOne(Builders<AIDelta>.Filter.Eq(x => x.id, id), updateOwnerFleets);

                    }
                    else
                    {
                        var updateOwnerFleets = Builders<AIGamma>.Update.Set(x => x.splitFleets, splitFleets).Set(x => x.fleetListId, fleetListId);
                        MongoDBSingleton.Instance().aiGammaCollection.UpdateOne(Builders<AIGamma>.Filter.Eq(x => x.id, id), updateOwnerFleets);
                    }
                }
                
                //On lance ensuite la création d'un FleetMoveEvent d'attaque sur la cible
                moveEvent = MoveManager.GetInstance().launchMove(firstAvailableFleet, baseToAttack.position, currentPosition, FleetObjectives.ATTACK, "AI " + name + " attacks", 0);
                Utilies.WriteEventCreationLog("MoveEvent", moveEvent.id, this);
                Utilies.SendEvent(moveEvent, channel, properties);
            }


            if (moveEvent != null && target.type == OwnerTypeConstant.PLAYER && !helpingPlayer)
            {
                //Augmentation de la réputation du joueur si l'IA l'attaque (= vengeance assouvie par l'IA)
                playersDictionary[target.id][PlayerElement.LastAttackTime] = DateTime.Now;
                playersDictionary[target.id][PlayerElement.AttackProbabilityBonus] = 0;
                moveEvent = null;
            }
        }

        /// <summary>
        /// Essaye de lancer un FleetMoveEvent de soutien en réponse à un FleetMoveEvent d'attaque
        /// </summary>
        /// <param name="currentPosition">Position actuelle de l'IA</param>
        /// <param name="attackFleetId">Identifiant de la flotte attaquante</param>
        /// <param name="attacker">Owner attaquant</param>
        /// <param name="defender"> Owner défenseur</param>
        /// <param name="baseToDefend">Base du défenseur</param>
        /// <param name="channel">Channel pour publier un Event RabbitMQ </param>
        /// <param name="properties"> propriétés pour le formatage correct du message RabbitMQ </param>
        private void TryLaunchDefendFleet(GlobalPosition currentPosition, string attackFleetId, Owner attacker, Owner defender, Base baseToDefend, IModel channel, IBasicProperties properties)
        {
            FleetMoveEvent? moveEvent = null;
            Fleet? firstAvailableFleet = null;
            //Pour chacune des flottes d'attaque
            for (int i = 0; i < fleetIdsGroupedByType[AIFleetTypes.attackFleet].Count; i++)
            {
                var fleetFilter = Builders<Fleet>.Filter.Eq(x => x.id, fleetIdsGroupedByType[AIFleetTypes.attackFleet][i]);
                //Si la flotte n'existe plus
                if (MongoDBSingleton.Instance().fleetsCollection.Find(fleetFilter).CountDocuments() == 0)
                {
                    //On la supprime des flottes de l'IA et on met à jour les données en BDD
                    fleetListId.Remove(fleetIdsGroupedByType[AIFleetTypes.attackFleet][i]);
                    fleetIdsGroupedByType[AIFleetTypes.attackFleet].Remove(fleetIdsGroupedByType[AIFleetTypes.attackFleet][i]);
                    if (type == OwnerTypeConstant.AI_DELTA)
                    {
                        var updateOwnerFleets = Builders<AIDelta>.Update.Set(x => x.fleetListId, fleetListId).Set(x => x.fleetIdsGroupedByType, fleetIdsGroupedByType);
                        MongoDBSingleton.Instance().aiDeltaCollection.UpdateOne(Builders<AIDelta>.Filter.Eq(x => x.id, id), updateOwnerFleets);

                    }
                    else
                    {
                        var updateOwnerFleets = Builders<AIGamma>.Update.Set(x => x.fleetListId, fleetListId).Set(x => x.fleetIdsGroupedByType, fleetIdsGroupedByType);
                        MongoDBSingleton.Instance().aiGammaCollection.UpdateOne(Builders<AIGamma>.Filter.Eq(x => x.id, id), updateOwnerFleets);
                    }
                }
                //Si la flotte existe
                else
                {
                    var fleet = MongoDBSingleton.Instance().fleetsCollection.Find(fleetFilter).First();
                    //On vérifie qu'on peut l'envoyer au combat
                    if (fleet.status != FleetStatusConstant.MOVING && fleet.status != FleetStatusConstant.FIGHTING && fleet.status != FleetStatusConstant.TRANSITING)
                    {
                        firstAvailableFleet = fleet;
                        break;
                    }

                }

            }
            //Si on a réussi à trouver une flotte pour le combat
            if (firstAvailableFleet != null)
            {
                //On récupère la flotte de l'attaquant
                var attackFleet = MongoDBSingleton.Instance().fleetsCollection.Find(Builders<Fleet>.Filter.Eq(x => x.id, attackFleetId)).First();
                
                float power = 10;
                if (attacker.type == OwnerTypeConstant.AI_DELTA)
                {
                    power += MongoDBSingleton.Instance().aiDeltaCollection.Find(Builders<AIDelta>.Filter.Eq(x => x.id, attacker.id)).First().aiBasePower;
                }
                else if (attacker.type == OwnerTypeConstant.AI_GAMMA)
                {
                    power += MongoDBSingleton.Instance().aiGammaCollection.Find(Builders<AIGamma>.Filter.Eq(x => x.id, attacker.id)).First().aiBasePower;
                }
                else
                {
                    //Calcul du pourcentage de la flotte à envoyer = proportionnel à la différence de puissance entre attaque et défense
                    //=> Si l'attaque est bien plus forte que la défense, on enverra plus de vaisseaux, et inversement
                    var attackerBasesList = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.ownerId, attacker.id)).ToList();
                    //On ajoute la puissance d'attaque de l'attaquant
                    foreach (var b in attackerBasesList)
                    {
                        float f = b.id == attacker.mainBaseId ? 1 : 0.5f;
                        foreach (var key in b.slots.Keys)
                        {
                            if (b.slots[key].built != null)
                            {
                                switch (b.slots[key].built.building.name)
                                {
                                    case "Chantier spatial":
                                        power += f * 2 * b.slots[key].built.level;
                                        break;
                                    case "HQ":
                                        power += f * (int)Math.Ceiling(b.slots[key].built.level / (float)2);
                                        break;
                                    case "Laboratoire":
                                        power += f * b.slots[key].built.level;
                                        break;
                                }
                            }
                        }
                    }
                }
                if (attacker.type == OwnerTypeConstant.AI_DELTA)
                {
                    power -= MongoDBSingleton.Instance().aiDeltaCollection.Find(Builders<AIDelta>.Filter.Eq(x => x.id, defender.id)).First().aiBasePower;
                }
                else if (attacker.type == OwnerTypeConstant.AI_GAMMA)
                {
                    power -= MongoDBSingleton.Instance().aiGammaCollection.Find(Builders<AIGamma>.Filter.Eq(x => x.id, defender.id)).First().aiBasePower;
                }
                else
                {
                    var defenderBaseList = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.ownerId, defender.id)).ToList();
                    //On retire la puissance de défense du défenseur
                    foreach (var b in defenderBaseList)
                    {
                        float f = b.id == defender.mainBaseId ? 1 : 0.5f;
                        foreach (var key in b.slots.Keys)
                        {
                            if (b.slots[key].built != null)
                            {
                                switch (b.slots[key].built.building.name)
                                {
                                    case "Chantier spatial":
                                        power -= f * b.slots[key].built.level;
                                        break;
                                    case "Plateforme de defense":
                                        power -= f * b.slots[key].built.level;
                                        break;
                                    case "HQ":
                                        power -= f * (int)Math.Ceiling(b.slots[key].built.level / (float)2);
                                        break;
                                    case "Laboratoire":
                                        power -= f * b.slots[key].built.level;
                                        break;
                                }
                            }
                        }
                    }
                }
                    
                //Dépendant de la puissance de la flotte IA, on multiplie par un facteur le pourcentage à envoyer
                switch (firstAvailableFleet.power)
                {
                    case <= 200: 
                        power *= 4;
                        break;
                    case <= 12000:
                        power *= 2;
                        break;
                    case <= 100000:
                        power *= 1;
                        break;
                    default:
                        power *= 0.5f;
                        break;
                }
                power = Math.Min(Math.Max(10, power), 100);
                //Si le pourcentage dépasse 100%, on envoie l'integralité de la flotte
                if (power < 100)
                {
                    power *= .01f;
                    //on créé une nouvelle flotte, qui contiendra les vaisseaux retirés de la flotte d'attaque
                    var splitFleet = new Fleet(firstAvailableFleet);
                    splitFleet.id = null;
                    splitFleet.status = FleetStatusConstant.STANDBY;
                    MongoDBSingleton.Instance().fleetsCollection.InsertOne(splitFleet);

                    firstAvailableFleet.power = 0;
                    splitFleet.power = 0;
                    firstAvailableFleet.amount = 0;
                    splitFleet.amount = 0;
                    //On divise ensuite la flotte en deux, une partie restant à la base et l'autre partie attaquant la cible
                    //La répartition des vaisseaux se fait selon le pourcentage calculé au-dessus
                    for (int j = 0; j < firstAvailableFleet.squadrons.Count; j++)
                    {
                        var key = firstAvailableFleet.squadrons.Keys.ToList()[j];
                        int shipTotal = firstAvailableFleet.squadrons[key].amount;
                        firstAvailableFleet.squadrons[key].amount = (int)Math.Ceiling(power * firstAvailableFleet.squadrons[key].amount);
                        firstAvailableFleet.squadrons[key].cumulatedPower = firstAvailableFleet.squadrons[key].amount * firstAvailableFleet.squadrons[key].power;
                        shipTotal -= firstAvailableFleet.squadrons[key].amount;
                        firstAvailableFleet.power += firstAvailableFleet.squadrons[key].cumulatedPower;
                        firstAvailableFleet.amount += firstAvailableFleet.squadrons[key].amount;

                        key = splitFleet.squadrons.Keys.ToList()[j];
                        splitFleet.squadrons[key].amount = shipTotal;
                        splitFleet.squadrons[key].cumulatedPower = splitFleet.squadrons[key].amount * splitFleet.squadrons[key].power;
                        splitFleet.squadrons[key].fleetId = splitFleet.id;
                        splitFleet.power += splitFleet.squadrons[key].cumulatedPower;
                        splitFleet.amount += splitFleet.squadrons[key].amount;
                    }
                    //On met les informations des deux flottes à jour en BDD
                    var fleetUpdate = Builders<Fleet>.Update.Set(x => x.squadrons, firstAvailableFleet.squadrons).Set(x => x.power, firstAvailableFleet.power).Set(x => x.power, firstAvailableFleet.power).
                                Set(x => x.amount, firstAvailableFleet.amount);
                    MongoDBSingleton.Instance().fleetsCollection.UpdateOne(Builders<Fleet>.Filter.Eq(x => x.id, firstAvailableFleet.id), fleetUpdate);
                    var splitFleetUpdate = Builders<Fleet>.Update.Set(x => x.squadrons, splitFleet.squadrons).Set(x => x.power, splitFleet.power).Set(x => x.power, splitFleet.power).
                                Set(x => x.amount, splitFleet.amount);
                    MongoDBSingleton.Instance().fleetsCollection.UpdateOne(Builders<Fleet>.Filter.Eq(x => x.id, splitFleet.id), splitFleetUpdate);

                    //On met à jour la liste des flottes de l'IA en BDD
                    splitFleets[firstAvailableFleet.id] = splitFleet.id;
                    fleetListId.Add(splitFleet.id);

                    if (type == OwnerTypeConstant.AI_DELTA)
                    {
                        var updateOwnerFleets = Builders<AIDelta>.Update.Set(x => x.splitFleets, splitFleets).Set(x => x.fleetListId, fleetListId);
                        MongoDBSingleton.Instance().aiDeltaCollection.UpdateOne(Builders<AIDelta>.Filter.Eq(x => x.id, id), updateOwnerFleets);

                    }
                    else
                    {
                        var updateOwnerFleets = Builders<AIGamma>.Update.Set(x => x.splitFleets, splitFleets).Set(x => x.fleetListId, fleetListId);
                        MongoDBSingleton.Instance().aiGammaCollection.UpdateOne(Builders<AIGamma>.Filter.Eq(x => x.id, id), updateOwnerFleets);
                    }
                }
                //On lance ensuite la création d'un FleetMoveEvent de soutien
                moveEvent = MoveManager.GetInstance().launchMove(firstAvailableFleet, baseToDefend.position, currentPosition, FleetObjectives.DEFEND, string.Format("AI {0} defends player {1}", name, baseToDefend.ownerName), 0);
                Utilies.WriteEventCreationLog("MoveEvent", moveEvent.id, this);
                Utilies.SendEvent(moveEvent, channel, properties);
            }

        }

        /// <summary>
        /// Gestion d'un eventResult de déplacement
        /// </summary>
        /// <param name="eventResult"> EventResult à traiter</param>
        private void ManageMoveEventResult(EventResult eventResult)
        {
            //Le cas d'une alliance sera à traiter dans le futur
            if (MongoDBSingleton.Instance().alliancesCollection.Find(Builders<Alliance>.Filter.Eq(x => x.id, eventResult.userId)).First().type == OwnerTypeConstant.ALLIANCE)
                return;

             if (MongoDBSingleton.Instance().reputationCollection.Find(Builders<ReputationObject>.Filter.Eq(x => x.id, eventResult.userId)).CountDocuments() > 0 &&
                !MongoDBSingleton.Instance().reputationCollection.Find(Builders<ReputationObject>.Filter.Eq(x => x.id, eventResult.userId)).First().reputationValues.ContainsKey(id))
            {
                //Si la source de l'Event est un joueur et qu'il interagit avec l'IA, un de ses alliés ou un de ses ennemis de l'IA, on lui donne une réputation avec l'IA
                if (eventResult.impactedOwnerId == id || allyIds.Contains(eventResult.impactedOwnerId) || enemyIds.Contains(eventResult.impactedOwnerId))
                {
                    var repValueUpdate = Builders<ReputationObject>.Update.Set(x => x.reputationValues[id], 500);
                    MongoDBSingleton.Instance().reputationCollection.UpdateOne(Builders<ReputationObject>.Filter.Eq(x => x.id, eventResult.userId), repValueUpdate);
                }
                else
                {
                    return;
                }
            }
            //Sinon si la personne ciblée par le déplacement est un joueur et qu'il n'a pas de réputation avec l'IA
            else if (MongoDBSingleton.Instance().reputationCollection.Find(Builders<ReputationObject>.Filter.Eq(x => x.id, eventResult.impactedOwnerId)).CountDocuments() > 0 &&
                !MongoDBSingleton.Instance().reputationCollection.Find(Builders<ReputationObject>.Filter.Eq(x => x.id, eventResult.impactedOwnerId)).First().reputationValues.ContainsKey(id))
            {
                //Si un allié ou un ennemi de l'IA interagit avec un joueur, alors on lui donne une réputation avec l'IA
                if (allyIds.Contains(eventResult.userId) || enemyIds.Contains(eventResult.userId))
                {
                    var repValueUpdate = Builders<ReputationObject>.Update.Set(x => x.reputationValues[id], 500);
                    MongoDBSingleton.Instance().reputationCollection.UpdateOne(Builders<ReputationObject>.Filter.Eq(x => x.id, eventResult.impactedOwnerId), repValueUpdate);
                }
                else
                {
                    return;
                }
            }
            //Si l'IA est à l'origine du déplacement
            if (eventResult.userId == id)
            {
                //On récupère la flotte s'étant déplacée
                var fleetId = ((JsonElement)eventResult.results[EventResultTypeConstants.MOVED_FLEET]).GetString();
                var basePosition = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.id, mainBaseId)).First().position;
                //Si la flotte est sur la base et qu'elle avait été séparée
                if (basePosition.Equals(eventResult.globalPosition) && splitFleets.ContainsKey(fleetId))
                {
                    //On remet les vaisseaux de la partie de flotte restée à la base dans la flotte revenue, et on supprime la flotte vidée
                    var fleet = MongoDBSingleton.Instance().fleetsCollection.Find(Builders<Fleet>.Filter.Eq(x => x.id, fleetId)).First();
                    var splitFleet = MongoDBSingleton.Instance().fleetsCollection.Find(Builders<Fleet>.Filter.Eq(x => x.id, splitFleets[fleetId])).First();
                    fleet.power = 0;
                    fleet.amount = 0;
                    foreach (var key in fleet.squadrons.Keys)
                    {
                        fleet.squadrons[key].amount += splitFleet.squadrons[key].amount;
                        fleet.squadrons[key].cumulatedPower += splitFleet.squadrons[key].cumulatedPower;
                        fleet.power += fleet.squadrons[key].cumulatedPower;
                        fleet.amount += fleet.squadrons[key].amount;
                    }
                    var fleetUpdate = Builders<Fleet>.Update.Set(x => x.squadrons, fleet.squadrons).Set(x => x.power, fleet.power).
                        Set(x => x.amount, fleet.amount);
                    MongoDBSingleton.Instance().fleetsCollection.UpdateOne(Builders<Fleet>.Filter.Eq(x => x.id, fleet.id), fleetUpdate);
                    //On met ensuite à jour les informations en BDD
                    splitFleets.Remove(fleetId);
                    if (type == OwnerTypeConstant.AI_DELTA)
                    {
                        var updateOwnerFleets = Builders<AIDelta>.Update.Set(x => x.splitFleets, splitFleets);
                        MongoDBSingleton.Instance().aiDeltaCollection.UpdateOne(Builders<AIDelta>.Filter.Eq(x => x.id, id), updateOwnerFleets);

                    }
                    else
                    {
                        var updateOwnerFleets = Builders<AIGamma>.Update.Set(x => x.splitFleets, splitFleets);
                        MongoDBSingleton.Instance().aiGammaCollection.UpdateOne(Builders<AIGamma>.Filter.Eq(x => x.id, id), updateOwnerFleets);
                    }
                    MongoDBSingleton.Instance().fleetsCollection.DeleteOne(Builders<Fleet>.Filter.Eq(x => x.id, splitFleet.id));
                }

            }
            var filter = Builders<ReputationObject>.Filter.Eq(x => x.id, eventResult.userId);
            if (MongoDBSingleton.Instance().reputationCollection.Find(filter).CountDocuments() == 0)
                return;
            var oldReputation = MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues[id];
            var newReputation = oldReputation;
            //if (eventResult.results.ContainsKey(EventResultTypeConstants.ATTACK) && ((JsonElement)eventResult.results[EventResultTypeConstants.ATTACK]).GetBoolean())
            //{
            //  
            //}

            //Si c'est un déplacement de soutien
            if (eventResult.results.ContainsKey(EventResultTypeConstants.SUPPORT) && ((JsonElement)eventResult.results[EventResultTypeConstants.SUPPORT]).GetBoolean())
            {
                ManageSupportEventResult(eventResult, oldReputation, newReputation);
            }
            //Si c'est un déplacement de construction de base
            if (eventResult.results.ContainsKey(EventResultTypeConstants.BUILD_BASE) && ((JsonElement)eventResult.results[EventResultTypeConstants.BUILD_BASE]).GetBoolean())
            {
                //Perte réputation si construit base proche IA belliciste ?
            }
            //Cas existants non traités
            //if (eventResult.results.ContainsKey(EventResultTypeConstants.ESCAPE) && ((JsonElement)eventResult.results[EventResultTypeConstants.ESCAPE]).GetBoolean())
            //{
            //}
            //if (eventResult.results.ContainsKey(EventResultTypeConstants.CANCELED_BASE_CONSTRUCT) && ((JsonElement)eventResult.results[EventResultTypeConstants.CANCELED_BASE_CONSTRUCT]).GetBoolean())
            //{
            //}
            if (eventResult.results.ContainsKey(EventResultTypeConstants.DELIVERED_RESOURCES) && (((JsonElement)eventResult.results[EventResultTypeConstants.DELIVERED_RESOURCES]).Deserialize<Dictionary<string, int>>()).Count > 0)
            {
                ManageDeliveredResourcesEventResult(eventResult, oldReputation, newReputation);
            }
        }

        /// <summary>
        /// Gestion d'un EventResult de combat
        /// </summary>
        /// <param name="eventResult"> EvenResult à traiter</param>
        /// <param name="rand"> Random</param>
        /// <param name="channel">Channel pour publier un Event RabbitMQ </param>
        /// <param name="properties"> propriétés pour le formatage correct du message RabbitMQ </param>
        private void ManageFightEventResult(EventResult eventResult, Random rand, IModel channel, IBasicProperties properties)
        {
            //Le cas d'une alliance sera à traiter dans le futur
            if (MongoDBSingleton.Instance().alliancesCollection.Find(Builders<Alliance>.Filter.Eq(x => x.id, eventResult.userId)).First().type == OwnerTypeConstant.ALLIANCE)
                return;

            //Liste des défenseurs du combat
            List<string> defenders = new List<string>();
            if (eventResult.results.ContainsKey(EventResultTypeConstants.DEFENDERS_ID))
                defenders = ((JsonElement)eventResult.results[EventResultTypeConstants.DEFENDERS_ID]).Deserialize<List<string>>();

            if (!defenders.Contains(eventResult.userId))
                return;

            // Décommenter les lignes utiles selon les besoins (actuellement commentées pour éviter des calculs inutiles)
            //Dictionary<string, int>? pilledResources = ((JsonElement)eventResult.results[EventResultTypeConstants.PILLED_RESOURCES]).Deserialize<Dictionary<string, int>>();
            //Dictionary<string, int>? lostResources = ((JsonElement)eventResult.results[EventResultTypeConstants.LOST_RESOURCES]).Deserialize<Dictionary<string, int>>();
            //Dictionary<string, int>? destroyedShip = ((JsonElement)eventResult.results[EventResultTypeConstants.DESTROYED_SHIP]).Deserialize<Dictionary<string, int>>();
            //destroyedPower
            //Dictionary<string, int>? lostShip = ((JsonElement)eventResult.results[EventResultTypeConstants.LOST_SHIP]).Deserialize<Dictionary<string, int>>();
            //lostPower
            //Dictionary<string, int>? bombedShip = ((JsonElement)eventResult.results[EventResultTypeConstants.BOMBED_SHIP]).Deserialize<Dictionary<string, int>>();
            //string destroyedBase = ((JsonElement)eventResult.results[EventResultTypeConstants.DESTROYED_BASE]).GetString();
            //string capturedBase = ((JsonElement)eventResult.results[EventResultTypeConstants.CAPTURED_BASE]).GetString();

            //Liste des attaquants du combat
            List<string> attackers = new List<string>();
            if (eventResult.results.ContainsKey(EventResultTypeConstants.ATTACKERS_ID))
                attackers = ((JsonElement)eventResult.results[EventResultTypeConstants.ATTACKERS_ID]).Deserialize<List<string>>();

            //Si l'IA a une personalité Warmonger, alors les joueurs attaquants gagnent un peu de réputation avec elle
            if (personality == Personality.Warmonger)
            {
                foreach (string enemy in attackers)
                {
                    var filter = Builders<ReputationObject>.Filter.Eq(x => x.id, enemy);
                    if (MongoDBSingleton.Instance().reputationCollection.Find(filter).CountDocuments() == 0 || !MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues.ContainsKey(id))
                        continue;
                    var oldReputation = MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues[id];
                    //Gain de réputation sans passer au-delà d'un seuil
                    updateOrSetPlayerReputation(enemy, oldReputation, Math.Min(oldReputation + Values.warmongerCombatReputationOffset, Values.neutralThresholdFactor * Values.maxReputation));
                }
            }
            //Si l'IA à une personalité pacifiste, alors tous les attaquants perdent un peu de réputation avec elle
            else if (personality == Personality.Pacifist)
            {
                foreach (string enemy in attackers)
                {
                    var filter = Builders<ReputationObject>.Filter.Eq(x => x.id, enemy);
                    if (MongoDBSingleton.Instance().reputationCollection.Find(filter).CountDocuments() == 0 || !MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues.ContainsKey(id))
                        continue;
                    var oldReputation = MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues[id];
                    updateOrSetPlayerReputation(enemy, oldReputation, oldReputation - Values.warmongerCombatReputationOffset);
                }
            }
            //Si l'IA est attaquée
            if (id.Equals(eventResult.userId))
            {
                //Perte de réputation pour tout les ennemis (l'IA a été attaquée)
                foreach (string enemy in attackers)
                {
                    var filter = Builders<ReputationObject>.Filter.Eq(x => x.id, enemy);
                    if (MongoDBSingleton.Instance().reputationCollection.Find(filter).CountDocuments() == 0)
                        continue;
                    if (!MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues.ContainsKey(id))
                    {
                        var update = Builders<ReputationObject>.Update.Set(x => x.reputationValues[id], 500);
                        MongoDBSingleton.Instance().reputationCollection.UpdateOne(filter, update);
                    }
                    var oldReputation = MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues[id];
                    var newReputation = oldReputation - ReputationModificationOnAttack(eventResult, enemy, false);
                    updateOrSetPlayerReputation(enemy, oldReputation, newReputation);
                }
            }
            //Sinon si c'est un allié de l'IA qui est attaqué
            else if (allyIds.Contains(eventResult.userId))
            {
                //Perte de réputation faible pour chacun des attaquants
                foreach (string enemy in attackers)
                {
                    var filter = Builders<ReputationObject>.Filter.Eq(x => x.id, enemy);
                    //On vérifie si l'attaquant est un joueur ou non
                    if (MongoDBSingleton.Instance().reputationCollection.Find(filter).CountDocuments() == 0)
                        continue;
                    //Si le joueur n'a pas de réputation avec l'IA, on lui en donne une
                    if (!MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues.ContainsKey(id))
                    {
                        var update = Builders<ReputationObject>.Update.Set(x => x.reputationValues[id], 500);
                        MongoDBSingleton.Instance().reputationCollection.UpdateOne(filter, update);
                    }
                    var oldReputation = MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues[id];
                    var newReputation = oldReputation - ReputationModificationOnAttack(eventResult, enemy, false) * Values.allyRepLoseFactor;
                    updateOrSetPlayerReputation(enemy, oldReputation, newReputation);
                }
            }
            //Sinon si c'est un ennemi de l'IA qui est attaqué
            else if (enemyIds.Contains(eventResult.userId))
            {
                //Gain de réputation faible pour chacun des attaquants
                foreach (string enemy in attackers)
                {
                    var filter = Builders<ReputationObject>.Filter.Eq(x => x.id, enemy);
                    //On vérifie si l'attaquant est un joueur
                    if (MongoDBSingleton.Instance().reputationCollection.Find(filter).CountDocuments() == 0)
                        continue;
                    //Si le joueur n'a pas de réputation avec l'IA, on lui en donne une
                    if (!MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues.ContainsKey(id))
                    {
                        var update = Builders<ReputationObject>.Update.Set(x => x.reputationValues[id], 500);
                        MongoDBSingleton.Instance().reputationCollection.UpdateOne(filter, update);
                    }
                    var oldReputation = MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues[id];
                    var newReputation = oldReputation + ReputationModificationOnAttack(eventResult, enemy, false) * Values.enemyRepWinFactor;
                    updateOrSetPlayerReputation(enemy, oldReputation, newReputation);
                }
            }
            //Si c'est l'IA qui attaque
            if (attackers.Contains(id))
            {
                //On vérifie que le défenseur est un joueur
                var filter = Builders<ReputationObject>.Filter.Eq(x => x.id, eventResult.userId);
                if (MongoDBSingleton.Instance().reputationCollection.Find(filter).CountDocuments() != 0)
                {
                    //Si le joueur n'a pas de réputation avec l'IA, on lui en donne une
                    if (!MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues.ContainsKey(id))
                    {
                        var update = Builders<ReputationObject>.Update.Set(x => x.reputationValues[id], 500);
                        MongoDBSingleton.Instance().reputationCollection.UpdateOne(filter, update);
                    }
                    var oldReputation = MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues[id];
                    var newReputation = oldReputation + ReputationModificationOnAttack(eventResult, eventResult.userId, true);
                    //Le joueur gagne un peu en réputation
                    updateOrSetPlayerReputation(eventResult.userId, oldReputation, Math.Min(newReputation, Values.neutralThresholdFactor * Values.maxReputation + 10));
                }
            }

            string playerId = eventResult.userId;
            var repFilter = Builders<ReputationObject>.Filter.Eq(x => x.id, playerId);
            //Si le défenseur n'est pas un joueur ou bien que c'est un joueur sans réputation avec l'IA, on return.
            if (MongoDBSingleton.Instance().reputationCollection.Find(repFilter).CountDocuments() == 0 || !MongoDBSingleton.Instance().reputationCollection.Find(repFilter).First().reputationValues.ContainsKey(id))
            {
                return;
            }
            float reputation = MongoDBSingleton.Instance().reputationCollection.Find(repFilter).First().reputationValues[id];

            //Si le joueur est en dehors de la zone de considération de l'Ia, on ne le considère que si sa réputation est suffisament basse ou haute avec l'IA
            if (!playersDictionary.ContainsKey(playerId) && (Utilies.getReputationRatio(reputation) >= Values.neutralThresholdFactor || Utilies.getReputationRatio(reputation) <= Values.hostileThresholdFactor))
            {
                playersDictionary.Add(playerId, new Dictionary<PlayerElement, object>());
                playersDictionary[playerId].Add(PlayerElement.LastAttackTime, DateTime.MinValue);
                playersDictionary[playerId].Add(PlayerElement.AttackProbabilityBonus, 0);
                playersDictionary[playerId].Add(PlayerElement.GiftProbabilityBonus, 0);
                playersDictionary[playerId].Add(PlayerElement.QuestProbabilityBonus, 0);
            }
            //On tente ensuite de faire une attaque en réaction à l'attaque
            var mainBase = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.id, mainBaseId)).First();
            var user = MongoDBSingleton.Instance().usersCollection.Find(Builders<User>.Filter.Eq(x => x.id, playerId)).First();
            ChooseAndExecuteAttack(rand, playerId, channel, properties, mainBase, user, Utilies.getPlayerReputationStatus(reputation));
            //ChooseAndExecuteAction(rand, playerId, channel, properties, reputation, mainBase);
        }

        /// <summary>
        /// Gestion d'un EventResult de quête
        /// </summary>
        /// <param name="eventResult"> EventResult à traiter</param>
        private void ManageQuestEventResult(EventResult eventResult)
        {
            var filter = Builders<ReputationObject>.Filter.Eq(x => x.id, eventResult.userId);
            //Si l'entité n'est pas un joueur, on retourne
            if (MongoDBSingleton.Instance().reputationCollection.Find(filter).CountDocuments() == 0)
                return;
            var oldReputation = MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues[id];
            var newReputation = oldReputation;

            //Si c'est un eventResult contenant un statut de quête, on le traite
            if (eventResult.results.ContainsKey(EventResultTypeConstants.QUEST_STATUS))
                AnalyseQuestEvent(eventResult, oldReputation, newReputation);
        }

        /// <summary>
        /// Gestion d'un EventResult de nouvel utilisateur
        /// </summary>
        /// <param name="eventResult"> EventResult à traiter</param>
        private void ManageNewUserEventResult(EventResult eventResult)
        {
            var filter = Builders<ReputationObject>.Filter.Eq(x => x.id, eventResult.userId);
            if (MongoDBSingleton.Instance().reputationCollection.Find(filter).CountDocuments() == 0)
                return;
            //var oldReputation = MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues[id];
            //var newReputation = oldReputation;

            var player = eventResult.userId;
            //Si le joueur est déjà dans le dictionnaire de joueur à considérer, alors on retourne
            if (playersDictionary.ContainsKey(player))
                return;
            //Sinon, on essaye de l'ajouter au dictionnaire des joueurs à considérer
            TryAddPlayerToConsideredPlayers(player);
        }

        /// <summary>
        /// Calcule la modification de réputation à la suite d'une attaque
        /// </summary>
        /// <param name="eventResult"> EventResult à traiter</param>
        /// <param name="player"> Joueur concerné</param>
        /// <param name="aiAttack"> indique si c'est une attaque de la part de l'IA ou non</param>
        /// <returns></returns>
        private float ReputationModificationOnAttack(EventResult eventResult, string player, bool aiAttack)
        {
            float percentageLostPower = 0;
            //Si des vaisseaux ont été détruits, on calcule le % de puissance perdue sur le pourcentage total de la flotte IA
            if (eventResult.results.ContainsKey(EventResultTypeConstants.LOST_POWER))
            {
                percentageLostPower = ((JsonElement)eventResult.results[EventResultTypeConstants.LOST_POWER]).GetInt32() / (maxPowerDefenseFleet + maxPowerTurretFleet);
            }

            Dictionary<string, int> lostResources = new Dictionary<string, int>();
            if (eventResult.results.ContainsKey(EventResultTypeConstants.LOST_RESOURCES))
                lostResources = ((JsonElement)eventResult.results[EventResultTypeConstants.LOST_RESOURCES]).Deserialize<Dictionary<string, int>>();
            float percentageLostResources = 0f;
            //Si ce n'est pas une attaque de la part de l'IA
            if (!aiAttack)
            {
                //On calcule le % de ressources perdues par rapport au stockage total de l'IA
                foreach (KeyValuePair<string, int> keyValuePair in lostResources)
                {
                    percentageLostResources += (float)keyValuePair.Value / (float)storageCapacities[keyValuePair.Key];
                }
                if (lostResources.Count > 0) percentageLostResources /= lostResources.Count;
            }
            //Si c'est une attaque de la part de l'IA
            else
            {
                //On calcule le % de ressources perdues par rapport au stockage total de l'IA
                foreach (KeyValuePair<string, int> keyValuePair in lostResources)
                {
                    percentageLostResources += (float)keyValuePair.Value / (float)storageCapacities[keyValuePair.Key];
                }
                var playerOwner = MongoDBSingleton.Instance().usersCollection.Find(Builders<User>.Filter.Eq(x => x.id, player)).First();
                var playerBase = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.id, playerOwner.mainBaseId)).First();
                //On calcule le % de ressources perdues par rapport au stockage total du joueur
                foreach (KeyValuePair<string, int> keyValuePair in lostResources)
                {
                    percentageLostResources += (float)keyValuePair.Value / (float)playerBase.ressources[keyValuePair.Key].storage;
                }
                if (lostResources.Count > 0) percentageLostResources /= (2 * lostResources.Count);
            }

            var res = Values.constantattackReputationLoss + Values.variableAttackReputationLoss * (Math.Min(1, percentageLostPower) + Math.Min(1, percentageLostResources)) / 2;
            return res;
        }

        /// <summary>
        /// Gestion d'un EventResult de support
        /// </summary>
        /// <param name="eventResult"> EventResult à traiter</param>
        /// <param name="oldReputation"> Ancienne réputation du joueur</param>
        /// <param name="newReputation"> Nouvelle réputation du joueur</param>
        private void ManageSupportEventResult(EventResult? eventResult, float oldReputation, float newReputation)
        {
            //Le cas d'une alliance sera à traiter dans le futur
            if (MongoDBSingleton.Instance().alliancesCollection.Find(Builders<Alliance>.Filter.Eq(x => x.id, eventResult.userId)).First().type == OwnerTypeConstant.ALLIANCE)
                return;
            if (id.Equals(eventResult.impactedOwnerId))
            {
                //Gain de réputation (le joueur vient aider l'IA)
                newReputation = Math.Min(Values.maxReputation,
                    newReputation + Values.helpReputationGain);

            }
            else if (allyIds.Contains(eventResult.impactedOwnerId))
            {
                //Augmentation de réputation (aide allié)
                newReputation = Math.Min(Values.maxReputation,
                    newReputation + Values.helpReputationGain * Values.allyHelpReputationFactor);
            }
            else if (enemyIds.Contains(eventResult.impactedOwnerId))
            {
                //perte de réputation (aide ennemi)
                newReputation = Math.Max(Values.minReputation,
                    newReputation - Values.helpReputationGain * Values.enemyHelpReputationFactor);
            }
            updateOrSetPlayerReputation(eventResult.userId, oldReputation, newReputation);
        }

        /// <summary>
        /// Gestion d'un EventResult de livraison
        /// </summary>
        /// <param name="eventResult"> EventResult à traiter</param>
        /// <param name="oldReputation"> Ancienne réputation du joueur</param>
        /// <param name="newReputation"> Nouvelle réputation du joueur</param>
        private void ManageDeliveredResourcesEventResult(EventResult? eventResult, float oldReputation, float newReputation)
        {
            //Le cas d'une alliance sera à traiter dans le futur
            if (MongoDBSingleton.Instance().alliancesCollection.Find(Builders<Alliance>.Filter.Eq(x => x.id, eventResult.userId)).First().type == OwnerTypeConstant.ALLIANCE)
                return;
            if (eventResult.userId.Equals(eventResult.impactedOwnerId))
                return;

            var baseFilter = Builders<Base>.Filter.Eq(x => x.id, mainBaseId);
            var mainBase = MongoDBSingleton.Instance().basesCollection.Find(baseFilter).First();

            float multiplicator = 1f;

            //Pour les traders, un pourcentage multiplicatif est appliqué au gain / perte de réputation
            if (personality == Personality.Trader)
                multiplicator = Values.traderTradeReputationMultiplicator;

            //Si aucune ressource n'a été livrée, on retourne
            if (!eventResult.results.ContainsKey(EventResultTypeConstants.DELIVERED_RESOURCES))
                return;

            var resourcesEvent = ((JsonElement)eventResult.results[EventResultTypeConstants.DELIVERED_RESOURCES]).Deserialize<Dictionary<string, int>>();

            //Si la livraison est à destination de l'IA
            if (id.Equals(eventResult.impactedOwnerId))
            {
                //Gain de réputation (le joueur vient donner des ressources à l'IA)
                newReputation = Math.Min(Values.maxReputation,
                    newReputation + ReputationGainOnDelivery(mainBase.ressources, resourcesEvent) * multiplicator);

            }
            //Si la livraison est à destination d'un allié de l'IA
            else if (allyIds.Contains(eventResult.impactedOwnerId))
            {
                //Augmentation de réputation (donne des ressources à un allié)
                newReputation = Math.Min(Values.maxReputation,
                    newReputation + ReputationGainOnDelivery(mainBase.ressources, resourcesEvent) * Values.allyResourcesDeliveryReputationFactor * multiplicator);
            }
            //Si la livraison est à destination d'un ennemi de l'IA
            else if (enemyIds.Contains(eventResult.impactedOwnerId))
            {
                //perte de réputation (donne des ressources à un ennemi)
                newReputation = Math.Max(Values.minReputation,
                    newReputation - ReputationGainOnDelivery(mainBase.ressources, resourcesEvent) * Values.enemyResourcesDeliveryReputationFactor * multiplicator);
            }
            //Mise à jour de la réputation
            updateOrSetPlayerReputation(eventResult.userId, oldReputation, newReputation);
        }

        /// <summary>
        /// Calcul de l'augmentation de la réputation pour une livraison
        /// </summary>
        /// <param name="resourcesBase"></param>
        /// <param name="resourcesEvent"></param>
        /// <returns></returns>
        private float ReputationGainOnDelivery(Dictionary<string, RessourceCumul> resourcesBase, Dictionary<string, int>? resourcesEvent)
        {
            if (resourcesEvent == null)
                return 0f;
            float reputationTotalGain = 0f;

            //L'augmentation de réputation est proportionelle à la quantité de ressources données par rapport au stockage max de l'IA
            foreach (KeyValuePair<string, int> eventKVP in resourcesEvent)
            {
                float vf = resourcesBase[eventKVP.Key].amount;
                float amountGiven = eventKVP.Value;
                float vs = resourcesBase[eventKVP.Key].storage;
                float reputationResourceGain = (Values.maxReputation - Values.minReputation) * Values.resourcesDeliveryFactor * amountGiven / (vs + vf);
                reputationTotalGain += reputationResourceGain;
            }

            return reputationTotalGain;
        }

        /// <summary>
        /// Analyse un EventResult de quête
        /// </summary>
        /// <param name="eventResult"> EventResult à traiter</param>
        /// <param name="oldReputation"> Ancienne réputation du joueur</param>
        /// <param name="newReputation"> Nouvelle réputation du joueur</param>
        private void AnalyseQuestEvent(EventResult eventResult, float oldReputation, float newReputation)
        {
            //Si l'IA n'est pas concernée par la quête, on retourne
            if (!id.Equals(eventResult.impactedOwnerId))
                return;
            //Si la quête a été réussie
            if (((JsonElement)eventResult.results[EventResultTypeConstants.QUEST_STATUS]).GetString() == FinishedQuestStatusConstant.SUCCESS)
            {
                this.QuestSuccessed(eventResult, oldReputation, newReputation);
            }
            //Si la quête a été rejetée
            else if (((JsonElement)eventResult.results[EventResultTypeConstants.QUEST_STATUS]).GetString() == FinishedQuestStatusConstant.REJECTED)
            {
                this.QuestRejected(eventResult, oldReputation, newReputation);
            }
            //Si la quête a été échouée
            else if (((JsonElement)eventResult.results[EventResultTypeConstants.QUEST_STATUS]).GetString() == FinishedQuestStatusConstant.FAILURE)
            {
                this.QuestFailed(eventResult, oldReputation, newReputation);
            }
            //Si la quête a expirée
            else if (((JsonElement)eventResult.results[EventResultTypeConstants.QUEST_STATUS]).GetString() == FinishedQuestStatusConstant.EXPIRED)
            {
                this.QuestExpired(eventResult, oldReputation, newReputation);
            }
        }

        /// <summary>
        /// Gère le cas où la quête est réussie.
        /// Le gain de réputation est donné dans les récompenses de la quête, mais on pourra envisager de faire
        /// d'autres choses dans le futur.
        /// </summary>
        /// <param name="eventResult"> EventResult à traiter</param>
        /// <param name="oldReputation"> Ancienne réputation du joueur</param>
        /// <param name="newReputation"> Nouvelle réputation du joueur</param>
        private void QuestSuccessed(EventResult eventResult, float oldReputation, float newReputation)
        {
        }

        /// <summary>
        /// Gère le cas où une quête est rejetée
        /// </summary>
        /// <param name="eventResult"> EventResult à traiter</param>
        /// <param name="oldReputation"> Ancienne réputation du joueur</param>
        /// <param name="newReputation"> Nouvelle réputation du joueur</param>
        private void QuestRejected(EventResult eventResult, float oldReputation, float newReputation)
        {
            //Si le joueur est allié avc l'IA, il perd en réputation s'il refuse la quête
            if (Utilies.getPlayerReputationStatus(oldReputation) == ReputationStatus.Ally)
            {
                newReputation = Math.Max(Values.minReputation,
                newReputation - Values.questRejectedReputationLoss);
                updateOrSetPlayerReputation(eventResult.userId, oldReputation, newReputation);
            }
        }

        /// <summary>
        /// Gère le cas où une quête est ratée
        /// </summary>
        /// <param name="eventResult"> EventResult à traiter</param>
        /// <param name="oldReputation"> Ancienne réputation du joueur</param>
        /// <param name="newReputation"> Nouvelle réputation du joueur</param>
        private void QuestFailed(EventResult eventResult, float oldReputation, float newReputation)
        {
            //Perte de réputation
            newReputation = Math.Max(Values.minReputation,
                newReputation - Values.questFailedReputationLoss);
            updateOrSetPlayerReputation(eventResult.userId, oldReputation, newReputation);
        }

        /// <summary>
        /// Gère le cas où une quête est expirée
        /// </summary>
        /// <param name="eventResult"> EventResult à traiter</param>
        /// <param name="oldReputation"> Ancienne réputation du joueur</param>
        /// <param name="newReputation"> Nouvelle réputation du joueur</param>
        private void QuestExpired(EventResult eventResult, float oldReputation, float newReputation)
        {
            //Si le joueur est allié ou ami avec une IA, il perd en réputation avec elle
            if (Utilies.getPlayerReputationStatus(oldReputation) == ReputationStatus.Ally || Utilies.getPlayerReputationStatus(oldReputation) == ReputationStatus.Friendly)
            {
                newReputation = Math.Max(Values.minReputation,
                newReputation - Values.questExpiredReputationLoss);
                updateOrSetPlayerReputation(eventResult.userId, oldReputation, newReputation);
            }
        }

        /// <summary>
        /// Met à jour la réputation du joueur en BDD ainsi que la liste des alliés et ennemis de l'IA
        /// </summary>
        /// <param name="playerId"> Identifiant du joueur</param>
        /// <param name="oldReputation"> Ancienne réputation du joueur</param>
        /// <param name="newReputation"> Nouvelle réputation du joueur</param>
        public void updateOrSetPlayerReputation(string playerId, float oldReputation, float newReputation)
        {
            int retry = 0;
            float delta = newReputation - oldReputation;
            var repFilter = Builders<ReputationObject>.Filter.Eq(x => x.id, playerId);
            oldReputation = MongoDBSingleton.Instance().reputationCollection.Find(repFilter).First().reputationValues[id];
            var updateReputation = Builders<ReputationObject>.Update.Set(x => x.reputationValues[id], Math.Min(Math.Max(oldReputation + delta, Values.minReputation), Values.maxReputation));
            //On réessaye dans le cas d'un accès concurrent aux données.
            while (retry < 10)
            {
                try
                {
                    MongoDBSingleton.Instance().reputationCollection.UpdateOne(repFilter, updateReputation);
                    break;

                }
                catch (Exception e)
                {
                    oldReputation = MongoDBSingleton.Instance().reputationCollection.Find(repFilter).First().reputationValues[id];
                    updateReputation = Builders<ReputationObject>.Update.Set(x => x.reputationValues[id], Math.Min(Math.Max(oldReputation + delta, Values.minReputation), Values.maxReputation));
                    retry++;
                }
            }

            // Supprime le joueur du dictonnaire pour le rajouter seulement si besoin est (plus simple à gérer)
            bool wasInPlayersDictionary = playersDictionary.TryGetValue(playerId, out var players);
            if (wasInPlayersDictionary)
                playersDictionary.Remove(playerId);
            var aiBase = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.id, mainBaseId)).First();
            var baseFilter = Builders<Base>.Filter.Eq(x => x.ownerId, playerId);
            var playerBases = MongoDBSingleton.Instance().basesCollection.Find(baseFilter).ToList();
            //Si le joueur à une base dans le rayon de considèration de l'IA, alors on l'ajoute au dictionnaire de joueurs
            //à considérer
            foreach (var b in playerBases)
            {
                if (CommonToolService.distance(aiBase.position, b.position) < Values.interactionRadius)
                {
                    if (wasInPlayersDictionary)
                        playersDictionary.Add(playerId, players);
                    else
                    {
                        playersDictionary.Add(playerId, new Dictionary<PlayerElement, object>());
                        playersDictionary[playerId].Add(PlayerElement.LastAttackTime, DateTime.MinValue);
                        playersDictionary[playerId].Add(PlayerElement.AttackProbabilityBonus, 0);
                        playersDictionary[playerId].Add(PlayerElement.GiftProbabilityBonus, 0);
                        playersDictionary[playerId].Add(PlayerElement.QuestProbabilityBonus, 0);
                    }
                    break;
                }
            }
            //Si le joueur est en dehors de ce rayon, on regarde si sa réputation est suffisament haute ou basse.
            float rep = MongoDBSingleton.Instance().reputationCollection.Find(repFilter).First().reputationValues[id];
            if (!playersDictionary.ContainsKey(playerId))
            {
                if (Utilies.getReputationRatio(rep) >= Values.neutralThresholdFactor || Utilies.getReputationRatio(rep) <= Values.hostileThresholdFactor)
                {
                    if (wasInPlayersDictionary)
                        playersDictionary.Add(playerId, players);
                    else
                    {
                        playersDictionary.Add(playerId, new Dictionary<PlayerElement, object>());
                        playersDictionary[playerId].Add(PlayerElement.LastAttackTime, DateTime.MinValue);
                        playersDictionary[playerId].Add(PlayerElement.AttackProbabilityBonus, 0);
                        playersDictionary[playerId].Add(PlayerElement.GiftProbabilityBonus, 0);
                        playersDictionary[playerId].Add(PlayerElement.QuestProbabilityBonus, 0);
                    }
                }
            }
            //On met à jour la liste des alliés et ennemis de l'IA
            UpdateAlliesEnemies(playerId, rep);
        }

        /// <summary>
        /// Met à jour la liste des alliées et ennemis de l'IA
        /// </summary>
        /// <param name="playerId"> Identifiant du joueur</param>
        /// <param name="reputation"> Réputation du joueur</param>
        private void UpdateAlliesEnemies(string playerId, float reputation)
        {
            var playerFilter = Builders<User>.Filter.Eq(x => x.id, playerId);
            var owner = MongoDBSingleton.Instance().usersCollection.Find(playerFilter).First();

            //Si le joueur est en guerre avec l'IA, on l'ajout à la liste des ennemis s'il n'y était pas déjà
            //Et on met l'IA dans la liste des ennemis du joueur
            if (Utilies.getPlayerReputationStatus(reputation) == ReputationStatus.War)
            {
                if (!enemyIds.Contains(playerId))
                    enemyIds.Add(playerId);
                allyIds.Remove(playerId);
                if (!owner.enemyIds.Contains(id))
                    owner.enemyIds.Add(id);
                owner.allyIds.Remove(id);

            }
            //Si le joueur est allié avec l'IA, on l'ajout à la liste des alliés s'il n'y était pas déjà
            //Et on met l'IA dans la liste des alliés du joueur
            else if (Utilies.getPlayerReputationStatus(reputation) == ReputationStatus.Ally)
            {
                enemyIds.Remove(playerId);
                if (!allyIds.Contains(playerId))
                    allyIds.Add(playerId);
                owner.enemyIds.Remove(id);
                if (!owner.allyIds.Contains(id))
                    owner.allyIds.Add(id);
            }
            //Si le joueur n'est dans aucun de ses status, alors on l'enlève des listes
            else
            {
                enemyIds.Remove(playerId);
                allyIds.Remove(playerId);
                owner.enemyIds.Remove(id);
                owner.allyIds.Remove(id);
            }

            //mise à jour du status du joueur en BDD
            var playerUpdate = Builders<User>.Update.Set(x => x.enemyIds, owner.enemyIds).Set(x => x.allyIds, owner.allyIds);
            MongoDBSingleton.Instance().usersCollection.UpdateOne(playerFilter, playerUpdate);

            //Mise à jour du statut de l'IA en BDD
            if (type == OwnerTypeConstant.AI_DELTA)
            {
                var aiUpdate = Builders<AIDelta>.Update.Set(x => x.allyIds, allyIds).Set(x => x.enemyIds, enemyIds);
                MongoDBSingleton.Instance().aiDeltaCollection.UpdateOne(Builders<AIDelta>.Filter.Eq(x => x.id, id), aiUpdate);
            }
            else
            {
                var aiUpdate = Builders<AIGamma>.Update.Set(x => x.allyIds, allyIds).Set(x => x.enemyIds, enemyIds);
                MongoDBSingleton.Instance().aiGammaCollection.UpdateOne(Builders<AIGamma>.Filter.Eq(x => x.id, id), aiUpdate);
            }
        }

        /// <summary>
        /// Essaye d'ajouter le joueur au dictionnaire des joueurs à considérer
        /// </summary>
        /// <param name="playerId"></param>
        private void TryAddPlayerToConsideredPlayers(string playerId)
        {
            var aiBase = MongoDBSingleton.Instance().basesCollection.Find(Builders<Base>.Filter.Eq(x => x.id, mainBaseId)).First();
            var baseFilter = Builders<Base>.Filter.Eq(x => x.ownerId, playerId);
            var playerBases = MongoDBSingleton.Instance().basesCollection.Find(baseFilter).ToList();
            //si le joueur à une base proche de l'IA, alors on l'ajoute au dictionnaire des joueurs à considérer par l'IA
            foreach (var b in playerBases)
            {
                if (CommonToolService.distance(aiBase.position, b.position) < Values.interactionRadius)
                {
                    //Si le joueur est dans le cercle de considération de l'IA, alors on lui attribue une réputation
                    if (MongoDBSingleton.Instance().reputationCollection.Find(Builders<ReputationObject>.Filter.Eq(x => x.id, playerId)).CountDocuments() > 0 && !MongoDBSingleton.Instance().reputationCollection.Find(Builders<ReputationObject>.Filter.Eq(x => x.id, playerId)).First().reputationValues.ContainsKey(id))
                    {
                        var playerUpdate = Builders<ReputationObject>.Update.Set(x => x.reputationValues[id], 500);
                        MongoDBSingleton.Instance().reputationCollection.UpdateOne(Builders<ReputationObject>.Filter.Eq(x => x.id, playerId), playerUpdate);
                    }
                    playersDictionary.Add(playerId, new Dictionary<PlayerElement, object>());
                    playersDictionary[playerId].Add(PlayerElement.LastAttackTime, DateTime.MinValue);
                    playersDictionary[playerId].Add(PlayerElement.AttackProbabilityBonus, 0);
                    playersDictionary[playerId].Add(PlayerElement.GiftProbabilityBonus, 0);
                    playersDictionary[playerId].Add(PlayerElement.QuestProbabilityBonus, 0);
                    break;
                }
            }
            //S'il est en dehors du rayon de considération de l'IA, on regarde si sa réputation est suffisament haute ou basse pour le considérer
            if (!playersDictionary.ContainsKey(playerId))
            {
                var filter = Builders<ReputationObject>.Filter.Eq(x => x.id, playerId);
                if (MongoDBSingleton.Instance().reputationCollection.Find(filter).CountDocuments() > 0 && MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues.ContainsKey(id))
                {
                    float rep = MongoDBSingleton.Instance().reputationCollection.Find(filter).First().reputationValues[id];
                    if (Utilies.getReputationRatio(rep) >= Values.neutralThresholdFactor || Utilies.getReputationRatio(rep) <= Values.hostileThresholdFactor)
                    {
                        playersDictionary.Add(playerId, new Dictionary<PlayerElement, object>());
                        playersDictionary[playerId].Add(PlayerElement.LastAttackTime, DateTime.MinValue);
                        playersDictionary[playerId].Add(PlayerElement.AttackProbabilityBonus, 0);
                        playersDictionary[playerId].Add(PlayerElement.GiftProbabilityBonus, 0);
                        playersDictionary[playerId].Add(PlayerElement.QuestProbabilityBonus, 0);
                    }
                }
            }
        }

        /// <summary>
        /// Boucle de comportement de l'IA
        /// </summary>
        /// <param name="rand"> Random</param>
        /// <param name="channel"> Channel pour publier un Event RabbitMQ </param>
        /// <param name="properties"> propriétés pour le formatage correct du message RabbitMQ </param>
        private void AIBehaviorLoop(Random rand, IModel channel, IBasicProperties properties)
        {
            var now = DateTime.Now;
            //Si l'IA a mis moins de IASleepTime secondes pour la boucle précédente, alors on dort pendant un certain temps.
            if (now.CompareTo(lastSleepTime.AddSeconds(Values.IAsleepTime)) < 0)
            {
                Thread.Sleep(lastSleepTime.AddSeconds(Values.IAsleepTime) - now);
            }

            var baseFilter = Builders<Base>.Filter.Eq(x => x.id, mainBaseId);
            var mainBase = MongoDBSingleton.Instance().basesCollection.Find(baseFilter).First();
            //Pour chacun des joueurs ayant une réputation avec l'IA et étant considérés par celle-ci
            foreach (var player in MongoDBSingleton.Instance().reputationCollection.AsQueryable().
                Where(x => playersDictionary.Keys.Contains(x.id)).
                Select(x => new Tuple<string, float>(x.id, x.reputationValues[id])))
            {
                try
                {
                    //On tente de faire des actions avec le joueur
                    var reputation = new List<float> { player.Item2, player.Item2 };
                    ChooseAndExecuteAction(rand, player.Item1, channel, properties, reputation[0], mainBase);
                    reputation[1] = Values.baseReputation + (reputation[1] - Values.baseReputation) * Values.reputationDecay;
                    //On met à jour la réputation avec un decay.
                    //La réputation tend en effet vers la valeur centrale au fil du temps, si aucun gain ou perte n'est appliqué.
                    updateOrSetPlayerReputation(player.Item1, reputation[0], reputation[1]);
                }
                catch (Exception e)
                {
                    Utilies.WriteErrorLog(e, this);
                }
            }
            lastSleepTime = lastSleepTime.AddSeconds(Values.IAsleepTime);


            //La base accumule des ressources
            mainBase.accumulateRessources(Values.ressourceAccumulationPercentage);
            var baseUpdate = Builders<Base>.Update.Set(x => x.ressources, mainBase.ressources);
            MongoDBSingleton.Instance().basesCollection.UpdateOne(baseFilter, baseUpdate);
        }
    }
}