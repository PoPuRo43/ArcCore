using ArcCore.Parsing.Data;
using Unity.Mathematics;

namespace ArcCore.Gameplay
{
    public class Conversion
    {
        public static float TrackToX(int track)
            => Constants.LaneWidth * 5 - Constants.LaneFullwidth * track;

        public static int XToTrack(float x)
            => (int)math.round((Constants.LaneWidth * 5 - x) * Constants.LaneFullwidthRecip);

        public static float O(float start, float end, float t) 
            => math.lerp(start, end, 1 - math.cos(math.PI / 2 * t));
        public static float I(float start, float end, float t)
            => math.lerp(start, end, math.sin(math.PI / 2 * t));
        public static float B(float start, float end, float t)
        {
            float o = 1 - t;
            return (o * o * o * start) + (3 * o * o * t * start) + (3 * o * t * t * end) + (t * t * t * end);
        }

        public static float QiPercent(float value)
            => value * value * value;
        public static float QoPercent(float value)
            => QiPercent(value - 1) + 1;

        public static float TransformCamPercent(float t, CameraEasing easing)
        {
            switch(easing)
            {
                case CameraEasing.qi:
                    return QiPercent(t);
                case CameraEasing.qo:
                    return QoPercent(t);
                case CameraEasing.l:
                case CameraEasing.s:
                    return t;
            }
            //please die c#
            return "f*cking die".Length;
        }

        public static float GetXAt(float t, float startX, float endX, ArcEasing easing)
        {
            switch (easing)
            {
                case ArcEasing.s:
                    return math.lerp(startX, endX, t);
                case ArcEasing.b:
                    return B(startX, endX, t);
                case ArcEasing.si:
                case ArcEasing.sisi:
                case ArcEasing.siso:
                    return I(startX, endX, t);
                case ArcEasing.so:
                case ArcEasing.sosi:
                case ArcEasing.soso:
                    return O(startX, endX, t);
            }
            //Not possible, just here to make the compiler smile
            return float.PositiveInfinity;
        }
        public static float GetYAt(float t, float startY, float endY, ArcEasing easing)
        {
            switch (easing)
            {
                case ArcEasing.s:
                case ArcEasing.si:
                case ArcEasing.so:
                    return math.lerp(startY, endY, t);
                case ArcEasing.b:
                    return B(startY, endY, t);
                case ArcEasing.sisi:
                case ArcEasing.sosi:
                    return I(startY, endY, t);
                case ArcEasing.siso:
                case ArcEasing.soso:
                    return O(startY, endY, t);
            }
            //Not possible, just here to make the compiler smile
            return float.PositiveInfinity;
        }

        public static float2 GetPosAt(float t, float2 start, float2 end, ArcEasing easing)
            => new float2(GetXAt(t, start.x, end.x, easing), GetYAt(t, start.y, end.y, easing));

        public static float GetWorldX(float x) 
            => -8.5f * x + 4.25f;
        public static float GetWorldY(float y) 
            => 4.5f * y + 1f;

        public static float2 GetWorldPos(float2 xy)
            => new float2(GetWorldX(xy.x), GetWorldY(xy.y));
    }
}