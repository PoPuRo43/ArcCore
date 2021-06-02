using Unity.Entities;
using Unity.Rendering;

namespace ArcCore.Gameplay.Components.Shaders
{
    [MaterialProperty("_Redmix", MaterialPropertyFormat.Float)]
    [GenerateAuthoringComponent]
    public struct Redmix : IComponentData
    {
        public float value;

        public Redmix(float value)
        {
            this.value = value;
        }
    }
}
