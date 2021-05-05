using Godot;
using System;

public class TestShape : Spatial
{
    public override void _PhysicsProcess(float delta)
    {
        Camera cam = GetViewport().GetCamera();
        Vector3 pos = cam.GlobalTransform.origin;
        pos.y = GlobalTransform.origin.y;

        Transform t = GlobalTransform;
        t.origin = pos;

        GlobalTransform = t;
    }
}
