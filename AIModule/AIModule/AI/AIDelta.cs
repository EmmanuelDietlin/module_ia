using AIModule.Common.constant.owner;
using AIModule.Common;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.AI
{
    [BsonIgnoreExtraElements]
    public class AIDelta : AI
    {
        public AIDelta() : base() { }

        public override void runAI(ConnectionFactory factory)
        {
            Console.CancelKeyPress += delegate
            {
                MongoDBSingleton.Instance().aiDeltaCollection.ReplaceOne(Builders<AIDelta>.Filter.Eq(x => x.id, id), this);
                return;
            };
            base.runAI(factory);
        }
    }
}
