using AIModule.Common.constant.Base;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Report
{
    public class ReportRef
    {
        [BsonId]
        public string id { get; set; }

        public string uuid { get; set; }

        public string libelle { get; set; }   //Texte a afficher dans la liste de message

        public string type { get; set; }

        [BsonElement("userId")]
        public List<string> userIds { get; set; }     //Utilisateur concernés par le rapport

        public DateTime date { get; set; }

        public DateTime expirationDate { get; set; }

        public GlobalPosition position { get; set; }

        public ReportRef buildRef() { return new ReportRef(id, uuid, libelle, type, userIds, date, expirationDate, position); }

        public ReportRef() { }

        public ReportRef(string id, string uuid, string libelle, string type, List<string> userIds, DateTime date, DateTime expirationDate, GlobalPosition position)
        {
            this.id = id;
            this.uuid = uuid;
            this.libelle = libelle;
            this.type = type;
            this.userIds = userIds;
            this.date = date;
            this.expirationDate = expirationDate;
            this.position = position;
        }
    }
}
