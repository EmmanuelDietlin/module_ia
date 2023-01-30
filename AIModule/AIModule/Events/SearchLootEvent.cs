using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIModule.Common.constant;
using AIModule.Common.constant.Base;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AIModule.Events
{
    internal class SearchLootEvent : Event
    {
        [BsonElement("base")]
        public string baseId { get; set; }             //Base où à lieu la construction

        public List<GlobalPosition> targets = new List<GlobalPosition>();

        public SearchLootEvent() : base()
        {
            this.type = EventTypeConstant.SEARCH;
            this.creation = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            this.baseId = "";
        }
    }
}
