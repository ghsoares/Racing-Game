using System;
using System.Linq;
using Godot;

public class Car : RigidBody
{
    public Wheel[] wheels { get; private set; }
    public Spatial wheelsRoot { get; private set; }

    public override void _Ready()
    {
        wheelsRoot = GetNode<Spatial>("Wheels");

        wheels = wheelsRoot.GetChildren().OfType<Wheel>().ToArray();
        foreach (Wheel w in wheels)
        {
            w.AddExclusion(this);
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        foreach (Wheel w in wheels)
        {
            w.UpdateWheel(state);
        }
    }
}
