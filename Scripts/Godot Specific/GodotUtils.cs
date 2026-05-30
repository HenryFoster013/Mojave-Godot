using Godot;
using System.Threading.Tasks;

public static class GodotUtils {

    public static async Task WaitFrames(this Node node, int frames) {
        for (int i = 0; i < frames; i++)
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
    }

}