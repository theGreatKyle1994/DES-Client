using BepInEx;
using BepInEx.Logging;
using DES.ConfigUI;
using DES.Patches;

namespace DES;

[BepInPlugin("com.TaintedHex.DES", "DES", "0.0.1")]
public class Plugin : BaseUnityPlugin
{
	public static ManualLogSource Log;
	
	private void Awake()
	{
		Log = Logger;
		
		// Configure BepinEx configuration
		Configuration.SetBepinExOptions(Config);
		Log.LogInfo("DES Plugin Loaded.");
	}
	
	private void Start() { new OnGameStartPatch().Enable(); }
}