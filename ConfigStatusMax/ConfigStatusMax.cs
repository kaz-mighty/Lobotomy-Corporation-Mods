using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using UnityEngine;
using WorkerSpine;
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
				var self = typeof(Harmony_Patch);
				string[] methods = { "MaxStatR", "MaxStatW", "MaxStatB", "MaxStatP" };
				foreach (string method in methods)
				{
					var target = targetClass.GetMethod(method);
					var prefix = new HarmonyMethod(self.GetMethod(method));
					harmony.Patch(target, prefix, null);
				}
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/BaseMods/error_ConfigStatusMax.txt", ex.ToString());
			}
		}

		static public bool MaxStatR(ref int __result)
		{
			__result = MaxStat("stat_max_r");
			return false;
		}

		static public bool MaxStatW(ref int __result)
		{
			__result = MaxStat("stat_max_w");
			return false;
		}

		static public bool MaxStatB(ref int __result)
		{
			__result = MaxStat("stat_max_b");
			return false;
		}

		static public bool MaxStatP(ref int __result)
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
					rankDataList = new List<RankData>();
					var rankNodes = statLevelNode.SelectNodes("rank");
					foreach(XmlNode rankNode in rankNodes)
					{
						var attributes = rankNode.Attributes;
						var p1 = int.Parse(attributes.GetNamedItem("orMore").InnerText);
						var p2 = double.Parse(attributes.GetNamedItem("growthRate").InnerText);

						// attributes = rankNode.SelectSingleNode("upgrade").Attributes;
						// var p3 = int.Parse(attributes.GetNamedItem("cost").InnerText);
						// var p4 = int.Parse(attributes.GetNamedItem("min").InnerText);
						// var p5 = int.Parse(attributes.GetNamedItem("max").InnerText);

						Harmony_Patch.rankDataList.Add(new RankData(p1, p2));
					}
				}
				var maxLevelNode = xml.SelectSingleNode("config/upgradeMaxLevel");
				if (maxLevelNode != null)
				{
					Harmony_Patch.upgradeMaxLevel = int.Parse(maxLevelNode.InnerText);
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

				foreach (var rankdata in rankDataList)
				{
					Debug.Log(rankdata.ToString());
				}
				Debug.Log(upgradeMaxLevel.ToString());
			}
		}

		static int[] maxStatValue = { 100, 120, 130, 150, 150 };
		static List<RankData> rankDataList = null;
		static int upgradeMaxLevel = 6;

	}

	public struct RankData
	{
		public readonly int orMore;
		public readonly double growthRate;
		public readonly int upgradeCost;
		public readonly int upgradeMin;
		public readonly int upgradeMax;
		public RankData(int p1, double p2)
		{
			orMore = p1;
			growthRate = p2;
			upgradeCost = 0;
			upgradeMin = 0;
			upgradeMax = 0;
		}
		public new string ToString()
		{
			return String.Format("{0} {1} {2} {3} {4}", orMore, growthRate, upgradeCost, upgradeMin, upgradeMax);
		}
	}

}
