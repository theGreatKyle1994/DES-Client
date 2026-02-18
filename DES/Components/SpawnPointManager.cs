using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using EFT.Communications;
using EFT.Game.Spawning;
using UnityEngine;
using Random = UnityEngine.Random;
using static EFT.UI.ConsoleScreen;
using static DES.ConfigUI.Configuration;
using static DES.Methods.Methods;

namespace DES.Components;

public class SpawnPointManager : MonoBehaviour
{
	// General Fields
	private ManualLogSource _logger = Plugin.Log;
	private readonly StringBuilder _sb = new();
	
	// Scale Fields
	private float _screenScale = 1.0f;
	private Camera _cam;
	
	// GUI Fields
	private GUIStyle _guiStyle;
	private readonly Color _backGroundColor = new(0f, 0f, 0f, 1f);
	private readonly Color _labelColor = new(1f, 1f, 1f, 1f);
	
	// Spawn Fields
	private List<SpawnPointMarker> _gameSpawnPoints = [];
	private readonly HashSet<string> _zoneNames = [];
	private readonly Dictionary<string, List<SpawnInstance>> _spawnZoneGroups = new();
	
	// Rendering Fields
	private enum CycleRendering
	{
		Left,
		Right,
		Up,
		Down
	};
	
	private readonly Dictionary<string, List<string>> _renderingData =
			new() { { "all", ["all"] }, { "none", ["none"] }, { "botZones", [] } };
	private int _renderingDataIndex;
	private readonly List<string> _renderingModes = [];
	private int _renderingModeIndex;
	private string _selectedDataMode = "all";
	private string _selectedRenderMode = "all";
	
	private void Awake()
	{
		_cam = Camera.main;
		_renderingModes.AddRange(_renderingData.Keys);
		
		// Check if DLSS is enabled and apply scale
		if (CameraClass.Instance.SSAA.isActiveAndEnabled)
		{
			_screenScale = (float)CameraClass.Instance.SSAA.GetOutputWidth() /
			               CameraClass.Instance.SSAA.GetInputWidth();
		}
		_gameSpawnPoints = FindObjectsOfType<SpawnPointMarker>().ToList();
		CreateSpawnsByZones();
	}
	
	private void Update()
	{
		if (!UseOverlay.Value) return;
		if (OptionUp.Value.IsDown()) { CycleRenderingMode(CycleRendering.Up); }
		if (OptionDown.Value.IsDown()) { CycleRenderingMode(CycleRendering.Down); }
		if (OptionLeft.Value.IsDown()) { CycleRenderingMode(CycleRendering.Left); }
		if (OptionRight.Value.IsDown()) { CycleRenderingMode(CycleRendering.Right); }
	}
	
	private void OnDestroy()
	{
		_zoneNames.Clear();
		_spawnZoneGroups.Clear();
		_gameSpawnPoints.Clear();
		Log($"ZoneNames: {_zoneNames.Count}");
		Log($"SpawnGroups: {_spawnZoneGroups.Count}");
		Log($"SpawnPoints: {_gameSpawnPoints.Count}");
	}
	
	private void OnGUI()
	{
		// Check if debug is enabled
		if (!UseOverlay.Value || _selectedDataMode == "none") return;
		
		// Build guiStyle once
		_guiStyle ??= new GUIStyle(GUI.skin.box)
		{
				alignment = TextAnchor.MiddleLeft,
				fontSize = 12,
				padding = new RectOffset(5, 5, 5, 5),
				richText = true
		};
		
		// Display each group
		foreach (var zoneGroup in _zoneNames)
		{
			// Ignore spawn zones not selected by rendering data
			if (zoneGroup != _selectedDataMode && _selectedRenderMode != "all") continue;
			foreach (var spawnInstance in _spawnZoneGroups[zoneGroup])
			{
				// Ignore behind camera
				var screenPos = _cam.WorldToScreenPoint(spawnInstance.Spawn.Position + Vector3.up * 1.5f);
				if (screenPos.z <= 0) continue;
				
				// Limit distance of markers
				var dist = (spawnInstance.Spawn.Position - _cam.transform.position).magnitude;
				var heightMult = GetHeightMult(spawnInstance);
				if (!(dist < RenderRange.Value * heightMult)) continue;
				
				// Set gui box size / postion and text
				SetGUIText(dist, heightMult, spawnInstance, zoneGroup);
				SetGUIBoxSize(screenPos, spawnInstance);
				
				// Render gui instance
				GUI.Box(spawnInstance.Display, spawnInstance.GUIText, _guiStyle);
			}
		}
	}
	
	private void SetGUIBoxSize(Vector3 screenPosition, SpawnInstance spawnInstance)
	{
		var guiSize = _guiStyle.CalcSize(spawnInstance.GUIText);
		spawnInstance.Display.x = screenPosition.x * _screenScale - guiSize.x / 2;
		spawnInstance.Display.y = Screen.height - (screenPosition.y * _screenScale + guiSize.y);
		spawnInstance.Display.size = guiSize;
	}
	
	private void SetGUIText(float cameraDistance, float heightMult, SpawnInstance spawnInstance, string zoneGroup)
	{
		// Calculate alpha for fade-out based on distance
		var alpha = UseOpacity.Value
				? Mathf.Clamp((cameraDistance - RenderRange.Value * heightMult) /
				              (OpacityRange.Value * heightMult - RenderRange.Value * heightMult) *
				              1f, 0f, 1f)
				: 1f;
		
		// Convert text colors to html color codes
		GUI.backgroundColor = _backGroundColor.SetAlpha(alpha);
		var labelColor = ColorUtility.ToHtmlStringRGBA(_labelColor.SetAlpha(alpha));
		var zoneColor = ColorUtility.ToHtmlStringRGBA(spawnInstance.ZoneGroupColor.SetAlpha(alpha));
		
		// Set gui text
		_sb.Clear();
		_sb.AppendFormat($"<color=#{labelColor}>ZoneGroup: </color>");
		_sb.AppendFormat($"<color=#{zoneColor}>{zoneGroup}</color>\n");
		_sb.AppendFormat($"<color=#{labelColor}>Position: </color>");
		_sb.AppendFormat($"<color=#{labelColor}>X:{spawnInstance.Spawn.Position.x:F2}, </color>");
		_sb.AppendFormat($"<color=#{labelColor}>Y:{spawnInstance.Spawn.Position.y:F2}, </color>");
		_sb.AppendFormat($"<color=#{labelColor}>Z:{spawnInstance.Spawn.Position.z:F2}</color>");
		spawnInstance.GUIText.text = _sb.ToString();
	}
	
	private void CreateSpawnsByZones()
	{
		foreach (var spawn in _gameSpawnPoints)
		{
			if (!spawn.SpawnPoint.BotZoneName.IsNullOrWhiteSpace())
			{
				if (!_spawnZoneGroups.ContainsKey(spawn.SpawnPoint.BotZoneName))
				{
					_spawnZoneGroups.Add(spawn.SpawnPoint.BotZoneName, []);
					_zoneNames.Add(spawn.SpawnPoint.BotZoneName);
				}
			} else if (!_spawnZoneGroups.ContainsKey("Ungrouped"))
			{
				_spawnZoneGroups.Add("Ungrouped", []);
				_zoneNames.Add("Ungrouped");
			}
			CreateMarkerData(spawn);
		}
		_renderingData["botZones"].AddRange(_zoneNames);
		Notification($"Found {_zoneNames.Count} total zones.", ENotificationIconType.Alert);
		
		// Get zone color group
		foreach (var zoneGroup in _zoneNames)
		{
			// Use random color
			var randomColor = zoneGroup != "Ungrouped"
					? new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 0f)
					: _labelColor;
			foreach (var spawnInstance in _spawnZoneGroups[zoneGroup]) spawnInstance.ZoneGroupColor = randomColor;
		}
	}
	
	private void CreateMarkerData(SpawnPointMarker spawn)
	{
		// Set marker position
		var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		marker.transform.position = spawn.Position;
		marker.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
		marker.GetComponent<Renderer>().material.color = Color.white;
		
		// Push new marker data
		var groupName = !spawn.SpawnPoint.BotZoneName.IsNullOrWhiteSpace() ? spawn.SpawnPoint.BotZoneName : "Ungrouped";
		_spawnZoneGroups[groupName].Add(new SpawnInstance()
		{
				Spawn = spawn, Marker = marker, GUIText = new GUIContent(), Display = new Rect(),
		});
	}
	
	private float GetHeightMult(SpawnInstance spawnInstance)
	{
		if (!UseBirdseye.Value) return 1f;
		
		// Calculate height multiplier for birdseye rendering
		var heightMult = 1f -
		                 (Math.Abs(_cam.transform.position.y - spawnInstance.Spawn.Position.y) -
		                  RenderRange.Value) /
		                 (1f - RenderRange.Value) +
		                 1f;
		if (heightMult < 1f) { heightMult = 1f; }
		return heightMult * BirdsEyeMult.Value * 4f;
	}
	
	private void CycleRenderingMode(CycleRendering direction)
	{
		// Check if mode values are empty
		if (direction is CycleRendering.Left or CycleRendering.Right &&
		    _renderingData[_renderingModes[_renderingModeIndex]].Count == 0)
		{
			Log($"RenderingDataMode: " +
			    $"'{_renderingData[_renderingModes[_renderingModeIndex]][_renderingDataIndex]}' is empty.");
			return;
		}
		
		// Handle mode cycling
		switch (direction)
		{
			case CycleRendering.Up:
			{
				_renderingModeIndex--;
				if (_renderingModeIndex < 0) _renderingModeIndex = _renderingModes.Count - 1;
				_renderingDataIndex = 0;
				break;
			}
			case CycleRendering.Down:
			{
				_renderingModeIndex++;
				if (_renderingModeIndex >= _renderingModes.Count) _renderingModeIndex = 0;
				_renderingDataIndex = 0;
				break;
			}
			case CycleRendering.Left:
			{
				_renderingDataIndex--;
				if (_renderingDataIndex < 0)
					_renderingDataIndex = _renderingData[_renderingModes[_renderingModeIndex]].Count - 1;
				break;
			}
			case CycleRendering.Right:
			{
				_renderingDataIndex++;
				if (_renderingDataIndex >= _renderingData[_renderingModes[_renderingModeIndex]].Count)
					_renderingDataIndex = 0;
				break;
			}
			default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
		}
		
		// Assign selected mode for gui processing
		_selectedRenderMode = _renderingModes[_renderingModeIndex];
		_selectedDataMode = _renderingData[_selectedRenderMode][_renderingDataIndex];
		Notification($"RenderMode: '{_selectedRenderMode}' | DataMode: '{_selectedDataMode}'");
	}
	
	private class SpawnInstance
	{
		public SpawnPointMarker Spawn;
		public GameObject Marker;
		public GUIContent GUIText;
		public Rect Display;
		public Color ZoneGroupColor;
	}
}