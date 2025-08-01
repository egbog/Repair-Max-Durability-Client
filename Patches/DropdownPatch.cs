using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using RepairDropdownInterface = GInterface37;
using RepairDropdown = GClass884;
using RepairKits = GClass883;

namespace RepairMaxDurabilityClient.Patches
{
    public class RepairWindowPatch : ModulePatch
    {
        private static PropertyInfo RepairKitsCollections;
        private static FieldInfo list_1;
        protected override MethodBase GetTargetMethod()
        {
            // The type argument needs to be the class that declares the property, not the return type
            RepairKitsCollections = AccessTools.Property(typeof(RepairDropdownInterface), "RepairKitsCollections");
            list_1 = AccessTools.Field(typeof(RepairDropdown), "list_1");

            return typeof(RepairWindow).GetMethod("method_2", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        public static void Postfix(ref RepairDropdownInterface __result, Item item, RepairControllerClass repairController)
        {
            // check that we actually have Spare firearm parts in our inventory
            // get List<GClass873> RepairKitsCollections from GInterface37 __result
            List<RepairKits> check = (List<RepairKits>)RepairKitsCollections.GetValue(__result);
            bool contains = check.Exists(x => x.LocalizedName.Contains("Spare firearm parts"));

            // we good to go
            if (contains)
            {
                // we have to generate a new GClass874 in order to edit the RepairKitsCollections->list_1
                // once it is returned and casted to a GInterface37, the values have no setter
                RepairDropdown gclass874 = new(item, repairController);

                // get list_1 from gclass873
                List<RepairKits> __list_1 = (List<RepairKits>)RepairKitsCollections.GetValue(gclass874);

                // if list contains our item then do work
                __list_1.Remove(__list_1.First(x => x.LocalizedName.Contains("Spare firearm parts")));
                // replace with our list_1 with Spare firearm parts removed
                list_1.SetValue(gclass874, __list_1);

                // change return value
                __result = gclass874;
            }
        }
    }
}
