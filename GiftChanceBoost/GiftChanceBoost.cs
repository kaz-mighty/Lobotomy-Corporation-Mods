using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using UnityEngine;
using Harmony;
using AssemblyState;


namespace GiftChanceBoost
{
	class Harmony_Patch : IObserver
	{
		public Harmony_Patch()
		{
			try
			{
				var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				LoadConfig(directory + "/config.xml");

				var harmony = HarmonyInstance.Create("kaz_mighty.GiftChanceBoost");

				var targetClass = typeof(UseSkill);
				var patchClass = typeof(Harmony_Patch);
				var target = AccessTools.Method(targetClass, "FinishWorkSuccessfully");
				var prefix = new HarmonyMethod(patchClass.GetMethod("PrefixFinishWork"));
				var postfix = new HarmonyMethod(patchClass.GetMethod("PostfixFinishWork"));
				harmony.Patch(target, prefix, postfix);

				targetClass = typeof(CreatureEquipmentMakeInfo);
				target = targetClass.GetMethod("GetProb");
				prefix = new HarmonyMethod(patchClass.GetMethod("GetProb"));
				harmony.Patch(target, prefix, null);

				targetClass = typeof(CreatureInfo.GiftSlot);
				target = targetClass.GetMethod("SetProb");
				prefix = new HarmonyMethod(patchClass.GetMethod("SetProb"));
				harmony.Patch(target, prefix, null);

				Notice.instance.Observe(NoticeName.OnReleaseGameManager, this);
				Notice.instance.Observe(NoticeName.OnGetEGOgift, this);
				
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/BaseMods/error_GiftChanceBoost.txt", ex.Message);
			}
		}

		public static void PrefixFinishWork(UseSkill __instance)
		{
			var makeInfo = __instance.targetCreature.metaInfo.equipMakeInfos.Find(
				(x) => x.equipTypeInfo.type == EquipmentTypeInfo.EquipmentType.SPECIAL
			);
			targetGift = makeInfo?.equipTypeInfo;
			canGetNewGift =
				targetGift != null
				&& __instance.targetCreature.GetObservationLevel() >= makeInfo.level
				&& !__instance.agent.Equipment.gifts.HasEquipment(targetGift.id)
				&& (
					!__instance.agent.Equipment.gifts.GetLockState(targetGift)
					|| UnitEGOgiftSpace.IsUniqueLock(targetGift.id)
				);
			isGetGift = false;
		}

		public void OnNotice(string notice, params object[] param)
		{
			if (notice == NoticeName.OnReleaseGameManager)
			{
				probMulti.Clear();
				return;
			}
			if (notice == NoticeName.OnGetEGOgift)
			{
				isGetGift = true;
				var model = param[1] as EGOgiftModel;
				// Check canGetNewGift because it may get a gift already obtained or slot locked.
				if (canGetNewGift && model != null)
				{
					probMulti.Remove(model.metaInfo);
				}
				return;
			}
		}

		public static void PostfixFinishWork(UseSkill __instance)
		{
			if (State.IsDebug)
			{
				Debug.Log(String.Format("FinishWork: canGet={0}, isGet={1}", canGetNewGift, isGetGift));
			}
			if (!canGetNewGift || isGetGift)
			{
				return;
			}
			if (!probMulti.ContainsKey(targetGift))
			{
				probMulti.Add(targetGift, 1.0f);
			}
			switch (__instance.GetCurrentFeelingState())
			{
				case CreatureFeelingState.GOOD:
					probMulti[targetGift] += addProbMulti[0];
					break;
				case CreatureFeelingState.NORM:
					probMulti[targetGift] += addProbMulti[1];
					break;
				case CreatureFeelingState.BAD:
					probMulti[targetGift] += addProbMulti[2];
					break;
				default:
					break;
			}
			if (probMulti[targetGift] < 0.1f)
			{
				probMulti[targetGift] = 0.1f;
			}
			if (State.IsDebug)
			{
				Debug.Log(String.Format("{0} gift prob: {1}", __instance.targetCreature.GetUnitName(), probMulti[targetGift]));
			}
		}

		public static bool GetProb(CreatureEquipmentMakeInfo __instance, out float __result)
		{
			if (probMulti.ContainsKey(__instance.equipTypeInfo))
			{
				__result = __instance.prob * probMulti[__instance.equipTypeInfo];
			} else
			{
				__result = __instance.prob;
			}
			if (ResearchDataModel.instance.IsUpgradedAbility("add_efo_gift_prob"))
			{
				__result *= 2.0f;
			}
			if (__result > 1.0f)
			{
				__result = 1.0f;
			}
			return false;
		}

		public static bool SetProb(CreatureInfo.GiftSlot __instance, float prob)
		{
			__instance.Title.text = string.Format(
				"{0} ({1}:{2:f1}%)", 
				LocalizeTextDataModel.instance.GetText("Inventory_GiftTitle"), 
				LocalizeTextDataModel.instance.GetText("CreatureInfo_Prob"), 
				prob * 100f
			);
			return false;
		}

		static void LoadConfig(string filePath)
		{
			var xml = new XmlDocument();
			xml.Load(filePath);

			var currentNode = xml.SelectSingleNode("config");
			if (currentNode != null)
			{
				var nodes = currentNode.SelectNodes("value");
				for (var i = 0; i < 3; i++)
				{
					float.TryParse(nodes[i]?.InnerText, out addProbMulti[i]);
				}
			}
		}

		static float[] addProbMulti = { 1.0f, 0.5f, 0.25f };
		static EquipmentTypeInfo targetGift = null;
		static bool canGetNewGift = false;
		static bool isGetGift = false;
		static Dictionary<EquipmentTypeInfo, float> probMulti = new Dictionary<EquipmentTypeInfo, float>();
	}
}
