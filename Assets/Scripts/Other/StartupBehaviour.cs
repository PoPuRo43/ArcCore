using UnityEngine;
using ArcCore.Serialization;

namespace ArcCore.Other
{
    public class StartupBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void OnAppStart()
        {
            //Setup files.
            FileManagement.OnAppStart();
        }
    }
}