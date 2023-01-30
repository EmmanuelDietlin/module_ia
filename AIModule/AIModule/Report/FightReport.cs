using AIModule.Common.constant;
using AIModule.Fight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Report
{
    public class FightReport : ReportRef
    {
        //Optionel, base où à eu lieu le combat
        public string baseId { get; set; }
        public string baseName { get; set; }

        public string baseOwnerName { get; set; }
        public string baseOwnerId { get; set; }

        public DateTime fin;
        public Dictionary<string, Fleet> attackInitialFleet { get; set; }
        public Dictionary<string, Fleet> attackFinalFleet { get; set; }
        public Dictionary<string, Fleet> defenseInitialFleet { get; set; }
        public Dictionary<string, Fleet> defenseFinalFleet { get; set; }

        public Dictionary<int, FightRound> rounds { get; set; }

        public Dictionary<string, Dictionary<string, int>> pilled { get; set; }     //Liste des ressources pillées par flotte

        public FightReport()
        {
            this.uuid = Guid.NewGuid().ToString();
            this.date = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            this.expirationDate = DateTime.SpecifyKind(DateTime.Now.AddDays(7), DateTimeKind.Unspecified);
            this.attackInitialFleet = new Dictionary<string, Fleet>();
            this.attackFinalFleet = new Dictionary<string, Fleet>();
            this.defenseInitialFleet = new Dictionary<string, Fleet>();
            this.defenseFinalFleet = new Dictionary<string, Fleet>();
            this.rounds = new Dictionary<int, FightRound>();
            this.pilled = new Dictionary<string, Dictionary<string, int>>();
            //hérité
            this.userIds = new List<string>();
            this.libelle = "Rapport de combat";
            this.type = ReportTypeConstant.FIGHT;
        }

        public FightReport(FightReport report)
        {
            this.uuid = Guid.NewGuid().ToString();
            this.date = report.date;
            this.expirationDate = report.expirationDate;
            this.attackInitialFleet = report.attackInitialFleet;
            this.attackFinalFleet = report.attackFinalFleet;
            this.defenseInitialFleet = report.defenseInitialFleet;
            this.defenseFinalFleet = report.defenseFinalFleet;
            this.rounds = report.rounds;
            this.pilled = report.pilled;

            this.baseId = report.baseId;
            this.baseName = report.baseName;

            this.baseOwnerName = report.baseOwnerName;
            this.baseOwnerId = report.baseOwnerId;
            this.position = report.position;

            this.fin = report.fin;
            //hérité
            this.userIds = report.userIds;
            this.libelle = report.libelle;
            this.type = ReportTypeConstant.FIGHT;
        }
    }
}
