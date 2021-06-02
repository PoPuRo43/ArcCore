using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Behaviours;
using Unity.Rendering;
using ArcCore.Gameplay.Data;
using ArcCore.Gameplay.Systems.Judgement;
using ArcCore.Gameplay.Components.Tags;
using ArcCore.Gameplay.Components.Shaders;

namespace ArcCore.Gameplay.Systems
{
    [UpdateAfter(typeof(FinalJudgeSystem))]
    public class ShaderParamsApplySystem : SystemBase
    {
        protected unsafe override void OnUpdate()
        {
            EntityManager entityManager = EntityManager;
            int currentTime = Conductor.Instance.receptorTime;
            NTrackArray<int> tracksHeld = InputManager.Instance.tracksHeld;

            //Hold notes
            Entities.WithNone<HoldLocked>().WithAll<ChartIncrTime>().ForEach(
                (ref Cutoff cutoff, ref Highlight highlight, in ChartTime time, in ChartLane lane) => 
                {
                    if (tracksHeld[lane.lane] > 0)
                    {
                        cutoff.value = Cutoff.Yes;
                        highlight.value = Highlight.Highlighted;
                    }
                    else
                    {
                        cutoff.value = Cutoff.No;
                        highlight.value = Highlight.Gray;
                    }
                }  
            ).Run();

            //Trace notes
            Entities.WithNone<ChartLane, ChartPosition, ArcData>().ForEach(
                (ref Cutoff cutoff, in ChartTime time) =>
                {
                    if (time.value > currentTime)
                    {
                        cutoff.value = Cutoff.Yes;
                    }
                }
            ).Run();
        }
    }
}