using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DES.Patches;

namespace DES;

[BepInPlugin("com.TaintedHex.DES", "DES", "0.0.1")]
public class Plugin : BaseUnityPlugin
{
	public static ManualLogSource Log;
	
	// BepinEx Options
	public static ConfigEntry<bool> SPMGUIEnabled;
	public static ConfigEntry<float> RenderRange;
	public static ConfigEntry<bool> UseOpacity;
	public static ConfigEntry<float> OpacityRange;
	
	private void Awake()
	{
		Log = Logger;
		
		// Generate BepinEx Options
		SPMGUIEnabled = Config.Bind("Spawn Point Manager",
				"Enable Overlay",
				false,
				"Enable/Disable SPM debug overlay.");
		RenderRange = Config.Bind("Spawn Point Manager",
				"Rendering Range",
				75f,
				new ConfigDescription("Maximum render range for overlay elements.",
						new AcceptableValueRange<float>(2f, 2000f)));
		UseOpacity = Config.Bind("Spawn Point Manager",
				"Enable Opacity Fall-Off",
				true,
				"Enable/Disable opacity fade. Full transparency uses 'Max Rendering Range'.");
		OpacityRange = Config.Bind("Spawn Point Manager",
				"Opacity Range",
				50f,
				new ConfigDescription("Maximum range for non-transparent elements before starting to fade.",
						new AcceptableValueRange<float>(1f, 1999f)));
		
		// Attach event method for max range updates
		RenderRange.SettingChanged += OnRangeChange;
		OpacityRange.SettingChanged += OnRangeChange;
		Log.LogInfo("DES Plugin Loaded.");
	}
	
	private void Start() { new OnGameStartPatch().Enable(); }
	
	private void OnRangeChange(object sender, EventArgs args)
	{
		if (OpacityRange.Value > RenderRange.Value) { OpacityRange.Value = RenderRange.Value - 1f; }
	}
}