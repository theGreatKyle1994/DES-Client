using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using EFT.Game.Spawning;
using UnityEngine;
using Random = UnityEngine.Random;
using static EFT.UI.ConsoleScreen;
using static DES.ConfigUI.Configuration;

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
		if (!useOverlay.Value) return;
		
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
				GUI.Box(spawnInstance.Display, spawnInstance.GUIText.text, _guiStyle);
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
		var alpha = 1f;
		if (UseOpacity.Value)
		{
			alpha = Mathf.Clamp((cameraDistance - RenderRange.Value * heightMult) /
			                    (OpacityRange.Value * heightMult - RenderRange.Value * heightMult) *
			                    1f, 0f, 1f);
		}
		
		// Convert text colors to html color codes
		GUI.backgroundColor = new Color(0f, 0f, 0f, alpha);
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
	
	private class SpawnInstance
	{
		public SpawnPointMarker Spawn;
		public GameObject Marker;
		public GUIContent GUIText;
		public Rect Display;
		public Color ZoneGroupColor;
	}
}