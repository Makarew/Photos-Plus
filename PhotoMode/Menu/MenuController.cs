using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Photos_Plus.Menu
{
    internal class MenuController : MonoBehaviour
    {
        public MenuTab[] tabs;

        public int currentTab;

        public float[] inputs = new float[5];

        private int deActive;

        private void Setup()
        {
            foreach (MenuTab tab in tabs)
            {
                tab.DeActive();
            }

            tabs[currentTab].Active();
        }

        public void NextTab()
        {
            tabs[currentTab].DeActive();

            currentTab++;

            if (currentTab >= tabs.Length)
                currentTab = 0;

            tabs[currentTab].Active();
        }

        public void PreviousTab()
        {
            tabs[currentTab].DeActive();

            currentTab--;

            if (currentTab < 0)
                currentTab = tabs.Length - 1;

            tabs[currentTab].Active();
        }

        public void SendInput(string input, float value)
        {
            switch(input)
            {
                case ("Up"):
                    inputs[0] = value;
                    break;
                case ("Down"):
                    inputs[1] = value;
                    break;
                case ("Left"):
                    inputs[2] = value;
                    break;
                case ("Right"):
                    inputs[3] = value;
                    break;
                case ("Select"):
                    inputs[4] = value;
                    break;
            }
        }

        private void Update()
        {
            if (deActive < 2)
            {
                deActive++;
                return;
            } else if (deActive == 2)
            {
                deActive++;
                Setup();
            }

            tabs[currentTab].SetInputs(inputs);
        }
    }
}
