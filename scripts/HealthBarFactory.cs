using Godot;

public static class HealthBarFactory
{
	public static (ProgressBar, Label) CreateScreenBar(Node owner, float maxValue, float yOffset, string labelText, Color barColor)
	{
		// Create canvas layer
		CanvasLayer canvas = new CanvasLayer { Name = $"{labelText}Canvas", Layer = 10 };
		owner.AddChild(canvas);

		// Create container
		Control container = new Control { Name = $"{labelText}Container" };
		container.AnchorLeft = container.AnchorRight = 0.0f;
		container.OffsetLeft = 10;
		container.OffsetTop = yOffset;
		container.OffsetRight = 210;
		container.OffsetBottom = yOffset + 20;
		canvas.AddChild(container);

		// Create progress bar
		ProgressBar bar = new ProgressBar { Name = $"{labelText}Bar", MinValue = 0, MaxValue = maxValue, Value = maxValue };
		bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.2f, 0.9f) });
		bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = barColor });
		container.AddChild(bar);

		// Create label
		Label label = new Label { Name = $"{labelText}Label", Text = $"{labelText}: {maxValue:F0} / {maxValue:F0}" };
		label.AddThemeColorOverride("font_color", barColor);
		label.AddThemeFontSizeOverride("font_size", 14);
		label.OffsetLeft = 10;
		label.OffsetTop = yOffset - 20;
		owner.AddChild(label);

		return (bar, label);
	}
}
