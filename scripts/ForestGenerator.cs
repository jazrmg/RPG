using Godot;
using System;

/// <summary>
/// 🌲 REALISTIC FOREST GENERATOR - Natural looking bright magical forest
/// Godot 4.6 Mono C# compatible version - no blocky Minecraft look
/// </summary>
public partial class ForestGenerator : Node3D
{
	// ===== TERRAIN SETTINGS =====
	[Export] public float TerrainSize = 100.0f;
	[Export] public Color TerrainColor = new Color(0.35f, 0.65f, 0.25f, 1);

	// ===== TREES SETTINGS =====
	[Export] public int TreeCount = 12;
	[Export] public float TreeHeightMin = 7.0f;
	[Export] public float TreeHeightMax = 12.0f;
	[Export] public float TreeCanopyMin = 4.5f;
	[Export] public float TreeCanopyMax = 7.0f;
	[Export] public Color TreeTrunkColor = new Color(0.35f, 0.22f, 0.12f, 1);
	[Export] public Color TreeLeafColor = new Color(0.25f, 0.5f, 0.2f, 1);

	// ===== ROCKS SETTINGS =====
	[Export] public int RockCount = 8;
	[Export] public float RockSizeMin = 1.5f;
	[Export] public float RockSizeMax = 4.5f;
	[Export] public Color RockColor = new Color(0.45f, 0.45f, 0.42f, 1);

	// ===== HILLS SETTINGS =====
	[Export] public int HillCount = 5;
	[Export] public float HillHeightMin = 1.5f;
	[Export] public float HillHeightMax = 4.0f;
	[Export] public float HillRadiusMin = 12.0f;
	[Export] public float HillRadiusMax = 20.0f;
	[Export] public Color HillColor = new Color(0.3f, 0.6f, 0.2f, 1);

	// ===== FLOWERS SETTINGS =====
	[Export] public int FlowerGroupCount = 8;
	[Export] public Color[] FlowerColors = new Color[]
	{
		new Color(0.95f, 0.2f, 0.6f, 1),   // Pink-red
		new Color(0.9f, 0.3f, 0.8f, 1),    // Magenta
		new Color(0.8f, 0.5f, 0.95f, 1),   // Purple
		new Color(0.95f, 0.8f, 0.2f, 1),   // Yellow
		new Color(1.0f, 0.4f, 0.2f, 1)     // Orange
	};

	// ===== GRASS PATCHES =====
	[Export] public int GrassClumpCount = 15;
	[Export] public Color GrassHighlightColor = new Color(0.45f, 0.75f, 0.3f, 1);

	// ===== LIGHTING SETTINGS =====
	[Export] public bool EnableSunlight = true;
	[Export] public Color SunColor = new Color(1.0f, 0.9f, 0.75f, 1);
	[Export] public float SunEnergy = 1.4f;

	[Export] public bool EnableMagicLights = true;
	[Export] public int MagicLightCount = 4;
	[Export] public float MagicLightIntensity = 1.8f;

	// ===== ATMOSPHERE =====
	[Export] public Color SkyColor = new Color(0.75f, 0.88f, 1.0f, 1);

	// ===== MAP BOUNDARY =====
	private float _mapMinX, _mapMaxX, _mapMinZ, _mapMaxZ;

	public override void _Ready()
	{
		_mapMinX = -TerrainSize / 2 + 15;
		_mapMaxX = TerrainSize / 2 - 15;
		_mapMinZ = -TerrainSize / 2 + 15;
		_mapMaxZ = TerrainSize / 2 - 15;

		GD.Print("🌲 REALISTIC FOREST GENERATOR - Starting...");

		CreateTerrainBase();
		CreateHills();
		CreateTrees();
		CreateRocks();
		CreateFlowers();
		CreateGrassClumps();
		SetupLighting();
		SetupAtmosphere();

		GD.Print("✅ FOREST GENERATED SUCCESSFULLY!");
		GD.Print($"📊 Stats: {TreeCount} trees, {RockCount} rocks, {HillCount} hills, {FlowerGroupCount} flowers");
	}

	// ===== REALISTIC TERRAIN =====
	private void CreateTerrainBase()
	{
		GD.Print("🟩 Creating natural terrain...");

		var terrain = new MeshInstance3D();
		terrain.Name = "TerrainBase";
		
		// Use simple plane - flat, natural terrain
		var planeMesh = new PlaneMesh();
		planeMesh.Size = new Vector2(TerrainSize, TerrainSize);
		terrain.Mesh = planeMesh;

		// Rotate to be horizontal (top-facing)
		terrain.RotateX(Mathf.Pi / 2);

		var material = new StandardMaterial3D();
		material.AlbedoColor = TerrainColor;
		material.Roughness = 0.95f;
		terrain.SetSurfaceOverrideMaterial(0, material);

		AddChild(terrain);

		// Add collision
		var staticBody = new StaticBody3D();
		staticBody.Name = "TerrainCollision";
		AddChild(staticBody);

		var collisionShape = new CollisionShape3D();
		collisionShape.Shape = new BoxShape3D { Size = new Vector3(TerrainSize, 0.5f, TerrainSize) };
		staticBody.AddChild(collisionShape);
	}

	// ===== REALISTIC HILLS =====
	private void CreateHills()
	{
		GD.Print($"⛰️ Creating {HillCount} rolling hills...");

		for (int i = 0; i < HillCount; i++)
		{
			var hill = new MeshInstance3D();
			hill.Name = $"Hill_{i}";

			float height = HillHeightMin + GD.Randf() * (HillHeightMax - HillHeightMin);
			float radius = HillRadiusMin + GD.Randf() * (HillRadiusMax - HillRadiusMin);

			// Use sphere for organic rounded hills
			var sphereMesh = new SphereMesh();
			hill.Mesh = sphereMesh;

			// Scale to make it look like a hill
			float scaleY = height / radius;
			hill.Scale = new Vector3(radius, scaleY, radius);

			Vector3 pos = RandomMapPosition();
			pos.Y = height * 0.5f;
			hill.Position = pos;

			var material = new StandardMaterial3D();
			material.AlbedoColor = HillColor;
			material.Roughness = 0.92f;
			hill.SetSurfaceOverrideMaterial(0, material);

			AddChild(hill);
		}
	}

	// ===== REALISTIC TREES =====
	private void CreateTrees()
	{
		GD.Print($"🌳 Creating {TreeCount} realistic trees...");

		for (int i = 0; i < TreeCount; i++)
		{
			SpawnRealisticTree();
		}
	}

	private void SpawnRealisticTree()
	{
		Vector3 treePos = RandomMapPosition();

		float trunkHeight = TreeHeightMin + GD.Randf() * (TreeHeightMax - TreeHeightMin);
		float canopySize = TreeCanopyMin + GD.Randf() * (TreeCanopyMax - TreeCanopyMin);

		// Trunk - capsule shape
		var trunk = new MeshInstance3D();
		trunk.Name = "TreeTrunk";
		
		var capsuleMesh = new CapsuleMesh();
		trunk.Mesh = capsuleMesh;
		trunk.Scale = new Vector3(0.5f, trunkHeight * 0.5f, 0.5f);  // Natural proportions
		trunk.Position = treePos + Vector3.Up * (trunkHeight * 0.5f);

		var trunkMat = new StandardMaterial3D();
		trunkMat.AlbedoColor = TreeTrunkColor;
		trunkMat.Roughness = 0.8f;
		trunk.SetSurfaceOverrideMaterial(0, trunkMat);
		AddChild(trunk);

		// Canopy - sphere for tree crown (realistic)
		var canopy = new MeshInstance3D();
		canopy.Name = "TreeCanopy";
		
		var sphereMesh = new SphereMesh();
		canopy.Mesh = sphereMesh;
		canopy.Scale = new Vector3(canopySize, canopySize * 0.9f, canopySize);  // Slightly compressed sphere
		canopy.Position = treePos + Vector3.Up * (trunkHeight + canopySize * 0.4f);

		// Natural green with slight variation
		var canopyColor = TreeLeafColor * new Color(
			0.9f + GD.Randf() * 0.2f,
			0.9f + GD.Randf() * 0.2f,
			0.9f + GD.Randf() * 0.2f,
			1.0f
		);

		var canopyMat = new StandardMaterial3D();
		canopyMat.AlbedoColor = canopyColor;
		canopyMat.Roughness = 0.7f;
		canopy.SetSurfaceOverrideMaterial(0, canopyMat);
		AddChild(canopy);
	}

	// ===== REALISTIC ROCKS =====
	private void CreateRocks()
	{
		GD.Print($"🪨 Creating {RockCount} natural boulders...");

		for (int i = 0; i < RockCount; i++)
		{
			var rock = new MeshInstance3D();
			rock.Name = $"Boulder_{i}";

			float size = RockSizeMin + GD.Randf() * (RockSizeMax - RockSizeMin);
			
			// Sphere for natural boulder
			var sphereMesh = new SphereMesh();
			rock.Mesh = sphereMesh;

			// Random variation for organic feel
			float scaleVar = 0.8f + GD.Randf() * 0.4f;
			rock.Scale = new Vector3(scaleVar * size, scaleVar * size * 0.8f, scaleVar * size);

			Vector3 pos = RandomMapPosition();
			pos.Y = size * 0.4f;
			rock.Position = pos;

			// Weathered natural color
			var rockColor = RockColor * new Color(
				0.9f + GD.Randf() * 0.2f,
				0.9f + GD.Randf() * 0.2f,
				0.9f + GD.Randf() * 0.2f,
				1.0f
			);

			var material = new StandardMaterial3D();
			material.AlbedoColor = rockColor;
			material.Roughness = 0.9f;
			rock.SetSurfaceOverrideMaterial(0, material);

			AddChild(rock);
		}
	}

	// ===== FLOWERS =====
	private void CreateFlowers()
	{
		GD.Print($"🌸 Creating {FlowerGroupCount} flower meadows...");

		for (int g = 0; g < FlowerGroupCount; g++)
		{
			Vector3 groupPos = RandomMapPosition();
			Color flowerColor = FlowerColors[GD.Randi() % FlowerColors.Length];

			// 3-6 flowers per group
			int flowersInGroup = 3 + (int)(GD.Randf() * 4);

			for (int f = 0; f < flowersInGroup; f++)
			{
				var flower = new MeshInstance3D();
				flower.Name = $"Flower_{g}_{f}";

				// Small sphere for flower
				var sphereMesh = new SphereMesh();
				flower.Mesh = sphereMesh;
				flower.Scale = new Vector3(0.25f, 0.25f, 0.25f);

				Vector3 offsetPos = groupPos + new Vector3(
					(float)GD.Randf() * 3 - 1.5f,
					0,
					(float)GD.Randf() * 3 - 1.5f
				);
				offsetPos.Y = 0.25f;
				flower.Position = offsetPos;

				var material = new StandardMaterial3D();
				material.AlbedoColor = flowerColor;
				material.Emission = flowerColor;
				material.EmissionEnergyMultiplier = 0.3f;
				flower.SetSurfaceOverrideMaterial(0, material);

				AddChild(flower);
			}
		}
	}

	// ===== GRASS CLUMPS =====
	private void CreateGrassClumps()
	{
		GD.Print($"🌱 Adding {GrassClumpCount} grass clumps...");

		for (int i = 0; i < GrassClumpCount; i++)
		{
			var grassClump = new MeshInstance3D();
			grassClump.Name = $"GrassClump_{i}";

			// Flat plane for grass patches
			var planeMesh = new PlaneMesh();
			planeMesh.Size = new Vector2(
				2.0f + GD.Randf() * 2.0f,
				2.0f + GD.Randf() * 2.0f
			);
			grassClump.Mesh = planeMesh;
			grassClump.RotateX(Mathf.Pi / 2);

			Vector3 pos = RandomMapPosition();
			pos.Y = 0.01f;
			grassClump.Position = pos;

			var material = new StandardMaterial3D();
			material.AlbedoColor = GrassHighlightColor;
			material.Roughness = 0.95f;
			grassClump.SetSurfaceOverrideMaterial(0, material);

			AddChild(grassClump);
		}
	}

	// ===== LIGHTING =====
	private void SetupLighting()
	{
		GD.Print("💡 Setting up realistic lighting...");

		if (EnableSunlight)
		{
			var sunlight = new DirectionalLight3D();
			sunlight.Name = "Sunlight";
			sunlight.LightEnergy = SunEnergy;
			sunlight.LightColor = SunColor;
			sunlight.Rotation = new Vector3(-0.5f, 0.7f, 0);
			sunlight.ShadowEnabled = true;

			AddChild(sunlight);
		}

		if (EnableMagicLights)
		{
			Color[] colors = new Color[]
			{
				new Color(0.3f, 1.0f, 0.7f, 1),    // Cyan
				new Color(0.7f, 0.3f, 1.0f, 1),    // Purple
				new Color(1.0f, 0.85f, 0.4f, 1),   // Golden
				new Color(0.4f, 0.8f, 1.0f, 1)     // Blue
			};

			for (int i = 0; i < MagicLightCount; i++)
			{
				var light = new OmniLight3D();
				light.Name = $"MagicLight_{i}";
				light.LightEnergy = MagicLightIntensity;
				light.OmniRange = 22.0f;
				light.LightColor = colors[i % colors.Length];

				Vector3 pos = RandomMapPosition();
				pos.Y = 4.0f + GD.Randf() * 3.0f;
				light.Position = pos;

				AddChild(light);
			}
		}
	}

	// ===== ATMOSPHERE =====
	private void SetupAtmosphere()
	{
		GD.Print("🌫️ Setting up atmosphere...");

		var worldEnv = GetTree().Root.FindChild("WorldEnvironment", true, false) as WorldEnvironment;
		if (worldEnv == null)
		{
			GD.PrintErr("⚠️ WorldEnvironment not found!");
			return;
		}

		if (worldEnv.Environment == null)
		{
			worldEnv.Environment = new Godot.Environment();
		}

		var env = worldEnv.Environment;
		env.BackgroundColor = SkyColor;
	}

	// ===== HELPER =====
	private Vector3 RandomMapPosition()
	{
		return new Vector3(
			(float)GD.Randf() * (_mapMaxX - _mapMinX) + _mapMinX,
			0,
			(float)GD.Randf() * (_mapMaxZ - _mapMinZ) + _mapMinZ
		);
	}
}
