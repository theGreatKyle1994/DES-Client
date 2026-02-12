using System.Reflection;
using DES.Components;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DES.Patches;

internal class OnGameStartPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
	}
	
	[PatchPrefix]
	private static void PatchPrefix(GameWorld __instance)
	{
		__instance.GetOrAddComponent<SpawnPointManager>();
	}
}