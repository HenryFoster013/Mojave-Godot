using Godot;
using static GodotUtils;
using System;

public partial class FadeIn : Control {

	[Export] public ColorRect ColourRect;
	[Export] public float Delay;
	[Export] public float FadeTime;

	static readonly Color TRANSPARENT = new Color(0f, 0f, 0f, 0f);
	static readonly Color BLACK = new Color(0f, 0f, 0f, 1f);

	public override void _Ready() {
		RunFadeSequence();
	}

	private async void RunFadeSequence() {

		await this.WaitFrames(2);
		
		ColourRect.SelfModulate = BLACK;
		Tween fadeTween = CreateTween();

		fadeTween.TweenInterval(Delay);
		
		fadeTween.TweenProperty(ColourRect, "self_modulate", TRANSPARENT, FadeTime)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
	}
}
