using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Harmony;


namespace LogEnable
{
	class Harmony_Patch
	{
		public Harmony_Patch()
		{
			try
			{
				var harmony = HarmonyInstance.Create("kaz_mighty.LogEnable");
				var assembly = Assembly.GetExecutingAssembly();
				harmony.PatchAll(assembly);
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/BaseMods/error_LogEnable.txt", ex.Message);
			}
		}
	}

	[HarmonyPatch(typeof(GlobalGameManager), "MessageHandler")]
	internal class NewMessageHandler
	{
		static bool Prefix(GlobalGameManager __instance, string logString, string stackTrace, LogType type)
		{
			if (type == LogType.Log)
			{
				var logOutput = Traverse.Create(__instance).Field("logOutput");
				logOutput.SetValue(logOutput.GetValue() + string.Format("Log : {0}{1}{1}", logString, Environment.NewLine));
				return false;
			}
			return true;
		}
	}
}
