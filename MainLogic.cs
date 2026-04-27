using Godot;
using System;
using System.Collections.Generic;

public partial class MainLogic : Node3D
{
	// UI References
	private Panel _infoPanel;
	private Label _coordLabel;
	private Label _bintanaLabel;
	private TextureRect _cameraDisplay;
	private Control _radarContainer;
	
	// UI States & Toggles
	private ColorRect _bootSplash;
	private Button _startButton;
	private ColorRect _placesListUI;
	private Button _closePlacesBtn;
	private Button _landmarkBtn;
	private Button _mapBtn;

	// AR Cube & 3D References
	private MeshInstance3D _arCube;
	private Button _placeBtn;
	private Camera3D _activeCam;

	// GPS & Logic
	private GodotObject _geoPlugin;
	private double _displayLat, _displayLon;
	private double _targetLat, _targetLon;
	private Dictionary<string, Sprite2D> _siteIcons = new Dictionary<string, Sprite2D>();
	private float _currentHeading = 0.0f;
	private bool _isGameStarted = false;

	public override void _Ready()
	{
		// 1. Node Mapping
		_infoPanel = GetNodeOrNull<Panel>("CanvasLayer/Panel");
		_coordLabel = GetNodeOrNull<Label>("CanvasLayer/CoordLabel");
		_bintanaLabel = GetNodeOrNull<Label>("CanvasLayer/BintanaLabel");
		_cameraDisplay = GetNodeOrNull<TextureRect>("CanvasLayer/CameraDisplay");
		_radarContainer = GetNodeOrNull<Control>("CanvasLayer/Minimap");
		_bootSplash = GetNodeOrNull<ColorRect>("CanvasLayer/BootSplash");
		_startButton = GetNodeOrNull<Button>("CanvasLayer/StartButton");
		_placesListUI = GetNodeOrNull<ColorRect>("CanvasLayer/PlacesList");
		_closePlacesBtn = GetNodeOrNull<Button>("CanvasLayer/PlacesList/Button");
		_landmarkBtn = GetNodeOrNull<Button>("CanvasLayer/LandmarkButton");
		_mapBtn = GetNodeOrNull<Button>("CanvasLayer/MapButton");

		// AR Mapping
		_arCube = GetNodeOrNull<MeshInstance3D>("ARCube");
		_placeBtn = GetNodeOrNull<Button>("CanvasLayer/PlaceButton");
		_activeCam = GetNodeOrNull<Camera3D>("Camera3D2");

		// 2. INITIAL STATE: Splash screen only
		HideEverythingInitial();
		SetupRadar();

		// BOOT SPLASH: 5 Seconds
		GetTree().CreateTimer(5.0).Timeout += () => 
		{
			if (_bootSplash != null) _bootSplash.Visible = false;
			if (_startButton != null) 
			{
				_startButton.Text = "{sTArt exploring}";
				_startButton.Visible = true;
			}
		};

		// START ACTION
		if (_startButton != null)
		{
			_startButton.Pressed += () => 
			{
				_startButton.Visible = false;
				_isGameStarted = true;
				ShowMainHUD();
				StartOldSystems(); 
			};
		}

		// PLACE CUBE ACTION
		if (_placeBtn != null)
		{
			_placeBtn.Pressed += () => 
			{
				if (_arCube != null && _activeCam != null)
				{
					_arCube.Visible = true;
					// Place cube 2 meters in front of the active camera
					Vector3 forward = -_activeCam.GlobalTransform.Basis.Z;
					_arCube.GlobalPosition = _activeCam.GlobalPosition + (forward * 2.0f);
				}
			};
		}

		// UI TOGGLE LOGIC
		if (_landmarkBtn != null) _landmarkBtn.Pressed += () => { _placesListUI.Visible = !_placesListUI.Visible; };
		if (_mapBtn != null) _mapBtn.Pressed += () => { _radarContainer.Visible = !_radarContainer.Visible; };
		if (_closePlacesBtn != null) _closePlacesBtn.Pressed += () => { _placesListUI.Visible = false; };
	}

	private void HideEverythingInitial()
	{
		if (_infoPanel != null) _infoPanel.Visible = false;
		if (_coordLabel != null) _coordLabel.Visible = false;
		if (_bintanaLabel != null) _bintanaLabel.Visible = false;
		if (_radarContainer != null) _radarContainer.Visible = false;
		if (_cameraDisplay != null) _cameraDisplay.Visible = false;
		if (_placesListUI != null) _placesListUI.Visible = false;
		if (_startButton != null) _startButton.Visible = false;
		if (_landmarkBtn != null) _landmarkBtn.Visible = false;
		if (_mapBtn != null) _mapBtn.Visible = false;
		if (_placeBtn != null) _placeBtn.Visible = false;
		if (_arCube != null) _arCube.Visible = false;
		if (_bootSplash != null) _bootSplash.Visible = true;
	}

	private void ShowMainHUD()
	{
		if (_coordLabel != null) _coordLabel.Visible = true;
		if (_bintanaLabel != null) _bintanaLabel.Visible = true;
		if (_cameraDisplay != null) _cameraDisplay.Visible = true;
		if (_landmarkBtn != null) _landmarkBtn.Visible = true;
		if (_mapBtn != null) _mapBtn.Visible = true;
		if (_placeBtn != null) _placeBtn.Visible = true;
	}

	private void StartOldSystems()
	{
		if (Engine.HasSingleton("Geolocation"))
		{
			_geoPlugin = Engine.GetSingleton("Geolocation");
			_geoPlugin.Connect("location_update", new Callable(this, nameof(OnLocationUpdate)));
			_geoPlugin.Call("start_updating_location");
		}

		CameraServer.Singleton.Call("set_monitoring_feeds", true);
		GetTree().CreateTimer(1.5).Timeout += () => 
		{
			var feeds = CameraServer.Feeds();
			if (feeds.Count > 0) 
			{
				var feed = feeds[0];
				var settings = new Godot.Collections.Dictionary();
				settings["format"] = "NV21"; // O kaya "NV21"
				feed.Call("set_format", 0, settings);
				var camTex = new CameraTexture();
				camTex.CameraFeedId = feed.GetId();
				camTex.Set("which_feed", 0); 

				if (_cameraDisplay != null)
				{
					_cameraDisplay.Texture = camTex;
					_cameraDisplay.Set("expand_mode", 1); 
					_cameraDisplay.Set("stretch_mode", 6); 

					Vector2 screenSize = GetViewport().GetVisibleRect().Size;
					_cameraDisplay.Size = new Vector2(screenSize.Y, screenSize.X); 
					_cameraDisplay.PivotOffset = _cameraDisplay.Size / 2;
					_cameraDisplay.RotationDegrees = 90;
					_cameraDisplay.Position = (screenSize / 2) - (_cameraDisplay.Size / 2);
					
					// Z-Order: Force Camera to back
					_cameraDisplay.ZIndex = -1;
					_coordLabel?.MoveToFront();
					_landmarkBtn?.MoveToFront();
					_mapBtn?.MoveToFront();
					_placeBtn?.MoveToFront();
				}
				feed.Call("set_active", true); 
			}
		};
	}

	public override void _Process(double delta)
	{
		if (!_isGameStarted) return;

		// --- AR ORIENTATION ---
		Vector3 gyro = Input.GetGyroscope();
		_currentHeading += gyro.Y * (float)delta;
		
		if (_activeCam != null)
		{
			_activeCam.RotationDegrees = new Vector3(
				_activeCam.RotationDegrees.X - (gyro.X * (float)delta * 57.29f),
				_activeCam.RotationDegrees.Y - (gyro.Y * (float)delta * 57.29f),
				0
			);
		}

		// GPS STATUS
		if (_targetLat == 0) 
		{
			if (_coordLabel != null) _coordLabel.Text = "GPS STATUS: Turn On Location";
			return; 
		}

		_displayLat = Mathf.Lerp(_displayLat, _targetLat, 10.0f * (float)delta);
		_displayLon = Mathf.Lerp(_displayLon, _targetLon, 10.0f * (float)delta);

		if (_coordLabel != null)
			_coordLabel.Text = $"GPS LIVE\nLAT: {_displayLat:F6}\nLON: {_displayLon:F6}";

		UpdateRadar(_displayLat, _displayLon, _currentHeading);
		
		double dist = CalculateDistance(_displayLat, _displayLon, 14.849288, 120.326777);
		if (_bintanaLabel != null)
			_bintanaLabel.Text = (dist <= 5.0) ? "MALAPIT KA SA BINTANA!" : $"Dist: {dist:F1}m";
		
		CheckHeritageDistance(_displayLat, _displayLon);
	}

	private void UpdateRadar(double pLat, double pLon, float heading)
	{
		var center = GetNodeOrNull<Control>("CanvasLayer/Minimap/Center");
		if (center == null) return;
		foreach (var site in HeritageLocations.Sites)
		{
			double dLat = (site.Latitude - pLat) * 111111;
			double dLon = (site.Longitude - pLon) * 111111 * Math.Cos(pLat * Math.PI / 180.0);
			if (_siteIcons.TryGetValue(site.Name, out Sprite2D icon))
			{
				icon.Position = (new Vector2((float)dLon, (float)-dLat) * 25.0f).Rotated(heading);
			}
		}
	}

	private void SetupRadar()
	{
		var center = GetNodeOrNull<Control>("CanvasLayer/Minimap/Center");
		if (center == null) return;
		foreach (var site in HeritageLocations.Sites)
		{
			Sprite2D icon = new Sprite2D();
			icon.Texture = GD.Load<Texture2D>("res://icon.svg"); 
			icon.Scale = new Vector2(0.12f, 0.12f);
			center.AddChild(icon);
			_siteIcons[site.Name] = icon;
		}
	}

	private void OnLocationUpdate(Godot.Collections.Dictionary location) {
		_targetLat = Variant.From(location["latitude"]).AsDouble();
		_targetLon = Variant.From(location["longitude"]).AsDouble();
		if (_displayLat == 0) { _displayLat = _targetLat; _displayLon = _targetLon; }
	}

	private double CalculateDistance(double lat1, double lon1, double lat2, double lon2) {
		double R = 6371000;
		var dLat = (lat2 - lat1) * (Math.PI / 180.0);
		var dLon = (lon2 - lon1) * (Math.PI / 180.0);
		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * (Math.PI / 180.0)) * Math.Cos(lat2 * (Math.PI / 180.0)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
	}

	private void CheckHeritageDistance(double lat, double lon) {
		foreach (var site in HeritageLocations.Sites) {
			double dist = CalculateDistance(lat, lon, site.Latitude, site.Longitude);
			if (dist <= site.DetectionRadius) {
				if (_infoPanel != null) {
					_infoPanel.GetNode<Label>("Label").Text = site.Name;
					_infoPanel.GetNode<RichTextLabel>("RichTextLabel").Text = site.Description;
					_infoPanel.Visible = true;
				}
				return;
			}
		}
		if (_infoPanel != null) _infoPanel.Visible = false;
	}
}
