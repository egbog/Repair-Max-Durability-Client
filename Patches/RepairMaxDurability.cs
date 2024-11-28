using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Reflection;
using SPT.Reflection.Patching;
using SPT.Common.Http;
using Newtonsoft.Json;
using Comfort.Common;
using UnityEngine.EventSystems;
using EFT.Communications;
using BepInEx.Logging;
using Newtonsoft.Json.Linq;
using System;

namespace MaxDura
{
	public class RepairMaxDurability : ModulePatch
	{
		public struct ItemInfo
		{
			public string itemId;
			public string repairKitId;
		}
		public static JObject Post(string url, string data)
		{
			return JObject.Parse(RequestHandler.PostJson(url, data));
		}
		public static bool CheckOwner(Item item)
		{
			return item.Owner.OwnerType == EOwnerType.Profile;
		}
		public static bool CheckName(Item item)
		{
			return item.LocalizedName().Contains("Spare firearm parts");
		}
		public static bool CheckDurabilityIsWithinRange(RepairableComponent repairableComponent1)
		{
			return Math.Abs(repairableComponent1.Durability - repairableComponent1.MaxDurability) <= 0.1f;
		}
		protected override MethodBase GetTargetMethod()
		{
			return typeof(ItemView).GetMethod("method_7", BindingFlags.Instance | BindingFlags.Public);
		}

		[PatchPrefix]
		public static bool Postfix(ref ItemContextClass dragItemContext, ref PointerEventData eventData)
		{
			ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource("MaxDura");
            
			ItemView componentInParent;
			ItemContextAbstractClass targetItemContextAbstractClass;
			Item targetItem = null;
			RepairableComponent repairableComponent = null;
			bool start = false;

			// make sure item is dragged onto another item, prevent nullpointers
			if (eventData.pointerEnter != null)
			{
				componentInParent = eventData.pointerEnter.GetComponentInParent<ItemView>();
				targetItemContextAbstractClass = componentInParent?.ItemContext;
				targetItem = targetItemContextAbstractClass?.Item;
			}

			// check if we dragged onto an item
			if (targetItem != null)
			{
				// make sure it's an item that can actually be repaired ie. weapon
				// must contain a RepairableComponent
				targetItem.TryGetItemComponent<RepairableComponent>(out repairableComponent);
				// make sure we aren't repairing armor
				targetItem.TryGetItemComponent<ArmorComponent>(out ArmorComponent armorComponent);

				// check target item ownership
				start = CheckName(dragItemContext.Item) && CheckOwner(targetItem) && repairableComponent != null && armorComponent == null;
			}

			// only do work when our item is dragged AND dragged onto another item
			// make sure the item being dragged is the repair kit
			if (start)
			{
				// check if the durability is below 100
				// set isRepairable to true if it is below 100
				bool isRepairable = repairableComponent.MaxDurability != 100f;
				bool isWithinRange = CheckDurabilityIsWithinRange(repairableComponent);

				if(!isRepairable) // item already at 100 max durability
				{
					Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ErrorMessage);
					NotificationManagerClass.DisplayMessageNotification("Weapon already at maximum durability", ENotificationDurationType.Default, ENotificationIconType.Alert, null);
					dragItemContext.DragCancelled();
					//log.LogInfo("NO REPAIR NECESSARY");
					return false;
				}

				if (!isWithinRange) // current durability is not at the maximum it can be at the moment
				{
					Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ErrorMessage);
					NotificationManagerClass.DisplayMessageNotification("Weapon not clean enough to install new parts", ENotificationDurationType.Default, ENotificationIconType.Alert, null);
					dragItemContext.DragCancelled();
					return false;
				}

				// if code runs to here, then we satisfied all conditions to start the repair process

				ItemInfo info = new() // setup json to send to server
				{
					itemId = targetItem.Id,
					repairKitId = dragItemContext.Item.Id
				};

                ParseProfile prof = new(Post("/MaxDura/CheckDragged", JsonConvert.SerializeObject(info))); // instantiate our profile parsing class
				bool updated = prof.UpdateValues(repairableComponent, dragItemContext.Item); // set durability and repair kit resource
				string status = updated ? "REPAIR SUCCESSFUL" : "REPAIR FAILED: JSON ERROR";

				if (updated) // success
				{
					// sound and notification
					Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.RepairComplete);
					NotificationManagerClass.DisplayMessageNotification(string.Format("{0} {1:F1}", "Weapon successfully repaired to".Localized(null),
						repairableComponent.MaxDurability), ENotificationDurationType.Default, ENotificationIconType.Default, null);
					log.LogInfo(status);
				}
				else // failure for some reason
				{
					Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ErrorMessage);
					NotificationManagerClass.DisplayMessageNotification("Repair failed: JSON error", ENotificationDurationType.Default, ENotificationIconType.Alert, null);
					log.LogError(status);
				}
				// whether repair fails or completes
				// stop original code from executing
				// in this case prevent repair window from opening
				return false;
            }
            else
				// if item being dragged is anything other than the repair kit
				// execute original code
                return true;
        }
    }
}