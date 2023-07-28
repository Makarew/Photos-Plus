using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Photos_Plus.Menu
{
    internal class MenuTab : MonoBehaviour
    {
        public MenuEvent currentEvent;

        public Image icon;

        private Transform highlight;

        private bool active;
        private GameObject content;

        private float[] inputs = new float[5];

        public bool forceUp;

        private float waitTimer;
        private float lastFrameTime;

        public enum EventType
        {
            Button,
            Slider,
            Selection
        }

        public EventType currentEventType;

        private void Start()
        {
            content = transform.Find("Content").gameObject;

            highlight = transform.Find("Highlight");

            Vector3 hiPos = highlight.position;
            hiPos.y = currentEvent.highlightPoint.position.y;
            highlight.position = hiPos;

            lastFrameTime = Time.realtimeSinceStartup;
        }

        public void SetNewEvent(MenuEvent nextEvent)
        {
            currentEvent.inputs = new float[5];
            waitTimer = 0.2f;

            currentEvent = nextEvent;

            if (nextEvent.GetComponent<MenuButton>())
            {
                currentEventType = EventType.Button;
            } else if (nextEvent.GetComponent<MenuSlider>())
            {
                currentEventType = EventType.Slider;
            } else if (nextEvent.GetComponent<MenuSelection>())
            {
                currentEventType = EventType.Selection;
            }

            Vector3 hiPos = highlight.position;
            hiPos.y = currentEvent.highlightPoint.position.y;
            highlight.position = hiPos;

            currentEvent.OnHighlight();
        }

        public void DeActive()
        {
            content.SetActive(false);
            icon.color = Color.gray;
            highlight.gameObject.SetActive(false);

            active = false;
        }

        public void Active()
        {
            content.SetActive(true);
            icon.color = Color.white;
            highlight.gameObject.SetActive(true);

            active = true;
        }

        public void SetInputs(float[] exValue)
        {
            inputs = exValue;
        }

        private void Update()
        {
            if (forceUp)
            {
                currentEvent.OnPressUp();
                forceUp = false;
            }

            if (inputs[0] == 0 && inputs[1] == 0)
                waitTimer = 0f;

            if (waitTimer > 0f)
            {
                float frameTime = Time.realtimeSinceStartup - lastFrameTime;
                lastFrameTime = Time.realtimeSinceStartup;
                waitTimer -= frameTime;
                return;
            }

            currentEvent.SetInputs(inputs);
        }
    }
}
