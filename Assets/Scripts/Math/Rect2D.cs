﻿using Unity.Mathematics;

namespace ArcCore.Math
{
    public readonly struct Rect2D
    {
        public readonly float2 min;
        public readonly float2 max;

        public float2 Center => (min + max) / 2;

        public Rect2D(float2 a, float2 b)
        {
            min = math.min(a, b);
            max = math.max(a, b);
        }

        public Rect2D(float ax, float ay, float bx, float by)
        {
            min = new float2(ax, ay);
            max = new float2(bx, by);
        }

        public bool CollidesWith(Rect2D other)
            => min.x <= other.max.x && max.x >= other.min.x
            && min.y <= other.max.y && max.y >= other.min.y;
        
        public bool CollidesWith(Circle2D other) 
        {
            if (ContainsPoint(other.center)) return true;

            float2 center = (min + max) / 2;
            float2 intervec = math.clamp(center - other.center, min, max);
            return math.length(intervec) <= other.radius;
        }
        public bool ContainsPoint(float2 pt)
            => min.x <= pt.x && pt.x <= max.x
            && min.y <= pt.y && pt.y <= max.y;

        public bool IsNone
            => float.IsNaN(min.x);

        public static readonly Rect2D none = new Rect2D(float.NaN, float.NaN, float.NaN, float.NaN);
    }
}
