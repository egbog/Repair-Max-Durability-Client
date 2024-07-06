using SPT.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MaxDura
{
	public class RepairWindowPatch : ModulePatch
	{
		private static PropertyInfo RepairKitsCollections;
		private static FieldInfo list_1;
		protected override MethodBase GetTargetMethod()
		{
			// The type argument needs to be the class that declares the property, not the return type
			RepairKitsCollections = AccessTools.Property(typeof(GInterface34), "RepairKitsCollections");
			list_1 = AccessTools.Field(typeof(GClass804), "list_1");

			return typeof(RepairWindow).GetMethod("method_2", BindingFlags.Instance | BindingFlags.Public);
		}
		[PatchPostfix]
		public static void Postfix(ref GInterface34 __result, Item item, RepairControllerClass repairController)
		{
			// check that we actually have Spare firearm parts in our inventory
			// get List<GClass803> RepairKitsCollections from GInterface34 __result
			List<GClass803> check = (List<GClass803>)RepairKitsCollections.GetValue(__result);
			bool contains = check.Exists(x => x.LocalizedName.Contains("Spare firearm parts"));

			// we good to go
			if (contains)
			{
				// we have to generate a new GClass804 in order to edit the RepairKitsCollections->list_1
				// once it is returned and casted to a GInterface34, the values have no setter
				GClass804 gclass804 = new GClass804(item, repairController);

				// get list_1 from gclass803
				List<GClass803> __list_1 = (List<GClass803>)RepairKitsCollections.GetValue(gclass804);

				// if list contains our item then do work
				__list_1.Remove(__list_1.First(x => x.LocalizedName.Contains("Spare firearm parts")));
				// replace with our list_1 with Spare firearm parts removed
				list_1.SetValue(gclass804, __list_1);

				// change return value
				__result = gclass804;
			}
		}
	}
}
