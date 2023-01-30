using AIModule.AI;
using AIModule.Bases;
using AIModule.Events;
using AIModule.Fight;
using AIModule.Owners;
using AIModule.Quest;
using AIModule.Reputation;
using AIModule.Ressource;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIModule.Common
{
    public sealed class MongoDBSingleton
    {
        private static MongoDBSingleton _instance;

        public IMongoDatabase database { get; private set; }
        public IMongoCollection<Base> basesCollection { get; private set; }
        public IMongoCollection<Fleet> fleetsCollection { get; private set; }
        public IMongoCollection<FleetMoveEvent> moveEventsCollection { get; private set; }
        public IMongoCollection<User> usersCollection { get; private set; }
        public IMongoCollection<RessourceDetail> ressourcesDetailCollection { get; private set; }
        public IMongoCollection<ReputationObject> reputationCollection { get; private set; }
        public IMongoCollection<Event> eventsCollections { get; private set; }
        public IMongoCollection<QuestTemplate> questTemplatesCollection { get; private set; }
        public IMongoCollection<WaitingQuest> waitingQuestsCollection { get; private set; }
        public IMongoCollection<Alliance> alliancesCollection { get; private set; }
        public IMongoCollection<AIDelta> aiDeltaCollection { get; private set; }
        public IMongoCollection<AIGamma> aiGammaCollection { get; private set; }
        public IMongoCollection<BuildingRef> buildingsCollection { get; private set; }



        public string mongoAddress { get; set; }

        public string mongoName { get; set; }


        private MongoDBSingleton(ConnectionInfos infos)
        {
            mongoAddress = infos.mongodb.uri;
            mongoName = infos.mongodb.database;
            var client = new MongoClient(mongoAddress);
            database = client.GetDatabase(mongoName);
            basesCollection = database.GetCollection<Base>("bases");
            moveEventsCollection = database.GetCollection<FleetMoveEvent>("events");
            usersCollection = database.GetCollection<User>("owners");
            ressourcesDetailCollection = database.GetCollection<RessourceDetail>("ressourceDetail");
            fleetsCollection = database.GetCollection<Fleet>("fleets");
            reputationCollection = database.GetCollection<ReputationObject>("reputation");
            eventsCollections = database.GetCollection<Event>("events");
            questTemplatesCollection = database.GetCollection<QuestTemplate>("quest_template");
            waitingQuestsCollection = database.GetCollection<WaitingQuest>("waiting_quest");
            alliancesCollection = database.GetCollection<Alliance>("owners");
            aiDeltaCollection = database.GetCollection<AIDelta>("owners");
            aiGammaCollection = database.GetCollection<AIGamma>("owners");
            buildingsCollection = database.GetCollection<BuildingRef>("buildings");
        }

        public static MongoDBSingleton Instance()
        {
            if (_instance == null)
            {
                ConnectionInfos infos;
                string connectionInfos = Path.Combine(Directory.GetCurrentDirectory(),"connectionInfos.json");

                using (var reader = new StreamReader(connectionInfos))
                {
                    string json = reader.ReadToEnd();
                    infos = JsonSerializer.Deserialize<ConnectionInfos>(json);
                }
                _instance = new MongoDBSingleton(infos);
            }
            return _instance;
        }

        //public IMongo


    }
}
