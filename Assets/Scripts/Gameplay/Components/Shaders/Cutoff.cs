using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Components.Shaders
{
    [MaterialProperty("_Cutoff", MaterialPropertyFormat.Float)]
    [GenerateAuthoringComponent]
    public struct Cutoff : IComponentData
    {
        public const float Yes = 1, No = 0;

        public float value;

        public Cutoff(float value)
        {
            this.value = (value == 0) ? 1 : math.sign(value);
        }
    }
}
