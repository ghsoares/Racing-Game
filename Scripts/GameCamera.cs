using System;
using Godot;

public class GameCamera : Camera
{
    public Transform cameraTransform { get; private set; }

    [Export] public float lerpPosition = 8f;
    [Export] public float lerpRotation = 4f;

    public override void _Ready()
    {
        cameraTransform = GlobalTransform;
    }

    public override void _PhysicsProcess(float delta)
    {
        Transform t = cameraTransform;
        Vector3 pos = cameraTransform.origin;
        Quat rot = new Quat(cameraTransform.basis).Normalized();
        Quat desiredRot = new Quat(GlobalTransform.basis).Normalized();

        pos = pos.LinearInterpolate(
            GlobalTransform.origin, Mathf.Clamp(lerpPosition * delta, 0f, 1f)
        );
        if (rot.IsNormalized() && desiredRot.IsNormalized())
        {
            rot = rot.Slerp(
                desiredRot, Mathf.Clamp(lerpRotation * delta, 0f, 1f)
            );
        }

        t.origin = pos;
        t.basis = new Basis(rot);
        cameraTransform = t;
    }

    public override void _Process(float delta)
    {
        VisualServer.CameraSetTransform(GetCameraRid(), cameraTransform);
    }
}
