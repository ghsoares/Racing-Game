using System;
using Godot;

public class GameCamera : Camera
{
    public Transform cameraTransform { get; private set; }
    public Vector3 prevPos { get; private set; }
    public Vector3 velocity { get; private set; }
    public Basis cameraBasis { get; private set; }

    [Export] public float zoom = 16f;
    [Export] public float lerpSpeed = 8f;
    [Export] public float lookAheadWeight = .5f;

    public override void _Ready()
    {
        cameraTransform = GlobalTransform;
        cameraBasis = cameraTransform.basis;
        prevPos = GlobalTransform.origin;
    }

    public override void _PhysicsProcess(float delta)
    {
        Transform desiredTransform = GlobalTransform;
        Vector3 o = desiredTransform.origin;

        velocity = (GlobalTransform.origin - prevPos) / delta;
        velocity *= new Vector3(1f, 0f, 1f);

        o += cameraBasis.z * zoom + velocity * lookAheadWeight;

        desiredTransform.origin = o;
        desiredTransform.basis = cameraBasis;

        cameraTransform = cameraTransform.InterpolateWith(
            desiredTransform, Mathf.Clamp(delta * lerpSpeed, 0f, 1f)
        );

        prevPos = GlobalTransform.origin;
    }

    public override void _Process(float delta)
    {
        VisualServer.CameraSetTransform(GetCameraRid(), cameraTransform);
    }
}
