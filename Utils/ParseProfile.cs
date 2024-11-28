using BepInEx.Logging;
using EFT.InventoryLogic;
using Newtonsoft.Json.Linq;

using Discarder = GClass2754;

namespace MaxDura
{
    public class ParseProfile
    {
        private readonly string idSophie, idBenji;
        private readonly int newD, maxD, repR;
        private readonly ManualLogSource log;

        public ParseProfile(JObject json)
        {
            log = BepInEx.Logging.Logger.CreateLogSource("MaxDura");
            // sophie = item repaired
            // benji = repair kit
            JToken sophie = json?.SelectToken("Items").First;
            JToken benji = sophie?.Next;

            if (sophie != null && benji != null)
            {
                idSophie = (string)sophie.SelectToken("_id");
                idBenji = (string)benji.SelectToken("_id");
                newD = (int)sophie.SelectToken("upd.Repairable.Durability");
                maxD = (int)sophie.SelectToken("upd.Repairable.MaxDurability");
                repR = (int)benji.SelectToken("upd.RepairKit.Resource");
            }
        }

        public bool UpdateValues(RepairableComponent targetItemRC, Item repairKit)
        {
            // set weapon durability
            if (targetItemRC.Item.Id == idSophie)
            {
                targetItemRC.Durability = newD;
                targetItemRC.MaxDurability = maxD;
                targetItemRC.Item.UpdateAttributes();
                //this.log.LogInfo(item.LocalizedName() + " REPAIRED TO: " + repairableComponent.MaxDurability);
            }
            else // something went wrong with json sent from server
                return false;

            // update repair kit resource
            if (repairKit.Id == idBenji)
            {
                RepairKitComponent repairKitComponent = repairKit.GetItemComponent<RepairKitComponent>();
                repairKitComponent.Resource = repR;
                repairKit.RaiseRefreshEvent(true);
                //this.log.LogInfo("NEW REPAIR RESOURCE: " + rkc.Resource);

                // delete repair kit at 0 resource or below
                if (repairKitComponent.Resource <= 0)
					Discarder.RemoveItem(repairKit);
                    //this.log.LogInfo("DESTROYED REPAIR KIT");

                return true; // all is well - minister fudge
            }
            else // here too
                return false;
        }
    }
}
