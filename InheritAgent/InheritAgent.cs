using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using UnityEngine;
using Harmony;
 

namespace InheritAgent
{
	class Harmony_Patch
	{
		public Harmony_Patch()
		{
			try
			{
				var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				LoadConfig(directory + "/config.xml");

				var harmony = HarmonyInstance.Create("kaz_mighty.InheritAgent");
				var target = AccessTools.Method(typeof(GlobalGameManager), "InitStoryMode");
				var replace = AccessTools.Method(typeof(Harmony_Patch), "InitStoryMode");
				harmony.Patch(target, new HarmonyMethod(replace), null);
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/BaseMods/error_InheritAgent.txt", ex.ToString());
			}
		}

		static bool InitStoryMode(GlobalGameManager __instance)
		{
			MoneyModel.instance.Init();
			SefiraManager.instance.ClearUnitData();
			OfficerManager.instance.Clear();
			AgentManager.instance.Clear();
			AgentManager.instance.LoadDelAgentData();
			CreatureManager.instance.Clear();
			PlayerModel.instance.Init();

			// Mod adddtion process
			// Agent are loaded before adding new agent.
			LoadDataOnlyAgent(__instance);

			__instance.gameMode = GameMode.STORY_MODE;
			StageRewardTypeInfo data = StageRewardTypeList.instance.GetData(0);
			if (data != null)
			{
				foreach (StageRewardTypeInfo.AgentRewardInfo agentRewardInfo in data.agentList)
				{
					AgentModel agentModel = AgentManager.instance.AddSpareAgentModel();
					agentModel.SetCurrentSefira(agentRewardInfo.sephira);
					agentModel.GetMovableNode().SetCurrentNode(MapGraph.instance.GetSepiraNodeByRandom(agentRewardInfo.sephira));
				}
				MoneyModel.instance.Add(data.money);
			}
			Traverse.Create(__instance).Field("bPlayingGame").SetValue(true);
			Traverse.Create(__instance).Field("calcTime").SetValue(true);
			return false;
		}

		static void LoadDataOnlyAgent(GlobalGameManager instance)
		{
			Traverse.Create(instance).Method("LoadData_prepprocess").GetValue();
			Dictionary<string, object> dic;
			try
			{
				dic = instance.LoadSaveFile();
			}
			catch (FileReadException)
			{
				return;
			}
			string saveVer = "old";
			GameUtil.TryGetValue(dic, "saveVer", ref saveVer);
			int lastDay = 0;
			Dictionary<int, Dictionary<string, object>> dayList = null;
			GameUtil.TryGetValue(dic, "dayList", ref dayList);
			GameUtil.TryGetValue(dic, "lastDay", ref lastDay);
			Dictionary<string, object> lastDayData = null;
			if (!dayList.TryGetValue(lastDay, out lastDayData))
			{
				throw new Exception("lastDay not found (saveVer : " + saveVer + ")");
			}
			Harmony_Patch.LoadDayOnlyAgent(instance, lastDayData);
			if (saveVer == "old")
			{
				instance.SaveData(true);
			}
		}

		static void LoadDayOnlyAgent(GlobalGameManager instance, Dictionary<string, object> data)
		{
			Dictionary<string, object> agents = null;
			Dictionary<string, object> agentName = null;
			GameUtil.TryGetValue(data, "agents", ref agents);
			if (GameUtil.TryGetValue(data, "agentName", ref agentName))
			{
				AgentNameList.instance.LoadData(agentName);
			}
			AgentManager.instance.LoadCustomAgentData();
			AgentManager.instance.LoadDelAgentData();
			LoadAgentData(AgentManager.instance, agents);
		}

		static void LoadAgentData(AgentManager instance, Dictionary<string, object> agents)
		{
			var ref_nextInstId = Traverse.Create(instance).Field("nextInstId");
			var nextInstId = ref_nextInstId.GetValue<int>();
			GameUtil.TryGetValue(agents, "nextInstId", ref nextInstId);
			ref_nextInstId.SetValue(nextInstId);

			var agentList = new List<Dictionary<string, object>>();
			GameUtil.TryGetValue(agents, "agentList", ref agentList);
			foreach (Dictionary<string, object> agent in agentList)
			{
				long id = 0L;
				GameUtil.TryGetValue(agent, "instanceId", ref id);
				if (!instance.DeletedContain(id))
				{
					AgentModel agentModel = new AgentModel(id);
					agentModel.LoadData(agent);
					FixInheritAgent(agentModel);
					instance.agentListSpare.Add(agentModel);
					Notice.instance.Send(NoticeName.AddNewAgent, new object[]
					{
					agentModel
					});
				}
			}
			if (Harmony_Patch.sort)
			{
				instance.agentListSpare.Sort((x, y) =>
				{
					if (x.instanceId < y.instanceId)
					{
						return -1;
					}
					else if (x.instanceId > y.instanceId)
					{
						return 1;
					}
					return 0;
				});
			}
		}

		static void FixInheritAgent(AgentModel agent)
		{
			var stats = agent.primaryStat;
			var agentLevel = agent.level;
			var defaultStat = AgentModel.GetDefaultStat();
			var hp = (int)(stats.hp * Harmony_Patch.ratio + defaultStat.hp * Harmony_Patch.addInitialRatio + Harmony_Patch.addValue);
			var mental = (int)(stats.mental * Harmony_Patch.ratio + defaultStat.mental * Harmony_Patch.addInitialRatio + Harmony_Patch.addValue);
			var work = (int)(stats.work * Harmony_Patch.ratio + defaultStat.work * Harmony_Patch.addInitialRatio + Harmony_Patch.addValue);
			var battle = (int)(stats.battle * Harmony_Patch.ratio + defaultStat.battle * Harmony_Patch.addInitialRatio + Harmony_Patch.addValue);
			stats.hp = Mathf.Clamp(hp, 15, stats.hp);
			stats.mental = Mathf.Clamp(mental, 15, stats.mental);
			stats.work = Mathf.Clamp(work, 15, stats.work);
			stats.battle = Mathf.Clamp(battle, 15, stats.battle);
			if (agent.level < agentLevel)
			{
				agent.InitTitle();
				agent.UpdateTitle(1);
			}

			if (!Harmony_Patch.equipment)
			{
				agent.ReleaseWeaponV2();
				agent.ReleaseArmor();
			}
			if (!Harmony_Patch.inheritGift)
			{
				foreach (var gift in agent.GetAllGifts())
				{
					agent.ReleaseEGOgift(gift);
				}
			}

			agent.currentSefira = "0";
		}

		static void LoadConfig(string filePath)
		{
			if (File.Exists(filePath))
			{
				var xml = new XmlDocument();
				xml.Load(filePath);
				var inheritNode = xml.SelectSingleNode("config/inherit");
				var node = inheritNode.SelectSingleNode("stat/ratio");
				if (node != null)
				{
					Harmony_Patch.ratio = double.Parse(node.InnerText);
				}
				node = inheritNode.SelectSingleNode("stat/addInitialRatio");
				if (node != null)
				{
					Harmony_Patch.addInitialRatio = double.Parse(node.InnerText);
				}
				node = inheritNode.SelectSingleNode("stat/addValue");
				if (node != null)
				{
					Harmony_Patch.addValue = double.Parse(node.InnerText);
				}
				node = inheritNode.SelectSingleNode("equipment");
				if (node != null)
				{
					Harmony_Patch.equipment = bool.Parse(node.InnerText);
				}
				node = inheritNode.SelectSingleNode("gift");
				if (node != null)
				{
					Harmony_Patch.inheritGift = bool.Parse(node.InnerText);
				}
				node = xml.SelectSingleNode("config/sort");
				if (node != null)
				{
					Harmony_Patch.sort = bool.Parse(node.InnerText);
				}
			}
		}
		static double ratio = 1.0;
		static double addInitialRatio = 0.0;
		static double addValue = 0.0;
		static bool equipment = true;
		static bool inheritGift = true;
		static bool sort = false;
	}
}
