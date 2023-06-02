using MelonLoader;
using UnityEngine;

namespace Photos_Plus
{
    public class Plugin : MelonMod
    {
        private Screenshot sc;
        public bool freezeGame;

        // Add The Screenshot Component To The Camera
        // Needs Rewrite
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            if (sceneName.EndsWith("_sn") || sceneName.EndsWith("_tl") || sceneName.EndsWith("_sd") || sceneName.EndsWith("_rg"))
            {
                sc = GameObject.FindObjectOfType<PlayerCamera>().gameObject.AddComponent<Screenshot>();
            } else
                sc = null;
        }
    }
}
