using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Harmony;
using AssemblyState;

/* todo
 - note:肩書をどうするか
 - ID順にソートするか否かを設定可能に
*/
 

namespace InheritAgent
{
	class Harmony_Patch
	{
		public Harmony_Patch()
		{
			try
			{
				if (State.IsDebug)
				{
					FileLog.Reset();
				}
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
			Dictionary<string, object> dic = instance.LoadSaveFile();
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
					agentModel.currentSefira = "0";
					instance.agentListSpare.Add(agentModel);
					Notice.instance.Send(NoticeName.AddNewAgent, new object[]
					{
					agentModel
					});
				}
			}
		}
	}
}
