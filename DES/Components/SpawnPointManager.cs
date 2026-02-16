using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using static EFT.UI.ConsoleScreen;
using EFT.Game.Spawning;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DES.Components;

public class SpawnPointManager : MonoBehaviour
{
	private ManualLogSource _logger = Plugin.Log;
	
	private float _screenScale = 1.0f;
	private GUIStyle _guiStyle;
	
	private List<SpawnPointMarker> _gameSpawnPoints = [];
	private readonly HashSet<string> _zoneNames = [];
	private readonly Dictionary<string, List<SpawnInstance>> _spawnZoneGroups = new();
	
	private Camera _cam;
	
	private void Awake()
	{
		_cam = Camera.main;
		
		// Check if DLSS is enabled and apply scale
		if (CameraClass.Instance.SSAA.isActiveAndEnabled)
		{
			_screenScale = (float)CameraClass.Instance.SSAA.GetOutputWidth() /
			               CameraClass.Instance.SSAA.GetInputWidth();
		}
		_gameSpawnPoints = FindObjectsOfType<SpawnPointMarker>().ToList();
		CreateSpawnsByZones();
	}
	
	private void OnDestroy()
	{
		_zoneNames.Clear();
		_spawnZoneGroups.Clear();
		_gameSpawnPoints.Clear();
	}
	
	private void OnGUI()
	{
		if (!Plugin.SPMGUIEnabled.Value) return;
		
		// Create GUIStyle once
		_guiStyle ??= new GUIStyle(GUI.skin.box)
		{
				alignment = TextAnchor.MiddleLeft,
				fontSize = 12,
				padding = new RectOffset(5, 5, 5, 5),
				richText = true
		};
		
		// Display each group
		foreach (var zoneGroup in _spawnZoneGroups.Keys)
		{
			foreach (var spawnInstance in _spawnZoneGroups[zoneGroup])
			{
				// Ignore behind camera
				var screenPos = _cam.WorldToScreenPoint(spawnInstance.Spawn.Position + (Vector3.up * 1.5f));
				if (screenPos.z <= 0) continue;
				
				// Limit distance of markers
				var camPos = _cam.transform.position;
				var dist = Mathf.RoundToInt((spawnInstance.Spawn.Position - camPos).magnitude);
				
				// Calculate height offset for birdseye rendering
				var heightRange =
						(1 -
						 (Mathf.RoundToInt(Math.Abs(camPos.y - spawnInstance.Spawn.Position.y)) -
						  Plugin.RenderRange.Value) /
						 (Plugin.OpacityRange.Value - Plugin.RenderRange.Value) +
						 1);
				if (heightRange < 1f) { heightRange = 1f; }
				
				// Limit distance of markers
				if (!(dist < Plugin.RenderRange.Value * heightRange)) continue;
				
				// Set gui box size / postion and text
				SetGUIText(dist, heightRange, spawnInstance, zoneGroup);
				SetGUIBoxSize(screenPos, spawnInstance);
				
				// Render gui instance
				GUI.Box(spawnInstance.Display, spawnInstance.GUIText.text, _guiStyle);
			}
		}
	}
	
	private void SetGUIBoxSize(Vector3 screenPosition, SpawnInstance spawnInstance)
	{
		var guiSize = _guiStyle.CalcSize(spawnInstance.GUIText);
		spawnInstance.Display.x = (screenPosition.x * _screenScale) - (guiSize.x / 2);
		spawnInstance.Display.y = Screen.height - ((screenPosition.y * _screenScale) + guiSize.y);
		spawnInstance.Display.size = guiSize;
	}
	
	private void SetGUIText(int cameraDistance, float heightRange, SpawnInstance spawnInstance, string zoneGroup)
	{
		// Calculate alpha for fade-out based on distance
		var alpha = Plugin.UseOpacity.Value
				? (cameraDistance - Plugin.RenderRange.Value * heightRange) /
				  (Plugin.OpacityRange.Value * heightRange - Plugin.RenderRange.Value * heightRange) *
				  1f
				: 1f;
		alpha = alpha switch { > 1f => 1f, < 0.01f => 0f, _ => alpha };
		GUI.backgroundColor = new Color(0f, 0f, 0f, alpha);
		
		// Convert text colors to html color codes
		var labelColor = ColorUtility.ToHtmlStringRGBA(new Color(1f, 1f, 1f, alpha));
		var zoneColor = ColorUtility.ToHtmlStringRGBA(spawnInstance.ZoneGroupColor.SetAlpha(alpha));
		
		// Set gui text
		spawnInstance.GUIText.text = $"<color=#{labelColor}>ZoneGroup: </color>" +
		                             $"<color=#{zoneColor}>{zoneGroup}</color>\n" +
		                             $"<color=#{labelColor}>Position: </color>" +
		                             $"<color=#{labelColor}>X:{spawnInstance.Spawn.Position.x:F2}, </color>" +
		                             $"<color=#{labelColor}>Y:{spawnInstance.Spawn.Position.y:F2}, </color>" +
		                             $"<color=#{labelColor}>Z:{spawnInstance.Spawn.Position.z:F2}</color>\n";
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
		Log($"Found {_zoneNames.Count} total zones.");
		
		// Get zone color group
		foreach (var zoneGroup in _zoneNames)
		{
			Log($"{zoneGroup}");
			
			// Use random color
			var randomColor = zoneGroup != "Ungrouped"
					? new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 0f)
					: new Color(1f, 1f, 1f, 1f);
			Log($"Zonegroup: {zoneGroup} assigned color ({randomColor.ToString()}) ");
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
	
	private class SpawnInstance
	{
		public SpawnPointMarker Spawn;
		public GameObject Marker;
		public GUIContent GUIText;
		public Rect Display;
		public Color ZoneGroupColor;
	}
}