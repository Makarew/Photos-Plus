using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace Photos_Plus.Menu
{
    internal class MenuSlider : MenuEvent
    {
        private TMP_Text valText;
        public float value;

        private Transform leftExtent;
        private Transform rightExtent;

        private float zeroPoint;
        private float jumpDistance;

        private void Start()
        {
            base.Start();

            valText = transform.parent.Find("ValueText").GetComponent<TMP_Text>();

            leftExtent = transform.Find("LeftExtent");
            rightExtent = transform.Find("RightExtent");

            zeroPoint = leftExtent.position.x;
            float dis = 0;

            if (leftExtent.position.x < 0)
            {
                if (rightExtent.position.x < 0)
                {
                    dis = -rightExtent.position.x - -leftExtent.position.x;
                }
                else
                {
                    dis = rightExtent.position.x + -leftExtent.position.x;
                }
            } else
            {
                dis = rightExtent.position.x - leftExtent.position.x;
            }

            jumpDistance = dis / 100;
        }

        public void IncreaseValue()
        {
            if (value < 100)
                value += 1f;

            if (value > 100)
                value = 100;
        }

        public void DecreaseValue()
        {
            if (value > 0)
                value -= 1f;

            if (value < 0)
                value = 0;
        }

        private void Update()
        {
            if (inputs[3] != 0)
                IncreaseValue();
            else if (inputs[2] != 0)
                DecreaseValue();

            valText.text = Mathf.Floor(value).ToString();

            Vector3 pos = transform.position;

            pos.x = zeroPoint + (value * jumpDistance);

            transform.position = pos;

            base.Update();
        }
    }
}
