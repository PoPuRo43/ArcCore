using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Components.Shaders
{
    [MaterialProperty("_Highlight", MaterialPropertyFormat.Float)]
    [GenerateAuthoringComponent]
    public struct Highlight : IComponentData
    {
        public const float Gray = -1, Normal = 0, Highlighted = 1;

        public float value;

        public Highlight(float value)
        {
            this.value = (value == 0) ? 1 : math.sign(value);
        }
    }
}
