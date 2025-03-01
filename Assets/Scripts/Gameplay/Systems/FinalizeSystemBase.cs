﻿using Unity.Entities;

namespace ArcCore.Gameplay.Systems
{
    public class FinalizeSystemBase : SystemBase
    {
        protected virtual bool DoParticle => false;
        protected sealed override void OnUpdate()
        {
            if (!PlayManager.IsActive) return;

            PlayManager.PlaybackBuffer();
            if (DoParticle)
            {
                PlayManager.PlaybackParticleBuffer();
            }
        }
    }
}