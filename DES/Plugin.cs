using BepInEx;
using BepInEx.Logging;
using DES.Patches;

namespace DES;

[BepInPlugin("com.taintedhex.des", "DES", "0.0.1")]
public class Plugin : BaseUnityPlugin
{
	public static ManualLogSource Log;
	
	private void Awake()
	{
		Log = Logger;
		Log.LogWarning("DES Plugin Loaded.");
	}
	
	private void Start() { new OnGameStartPatch().Enable(); }
}