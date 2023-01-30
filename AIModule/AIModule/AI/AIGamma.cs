using AIModule.Common;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.AI
{
    [BsonIgnoreExtraElements]
    public class AIGamma : AI
    {
        public AIGamma() : base() { }

        public override void runAI(ConnectionFactory factory)
        {
            Console.CancelKeyPress += delegate
            {
                MongoDBSingleton.Instance().aiGammaCollection.ReplaceOne(Builders<AIGamma>.Filter.Eq(x => x.id, id), this);
                return;
            };
            base.runAI(factory);
        }
    }
}
