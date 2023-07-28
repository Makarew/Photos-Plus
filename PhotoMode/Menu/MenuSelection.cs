using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Photos_Plus.Menu
{
    internal class MenuSelection : MenuEvent
    {
        public int value;
        public List<string> labels;

        internal bool rightPressed;
        internal bool leftPressed;

        private TMP_Text text;

        private void Start()
        {
            base.Start();

            text = transform.Find("Label").GetComponent<TMP_Text>();

            for (int i = 0; i < labels.Count; i++)
            {
                labels[i] = "< " + labels[i] + " >";
            }

            value = 0;
            text.text = labels[0];
        }

        private void Update()
        {
            if (inputs[2] != 0) OnLeft();
            else leftPressed = false;

            if (inputs[3] != 0) OnRight();
            else rightPressed = false;

            base.Update();
        }

        private void OnLeft()
        {
            if (leftPressed) return;

            value--;

            if (value < 0)
                value = labels.Count - 1;

            leftPressed = true;

            text.text = labels[value];
        }

        private void OnRight()
        {
            if (rightPressed) return;

            value++;

            if (value >= labels.Count)
                value = 0;

            rightPressed = true;

            text.text = labels[value];
        }

        public void UpdateLabel()
        {
            text.text = labels[value];
        }
    }
}
