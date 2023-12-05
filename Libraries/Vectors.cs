using Unity.Mathematics;
using System;

namespace DojoArenaKT;
internal static class VectorExtensions
{
    public static double Distance(this float3 pos1, float3 pos2)
    {
        return Math.Sqrt((Math.Pow(pos1.x - pos2.x, 2) + Math.Pow(pos1.z - pos2.z, 2)));
    }
}