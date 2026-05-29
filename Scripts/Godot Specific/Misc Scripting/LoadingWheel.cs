using Godot;
using System;

public partial class LoadingWheel : Node {

    [Export] public Control Wheel;
    const float SPEED = 30f;

    public override void _Process(double delta) {
        Wheel.RotationDegrees += SPEED * (float)delta;
    }
}