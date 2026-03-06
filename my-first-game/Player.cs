using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export]
	public float Speed = 200.0f; // Speed can be adjusted in the Inspector

	private double _animTimer = 0.0;
	
	// We create a variable to hold your picture
	private Sprite2D _sprite;

	public override void _Ready()
	{
		// When the game starts, grab the child Sprite2D node and save it!
		_sprite = GetNode<Sprite2D>("PlayerImage");
	}

	// Notice we changed _Process to _PhysicsProcess! This is better for CharacterBody2D
	public override void _PhysicsProcess(double delta) 
	{
		Vector2 direction = Vector2.Zero;

		// Check for arrow key inputs
		if (Input.IsActionPressed("ui_right"))
		{
			direction.X += 1;
			_sprite.FlipH = false; // Keep original picture (faces right)
		}
		if (Input.IsActionPressed("ui_left"))
		{
			direction.X -= 1;
			_sprite.FlipH = true; // Mirror the picture (faces left)
		}
		if (Input.IsActionPressed("ui_down"))
		{
			direction.Y += 1;
		}
		if (Input.IsActionPressed("ui_up"))
		{
			direction.Y -= 1;
		}

		// Apply movement and animation
		if (direction != Vector2.Zero)
		{
			// Set the Velocity for physics movement
			Velocity = direction.Normalized() * Speed;
			PlayWalkAnimation(delta);
		}
		else
		{
			// Stop moving
			Velocity = Vector2.Zero;
			
			// When NOT moving, reset the child sprite to the first frame
			_sprite.Frame = 0; 
			_animTimer = 0.0; 
		}

		// This is the magic command that handles walls and collisions!
		MoveAndSlide(); 
	}

	private void PlayWalkAnimation(double delta)
	{
		_animTimer += delta * 10; // Adjust '10' to change animation speed
		_sprite.Frame = (int)_animTimer % 7; // Tell the child sprite to cycle frames
	}
}
