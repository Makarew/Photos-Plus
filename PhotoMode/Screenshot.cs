using UnityEngine;
using System.IO;
using Rewired;
using MelonLoader;

namespace Photos_Plus
{
    internal class Screenshot : MonoBehaviour
    {
        // Game Window Resolution
        public int resWidth = 1920;
        public int resHeight = 1080;

        // Resolution Mulitplier Used During Image Rendering
        public float multiplier = 1;

        // Primary Camera
        public Camera camera;

        // Name And Directory Of The Last Saved Image
        public string filename;

        // Camera Movement Speed
        public float moveSpeed = 5f;
        public float rotateSpeed = 60f;
        public float moveMultiplier = 1;

        // Are Freecam And Photo Mode Active
        private bool freecam;

        // Camera's Original Rotation Before Activating Freecam - Remove?
        private Vector2 cRot;

        // Input Controller
        internal Player p;

        // realtimeSinceStartup Of The Last Frame
        // Replaces deltaTime Because
        // timeScale Gets Set To 0 Causing deltaTime To Return 0
        private float lastFrameTime;

        // What Should Get Rendered
        // 0 - Render Everything
        // 1 - Render Anything Tagged "Player"
        // 2 - Render Only The Player Character
        private int renderIsolated = 0;

        // Displays Controls On Screen
        private GUIContent gui = new GUIContent(
            "Photo Mode Active\n" +
            "\n" +
            "Currently Rendering: Entire Screen\n" +
            "~~~~~~~~~~~~~~~~~\n" +
            "Controls\n" +
            "A: Move Up\n" +
            "B: Move Down\n" +
            "Left Trigger: Change Camera Speed\n" +
            "\n" +
            "D-Pad Left: Toggle Character Isolation\n" +
            "Y: Teleport Character\n" +
            "X: Take Screenshot");

        // Displays Render Resolution On Screen
        private GUIContent resGui = new GUIContent(
            "Current Render Resolution\n" +
            "1920x1080");

        // Prevents Axis Inputs From Firing Every Frame
        private bool dpadLeft;
        private bool dpadRight;
        private bool dpadUp;
        private bool dpadDown;

        private void Start()
        {
            // Get The Input Controller

            p = ReInput.players.GetPlayer(0);

            //Type t = typeof(RInput);
            //RInput rin = Singleton<RInput>.Instance;

            //FieldInfo field = t.GetField("P", BindingFlags.Instance | BindingFlags.NonPublic);
            //p = (Player)field.GetValue(rin);
        }

        // Get New Filename
        public static string ScreenShotName(int width, int height)
        {
            return string.Format("{0}/../screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        }

        private void Update()
        {
            // Make Sure The Camera Exists
            if (camera == null)
            {
                try
                {
                    camera = GameObject.Find("PlayerCamera").GetComponent<Camera>();
                }
                catch
                {
                    Melon<Plugin>.Logger.Error("Unable To Find Camera");
                    return;
                }
            }

            //Player p = ReInput.players.GetPlayer(0);

            // Activate Freecam And Photo Mode When The Player Presses "Back"
            if (p.GetButtonDown("Back") && !freecam) 
            {
                // Toggle Freecam
                freecam = true;
                // Disable Original Camera Controls
                // Prevents Unwanted Movement
                gameObject.GetComponent<PlayerCamera>().enabled = false;
                // Get Camera's Original Rotation - Remove?
                cRot.x = transform.rotation.eulerAngles.x;
                cRot.y = transform.rotation.eulerAngles.y;

                // Always Start Rendering Entire Scene When First Activated
                renderIsolated = 0;

                // Make Sure The UI Says "Entire Screen"
                if (gui.text.Contains("Character Only - No Terrain Effects"))
                {
                    gui.text = gui.text.Replace("Character Only - No Terrain Effects", "Entire Screen");
                } else if (gui.text.Contains("Character Only"))
                    gui.text = gui.text.Replace("Character Only", "Entire Screen");

                // Get Game Window Resolution
                resWidth = Display.main.renderingWidth;
                resHeight = Display.main.renderingHeight;
                // Reset Render Resolution Multiplier
                multiplier = 1;
                // Make Sure The UI Says The Correct Resolution
                UpdateResolutionGUI();

                // Disable The Character Controller
                GameObject.FindObjectOfType<PlayerBase>().enabled = false;

                // Store The Time - Remove?
                lastFrameTime = Time.realtimeSinceStartup;

                // Pause The Game
                // Prevents GameManager From Forcing timeScale Back To 1
                Singleton<GameManager>.Instance.GameState = GameManager.State.Paused;

                // Hide The Game's UI
                GameObject.FindObjectOfType<UI>().GetComponent<Canvas>().enabled = false;

                // Freeze The Game
                Time.timeScale = 0;
            } 
            // Return To Normal Gameplay When The Player Presses "Back"
            // While Freecam And Photo Mode Are Activated
            else if (p.GetButtonDown("Back") && freecam)
            {
                // Toggle Freecam
                freecam = false;
                // Enable The Original Camera Controls
                // Resets Position And Rotation To The Correct Place
                // From Before Freecam Being Activated
                gameObject.GetComponent<PlayerCamera>().enabled = true;

                // Enable The Camera Controller
                GameObject.FindObjectOfType<PlayerBase>().enabled = true;

                // Unpause The Game
                // Should Reset timeScale
                Singleton<GameManager>.Instance.GameState = GameManager.State.Playing;

                // Enable The Game's UI
                GameObject.FindObjectOfType<UI>().GetComponent<Canvas>().enabled = true;
            }

            // Use Freecam Controls When Freecam Is Activated
            if (freecam)
            {
                DoMove();
            }
        }

        private void DoMove()
        {
            // Vector That Stores How Much The Camera Should Move This Frame
            Vector3 moveVector = new Vector3();

            // deltaTime Replacement
            float frameTime = Time.realtimeSinceStartup - lastFrameTime;

            // Change The Camera Movement Multiplier Using Analogue Controls
            moveMultiplier = p.GetAxis("Left Trigger") * 8 + 1;

            // Use Left Stick Analogue Y Axis To Set The Forward Movement Of The Camera
            if (p.GetAxis("Left Stick Y") != 0)
            {
                moveVector += transform.forward * moveSpeed * p.GetAxis("Left Stick Y") * moveMultiplier * frameTime;
            }

            // Use Left Stick Analogue X Axis To Set The Sideways Movement Of The Camera
            if (p.GetAxis("Left Stick X") != 0)
            {
                moveVector += transform.right * moveSpeed * p.GetAxis("Left Stick X") * moveMultiplier * frameTime;
            }

            // Use The A And B Buttons To Set The Upwards Movement Of The Camera
            if (p.GetButton("Button A"))
            {
                moveVector += transform.up * moveSpeed * frameTime;
            } else if (p.GetButton("Button B"))
            {
                moveVector -= transform.up * moveSpeed * frameTime;
            }

            // Move The Camera
            transform.position += moveVector;

            // Change moveVector To The Camera's Rotation
            moveVector = transform.localEulerAngles;

            // Use Right Stick Analogue Y Axis To Set The Vertical Rotation Of The Camera
            if (p.GetAxis("Right Stick Y") != 0)
            {
                moveVector.x -= rotateSpeed * p.GetAxis("Right Stick Y") * frameTime;
            }
            // Use Right Stick Analogue X Axis To Set The Horizontal Rotation Of The Camera
            if (p.GetAxis("Right Stick X") != 0)
            {
                moveVector.y += rotateSpeed * p.GetAxis("Right Stick X") * frameTime;
            }

            // Prevent The Camera From Rotating Upside Down
            if (moveVector.x >= 89 && moveVector.x < 100)
            {
                moveVector.x = 89;
            } else if (moveVector.x < 0)
            {
                moveVector.x += 360;
            } else if (moveVector.x > 360)
            {
                moveVector.x -= 360;
            } else if (moveVector.x <= 270 && moveVector.x > 150)
            {
                moveVector.x = 270;
            }

            // Rotate The Camera
            transform.localEulerAngles = moveVector;

            // Position 2 Units In Front Of The Current View For Objects To Teleport To
            Vector3 tpPos = transform.position + (transform.forward * 2);

            // Teleport The Player Character To tpPos When The Player Presses "Y"
            if (p.GetButtonDown("Button Y"))
            {
                GameObject.FindObjectOfType<PlayerBase>().transform.position = tpPos;
            }

            // Take A Screenshot When The Player Presses "X"
            if (p.GetButtonDown("Button X"))
            {
                Melon<Plugin>.Logger.Msg("Taking Screenshot");
                DoScreenshot();
                Melon<Plugin>.Logger.Msg("Took Screenshot");
                Melon<Plugin>.Logger.Msg(string.Format("Screenshot saved to: {0}", filename));
            }

            // Change The Render Isolation Mode And UI Text When The Player Presses Left On The D-Pad
            if (p.GetAxis("D-Pad X") < 0)
            {
                // Make Sure The Input Only Fires Once Per Button Press
                if (!dpadLeft)
                {
                    renderIsolated++;

                    if (renderIsolated > 2)
                        renderIsolated = 0;

                    if (renderIsolated == 1)
                        gui.text = gui.text.Replace("Entire Screen", "Character Only");
                    else if (renderIsolated == 2)
                        gui.text = gui.text.Replace("Character Only", "Character Only - No Terrain Effects");
                    else
                        gui.text = gui.text.Replace("Character Only - No Terrain Effects", "Entire Screen");

                    dpadLeft = true;
                }
            }
            else if (dpadLeft)
                dpadLeft = false;

            // D-Pad Right - Unused
            if (p.GetAxis("D-Pad X") > 0)
            {
                if (!dpadRight)
                {
                    dpadRight = true;
                }
            } else if (dpadRight) dpadRight = false;

            // Increase The Render Resolution Multiplier When The Player Presses Up On The D-Pad
            if (p.GetAxis("D-Pad Y") > 0)
            {
                if (!dpadUp)
                {
                    dpadUp = true;

                    multiplier += 0.5f;

                    // Update The UI Text With The New Resolution
                    UpdateResolutionGUI();
                }
            }
            else if (dpadUp) dpadUp = false;

            // Decrease The Render Resolution Multiplier When The Player Presses Down On The D-Pad
            if (p.GetAxis("D-Pad Y") < 0)
            {
                if (!dpadDown)
                {
                    dpadDown = true;

                    // Don't Lower The Resolution Below Half
                    if (multiplier > 0.6f)
                        multiplier -= 0.5f;

                    // Update The UI Text With The New Resolution
                    UpdateResolutionGUI();
                }
            }
            else if (dpadDown) dpadDown = false;

            // Update lastFrameTime
            lastFrameTime = Time.realtimeSinceStartup;
        }

        // Exports A Screenshot
        public void DoScreenshot()
        {
            // Create The Directory If It Doesn't Exist
            if (!Directory.Exists(Application.dataPath + "/../screenshots"))
            {
                Directory.CreateDirectory(Application.dataPath + "/../screenshots");
            }

            // Create A Texture2D To Apply The Render To Before Export
            Texture2D screenShot = new Texture2D(Mathf.RoundToInt(float.Parse(resWidth.ToString()) * multiplier), Mathf.RoundToInt(float.Parse(resHeight.ToString()) * multiplier), TextureFormat.RGBA32, false);

            // Set The Pixels Of The Texture2D Based On The Render Mode
            if (renderIsolated == 1)
                screenShot.SetPixels(RenderPlayerOnly().GetPixels());
            else if (renderIsolated == 0)
                screenShot.SetPixels(CamsToTexture().GetPixels());
            else if (renderIsolated == 2)
            {
                // Hide Terrain Effects Before Rendering
                GameObject effects = GameObject.Find("Foostep");
                effects.SetActive(false);
                screenShot.SetPixels(RenderPlayerOnly().GetPixels());
                // Re-Enable Terrain Effects After Rendering
                effects.SetActive(true);
            }

            // Encode The Texture2D To A .png
            byte[] bytes = screenShot.EncodeToPNG();
            // Get The Filename
            filename = ScreenShotName(Mathf.RoundToInt(float.Parse(resWidth.ToString()) * multiplier), Mathf.RoundToInt(float.Parse(resHeight.ToString()) * multiplier));
            // Create The File
            File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}", filename));
        }

        // Renders The Entire Screen
        private Texture2D CamsToTexture()
        {
            // Create A Texture2D To Apply The Render To
            Texture2D mainShot = new Texture2D(Mathf.RoundToInt(float.Parse(resWidth.ToString()) * multiplier), Mathf.RoundToInt(float.Parse(resHeight.ToString()) * multiplier), TextureFormat.RGBA32, false);

            // Create A Render Texture
            RenderTexture rt = new RenderTexture(Mathf.RoundToInt(float.Parse(resWidth.ToString()) * multiplier), Mathf.RoundToInt(float.Parse(resHeight.ToString()) * multiplier), 24);

            // Render The Skybox To The Render Texture
            Camera skyCam = camera.transform.Find("SkyboxCamera").GetComponent<Camera>();
            skyCam.targetTexture = rt;
            skyCam.Render();

            // Render The Main Camera To The Render Texture And Increase The Render Distance
            camera.targetTexture = rt;
            camera.farClipPlane = 10000f;
            camera.Render();

            // Set Our Render Texture As The Active Render Texture
            RenderTexture.active = rt;

            // Render The Render Texture To The Texture2D
            mainShot.ReadPixels(new Rect(0, 0, Mathf.RoundToInt(float.Parse(resWidth.ToString()) * multiplier), Mathf.RoundToInt(float.Parse(resHeight.ToString()) * multiplier)), 0, 0);

            // Reset Cameras And Remove The Active Render Texture
            skyCam.targetTexture = null;
            camera.targetTexture = null;
            RenderTexture.active = null;

            // Destroy Our Render Texture
            Destroy(rt);

            // Reset The Main Camera Render Distance
            camera.farClipPlane = 4500f;

            // Return The Render
            return mainShot;
        }

        // Renders Only The Player Layer
        private Texture2D RenderPlayerOnly()
        {
            // Create A Texture2D To Apply The Render To
            Texture2D mainShot = new Texture2D(Mathf.RoundToInt(float.Parse(resWidth.ToString()) * multiplier), Mathf.RoundToInt(float.Parse(resHeight.ToString()) * multiplier), TextureFormat.RGBA32, false);

            // Create A Render Texture
            RenderTexture rt = new RenderTexture(Mathf.RoundToInt(float.Parse(resWidth.ToString()) * multiplier), Mathf.RoundToInt(float.Parse(resHeight.ToString()) * multiplier), 24);
            // Set The Main Camera's Render Texture
            camera.targetTexture = rt;

            // Store The Original Culling Mask
            int mask = camera.cullingMask;

            // Stop Rendering All Layers On The Main Camera Except For The Player Layer
            for (int i = 0; i < 31; i++)
            {
                if (i != 8)
                {
                    camera.cullingMask &= ~(1 << i);
                }
            }

            // Increase The Render Distance On The Main Camera
            camera.farClipPlane = 10000f;

            // Render The Main Camera To The Render Texture
            camera.Render();

            // Set Our Render Texture As The Active Render Texture
            RenderTexture.active = rt;

            // Render The Render Texture To The Texture2D
            mainShot.ReadPixels(new Rect(0, 0, Mathf.RoundToInt(float.Parse(resWidth.ToString()) * multiplier), Mathf.RoundToInt(float.Parse(resHeight.ToString()) * multiplier)), 0, 0);

            // Reset Camera And Remove The Active Render Texture
            camera.targetTexture = null;
            RenderTexture.active = null;

            // Destroy Our Render Texture
            Destroy(rt);

            // Reset The Main Camera Render Distance And Culling Mask
            camera.farClipPlane = 4500f;
            camera.cullingMask = mask;

            // Return The Render
            return mainShot;
        }

        // Update The UI With The Current Render Resolution
        private void UpdateResolutionGUI()
        {
            // Apply A Warning On Large Renders
            if (resWidth * multiplier > 7680)
            {
                resGui.text = "Current Render Resolution\n" +
                                (resWidth * multiplier) + "x" + (resHeight * multiplier) + "\n" +
                                "\nWARNING\n" +
                                "You are rendering at a high resolution\n" +
                                "This can cause long render times and crashing\n" +
                                "It is recommended to lower the render resolution";
            }
            else
            {
                resGui.text = "Current Render Resolution\n" +
                                (resWidth * multiplier) + "x" + (resHeight * multiplier);
            }
        }

        // Render The UI
        void OnGUI()
        {
            if (freecam)
            {
                GUILayout.Box(gui);
                GUILayout.Box(resGui);
            }
        }
    }
}
