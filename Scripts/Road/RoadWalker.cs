using Godot;
using System;

public class RoadWalker
{
    public Vector3 origin;
    public float direction;
    public float lookAngle = 70f;
    public float lookSpacing = 8f;

    public void Walk(Vector3 targetPoint) {
        Terrain terrain = Terrain.instance;

        Vector3 targetDir = (targetPoint - origin).Normalized();
        Vector3 bestP = origin;
        float bestDir = direction;
        float bestFitness = float.MinValue;

        for (float ang = -lookAngle; ang <= lookAngle; ang += 1f) {
            float angRad = Mathf.Deg2Rad(ang);
            Vector2 p = Vector2.Right.Rotated(direction + angRad) * lookSpacing;
            Vector3 p3D = new Vector3(p.x, 0f, p.y) + origin;
            p3D.y = terrain.GetHeight(p3D);
            float hDiff = (p3D.y - origin.y) / lookSpacing;
            Vector3 dir = (p3D - origin).Normalized();

            if (hDiff >= -.5f && hDiff <= .5f) {
                float fitness = 0f;
                //float fitness = -ang;
                fitness += dir.Dot(targetDir) * 1f;
                fitness -= Mathf.Abs(hDiff * 1f);
                if (fitness > bestFitness) {
                    bestFitness = fitness;
                    bestP = p3D;
                    bestDir = direction + angRad;
                }
            }
        }
            
        origin = bestP;
        direction = bestDir;
    }

    /*
    Vector3 walkerOrigin = a;
        Vector3 walkerDir = (b - a).Normalized();
        float walkerAngle = new Vector2(walkerDir.x, walkerDir.z).Angle();

        float lookAngle = 60f;
        float lookSpacing = 8f;
    */
}
