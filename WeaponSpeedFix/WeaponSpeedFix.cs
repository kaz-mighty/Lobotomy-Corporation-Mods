using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using WorkerSpine;
using Harmony;
using AssemblyState;


namespace WeaponSpeedFix
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
				var harmony = HarmonyInstance.Create("kaz_mighty.WeaponSpeedFix");
				var assembly = Assembly.GetExecutingAssembly();
				harmony.PatchAll(assembly);
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/BaseMods/error_WeaponSpeedFix.txt", ex.Message);
			}
		}
	}

	[HarmonyPatch(typeof(WorkerSpineAnimatorData), "LoadData")]
    internal class WorkerSpineAnimatorData_Patch
    {
		static bool Prepare()
		{
			foreach (DirectoryInfo directoryInfo in Add_On.instance.DirList)
			{
				if (File.Exists(directoryInfo.FullName + "/WeaponSpeedFix.dll"))
				{
					WorkerSpineAnimatorData_Patch.dirPath = directoryInfo.FullName;
					break;
				}
			}
			assetBundle = AssetBundle.LoadFromFile(WorkerSpineAnimatorData_Patch.dirPath + "/assets");
			if (State.IsDebug)
			{
				var assetNames = assetBundle.GetAllAssetNames();
				foreach (string assetName in assetNames)
				{
					FileLog.Log(assetName);
				}
			}
			return true;
		}

		static void Postfix(WorkerSpineAnimatorData __instance)
		{
			if (__instance.id >= 0 && __instance.id <= 29)
			{
				var animatorSrc = "Assets/" + __instance.animatorSrc + ".controller";
				if (WorkerSpineAnimatorData_Patch.assetBundle.Contains(animatorSrc))
				{
					__instance.animator = WorkerSpineAnimatorData_Patch.assetBundle.LoadAsset<RuntimeAnimatorController>(animatorSrc);
					if (State.IsDebug)
					{
						FileLog.Log(string.Format("Loaded Asset. (id = {0}, name = {1})", __instance.id, __instance.name));
					}
				}
				else
				{
					if (State.IsDebug)
					{
						FileLog.Log(string.Format("Not loaded Asset. (id = {0}, name = {1})", __instance.id, __instance.name));
					}
				}
			}
		}

		private static string dirPath;
		private static AssetBundle assetBundle;
    }


}
