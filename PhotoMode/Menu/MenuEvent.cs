using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Photos_Plus.Menu
{
    internal class MenuEvent : MonoBehaviour
    {
        public Transform highlightPoint;

        public MenuEvent eventUp;
        public MenuEvent eventDown;

        private MenuTab thisMenu;

        public float[] inputs = new float[5];

        internal float waitTimer = 0.2f;
        internal float lastFrameTime;

        internal void Start()
        {
            thisMenu = GetComponentInParent<MenuTab>();

            waitTimer = 0.2f;
        }

        public void OnPressUp()
        {
            if (eventUp != null)
                thisMenu.SetNewEvent(eventUp);
        }

        public void OnPressDown()
        {
            if (eventDown != null)
                thisMenu.SetNewEvent(eventDown);
        }

        public void SetInputs(float[] exValues)
        {
            inputs = exValues;
        }

        public void OnHighlight()
        {
            waitTimer = 0.2f;
            lastFrameTime = Time.realtimeSinceStartup;
        }

        public void Update()
        {
            if (inputs[0] == 0 && inputs[1] == 0)
                waitTimer = 0;

            if (waitTimer > 0)
            {
                float frameTime = Time.realtimeSinceStartup - lastFrameTime;
                lastFrameTime = Time.realtimeSinceStartup;
                waitTimer -= frameTime;
                return;
            }

            if (inputs[0] != 0)
                OnPressUp();
            else if (inputs[1] != 0)
                OnPressDown();
        }
    }
}
