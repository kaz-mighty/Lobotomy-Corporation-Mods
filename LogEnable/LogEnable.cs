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
				FileLog.Reset();
				Application.logMessageReceived += this.MessageHandler;
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/BaseMods/error_LogEnable.txt", ex.Message);
			}
		}

		private void MessageHandler(string logString, string stackTrace, LogType type)
		{
			FileLog.Log(string.Format("{0} : {1}{2}", type.ToString(), logString, Environment.NewLine));
			if (type != LogType.Log && stackTrace != "")
			{
				FileLog.Log(string.Format("StackTrace : {0}{1}", stackTrace, Environment.NewLine));
			}
		}
	}

}
