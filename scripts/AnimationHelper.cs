using Godot;

public static class AnimationHelper
{
	public static Animation ExtractAnimationFromFbx(string fbxPath, bool loop)
	{
		PackedScene scene = GD.Load<PackedScene>(fbxPath);
		if (scene == null)
		{
			return null;
		}

		Node instance = scene.Instantiate();
		AnimationPlayer animPlayer = FindAnimationPlayer(instance);
		if (animPlayer == null)
		{
			instance.QueueFree();
			return null;
		}

		Animation anim = FindAnimation(animPlayer);
		instance.QueueFree();

		if (anim == null)
		{
			return null;
		}

		// Remove Hips position track (keep rotation)
		for (int i = anim.GetTrackCount() - 1; i >= 0; i--)
		{
			if (anim.TrackGetPath(i).ToString().Contains("Hips") && anim.TrackGetType(i) == Animation.TrackType.Position3D)
				anim.RemoveTrack(i);
		}

		anim.LoopMode = loop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;
		return anim;
	}

	private static Animation FindAnimation(AnimationPlayer animPlayer)
	{
		// Look for "mixamo_com" first
		foreach (string lib in animPlayer.GetAnimationLibraryList())
		{
			AnimationLibrary library = animPlayer.GetAnimationLibrary(lib);
			if (library.HasAnimation("mixamo_com"))
				return (Animation)library.GetAnimation("mixamo_com").Duplicate();
		}

		// Fallback to any animation (skip "Take 001")
		foreach (string lib in animPlayer.GetAnimationLibraryList())
		{
			AnimationLibrary library = animPlayer.GetAnimationLibrary(lib);
			foreach (string anim in library.GetAnimationList())
			{
				if (anim != "Take 001")
					return (Animation)library.GetAnimation(anim).Duplicate();
			}
		}

		return null;
	}

	private static AnimationPlayer FindAnimationPlayer(Node node)
	{
		if (node is AnimationPlayer ap) return ap;
		foreach (Node child in node.GetChildren())
		{
			AnimationPlayer found = FindAnimationPlayer(child);
			if (found != null) return found;
		}
		return null;
	}
}
