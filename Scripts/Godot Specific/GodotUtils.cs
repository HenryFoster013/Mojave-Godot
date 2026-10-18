using Godot;
using System.Threading.Tasks;

public static class GodotUtils {

    // ----- // CONSTANTS // ----- //

    public const float  TROOP_SLIDER_WIDTH             = 340;
    public const float  TROOP_SLIDER_HEIGHT_SUBHEADING = 65f;
    public const float  TROOP_SLIDER_HEIGHT_DEFAULT    = 55f;

    public const float  BUTTON_HOVER_SCALE   = 1.04f;
    public const float  BUTTON_PRESSED_SCALE = 0.97f;
    public const double BUTTON_ANIM_DURATION = 0.12;

    public const float  PANEL_ANIM_SCALE     = 0.97f;
    public const double PANEL_ANIM_DURATION  = 0.16;
    public const double SKIP_BUTTON_COOLDOWN = 0.5;

    public static async Task WaitFrames(this Node node, int frames) {
        for (int i = 0; i < frames; i++)
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    // ----- // ----- // ANIMATIONS // ----- // ----- //

    public static void AddButtonAnimations(this Node root) {
        if (root == null) return;
        foreach (Node node in root.FindChildren("*", "Button", true, false))
            if (node is Button button) button.AddButtonAnimation();
    }

    public static void AddButtonAnimation(this Button button) {
        if (button == null) return;

        Vector2 base_scale    = button.Scale;
        Vector2 hover_scale   = base_scale * BUTTON_HOVER_SCALE;
        Vector2 pressed_scale = base_scale * BUTTON_PRESSED_SCALE;

        button.PivotOffset = button.Size / 2f;
        Tween active_tween = null;
        bool hovered = false;

        void AnimateTo(Vector2 target_scale) {
            active_tween?.Kill();
            active_tween = button.CreateTween();
            active_tween.TweenProperty(button, "scale", target_scale, BUTTON_ANIM_DURATION)
                .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }

        button.MouseEntered += () => { hovered = true; AnimateTo(hover_scale); };
        button.MouseExited  += () => { hovered = false; AnimateTo(base_scale); };
        button.ButtonDown   += () => AnimateTo(pressed_scale);
        button.ButtonUp     += () => AnimateTo(hovered ? hover_scale : base_scale);
    }

    public static void AnimatePanel(this Control panel) {
        if (panel == null) return;

        Vector2 target_scale = panel.Scale;
        panel.PivotOffset = panel.Size / 2f;
        panel.Scale = target_scale * PANEL_ANIM_SCALE;

        Tween tween = panel.CreateTween();
        tween.TweenProperty(panel, "scale", target_scale, PANEL_ANIM_DURATION)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }

}