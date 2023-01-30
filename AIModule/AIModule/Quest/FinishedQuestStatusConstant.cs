using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Quest
{
    public class FinishedQuestStatusConstant
    {
        public const string SUCCESS = "SUCCESS";
        public const string FAILURE = "FAILURE";
        public const string EXPIRED = "EXPIRED";
        public const string REJECTED = "REJECTED";

        private string code;

        private string libelle;

        public String getCode()
        {
            return code;
        }

        public String getLibelle()
        {
            return libelle;
        }

        public FinishedQuestStatusConstant(String code, String libelle)
        {
            this.code = code;
            this.libelle = libelle;
        }
    }
}