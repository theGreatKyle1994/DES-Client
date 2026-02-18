using System;
using BepInEx.Configuration;
using DES.Models;
using UnityEngine;

namespace DES.ConfigUI;

internal static class Configuration
{
	// Config Options
	public static ConfigEntry<bool> UseOverlay;
	public static ConfigEntry<KeyboardShortcut> OptionUp;
	public static ConfigEntry<KeyboardShortcut> OptionDown;
	public static ConfigEntry<KeyboardShortcut> OptionLeft;
	public static ConfigEntry<KeyboardShortcut> OptionRight;
	public static ConfigEntry<float> RenderRange;
	public static ConfigEntry<bool> UseBirdseye;
	public static ConfigEntry<float> BirdsEyeMult;
	public static ConfigEntry<bool> UseOpacity;
	public static ConfigEntry<float> OpacityRange;
	
	public static void SetBepinExOptions(ConfigFile Config)
	{
		// Generate BepinEx Options
		UseOverlay = Config.Bind("1. General",
				"Enable Overlay",
				false,
				new ConfigDescription("Enable/Disable GUI debug overlay.",
						null,
						new ConfigurationManagerAttributes { Order = 50 }));
		OptionUp = Config.Bind("1. General",
				"Previous Rendering Mode",
				new KeyboardShortcut(KeyCode.UpArrow),
				new ConfigDescription("Cycles to the previous rendering mode.",
						null,
						new ConfigurationManagerAttributes { Order = 45 }));
		OptionDown = Config.Bind("1. General",
				"Next Rendering Mode",
				new KeyboardShortcut(KeyCode.DownArrow),
				new ConfigDescription("Cycles to the next rendering mode.",
						null,
						new ConfigurationManagerAttributes { Order = 40 }));
		OptionLeft = Config.Bind("1. General",
				"Previous Sub-Rendering Mode",
				new KeyboardShortcut(KeyCode.LeftArrow),
				new ConfigDescription("Cycles to the previous sub-rendering mode.",
						null,
						new ConfigurationManagerAttributes { Order = 35 }));
		OptionRight = Config.Bind("1. General",
				"Next Sub-Rendering Mode",
				new KeyboardShortcut(KeyCode.RightArrow),
				new ConfigDescription("Cycles to the next sub-rendering mode.",
						null,
						new ConfigurationManagerAttributes { Order = 30 }));
		RenderRange = Config.Bind("2. Rendering",
				"Rendering Range",
				15f,
				new ConfigDescription("Render range for all overlay elements.",
						new AcceptableValueRange<float>(1f, 2000f),
						new ConfigurationManagerAttributes { Order = 25 }));
		UseBirdseye = Config.Bind("2. Rendering",
				"Enable BirdsEye Rendering",
				true,
				new ConfigDescription("Enable/Disable Rendering range scaling with camera height. " +
				                      "This will scale 'Rendering Range'.",
						null,
						new ConfigurationManagerAttributes { Order = 20 }));
		BirdsEyeMult = Config.Bind("2. Rendering",
				"BirdsEye Multiplier",
				1f,
				new ConfigDescription("Multiplies the BirdsEye effect. " +
				                      "1.0 = default. 2.0 = 2x faster. 0.5 = Half as fast.",
						new AcceptableValueRange<float>(0.01f, 2f),
						new ConfigurationManagerAttributes { Order = 15 }));
		UseOpacity = Config.Bind("2. Rendering",
				"Enable Opacity Fall-Off",
				true,
				new ConfigDescription("Enable/Disable opacity fade.",
						null,
						new ConfigurationManagerAttributes { Order = 10 }));
		OpacityRange = Config.Bind("2. Rendering",
				"Opacity Range",
				10f,
				new ConfigDescription("Maximum range for non-transparent elements before starting to fade. " +
				                      "Full transparency uses the 'Rendering Range' value.",
						new AcceptableValueRange<float>(1f, 1999f),
						new ConfigurationManagerAttributes { Order = 5 }));
		
		// Attach event method for max range updates
		RenderRange.SettingChanged += OnOpacityRangeChange;
		OpacityRange.SettingChanged += OnOpacityRangeChange;
	}
	
	private static void OnOpacityRangeChange(object sender, EventArgs args)
	{
		if (OpacityRange.Value > RenderRange.Value) { OpacityRange.Value = RenderRange.Value - 1f; }
	}
}