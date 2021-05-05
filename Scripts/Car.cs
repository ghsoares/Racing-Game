using System;
using System.Linq;
using Godot;

public class Car : RigidBody
{
    public Wheel[] wheels { get; private set; }
    public Spatial wheelsRoot { get; private set; }
    public float frontWheelOff { get; private set; }
    public float rearWheelOff { get; private set; }
    public float steeringAngle { get; private set; }

    public float desiredSteeringAngle { get; set; }
    public bool accelerating { get; set; }
	public bool rev {get; set;}
    public bool drifting { get; set; }

    [Export] public float motorForce = 64f;
	[Export] public float revForce = 32f;
    [Export] public float maxSteeringAngle = 15f;

    public override void _Ready()
    {
        wheelsRoot = GetNode<Spatial>("Wheels");

        frontWheelOff = float.MinValue;
        rearWheelOff = float.MaxValue;

        wheels = wheelsRoot.GetChildren().OfType<Wheel>().ToArray();
        foreach (Wheel w in wheels)
        {
            w.AddExclusion(this);
            frontWheelOff = Mathf.Max(frontWheelOff, w.Transform.origin.z);
            rearWheelOff = Mathf.Min(rearWheelOff, w.Transform.origin.z);
        }
        foreach (Wheel w in wheels)
        {
            w.rearWheelOff = rearWheelOff - frontWheelOff;
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        desiredSteeringAngle = Input.GetActionStrength("steer_right") - Input.GetActionStrength("steer_left");
        desiredSteeringAngle *= Mathf.Pi;
        accelerating = Input.IsActionPressed("accelerate");
        rev = Input.IsActionPressed("rev");
        drifting = Input.IsActionPressed("drift");

        float maxStAngleRad = Mathf.Deg2Rad(maxSteeringAngle);
        desiredSteeringAngle = Mathf.Clamp(
            desiredSteeringAngle, -maxStAngleRad, maxStAngleRad
        );
        steeringAngle = Mathf.Lerp(
            steeringAngle, desiredSteeringAngle, Mathf.Clamp(4f * delta, 0f, 1f)
        );
    }

    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        float torque = 0f;
        if (accelerating) torque += motorForce;
		if (rev) torque -= revForce;

        foreach (Wheel w in wheels)
        {
            w.torque = torque;
            w.steeringAngle = steeringAngle;
			w.sideFrictionMultiply = drifting ? .5f : 1f;
			//w.sideFrictionMultiply = 0f;

            w.UpdateWheel(state);

            GlobalTransform = state.Transform;
            LinearVelocity = state.LinearVelocity;
            AngularVelocity = state.AngularVelocity;
        }
    }
}
