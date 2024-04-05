using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Reflection;
using Aki.Reflection.Patching;
using Aki.Common.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using BSG.CameraEffects;
using Comfort.Common;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using HarmonyLib;
using EFT.Communications;
using BepInEx.Logging;
using UnityEngine.Yoga;
using System.Security.Policy;
using Newtonsoft.Json.Serialization;
using Aki.Common.Utils;
using Newtonsoft.Json.Linq;

// check ownership to prevent nullpointer when repairing trader stock

namespace MaxDura
{
	public struct ItemInfo
	{
		public string name;
		public string id;
		public float durability;
		public float maxDurability;
		public string repairKitId;
	}

	public static class Constants
	{
		public static bool isRepairable = false;
		public static bool isOwned = false;
	}

	public class RepairMaxDurability : ModulePatch
	{
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
			return item.LocalizedName() == "Spare Firearm Parts";
		}
		protected override MethodBase GetTargetMethod()
		{
			return typeof(ItemView).GetMethod("method_7", BindingFlags.Instance | BindingFlags.Public);
		}

		[PatchPrefix]
		public static bool Postfix(ref ItemView __instance, ItemContextClass dragItemContext, PointerEventData eventData)
		{
			ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource("MaxDura");
            
			ItemView componentInParent;
			ItemContextAbstractClass targetItemContextAbstractClass;
			Item targetItem = null;
			RepairableComponent repairableComponent = null;

			// make sure item is dragged onto another item, prevent nullpointers
			if (eventData.pointerEnter != null)
			{
				componentInParent = eventData.pointerEnter.GetComponentInParent<ItemView>();
				targetItemContextAbstractClass = componentInParent?.ItemContext;
				targetItem = targetItemContextAbstractClass?.Item;
			}

			if (targetItem != null)
			{
				// make sure it's an item that can actually be repaired ie. weapon
				// must contain a RepairableComponent
				targetItem.TryGetItemComponent<RepairableComponent>(out repairableComponent);
				// check target item ownership
				Constants.isOwned = CheckOwner(targetItem);
			}

			// only do work when our item is dragged AND dragged onto another item
			// make sure the item being dragged is the repair kit
			if (Constants.isOwned && CheckName(dragItemContext.Item) && repairableComponent != null)
			{
				// check if the durability is below 100
				// set isRepairable to true if it is below 100
				Constants.isRepairable = repairableComponent.MaxDurability != 100;

				if (Constants.isRepairable)
				{
					ItemInfo info = new() // setup json to send to server
					{
						name = targetItem.LocalizedName(),
						id = targetItem.Id,
						durability = repairableComponent.Durability,
						maxDurability = repairableComponent.MaxDurability,
						repairKitId = dragItemContext.Item.Id
					};

                    ParseProfile prof = new(Post("/MaxDura/CheckDragged", JsonConvert.SerializeObject(info))); // instantiate our profile parsing class
					bool updated = prof.UpdateValues(repairableComponent, dragItemContext.Item); // instantiate our profile parsing class
					string status = updated ? "REPAIR SUCCESSFUL" : "REPAIR FAILED: JSON ERROR";

					if (updated) // success
					{
						// sound and notification
						Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.RepairComplete);
						NotificationManagerClass.DisplayMessageNotification(string.Format("{0} {1:F1}", "Item successfully repaired to".Localized(null),
							repairableComponent.MaxDurability), ENotificationDurationType.Default, ENotificationIconType.Default, null);
						log.LogInfo(status);
					}
					else // failure for some reason
					{
						Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ErrorMessage);
						NotificationManagerClass.DisplayMessageNotification("Repair failed: JSON error", ENotificationDurationType.Default, ENotificationIconType.Alert, null);
						log.LogError(status);
					}
                }
				else // item already at 100 max durability
				{
					Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ErrorMessage);
					NotificationManagerClass.DisplayMessageNotification("Item already at maximum durability", ENotificationDurationType.Default, ENotificationIconType.Alert, null);
					log.LogInfo("NO REPAIR NECESSARY");
					dragItemContext.DragCancelled();
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