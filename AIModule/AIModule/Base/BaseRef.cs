using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace AIModule.Bases
{
    public class BaseRef
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id;

        public string name;

        public bool visible;

        public string type;

        public string ownerId;

        public string ownerName;

        public bool isAttackable;

        public BaseRef() { }

        public BaseRef(string id, string name, bool visible, string type, string ownerId, string ownerName, bool isAttackable)
        {
            this.id = id;
            this.name = name;
            this.visible = visible;
            this.type = type;
            this.ownerId = ownerId;
            this.ownerName = ownerName;
            this.isAttackable = isAttackable;
        }

        public BaseRef buildRef()
        {
            return new BaseRef(id, name, visible, type, ownerId, ownerName, isAttackable);
        }

        public BaseRef(BaseRef baseRef)
        {
            this.id = baseRef.id;
            this.name = baseRef.name;
            this.visible = baseRef.visible;
            this.type = baseRef.type;
            this.ownerId = baseRef.ownerId;
            this.ownerName= baseRef.ownerName;
            this.isAttackable= baseRef.isAttackable;
        }

    }
}
