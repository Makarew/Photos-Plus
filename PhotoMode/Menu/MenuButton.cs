using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Photos_Plus.Menu
{
    internal class MenuButton : MenuEvent
    {
        public delegate void clickEvent();
        public clickEvent onClickEvent;

        private bool didPress;

        public void OnClick()
        {
            onClickEvent();
        }

        private void Update()
        {
            if (inputs[4] > 0)
            {
                //Make Sure The Input Only Fires Once Per Button Press
                if (!didPress)
                {
                    OnClick();

                    didPress = true;
                }
            }
            else if (didPress)
                didPress = false;

            base.Update();
        }
    }
}
