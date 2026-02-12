using System.Linq;
using UnityEngine;
using static DES.Plugin;

namespace DES.Components;

public class SpawnPointManager : MonoBehaviour
{
	private void Awake()
	{
		var botZones = LocationScene.GetAllObjectsAndWhenISayAllIActuallyMeanIt<BotZone>().ToList();
		Log.LogDebug("______________");
		foreach (var zone in botZones)
		{
			if (zone.CanSpawnBoss) Log.LogDebug($"BossZone: {zone.NameZone}");
			if (zone.SnipeZone) Log.LogDebug($"SniperZone: {zone.NameZone}");
		}
		Log.LogDebug("______________");
	}
}