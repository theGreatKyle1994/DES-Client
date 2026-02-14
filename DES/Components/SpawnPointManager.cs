using static EFT.UI.ConsoleScreen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EFT.Game.Spawning;
using UnityEngine;

namespace DES.Components;

public class SpawnPointManager : MonoBehaviour
{
	private float _screenScale = 1.0f;
	private GUIStyle _guiStyle;
	private readonly StringBuilder _sb = new();
	
	private List<SpawnPointMarker> _gameSpawnPoints = [];
	private HashSet<string> _zoneNames = [];
	private Dictionary<string, List<SpawnInstance>> _spawnZoneGroups = new();
	
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
		SortSpawnsByZones();
	}
	
	private void OnGUI()
	{
		// Create GUIStyle once
		_guiStyle ??= new GUIStyle(GUI.skin.box)
		{
				alignment = TextAnchor.MiddleLeft, fontSize = 14, padding = new RectOffset(5, 5, 5, 5)
		};
		
		// Display each group
		foreach (var zoneGroup in _spawnZoneGroups.Keys)
		{
			foreach (var spawnInstance in _spawnZoneGroups[zoneGroup])
			{
				// Ignore behind camera
				var screenPos = _cam.WorldToScreenPoint(spawnInstance.Marker.transform.position + (Vector3.up * 1.5f));
				if (screenPos.z <= 0) continue;
				
				// Limit distance of markers
				var camPos = _cam.transform.position;
				var dist = Mathf.RoundToInt((spawnInstance.Marker.transform.position - camPos).magnitude);
				if (spawnInstance.GUIText.text.Length <= 0 || !(dist < 300f)) continue;
				
				// Set gui box size / postion
				var guiSize = _guiStyle.CalcSize(spawnInstance.GUIText);
				spawnInstance.Display.x = (screenPos.x * _screenScale) - (guiSize.x / 2);
				spawnInstance.Display.y = Screen.height - ((screenPos.y * _screenScale) + guiSize.y);
				spawnInstance.Display.size = guiSize;
				
				// Use distance fade out
				var alpha = (dist - 150f) / (0f - 150f) * 1f;
				alpha = alpha switch { > 1f => 1f, < 0.01f => 0f, _ => alpha };
				GUI.contentColor = new Color(1, 1, 1, alpha);
				GUI.backgroundColor = new Color(0, 0, 0, alpha);
				GUI.Box(spawnInstance.Display, spawnInstance.GUIText, _guiStyle);
			}
		}
	}
	
	private void OnDestroy()
	{
		_zoneNames.Clear();
		_spawnZoneGroups.Clear();
	}
	
	private void SortSpawnsByZones()
	{
		foreach (var spawn in _gameSpawnPoints)
		{
			if (spawn.SpawnPoint.BotZoneName != null)
			{
				if (!_spawnZoneGroups.ContainsKey(spawn.SpawnPoint.BotZoneName))
				{
					_spawnZoneGroups.Add(spawn.SpawnPoint.BotZoneName, []);
					_zoneNames.Add(spawn.SpawnPoint.BotZoneName);
				}
			} else if (!_spawnZoneGroups.ContainsKey("ungrouped")) { _spawnZoneGroups.Add("ungrouped", []); }
			CreateSpawnMarker(spawn);
		}
		Log($"Found {_zoneNames.Count} total zones.");
	}
	
	private void CreateSpawnMarker(SpawnPointMarker spawn)
	{
		// Create marker text
		_sb.Clear();
		_sb.AppendFormat($"Position | ");
		_sb.AppendFormat($"( X:{spawn.Position.x:F2} Y:{spawn.Position.y:F2} Z:{spawn.Position.z:F2} )");
		
		// _sb.AppendFormat($"<color=white>Position | </color>");
		// _sb.AppendFormat(
		// 		$"<color=green>( X:{spawn.Position.x:F2} Y:{spawn.Position.y:F2} Z:{spawn.Position.z:F2} )</color>\n");
		
		// Set marker position
		var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		marker.transform.position = spawn.Position;
		marker.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
		marker.GetComponent<Renderer>().material.color = Color.white;
		
		// Push new marker data
		var groupName = spawn.SpawnPoint.BotZoneName ?? "ungrouped";
		_spawnZoneGroups[groupName].Add(new SpawnInstance()
		{
				Marker = marker, GUIText = new GUIContent() { text = _sb.ToString() }, Display = new Rect()
		});
	}
	
	private class SpawnInstance
	{
		public GameObject Marker;
		public GUIContent GUIText;
		public Rect Display;
	}
}