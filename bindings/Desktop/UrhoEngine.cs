﻿using System;

namespace Urho
{
	public static class UrhoEngine
	{
		public static void Init(string resourcesDirectory)
		{
			Environment.CurrentDirectory = resourcesDirectory;
			if (Environment.OSVersion.Platform == PlatformID.Win32NT && !Environment.Is64BitProcess)
			{
				throw new NotSupportedException("MonoUrho for Windows supports only 64bit mode (change target platform from Any CPU or x86 to x64)");
			}
		}
	}
}