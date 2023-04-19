using System;
using System.Collections.Generic;
using System.Text;

namespace AssemblyState
{
	internal static class State
	{
		public static bool IsDebug =>
#if DEBUG
			true;
#else
			false;
#endif
	}
}
