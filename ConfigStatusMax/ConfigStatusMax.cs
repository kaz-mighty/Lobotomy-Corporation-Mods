using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using UnityEngine;
using Harmony;
using AssemblyState;


namespace ConfigStatusMax
{
	class Harmony_Patch
	{
		public Harmony_Patch()
		{
			try
			{
				var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				LoadConfig(directory + "/config.xml");

				var harmony = HarmonyInstance.Create("kaz_mighty.ConfigStatusMax");

				var targetClass = typeof(WorkerPrimaryStat);
				var patchClass = typeof(Harmony_Patch);
				MethodInfo target;
				HarmonyMethod prefix;
				string[] methods = { "MaxStatR", "MaxStatW", "MaxStatB", "MaxStatP" };
				foreach (string method in methods)
				{
					target = targetClass.GetMethod(method);
					prefix = new HarmonyMethod(patchClass.GetMethod(method));
					harmony.Patch(target, prefix, null);
				}

				targetClass = typeof(UseSkill);
				patchClass = typeof(StatLevel);
				target = AccessTools.Method(targetClass, "CalculateLevelExp");
				prefix = new HarmonyMethod(patchClass.GetMethod("CalculateLevelExp"));
				harmony.Patch(target, prefix, null);

				targetClass = typeof(AgentModel);
				target = targetClass.GetMethod("CalculateStatLevel");
				prefix = new HarmonyMethod(patchClass.GetMethod("CalculateStatLevel"));
				harmony.Patch(target, prefix, null);
				target = targetClass.GetMethod("CalculateStatLevelForCustomizing");
				prefix = new HarmonyMethod(patchClass.GetMethod("CalculateStatLevelForCustomizing"));
				harmony.Patch(target, prefix, null);

				targetClass = typeof(Customizing.StatUI);
				target = targetClass.GetProperty("MaxStatLevel").GetGetMethod();
				prefix = new HarmonyMethod(patchClass.GetMethod("GetMaxStatLevel"));
				harmony.Patch(target, prefix, null);

				targetClass = typeof(Customizing.CustomizingWindow);
				target = targetClass.GetMethod("SetAgentStatBonus");
				prefix = new HarmonyMethod(patchClass.GetMethod("SetAgentStatBonus"));
				harmony.Patch(target, prefix, null);
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/BaseMods/error_ConfigStatusMax.txt", ex.ToString());
			}
		}

		public static bool MaxStatR(ref int __result)
		{
			__result = MaxStat("stat_max_r");
			return false;
		}

		public static bool MaxStatW(ref int __result)
		{
			__result = MaxStat("stat_max_w");
			return false;
		}

		public static bool MaxStatB(ref int __result)
		{
			__result = MaxStat("stat_max_b");
			return false;
		}

		public static bool MaxStatP(ref int __result)
		{
			__result = MaxStat(null);
			return false;
		}

		static int MaxStat(string abilityName)
		{
			if (GlobalEtcDataModel.instance.hiddenEndingDone)
			{
				return maxStatValue[4];
			}
			if (GlobalEtcDataModel.instance.trueEndingDone)
			{
				return maxStatValue[3];
			}
			if (MissionManager.instance.ExistsFinishedBossMission(SefiraEnum.CHOKHMAH))
			{
				return maxStatValue[2];
			}
			if (abilityName != null && ResearchDataModel.instance.IsUpgradedAbility(abilityName))
			{
				return maxStatValue[1];
			}
			return maxStatValue[0];
		}

		static void LoadConfig(string filePath)
		{
			if (File.Exists(filePath))
			{
				var xml = new XmlDocument();
				xml.Load(filePath);
				var maxStatNode = xml.SelectSingleNode("config/maxStat");
				if (maxStatNode != null)
				{
					var valueNodes = maxStatNode.SelectNodes("value");
					for(var i = 0; i < maxStatValue.Length; i++)
					{
						var valueNode = valueNodes[i];
						if (valueNode != null)
						{
							Harmony_Patch.maxStatValue[i] = int.Parse(valueNode.InnerText);
						}
					}
				}
				var statLevelNode = xml.SelectSingleNode("config/statLevel");
				if (statLevelNode != null)
				{
					StatLevel.rankDataList = new List<RankData>();
					var rankNodes = statLevelNode.SelectNodes("rank");
					foreach(XmlNode rankNode in rankNodes)
					{
						var attributes = rankNode.Attributes;
						var p1 = int.Parse(attributes.GetNamedItem("orMore").InnerText);
						var p2 = float.Parse(attributes.GetNamedItem("growthRate").InnerText);

						attributes = rankNode.SelectSingleNode("upgrade").Attributes;
						var p3 = int.Parse(attributes.GetNamedItem("cost").InnerText);
						var p4 = int.Parse(attributes.GetNamedItem("min").InnerText);
						var p5 = int.Parse(attributes.GetNamedItem("max").InnerText);

						StatLevel.rankDataList.Add(new RankData(p1, p2, p3, p4, p5));
					}
					StatLevel.InitCost();
				}
				var maxLevelNode = xml.SelectSingleNode("config/upgradeMaxLevel");
				if (maxLevelNode != null)
				{
					StatLevel.upgradeMaxLevel = int.Parse(maxLevelNode.InnerText);
				}
			}

			if (State.IsDebug)
			{
				var log = "";
				foreach (int p in maxStatValue)
				{
					log += p.ToString() + " ";
				}
				Debug.Log(log);

				foreach (var rankdata in StatLevel.rankDataList)
				{
					Debug.Log(rankdata.ToString());
				}
				Debug.Log(StatLevel.upgradeMaxLevel.ToString());
			}
		}

		static int[] maxStatValue = { 100, 120, 130, 150, 150 };

	}

	public struct RankData
	{
		public readonly int orMore;
		public readonly float growthRate;
		public readonly int upgradeCost;
		public readonly int upgradeMin;
		public readonly int upgradeMax;
		public RankData(int p1, float p2, int p3, int p4, int p5)
		{
			orMore = p1;
			growthRate = p2;
			upgradeCost = p3;
			upgradeMin = p4;
			upgradeMax = p5;
		}
		public new string ToString()
		{
			return String.Format("{0} {1} {2} {3} {4}", orMore, growthRate, upgradeCost, upgradeMin, upgradeMax);
		}
	}

	public static class StatLevel
	{
		public static List<RankData> rankDataList = null;
		public static int upgradeMaxLevel = 6;
		public static float[] diffGrowthRate = { 1.4f, 1.2f, 1f, 1f, 0.8f, 0.6f, 0.4f, 0.2f };

		
		public static void InitCost()
		{
			var costN = new int[rankDataList.Count];
			var costP = new int[rankDataList.Count];
			for (var i = 0; i < rankDataList.Count; i++)
			{
				costN[i] = rankDataList[i].upgradeCost;
				costP[i] = rankDataList[i].upgradeCost * 3;
			}
			Customizing.StatUI.RCost = costN;
			Customizing.StatUI.WCost = costN;
			Customizing.StatUI.BCost = costN;
			Customizing.StatUI.PCost = costP;
		}


		public static bool CalculateStatLevel(ref int __result, int stat)
		{
			var i = Math.Min(5, rankDataList.Count) - 1;
			__result = 1;
			for (; i >= 0; i--)
			{
				if (stat >= rankDataList[i].orMore)
				{
					__result = i + 1;
					break;
				}
			}
			return false;
		}

		public static bool CalculateStatLevelForCustomizing(ref int __result, int stat)
		{
			__result = 1;
			for (var i = rankDataList.Count - 1; i >= 0; i--)
			{
				if (stat >= rankDataList[i].orMore)
				{
					__result = i + 1;
					break;
				}
			}
			Debug.Log(String.Format("CalculateStatLevelforCustomizing: {0}", __result));
			return false;
		}

		public static bool CalculateLevelExp(UseSkill __instance, ref float __result, RwbpType rwbpType)
		{
			int statLevelEx = 0;
			WorkerPrimaryStat addedStat = __instance.agent.primaryStat.GetAddedStat(__instance.agent.primaryStatExp);
			float expMulti = 1f;
			switch (rwbpType)
			{
				case RwbpType.R:
					statLevelEx = AgentModel.CalculateStatLevelForCustomizing(addedStat.hp);
					break;
				case RwbpType.W:
					statLevelEx = AgentModel.CalculateStatLevelForCustomizing(addedStat.mental);
					break;
				case RwbpType.B:
					statLevelEx = AgentModel.CalculateStatLevelForCustomizing(addedStat.work);
					break;
				case RwbpType.P:
					statLevelEx = AgentModel.CalculateStatLevelForCustomizing(addedStat.battle);
					break;
			}
			int statLevel = Math.Min(statLevelEx, 5);
			int levelDiff = statLevel - __instance.targetCreature.GetRiskLevel() + 3;
			if (levelDiff >= 0 && levelDiff < diffGrowthRate.Length)
			{
				expMulti = diffGrowthRate[levelDiff];
			}

			var growthRate = 0.0f;
			if (statLevelEx <= 0 || statLevelEx > rankDataList.Count)
			{
				growthRate = rankDataList[rankDataList.Count - 1].growthRate;
				Debug.LogError("statLevelEx is Out of Range!");
			}
			else
			{
				growthRate = rankDataList[statLevelEx - 1].growthRate;
			}
			expMulti *= growthRate;
			if (rwbpType == RwbpType.P)
			{
				expMulti /= 3f;
			}
			__result = expMulti;
			Debug.Log(String.Format("statLevel: {0}", statLevel));
			Debug.Log(String.Format("statLevelEx: {0}", statLevelEx));
			Debug.Log(String.Format("growthRate: {0}", growthRate));
			Debug.Log(String.Format("expMulti: {0}", __result));
			return false;
		}

		public static bool GetMaxStatLevel(ref int __result)
		{
			__result = (!MissionManager.instance.ExistsFinishedBossMission(SefiraEnum.CHOKHMAH)) ? 5 : upgradeMaxLevel;
			return false;
		}

		public static bool SetAgentStatBonus(AgentModel agent, Customizing.AgentData data)
		{
			int level = agent.level;
			agent.primaryStat.hp = SetRandomStatValue(agent.primaryStat.hp, data.RLevel, data.statBonus.rBonus);
			agent.primaryStat.mental = SetRandomStatValue(agent.primaryStat.mental, data.WLevel, data.statBonus.wBonus);
			agent.primaryStat.work = SetRandomStatValue(agent.primaryStat.work, data.BLevel, data.statBonus.bBonus);
			agent.primaryStat.battle = SetRandomStatValue(agent.primaryStat.battle, data.PLevel, data.statBonus.pBonus);
			agent.UpdateTitle(level);
			return false;
		}

		public static int SetRandomStatValue(int original, int currentLevel, int bonusLevel)
		{
			if (bonusLevel == 0)
			{
				return original;
			}
			int totalLevel = currentLevel + bonusLevel;
			if (totalLevel <= 0 || totalLevel > rankDataList.Count)
			{
				Debug.LogError(string.Format("SetRandomStatValue: totalLevel {0} is Out of Range!", totalLevel));
				return original;
			}
			int min = rankDataList[totalLevel - 1].upgradeMin;
			int max = rankDataList[totalLevel - 1].upgradeMax;
			return UnityEngine.Random.Range(min, max);
		}

	}

}
