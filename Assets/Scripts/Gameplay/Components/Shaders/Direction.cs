using Unity.Entities;
using Unity.Rendering;
using Unity.Burst;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Components.Shaders
{
    [MaterialProperty("_Direction", MaterialPropertyFormat.Float)]
    [GenerateAuthoringComponent]
    public struct Direction : IComponentData
    {
        public float value;

        public Direction(float value)
        {
            this.value = (value == 0) ? 1 : math.sign(value);
        }
    }
}
