using AIModule.Common.constant.owner;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Owners
{
    [BsonIgnoreExtraElements]
    public class Alliance : AllianceRef
    {
        public Dictionary<string, string> members { get; set; }
        public Dictionary<string, int> memberRoles { get; set; }
        public List<string> libelleRole { get; set; }

        /*Indique les candidatures/invitations. Id, boolean.
        * Vrai si invitation (validation nécessaire par le joueur), faux si candidature (validation nécessaire par l'alliance)*/
        public Dictionary<string, bool> pendingMembers { get; set; }
        public Dictionary<string, string> assignedFleet { get; set; }        //Permet d'assigner des flottes d'alliance a des membres

        public int maxMember { get; set; }

        public Alliance(AllianceRef allianceRef, string ownerId, string userName)
        {

            libelleRole = new List<string>
            {
                "Owner",
                "Leader",
                "Manager",
                "Member",
                "Newcommer"
            };
            assignedFleet = new Dictionary<string, string>();

            name = allianceRef.name;
            shortName = allianceRef.shortName;
            description = allianceRef.description;
            members = new Dictionary<string, string>();
            members[ownerId] = userName;
            memberRoles = new Dictionary<string, int>();
            memberRoles[ownerId] = 0;
            pendingMembers = new Dictionary<string, bool>();
            baseListId = new Dictionary<string, string>();
            maxMember = 10;
            fleetListId = new List<string>();
            effects = new Dictionary<string, int>();
            effectSources = new Dictionary<string, string>();

            researchMap = new Dictionary<string, int>();
            pendingResearch = new Dictionary<string, DateTime>();

            leaderId = ownerId;
            leaderName = userName;

            type = OwnerTypeConstant.ALLIANCE;
            money = 0;
        }
    }
}
