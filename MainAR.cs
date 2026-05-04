using Godot;
using System;

public partial class MainAR : Node3D
{
    private Camera3D _camera3D;
    private TextureRect _cameraDisplay;

    public override void _Ready()
    {
        _camera3D = GetNode<Camera3D>("Camera3D");
        _cameraDisplay = GetNode<TextureRect>("CanvasLayer2/CameraDisplay");

        CameraServer.Singleton.Call("set_monitoring_feeds", true);
        
        GetTree().CreateTimer(1.0f).Timeout += () => 
        {
            var feeds = CameraServer.Feeds();
            if (feeds.Count > 0)
            {
                var feed = feeds[0];
                
                // FIX: Pick the first valid format (Index 0) to resolve the error
                var settings = new Godot.Collections.Dictionary();
                feed.Call("set_format", 0, settings); 
                
                var camTex = new CameraTexture();
                camTex.CameraFeedId = feed.GetId();
                
                _cameraDisplay.Texture = camTex;
                feed.SetActive(true);
            }
        };
    }

    public override void _Process(double delta)
    {
        Vector3 gyro = Input.GetGyroscope();
        Vector3 currentRot = _camera3D.Rotation;
        
        // Ensure gyroscope is enabled in Project Settings for this to work
        currentRot.X -= gyro.X * (float)delta;
        currentRot.Y -= gyro.Y * (float)delta;
        _camera3D.Rotation = currentRot;
    }
}