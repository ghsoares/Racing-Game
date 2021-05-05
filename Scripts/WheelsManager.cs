using Godot;
using System;

public class WheelsManager : Spatial
{
    public Wheel[] wheels { get; private set; }

    [Export] public float minRange = .25f;
    [Export] public float maxRange = 1f;
    [Export] public float wheelRadius = .25f;
    [Export] public float previewFrequency = .25f;
    [Export] public float springForce = 100f;
    [Export] public float springDrag = 8f;
    [Export] public float springAngularDrag = 4f;
    [Export] public float sideFriction = 2f;
    [Export] public float forwardFriction = 1f;
    [Export] public Curve sideFrictionCurve;
    [Export] public Curve forwardFrictionCurve;
    [Export] public float sideSlipMax = 32f;
    [Export] public float forwardSlipMax = 16f;
    [Export] public float forceMultiply = .25f;
    [Export(PropertyHint.Layers3dPhysics)] public uint collisionLayer = 1;
}
