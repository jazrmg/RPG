using Godot;

public partial class MobilePerformance : Node
{
	[Export] public bool ForceOptimize = false;

	public override void _Ready()
	{
		bool isMobile = OS.GetName() == "Android" || OS.GetName() == "iOS";

		if (isMobile || ForceOptimize)
		{
			ApplyMobileOptimizations();
		}
	}

	private void ApplyMobileOptimizations()
	{
		// Balanced for budget phones: good visuals + smooth 60 FPS
		
		RenderingServer.DirectionalSoftShadowFilterSetQuality(
			RenderingServer.ShadowQuality.SoftVeryLow);
		RenderingServer.PositionalSoftShadowFilterSetQuality(
			RenderingServer.ShadowQuality.SoftVeryLow);

		ProjectSettings.SetSetting("rendering/lights_and_shadows/directional_shadow/size", 1024);
		ProjectSettings.SetSetting("rendering/lights_and_shadows/directional_shadow/soft_shadow_filter_quality", 0);

		GetViewport().Msaa3D = Viewport.Msaa.Disabled;
		GetViewport().Msaa2D = Viewport.Msaa.Disabled;

		GetViewport().ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;

		// 85% resolution rendering (better than 70% but still efficient)
		GetViewport().Scaling3DMode = Viewport.Scaling3DModeEnum.Bilinear;
		GetViewport().Scaling3DScale = 0.85f;

		ProjectSettings.SetSetting("rendering/environment/ssao/quality", 0);
		ProjectSettings.SetSetting("rendering/environment/ssil/quality", 0);
		ProjectSettings.SetSetting("rendering/environment/volumetric_fog/volume_size", 32);

		// 60 physics ticks per second (smooth physics)
		Engine.PhysicsTicksPerSecond = 60;

		// 60 FPS (smooth gameplay, reasonable battery)
		Engine.MaxFps = 60;

		ReduceCameraFar();
		OptimizeTerrain();
		OptimizeLights();
	}

	private void ReduceCameraFar()
	{
		var player = GetTree().Root.FindChild("Player3D", true, false) as Node3D;
		if (player != null)
		{
			var camera = player.FindChild("Camera3D", true, false) as Camera3D;
			if (camera != null)
			{
				camera.Far = 100.0f;
				camera.Near = 0.1f;
			}
		}
	}

	private void OptimizeTerrain()
	{
		var terrain = GetTree().Root.FindChild("Terrain3D", true, false);
		if (terrain != null)
		{
			terrain.Set("mesh_lods", 2);
			terrain.Set("mesh_size", 16);
			terrain.Set("collision_radius", 8);
		}
	}

	private void OptimizeLights()
	{
		var light = GetTree().Root.FindChild("DirectionalLight3D", true, false) as DirectionalLight3D;
		if (light != null)
		{
			light.ShadowEnabled = true;
			light.DirectionalShadowMaxDistance = 30.0f;
			light.LightEnergy = 0.8f;
		}
	}
}
