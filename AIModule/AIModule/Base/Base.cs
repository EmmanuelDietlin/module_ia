using AIModule.Common.constant.Base;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Bases
{
    [BsonIgnoreExtraElements]
    public class Base : BaseRef
    {
        //[BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<string, BaseSlot> slots { get; set; }     //Liste des emplacements avec les batiments associés

        public Dictionary<string, RessourceCumul> ressources { get; set; }


        public Dictionary<string, int> effects { get; set; }


        public Dictionary<string, string> effectSource { get; set; }      //Code, id


        public GlobalPosition position { get; set; }

        public Dictionary<string, int> shipStorage { get; set; }

        public string turretFleet { get; set; }

        //Indique si un combat est en cours sur la base et si oui l'id de l'event
        public string pendingFightId { get; set; }

        //data duplication
        public Dictionary<string, int> buildingList { get; set; }

        public Dictionary<string, int> buildingAmount { get; set; }

        public int curentConstruction { get; set; }

        public int curentShipConstruction { get; set; }

        public int curentTurretConstruction { get; set; }

        public int currentSearch { get; set; }

        public bool hasPortal { get; set; }

        public long version { get; set; }

        public string _class { get; set; } = "fr.need_a_name.bdd_module.base.entity.Base";

        
        public void recalcDuplicate()
        {
            Dictionary<string, int> buildingList = new Dictionary<string, int>();
            Dictionary<string, int> buildingAmount = new Dictionary<string, int>();
            foreach (var slot in slots)
            {
                Built built = slot.Value.built;
                if (built != null)
                {
                    if (built.HP > 0)
                    {
                        string buildingId = built.building.id;
                        if (buildingList.ContainsKey(buildingId))
                        {                    //Si le batiment est déjà dans la liste
                            if (buildingList[buildingId] < built.level)
                            {     //On le met a jour si son niveau est plus élevé
                                buildingList[buildingId] = built.level;
                            }
                            buildingAmount[buildingId] = buildingAmount[buildingId] + 1;
                        }
                        else
                        {
                            buildingList[built.building.id] = built.level;
                            buildingAmount[built.building.id] = 1;
                        }
                    }
                }
            };
            this.buildingList = buildingList;
            this.buildingAmount = buildingAmount;
        }

        public Base(string name, bool visible, string type)
        {
            this.shipStorage = new Dictionary<string, int>();
            this.ressources = new Dictionary<string, RessourceCumul>
            {
                {RessourcesConstant.METAL, new RessourceCumul() },
                {RessourcesConstant.CRISTAL, new RessourceCumul() },
                {RessourcesConstant.ORGANIC, new RessourceCumul() },
                {RessourcesConstant.ENERGY, new RessourceCumul()},
            };
            //Base
            this.slots = new Dictionary<string, BaseSlot>()
            {
                {"0", new BaseSlot(BaseSlotTypeConstant.METAL) },
                {"1", new BaseSlot(BaseSlotTypeConstant.CRISTAL) },
                {"2", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"3", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"4", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"5", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"6", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"7", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"8", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"9", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"10", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"11", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"12", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"13", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"14", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"15", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
                {"16", new BaseSlot(BaseSlotTypeConstant.DEFAULT) },
            };
            this.effects = new Dictionary<string, int>();

            this.recalcDuplicate();

            this.name = name;
            this.visible = visible;
            this.type = type;

            this.curentConstruction = 0;
            this.curentTurretConstruction = 0;
            this.curentShipConstruction = 0;
            this.currentSearch = 0;
        }

        public Base(Base bse)
        {
            this.name = bse.name;
            this.visible = bse.visible;
            this.type = bse.type;
            this.ownerId = bse.ownerId;
            this.ownerName = bse.ownerName;
            this.slots = bse.slots;
            this.ressources = bse.ressources;
            this.effects = bse.effects;
            this.position = bse.position;
            this.shipStorage = bse.shipStorage;
            this.pendingFightId = bse.pendingFightId;
            this.buildingAmount = bse.buildingAmount;
            this.buildingList = bse.buildingList;
            this.curentConstruction = 0;
            this.curentTurretConstruction = 0;
            this.curentShipConstruction = 0;
            this.currentSearch = 0;
        }

        public Base(Dictionary<string, BaseSlot> slots, Dictionary<string, RessourceCumul> ressources, Dictionary<string, int> effects, Dictionary<string, string> effectSource, GlobalPosition position, Dictionary<string, int> shipStorage, string turretFleet, string pendingFightId, Dictionary<string, int> buildingList, Dictionary<string, int> buildingAmount, int curentConstruction, int curentShipConstruction, int curentTurretConstruction, int currentSearch, bool hasPortal, long version, string id, string name, bool visible, string type, string ownerId, string ownerName, bool isAttackable) : base(id, name, visible, type, ownerName, ownerId, isAttackable)
        {
            this.slots = slots;
            this.ressources = ressources;
            this.effects = effects;
            this.effectSource = effectSource;
            this.position = position;
            this.shipStorage = shipStorage;
            this.turretFleet = turretFleet;
            this.pendingFightId = pendingFightId;
            this.buildingList = buildingList;
            this.buildingAmount = buildingAmount;
            this.curentConstruction = curentConstruction;
            this.curentShipConstruction = curentShipConstruction;
            this.curentTurretConstruction = curentTurretConstruction;
            this.currentSearch = currentSearch;
            this.hasPortal = hasPortal;
            this.version = version;
        }

        public Base() { }

        public void accumulateRessources(float percentage)
        {
            foreach (var key in ressources.Keys)
            {
                ressources[key].addAndRefresh((int)(percentage * 0.01f * ressources[key].storage));
            }
        }
    }
}
