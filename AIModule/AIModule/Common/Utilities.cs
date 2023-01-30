using AIModule.Common.constant.Base;
using AIModule.Events;
using AIModule.Owners;
using MongoDB.Driver;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using static AIModule.Common.constant.owner.Personality;

namespace AIModule.Common
{
    #region Enum

    public enum ReputationStatus { War, Hostile, Neutral, Friendly, Ally }

    public enum PlayerElement
    {
        LastAttackTime,
        GiftProbabilityBonus,
        AttackProbabilityBonus,
        QuestProbabilityBonus
    }

    public enum InteractionType
    {
        Attack,
        Quest,
        Gift
    }

    public enum InteractionInformation
    {
        MaximumProbability,
        ProbabilityBonus
    }

    #endregion

    public class TemplateElement
    {
        public string fleetName { get; set; }
        public int maxFleetNumber { get; set; }
        public int fleetPower { get; set; }

        public TemplateElement(string fleetName, int maxFleetNumber, int fleetPower)
        {
            this.fleetName = fleetName;
            this.maxFleetNumber = maxFleetNumber;
            this.fleetPower = fleetPower;
        }

        public TemplateElement() { }
    }

    public static class Values
    {
        #region ReputationValues
        public const int maxReputation = 1000;
        public const int minReputation = 0;
        public const int baseReputation = 500;


        /// <summary>
        /// Ratio réputation <= 0.2 : war
        /// </summary>
        public const float warThresholdFactor = .2f;
        /// <summary>
        /// Ratio réputation <= 0.4 et > 0.2 : unfriedly
        /// </summary>
        public const float hostileThresholdFactor = .4f;
        /// <summary>
        /// Ratio réputation <= 0.6 et > 0.4 : neutral
        /// </summary>
        public const float neutralThresholdFactor = .6f;
        /// <summary>
        /// Ratio réputation <= 0.8 et > 0.6 : friendly
        /// Ratio réputation <= 1 et > 0.8 : ally
        /// </summary>
        public const float friendlyThresholdFactor = .8f;
        #endregion

        #region ReputationVariationValues
        //Reputation towards warmonger increases when fighting other entites
        public const int warmongerCombatReputationOffset = 10;
        //Reputation towards pacifist decreases when fighting other entites
        public const int pacifistCombatReputationOffset = 10;
        //Trading with other entites is considered more important by traders
        public const float traderTradeReputationMultiplicator = 1.1f;
        public const int constantattackReputationLoss = 25;
        public const int variableAttackReputationLoss = 200;
        public const float allyRepLoseFactor = .2f;
        public const float enemyRepWinFactor = .2f;
        public const int questRejectedReputationLoss = 3;
        public const int questExpiredReputationLoss = 0;
        public const int questFailedReputationLoss = 2;
        public const int helpReputationGain = 5;
        public const float allyHelpReputationFactor = .2f;
        public const float enemyHelpReputationFactor = .2f;
        public const float resourcesDeliveryFactor = 0.25f;
        public const float allyResourcesDeliveryReputationFactor = .2f;
        public const float enemyResourcesDeliveryReputationFactor = .2f;

        private const float percentageReputationLostPerDay = 5f * speedMultiplicatorFactor;
        private const float dailyMultiplicator = 1f - percentageReputationLostPerDay / 100;
        public static readonly float reputationDecay = (float)Math.Pow(dailyMultiplicator, 1f / nbActionPerDay);
        #endregion


        #region Actions
        private const float speedMultiplicatorFactor = 1;

        public const float nbActionPerDay = (int)(1440f * 0.1 * speedMultiplicatorFactor);
        //Sleep time in seconds
        public const float IAsleepTime = 60 * 1440f / nbActionPerDay;

        //In seconds
        public const int attackCooldown = 300;
        //In seconds
        public const float fleetsBuildCooldown = 300;

        public const int interactionRadius = 15000;
        public const float ressourceAccumulationPercentage = 5f;
        #endregion

        //Cette section sera utile lorsque le commerce sera implémenté
        #region Trade
        public static readonly Dictionary<string, int> marketPrices = new Dictionary<string, int>
        {
            { RessourcesConstant.CRISTAL, 1 },
            { RessourcesConstant.METAL, 1 },
            { RessourcesConstant.ORGANIC, 1 },
            { RessourcesConstant.ENERGY, 1 }
        };
        /// <summary>
        /// Liste des ressources existantes
        /// </summary>
        public static readonly List<string> ressources = new List<string>
        {
            RessourcesConstant.CRISTAL, RessourcesConstant.METAL, RessourcesConstant.ENERGY, RessourcesConstant.ORGANIC
        };
        public const float ressourcePreferenceFactor = 2f;
        public const float ressourceSaturationFactor = 2f;
        #endregion

        #region Fleets
        public const float fleetDepletionThreshold = .1f;
        public static readonly float[] fleetReplenishThreshold = { .5f, 1f, .75f };
        public const int fleetsReplenishCooldown = 100;
        #endregion

        #region Quests
        public const int playerMaxAiQuestNumber = 5;
        #endregion

        #region AttackAndDefense
        public const int maxPowerGapFactor = 3;
        public const int minAttackPower = 10;
        public const float helpDefendProbability = 0.1f;
        public const float helpAttackProbability = 0.1f;
        #endregion

        /// <summary>
        /// Dictionnaire avec en
        /// 1ère clé la personnalité de l'IA : pacifiste, commerçante, etc...
        /// 2ème clé le type d'intéraction : attaque, quête, etc...
        /// 3ème clé le type d'information voulue : facteur multiplicatif de proba, la valeur max de proba, etc...
        /// 4ème clé le status de réputation : guerre, hostile, etc...
        /// en valeur la valeur recherchée
        /// </summary>
        private static ImmutableDictionary<(string, InteractionType, InteractionInformation, ReputationStatus), float> interactionsProbabilitiesInformations = new Dictionary<(string, InteractionType, InteractionInformation, ReputationStatus), float>()
        {
            #region Pacifist
#region Attack
            { (Pacifist, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.War), .25f},
            { (Pacifist, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.Hostile), 0f},
            { (Pacifist, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.Neutral), 0f},
            { (Pacifist, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.Friendly), 0f},
            { (Pacifist, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.Ally), 0f},

            { (Pacifist, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.War), .01f},
            { (Pacifist, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.Hostile), 0f},
            { (Pacifist, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.Neutral),  0f},
            { (Pacifist, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.Friendly), 0f},
            { (Pacifist, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.Ally), 0f},
            #endregion
#region Quest
            { (Pacifist, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.War), 0f},
            { (Pacifist, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.Hostile), 0.1f},
            { (Pacifist, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.Neutral), 0.3f},
            { (Pacifist, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.Friendly), 0.5f},
            { (Pacifist, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.Ally), 1f},

            { (Pacifist, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.War), 0f},
            { (Pacifist, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.Hostile),  0.000033f},
            { (Pacifist, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.Neutral), 0.00066f},
            { (Pacifist, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.Friendly), 0.0033f},
            { (Pacifist, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.Ally), 0.01f},
            #endregion
#region Gift
            { (Pacifist, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.War), 0f},
            { (Pacifist, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.Hostile), 0f},
            { (Pacifist, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.Neutral), 0f},
            { (Pacifist, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.Friendly), 0.3f},
            { (Pacifist, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.Ally), 0.5f},

            { (Pacifist, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.War), 0f},
            { (Pacifist, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.Hostile), 0f},
            { (Pacifist, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.Neutral), 0f},
            { (Pacifist, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.Friendly), 0.01f},
            { (Pacifist, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.Ally), 0.0166f},
            #endregion
            #endregion
            #region Warmonger
#region Attack
            { (Warmonger, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.War), 1f},
            { (Warmonger, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.Hostile), 0.4f},
            { (Warmonger, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.Neutral), 0.1f},
            { (Warmonger, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.Friendly), 0f},
            { (Warmonger, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.Ally), 0f},

            { (Warmonger, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.War), .033f},
            { (Warmonger, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.Hostile), 0.0066f},
            { (Warmonger, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.Neutral), 0.00033f},
            { (Warmonger, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.Friendly), 0f},
            { (Warmonger, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.Ally), 0f},
            #endregion
#region Quest
            { (Warmonger, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.War), 0f},
            { (Warmonger, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.Hostile), 0f},
            { (Warmonger, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.Neutral), 0.3f},
            { (Warmonger, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.Friendly), 0.5f},
            { (Warmonger, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.Ally), 1f},

            { (Warmonger, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.War), 0f},
            { (Warmonger, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.Hostile), 0f},
            { (Warmonger, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.Neutral), 0.00033f},
            { (Warmonger, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.Friendly), 0.0033f},
            { (Warmonger, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.Ally),  0.0166f},
            #endregion
#region Gift
            { (Warmonger, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.War), 0f},
            { (Warmonger, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.Hostile), 0f},
            { (Warmonger, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.Neutral), 0f},
            { (Warmonger, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.Friendly), 0.25f},
            { (Warmonger, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.Ally), 0.4f},

            { (Warmonger, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.War), 0f},
            { (Warmonger, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.Hostile), 0f},
            { (Warmonger, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.Neutral), 0f},
            { (Warmonger, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.Friendly), 0.0033f},
            { (Warmonger, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.Ally), 0.01f},
            #endregion
            #endregion
            #region Trader
#region Attack
            { (Trader, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.War), .5f},
            { (Trader, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.Hostile), 0.25f},
            { (Trader, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.Neutral), 0f},
            { (Trader, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.Friendly), 0f},
            { (Trader, InteractionType.Attack, InteractionInformation.MaximumProbability, ReputationStatus.Ally), 0f},

            { (Trader, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.War), .01f},
            { (Trader, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.Hostile), 0.00166f},
            { (Trader, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.Neutral), 0f},
            { (Trader, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.Friendly), 0f},
            { (Trader, InteractionType.Attack, InteractionInformation.ProbabilityBonus, ReputationStatus.Ally), 0f},
            #endregion
#region Quest
            { (Trader, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.War), 0f},
            { (Trader, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.Hostile), 0f},
            { (Trader, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.Neutral), 0.3f},
            { (Trader, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.Friendly), 0.5f},
            { (Trader, InteractionType.Quest, InteractionInformation.MaximumProbability, ReputationStatus.Ally), 1f},

            { (Trader, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.War), 0f},
            { (Trader, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.Hostile), 0f},
            { (Trader, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.Neutral), 0.001f},
            { (Trader, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.Friendly), 0.0066f},
            { (Trader, InteractionType.Quest, InteractionInformation.ProbabilityBonus, ReputationStatus.Ally), 0.0166f},
            #endregion
#region Gift
            { (Trader, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.War), 0f},
            { (Trader, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.Hostile), 0f},
            { (Trader, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.Neutral), 0.1f},
            { (Trader, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.Friendly), 0.5f},
            { (Trader, InteractionType.Gift, InteractionInformation.MaximumProbability, ReputationStatus.Ally), 0.7f},

            { (Trader, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.War), 0f},
            { (Trader, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.Hostile), 0f},
            { (Trader, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.Neutral), 0.0033f},
            { (Trader, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.Friendly), 0.0166f},
            { (Trader, InteractionType.Gift, InteractionInformation.ProbabilityBonus, ReputationStatus.Ally), 0.033f},
#endregion
            #endregion
        }.ToImmutableDictionary();

        public static ImmutableDictionary<(string, InteractionType, InteractionInformation, ReputationStatus), float> InteractionsProbabilitiesInformations { get { return interactionsProbabilitiesInformations; } }

    }

    public static class Utilies
    {
        //Method to send Events into RabbitMQ; the Event must have already been stored in the database
        //It verifies that the event was previously stored in the database
        public static void SendEvent(Event sendEvent, IModel channel, IBasicProperties properties)
        {

            if (MongoDBSingleton.Instance().eventsCollections.Find(Builders<Event>.Filter.Eq(x => x.id, sendEvent.id)).CountDocuments() == 0)
                throw new ResourceNotFoundException("The event was not stored in the database");

            var Event = new Event(sendEvent);
            var jsonMsg = JsonSerializer.Serialize(Event);
            var body = Encoding.UTF8.GetBytes(jsonMsg);


            channel.BasicPublish(exchange: "",
                                            routingKey: "toSchedule",
                                            basicProperties: properties,
                                            body: body);
        }


        //Returns how the player is considered by the AI regarding to its reputation for this AI
        public static ReputationStatus getPlayerReputationStatus(float reputation)
        {
            float percentage = getReputationRatio(reputation);
            switch (percentage)
            {
                case < Values.warThresholdFactor:
                    return ReputationStatus.War;
                case < Values.hostileThresholdFactor:
                    return ReputationStatus.Hostile;
                case < Values.neutralThresholdFactor:
                    return ReputationStatus.Neutral;
                case < Values.friendlyThresholdFactor:
                    return ReputationStatus.Friendly;
                default:
                    return ReputationStatus.Ally;
            }
        }

        public static float getReputationRatio(float rep)
        {
            return (rep - Values.minReputation) / (Values.maxReputation - Values.minReputation);
        }


        public static void WriteEventCreationLog(string evnt, string evnt_id, AI.AI ai)
        {
            int nbrOfTries = 0;
        tryAgain:
            try
            {
                using (StreamWriter fs = File.AppendText(Path.Combine(Directory.GetCurrentDirectory(), "AIlogs.txt")))
                {
                    fs.WriteLineAsync(string.Format("{0:s}: {1} (id : {2}) created event {3} of id {4}", DateTime.Now, ai.name, ai.id, evnt, evnt_id));
                }
                Console.WriteLine(string.Format("{0:s}: {1} (id : {2}) created event {3} of id {4}", DateTime.Now, ai.name, ai.id, evnt, evnt_id));
            }
            catch (Exception ex)
            {
                nbrOfTries++;
                if (nbrOfTries < 10)
                    goto tryAgain;
                else return;
            }

            try
            {
                //Compression du fichier si de taille > 100Mo
                if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt")))
                {
                    var fileInfos = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt"));
                    if (fileInfos.Length > 100000000)
                    {
                        string file = Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt");
                        string newFileName = string.Format(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs_{0}.gz"), File.GetLastWriteTime(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt")).ToString("yyyy'-'MM'-'dd'_'HH'-'mm'-'ss"));
                        using FileStream originalFileStream = File.Open(file, FileMode.Open);
                        using FileStream compressedFileStream = File.Create(newFileName);
                        using var compressor = new GZipStream(compressedFileStream, CompressionMode.Compress);
                        originalFileStream.CopyTo(compressor);
                        File.Create(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt"));
                    }
                }
                var files = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "logs"), "AIlogs_*").Select(x => new FileInfo(x)).ToList();
                files.Sort((x, y) => y.CreationTime.CompareTo(x.CreationTime));
                while(files.Count > 10)
                {
                    files[files.Count - 1].Delete();
                    files.RemoveAt(files.Count - 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void WriteEventResultReceivedLog(EventResult eventResult, AI.AI ai)
        {
            int nbrOfTries = 0;
        tryAgain:
            try
            {
                using (StreamWriter fs = File.AppendText(Path.Combine(Directory.GetCurrentDirectory(), "AIlogs.txt")))
                {
                    fs.WriteLineAsync(string.Format("{0:s} : {1} (id : {2}) received eventResult of type {3} (id : {4})",DateTime.Now, ai.name, ai.id, eventResult.eventType, eventResult.id));
                }
                Console.WriteLine("{0:s} : {1} (id : {2}) received eventResult of type {3} (id : {4})", DateTime.Now, ai.name, ai.id, eventResult.eventType, eventResult.id);
            }
            catch (Exception ex)
            {
                nbrOfTries++;
                if (nbrOfTries < 10)
                    goto tryAgain;
                else return;
            }

            

            try
            {
                //Compression du fichier si de taille > 100Mo
                if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt")))
                {
                    var fileInfos = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt"));
                    if (fileInfos.Length > 100000000)
                    {
                        string file = Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt");
                        string newFileName = string.Format(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs_{0}.gz"), File.GetLastWriteTime(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt")).ToString("yyyy'-'MM'-'dd'_'HH'-'mm'-'ss"));
                        using FileStream originalFileStream = File.Open(file, FileMode.Open);
                        using FileStream compressedFileStream = File.Create(newFileName);
                        using var compressor = new GZipStream(compressedFileStream, CompressionMode.Compress);
                        originalFileStream.CopyTo(compressor);
                        File.Create(Path.Combine(Directory.GetCurrentDirectory(), "logs", "AIlogs.txt"));
                    }
                }
                var files = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "logs"), "AIlogs_*").Select(x => new FileInfo(x)).ToList();
                files.Sort((x, y) => y.CreationTime.CompareTo(x.CreationTime));
                while (files.Count > 10)
                {
                    files[files.Count - 1].Delete();
                    files.RemoveAt(files.Count - 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void WriteErrorLog(Exception exception, AI.AI ai)
        {
            int nbrOfTries = 0;
        tryAgain:
            try
            {
                using (StreamWriter fs = File.AppendText(Path.Combine(Directory.GetCurrentDirectory(), "Error_logs.txt")))
                {
                    fs.WriteLineAsync(string.Format("{0} | {1} : {2}", DateTime.Now,ai.name, exception.ToString()));
                }
                Console.WriteLine("{0} : Found error, storing it in log file", ai.name);
            }
            catch (Exception ex)
            {
                nbrOfTries++;
                if (nbrOfTries < 10)
                    goto tryAgain;
                else return;
            }

            try
            {
                //Compression du fichier si de taille > 100Mo
                if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "logs", "Error_logs.txt")))
                {
                    var fileInfos = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "logs", "Error_logs.txt"));
                    if (fileInfos.Length > 100000000)
                    {
                        string file = Path.Combine(Directory.GetCurrentDirectory(), "logs", "Error_logs.txt");
                        string newFileName = string.Format(Path.Combine(Directory.GetCurrentDirectory(), "logs","Error_logs_{0}.gz"), File.GetLastWriteTime(Path.Combine(Directory.GetCurrentDirectory(), "logs", "Error_logs.txt")).ToString("yyyy'-'MM'-'dd'_'HH'-'mm'-'ss"));
                        using FileStream originalFileStream = File.Open(file, FileMode.Open);
                        using FileStream compressedFileStream = File.Create(newFileName);
                        using var compressor = new GZipStream(compressedFileStream, CompressionMode.Compress);
                        originalFileStream.CopyTo(compressor);
                        File.Create(Path.Combine(Directory.GetCurrentDirectory(), "logs", "Error_logs.txt"));
                    }
                }
                var files = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "logs"), "Error_logs_*").Select(x => new FileInfo(x)).ToList();
                files.Sort((x, y) => y.CreationTime.CompareTo(x.CreationTime));
                while (files.Count > 10)
                {
                    files[files.Count - 1].Delete();
                    files.RemoveAt(files.Count - 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        
    }
}
