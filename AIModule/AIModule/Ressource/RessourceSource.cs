using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Ressource
{
    public class RessourceSource
    {
        public string libelle { get; set; }

        public int amount { get; set; }         //Valeur

        public RessourceSource(string libelle, int amount)
        {
            this.libelle = libelle;
            this.amount = amount;
        }

        public RessourceSource() { }
    }
}
