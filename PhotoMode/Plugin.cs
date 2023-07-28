using MelonLoader;
using System.IO;
using UnityEngine;

namespace Photos_Plus
{
    public class Plugin : MelonMod
    {
        private Screenshot sc;
        public bool freezeGame;

        internal AssetBundle bundle;
        internal GameObject menu;

        // Add The Screenshot Component To The Camera
        // Needs Rewrite
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);

            if (GameObject.FindObjectOfType<Screenshot>())
                return;

            if (GameObject.FindObjectOfType<PlayerCamera>())
            {
                sc = GameObject.FindObjectOfType<PlayerCamera>().gameObject.AddComponent<Screenshot>();
            } else
                sc = null;

            if (menu == null)
            {
                bundle = AssetBundle.LoadFromFile(Path.Combine(MelonHandler.ModsDirectory, "P06ML", "Mods", "Plugins", "Photos Plus", "Menu"));

                menu = bundle.LoadAsset("PhotosPlusUI") as GameObject;
            }
        }

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();

            bundle = AssetBundle.LoadFromFile(Path.Combine(MelonHandler.ModsDirectory, "P06ML", "Mods", "Plugins", "Photos Plus", "Menu"));

            menu = bundle.LoadAsset("PhotosPlusUI") as GameObject;
        }
    }
}
