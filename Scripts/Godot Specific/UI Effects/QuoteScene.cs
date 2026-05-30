using Godot;
using static GodotUtils;
using System;

public partial class QuoteScene : Node {

    [Export] public RichTextLabel QuoteLabel;
    [Export] public ColorRect CoveringColourRect;

    const string NEXT_SCENE = "res://Scenes/base_game.tscn";
    const string PATH = "res://Data/quotes.txt";
    const float FADETIME = 2.0f;
    const float PAUSETIME = 2.0f;
    static readonly Color TRANSPARENT = new Color(0f, 0f, 0f, 0f);
    static readonly Color BLACK = new Color(0f, 0f, 0f, 1f);

    public override void _Ready() {
        ResetOpacity();
        DisplayRandomQuote();
    }

    void ResetOpacity() {
        CoveringColourRect.SelfModulate = BLACK;
    }

    private void DisplayRandomQuote() {

        string rawText = FileAccess.GetFileAsString(PATH);

        string[] lines = rawText.Split(
            new[] { "\r\n", "\r", "\n" },
            StringSplitOptions.RemoveEmptyEntries
        );

        int randomIndex = Random.Shared.Next(0, lines.Length);
        string[] splitData = lines[randomIndex].Trim().Split('$', 2);
        string quote = splitData[0].Trim();
        string name  = splitData[1].Trim();

        QuoteLabel.Text = $"[i]\"{quote}\"[/i]\n\n- {name}";
        RunFadeSequence();
    }

    private async void RunFadeSequence() {

        await this.WaitFrames(2);

        Tween fadeTween = CreateTween();

        fadeTween.TweenInterval(1);
        
        fadeTween.TweenProperty(CoveringColourRect, "self_modulate", TRANSPARENT, FADETIME)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        fadeTween.TweenInterval(PAUSETIME);

        fadeTween.TweenProperty(CoveringColourRect, "self_modulate", BLACK, FADETIME)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);

        fadeTween.Finished += ChangeScene;
    }

    private void ChangeScene() {
        GetTree().ChangeSceneToFile(NEXT_SCENE);
    }
}