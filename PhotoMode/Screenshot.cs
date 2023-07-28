using UnityEngine;
using System.IO;
using Rewired;
using MelonLoader;
using System.Collections.Generic;
using UnityStandardAssets.ImageEffects;
using Photos_Plus.Menu;
using TMPro;

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
        // 2 - Render Only The Player Character - To Be Removed
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
            "X: Take Screenshot\n" +
            "\n" +
            "D-Pad Right: Toggle Object/Camera Movement\n" +
            "RB/LB: Cycle Between Objects");

        // Displays Render Resolution On Screen
        private GUIContent resGui = new GUIContent(
            "Current Render Resolution\n" +
            "1920x1080");

        // Prevents Axis Inputs From Firing Every Frame
        private bool dpadLeft;
        private bool dpadRight;
        private bool dpadUp;
        private bool dpadDown;

        // Character Data
        private PlayerBase playerChar;
        private List<PlayerBase> spawnedChars = new List<PlayerBase>();

        // Objects
        private List<Transform> myTransforms = new List<Transform>();
        private int selectedObject;
        private bool manipulateObject;

        private List<bool> characterEffects = new List<bool>();
        private List<bool> superEffects = new List<bool>();
        private List<bool> terrainEffects = new List<bool>();

        private MenuController menuController;
        private ObjectControl oc;

        private GameObject targetParticle;

        private int playerOrigState;
        private float playerOrigStateTime;

        private int currentAnimation;

        private string[] animNames = new string[3]
        {
            "Idle",
            "Victory",
            "Run"
        };

        public MenuController GetMenuController()
        {
            return menuController;
        }

        private void Start()
        {
            oc = gameObject.AddComponent<ObjectControl>();

            // Get The Input Controller

            p = ReInput.players.GetPlayer(0);

            menuController = GameObject.Instantiate(Melon<Plugin>.Instance.menu).GetComponent<MenuController>();

            menuController.tabs[0].transform.Find("Content/TakeScreenshot").GetComponent<MenuButton>().onClickEvent = new MenuButton.clickEvent(DoScreenshot);
            menuController.tabs[0].transform.Find("Content/TakeScreenshotChar").GetComponent<MenuButton>().onClickEvent = new MenuButton.clickEvent(DoScreenshotIso);

            menuController.tabs[1].transform.Find("Content/SpawnCharacterButton").GetComponent<MenuButton>().onClickEvent = new MenuButton.clickEvent(oc.SpawnCharacter);
            menuController.tabs[1].transform.Find("Content/SpawnEnemyButton").GetComponent<MenuButton>().onClickEvent = new MenuButton.clickEvent(oc.SpawnEnemy);
            menuController.tabs[2].transform.Find("Content/DeleteButton").GetComponent<MenuButton>().onClickEvent = new MenuButton.clickEvent(RemoveChar);
            menuController.tabs[2].transform.Find("Content/NextFrameButton").GetComponent<MenuButton>().onClickEvent = new MenuButton.clickEvent(UpdateFrame);

            menuController.tabs[0].transform.Find("Content/MoveSpeed/Slider/Handle").GetComponent<MenuSlider>().value = moveSpeed * 10;
            menuController.tabs[0].transform.Find("Content/MoveMulti/Slider/Handle").GetComponent<MenuSlider>().value = 50;
            menuController.tabs[0].transform.Find("Content/RotSpeed/Slider/Handle").GetComponent<MenuSlider>().value = rotateSpeed;
            menuController.tabs[0].transform.Find("Content/Tilt/Slider/Handle").GetComponent<MenuSlider>().value = 50;

            targetParticle = menuController.transform.Find("PhotoTarget").gameObject;

            oc.AddEnemyList();

            menuController.gameObject.SetActive(false);
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

                // Disable The Camera's Animator
                // Needed To Make Sure Cutscenes Don't Break Controls
                if (transform.Find("PlayerCamera").GetComponent<Animator>())
                {
                    transform.Find("PlayerCamera").GetComponent<Animator>().enabled = false;
                }

                // Reset The Camera's Position And Rotation
                // Needed To Make Sure Cutscenes Don't Break Controls
                transform.Find("PlayerCamera").localPosition = Vector3.zero;
                transform.Find("PlayerCamera").localEulerAngles = Vector3.zero;

                // Get The Current Player Character
                playerChar = GameObject.FindObjectOfType<PlayerBase>();

                // Clear The Object List And Add The Player Character
                myTransforms.Clear();
                myTransforms.Add(playerChar.transform);

                // Set The Player Character As The Currently Selected Object
                selectedObject = 0;

                // Set The Game To Use Camera Controls And Not Object Controls
                manipulateObject = false;

                // Clear Effect Toggles And Add One Toggle For The Player Character
                characterEffects.Clear();
                superEffects.Clear();
                terrainEffects.Clear();

                characterEffects.Add(false);
                superEffects.Add(false);
                terrainEffects.Add(false);

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

                // Show The Cursor
                Cursor.visible = true;

                // Freeze The Game
                Time.timeScale = 0;

                AnimatorStateInfo asi = playerChar.transform.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0);
                playerOrigState = asi.shortNameHash;
                playerOrigStateTime = asi.normalizedTime;

                menuController.tabs[0].transform.Find("Content/Tilt/Slider/Handle").GetComponent<MenuSlider>().value = 50;
                menuController.gameObject.SetActive(true);
                menuController.tabs[2].transform.Find("Content/Object").GetComponent<MenuSelection>().value = 0;
                menuController.tabs[2].transform.Find("Content/Object").GetComponent<MenuSelection>().labels = new List<string>
                {
                    "< Player >"
                };

                if (oc.animations.Count == 0)
                    oc.animations.Add(0);
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

                // Enable The Camera Animator
                // Needed For Cutscenes
                if (transform.Find("PlayerCamera").GetComponent<Animator>())
                {
                    transform.Find("PlayerCamera").GetComponent<Animator>().enabled = true;
                }

                // Unpause The Game
                // Should Reset timeScale
                Singleton<GameManager>.Instance.GameState = GameManager.State.Playing;

                // Enable The Game's UI
                GameObject.FindObjectOfType<UI>().GetComponent<Canvas>().enabled = true;

                // Destroy All Spawned Characters
                foreach (PlayerBase character in spawnedChars)
                {
                    Destroy(character.gameObject);
                }

                // Hide The Cursor
                Cursor.visible = false;

                // Remove All Items From spawnedChars
                // Most Items Shouldn't Exist Anymore
                spawnedChars.Clear();

                // Make Sure The Original Player Character Is Playable
                ReSetCharacter();

                oc.ToggleAnimation(playerChar, "Idle");

                playerChar.transform.GetComponentInChildren<Animator>().Play(playerOrigState, 0, playerOrigStateTime);

                oc.animations.Clear();

                menuController.gameObject.SetActive(false);
            }

            // Use Freecam Controls When Freecam Is Activated
            if (freecam)
            {
                DoMove();
            }
        }

        private void DoMove()
        {
            selectedObject = menuController.tabs[2].transform.Find("Content/Object").GetComponent<MenuSelection>().value;
            currentAnimation = oc.animations[selectedObject];

            menuController.tabs[2].transform.Find("Content/Animation").GetComponent<MenuSelection>().value = currentAnimation;
            menuController.tabs[2].transform.Find("Content/Animation").GetComponent<MenuSelection>().UpdateLabel();

            menuController.tabs[2].transform.Find("Content/CharEffects").GetComponent<MenuSelection>().value = characterEffects[selectedObject] ? 1 : 0;
            menuController.tabs[2].transform.Find("Content/SuperEffects").GetComponent<MenuSelection>().value = superEffects[selectedObject] ? 1 : 0;
            menuController.tabs[2].transform.Find("Content/TerrainEffects").GetComponent<MenuSelection>().value = terrainEffects[selectedObject] ? 1 : 0;

            menuController.tabs[2].transform.Find("Content/CharEffects").GetComponent<MenuSelection>().UpdateLabel();
            menuController.tabs[2].transform.Find("Content/SuperEffects").GetComponent<MenuSelection>().UpdateLabel();
            menuController.tabs[2].transform.Find("Content/TerrainEffects").GetComponent<MenuSelection>().UpdateLabel();

            // Vector That Stores How Much The Camera Should Move This Frame
            Vector3 moveVector = new Vector3();

            // deltaTime Replacement
            float frameTime = Time.realtimeSinceStartup - lastFrameTime;

            moveSpeed = menuController.tabs[0].transform.Find("Content/MoveSpeed/Slider/Handle").GetComponent<MenuSlider>().value / 10;
            rotateSpeed = menuController.tabs[0].transform.Find("Content/RotSpeed/Slider/Handle").GetComponent<MenuSlider>().value;

            // Change The Camera Movement Multiplier Using Analogue Controls
            moveMultiplier = p.GetAxis("Left Trigger") * (menuController.tabs[0].transform.Find("Content/MoveMulti/Slider/Handle").GetComponent<MenuSlider>().value / 5) + 1;

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
            if (p.GetButton("Button Y"))
            {
                moveVector += transform.up * moveSpeed * frameTime;
            } else if (p.GetButton("Button X"))
            {
                moveVector -= transform.up * moveSpeed * frameTime;
            }

            // Move The Camera
            if (manipulateObject)
            {
                myTransforms[selectedObject].position += moveVector;
                targetParticle.SetActive(true);
                targetParticle.transform.position = myTransforms[selectedObject].position;

                // targetParticle.GetComponent<ParticleSystem>().Simulate(targetParticle.GetComponent<ParticleSystem>().time + frameTime);
                // targetParticle.transform.Find("Particle System").GetComponent<ParticleSystem>().Simulate(targetParticle.transform.Find("Particle System").GetComponent<ParticleSystem>().time + frameTime);
            }
            else
            {
                transform.position += moveVector;
                targetParticle.SetActive(false);
            }

            // Change moveVector To The Camera's Rotation
            if (manipulateObject)
                moveVector = myTransforms[selectedObject].localEulerAngles;
            else
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

            if (!manipulateObject) moveVector.z = (menuController.tabs[0].transform.Find("Content/Tilt/Slider/Handle").GetComponent<MenuSlider>().value - 50) * 3;

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
            if (manipulateObject)
                myTransforms[selectedObject].localEulerAngles = moveVector;
            else
                transform.localEulerAngles = moveVector;

            // Position 2 Units In Front Of The Current View For Objects To Teleport To
            Vector3 tpPos = transform.position + (transform.forward * 2);

            if (p.GetButtonDown("Button A"))
            {
                menuController.inputs[4] = 1;
            } else
            {
                menuController.inputs[4] = 0;
            }

            if (p.GetButtonDown("Button B"))
            {
                menuController.gameObject.SetActive(!menuController.gameObject.activeSelf);
            }

            // Teleport The Player Character To tpPos When The Player Presses "Y"
            //if (p.GetButtonDown("Button Y"))
            //{
            //    myTransforms[selectedObject].position = tpPos;
            //}

            //// Take A Screenshot When The Player Presses "X"
            //if (p.GetButtonDown("Button X"))
            //{
            //    Melon<Plugin>.Logger.Msg("Taking Screenshot");
            //    DoScreenshot();
            //    Melon<Plugin>.Logger.Msg("Took Screenshot");
            //    Melon<Plugin>.Logger.Msg(string.Format("Screenshot saved to: {0}", filename));
            //}

            // Change The Render Isolation Mode And UI Text When The Player Presses Left On The D-Pad
            if (p.GetAxis("D-Pad X") < 0)
            {
                menuController.inputs[2] = 1;

                if (menuController.tabs[2].currentEvent.name == "Animation" && menuController.tabs[2].currentEvent.GetComponent<MenuSelection>().leftPressed == false)
                {
                    currentAnimation = menuController.tabs[2].currentEvent.GetComponent<MenuSelection>().value - 1;

                    if (currentAnimation < 0)
                        currentAnimation = 1;
                }

                if (menuController.tabs[2].currentEvent.name == "CharEffects" && menuController.tabs[2].currentEvent.GetComponent<MenuSelection>().leftPressed == false)
                {
                    characterEffects[selectedObject] = !characterEffects[selectedObject];
                }
                if (menuController.tabs[2].currentEvent.name == "SuperEffects" && menuController.tabs[2].currentEvent.GetComponent<MenuSelection>().leftPressed == false)
                {
                    superEffects[selectedObject] = !superEffects[selectedObject];
                }
                if (menuController.tabs[2].currentEvent.name == "TerrainEffects" && menuController.tabs[2].currentEvent.GetComponent<MenuSelection>().leftPressed == false)
                {
                    terrainEffects[selectedObject] = !terrainEffects[selectedObject];
                }

                // Make Sure The Input Only Fires Once Per Button Press
                //if (!dpadLeft)
                //{
                //    renderIsolated++;

                //    if (renderIsolated > 1)
                //        renderIsolated = 0;

                //    if (renderIsolated == 1)
                //        gui.text = gui.text.Replace("Entire Screen", "Character Only");
                //    else if (renderIsolated == 2)
                //        gui.text = gui.text.Replace("Character Only", "Entire Screen");
                //    else
                //        gui.text = gui.text.Replace("Character Only - No Terrain Effects", "Entire Screen");

                //    dpadLeft = true;
                //}
            }
            else
                menuController.inputs[2] = 0;
            //else if (dpadLeft)
            //    dpadLeft = false;

            // Toggle Between Moving The Camera And The Currently Selected Object When The Player
            // Presses Right On The D-Pad
            if (p.GetAxis("D-Pad X") > 0)
            {
                menuController.inputs[3] = 1;

                if (menuController.tabs[2].currentEvent.name == "Animation" && menuController.tabs[2].currentEvent.GetComponent<MenuSelection>().rightPressed == false)
                {
                    currentAnimation = menuController.tabs[2].currentEvent.GetComponent<MenuSelection>().value + 1;

                    if (currentAnimation >= 2)
                        currentAnimation = 0;
                }

                if (menuController.tabs[2].currentEvent.name == "CharEffects" && menuController.tabs[2].currentEvent.GetComponent<MenuSelection>().rightPressed == false)
                {
                    characterEffects[selectedObject] = !characterEffects[selectedObject];
                }
                if (menuController.tabs[2].currentEvent.name == "SuperEffects" && menuController.tabs[2].currentEvent.GetComponent<MenuSelection>().rightPressed == false)
                {
                    superEffects[selectedObject] = !superEffects[selectedObject];
                }
                if (menuController.tabs[2].currentEvent.name == "TerrainEffects" && menuController.tabs[2].currentEvent.GetComponent<MenuSelection>().rightPressed == false)
                {
                    terrainEffects[selectedObject] = !terrainEffects[selectedObject];
                }
                //if (!dpadRight)
                //{
                //    dpadRight = true;

                //    manipulateObject = !manipulateObject;
                //}
            }
            else
                menuController.inputs[3] = 0;
            //else if (dpadRight) dpadRight = false;

            // Increase The Render Resolution Multiplier When The Player Presses Up On The D-Pad
            if (p.GetAxis("D-Pad Y") > 0)
            {
                menuController.inputs[0] = 1;

                //if (!dpadUp)
                //{
                //    dpadUp = true;

                //    multiplier += 0.5f;

                //    // Update The UI Text With The New Resolution
                //    UpdateResolutionGUI();
                //}
            }
            else
                menuController.inputs[0] = 0;
            //else if (dpadUp) dpadUp = false;

            // Decrease The Render Resolution Multiplier When The Player Presses Down On The D-Pad
            if (p.GetAxis("D-Pad Y") < 0)
            {
                menuController.inputs[1] = 1;

                //if (!dpadDown)
                //{
                //    dpadDown = true;

                //    // Don't Lower The Resolution Below Half
                //    if (multiplier > 0.6f)
                //        multiplier -= 0.5f;

                //    // Update The UI Text With The New Resolution
                //    UpdateResolutionGUI();
                //}
            }
            else
                menuController.inputs[1] = 0;
            //else if (dpadDown) dpadDown = false;

            // Cycle Through Spawned Objects Using The Bumpers
            if (p.GetButtonDown("Right Bumper"))
            {
                menuController.NextTab();

                if (menuController.currentTab == 2)
                    manipulateObject = true;
                else
                    manipulateObject = false;

                //selectedObject++;
                //if (selectedObject >= myTransforms.Count)
                //    selectedObject = 0;
            }

            if (p.GetButtonDown("Left Bumper"))
            {
                menuController.PreviousTab();

                if (menuController.currentTab == 2)
                    manipulateObject = true;
                else
                    manipulateObject = false;

                //selectedObject--;
                //if (selectedObject < 0)
                //    selectedObject = myTransforms.Count - 1;
            }

            if (currentAnimation != oc.animations[selectedObject])
            {
                oc.animations[selectedObject] = currentAnimation;

                oc.ToggleAnimation(myTransforms[selectedObject].GetComponent<PlayerBase>(), animNames[currentAnimation]);
            }

            // Update lastFrameTime
            lastFrameTime = Time.realtimeSinceStartup;
        }

        public void DoScreenshotIso()
        {
            renderIsolated = 1;
            DoScreenshot();
            renderIsolated = 0;
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
            // Turn Each Character's Effects On/Off
            ToggleCharacterEffects();
            ToggleCharacterSuperEffects();
            ToggleCharacterTerrainEffects();

            // Disable Bloom - Prevents A Shadow Around Characters
            transform.Find("PlayerCamera").GetComponent<LegacyBloom>().enabled = false;

            // Render Shadow's Inhibiter Rings
            if (GameObject.Find("shadow_L_limiter.xno"))
            {
                GameObject.Find("shadow_L_limiter.xno").layer = 8;
                GameObject.Find("shadow_R_limiter.xno").layer = 8;
            }

            // Show Custom Characters
            ShowCustomModelIsolated();

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

            // Reset Each Character's Effects To The State They Were Before
            ResetAllPlayerLayers();

            // Enable Bloom
            transform.Find("PlayerCamera").GetComponent<LegacyBloom>().enabled = true;

            // Reset Shadow's Inhibiter Rings To Their Default State
            if (GameObject.Find("shadow_L_limiter.xno"))
            {
                GameObject.Find("shadow_L_limiter.xno").layer = 13;
                GameObject.Find("shadow_R_limiter.xno").layer = 13;
            }

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

        // Render The UI - Old
        void OnGUI()
        {
            return;

            if (freecam)
            {
                GUILayout.Box(gui);
                GUILayout.Box(resGui);

                GUILayout.BeginVertical("box");

                GUILayout.Label("Spawn Characters");

                PlayerBase pbase;

                if (GUILayout.Button("Sonic"))
                {
                    pbase = CreateCharacter("Sonic_New");
                    spawnedChars.Add(pbase);
                    myTransforms.Add(pbase.transform);

                    characterEffects.Add(false);
                    superEffects.Add(false);
                    terrainEffects.Add(false);
                }
                if (GUILayout.Button("Tails"))
                {
                    pbase = CreateCharacter("Tails");
                    spawnedChars.Add(pbase);
                    myTransforms.Add(pbase.transform);

                    characterEffects.Add(false);
                    superEffects.Add(false);
                    terrainEffects.Add(false);
                }
                if (GUILayout.Button("Knuckles"))
                {
                    pbase = CreateCharacter("Knuckles");
                    spawnedChars.Add(pbase);
                    myTransforms.Add(pbase.transform);

                    characterEffects.Add(false);
                    superEffects.Add(false);
                    terrainEffects.Add(false);
                }
                if (GUILayout.Button("Shadow"))
                {
                    pbase = CreateCharacter("Shadow");
                    spawnedChars.Add(pbase);
                    myTransforms.Add(pbase.transform);

                    characterEffects.Add(false);
                    superEffects.Add(false);
                    terrainEffects.Add(false);
                }
                if (GUILayout.Button("Rouge"))
                {
                    pbase = CreateCharacter("Rouge");
                    spawnedChars.Add(pbase);
                    myTransforms.Add(pbase.transform);

                    characterEffects.Add(false);
                    superEffects.Add(false);
                    terrainEffects.Add(false);
                }
                if (GUILayout.Button("Omega"))
                {
                    pbase = CreateCharacter("Omega");
                    spawnedChars.Add(pbase);
                    myTransforms.Add(pbase.transform);

                    characterEffects.Add(false);
                    superEffects.Add(false);
                    terrainEffects.Add(false);
                }
                if (GUILayout.Button("Silver"))
                {
                    pbase = CreateCharacter("Silver");
                    spawnedChars.Add(pbase);
                    myTransforms.Add(pbase.transform);

                    characterEffects.Add(false);
                    superEffects.Add(false);
                    terrainEffects.Add(false);
                }
                if (GUILayout.Button("Blaze"))
                {
                    pbase = CreateCharacter("Blaze");
                    spawnedChars.Add(pbase);
                    myTransforms.Add(pbase.transform);

                    characterEffects.Add(false);
                    superEffects.Add(false);
                    terrainEffects.Add(false);
                }
                if (GUILayout.Button("Amy"))
                {
                    pbase = CreateCharacter("Amy");
                    spawnedChars.Add(pbase);
                    myTransforms.Add(pbase.transform);

                    characterEffects.Add(false);
                    superEffects.Add(false);
                    terrainEffects.Add(false);
                }
                if (GUILayout.Button("Sonic And Elise"))
                {
                    pbase = CreateCharacter("Princess");
                    spawnedChars.Add(pbase);
                    myTransforms.Add(pbase.transform);

                    characterEffects.Add(false);
                    superEffects.Add(false);
                    terrainEffects.Add(false);
                }

                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");

                GUILayout.Label("Selected Object: " + myTransforms[selectedObject].name);

                GUILayout.Label("\n-Following Toggles Only For Character Only Renders-");

                characterEffects[selectedObject] = GUILayout.Toggle(characterEffects[selectedObject], "Display Character Effects");
                superEffects[selectedObject] = GUILayout.Toggle(superEffects[selectedObject], "Display Character Super Effects");
                terrainEffects[selectedObject] = GUILayout.Toggle(terrainEffects[selectedObject], "Display Character Terrain Effects");

                if (manipulateObject)
                    GUILayout.Label("Moving Object");
                else
                    GUILayout.Label("Moving Camera");
                GUILayout.EndVertical();
            }
        }

        public void AddChar(string character)
        {
            PlayerBase pbase = CreateCharacter(character);
            spawnedChars.Add(pbase);
            myTransforms.Add(pbase.transform);

            characterEffects.Add(false);
            superEffects.Add(false);
            terrainEffects.Add(false);
        }

        public void RemoveChar()
        {
            if (menuController.tabs[2].transform.Find("Content/Object").GetComponent<MenuSelection>().value == 0) return;

            characterEffects.RemoveAt(selectedObject);
            superEffects.RemoveAt(selectedObject);
            terrainEffects.RemoveAt(selectedObject);

            if (myTransforms[selectedObject].GetComponent<PlayerBase>())
            {
                PlayerBase pbase = myTransforms[selectedObject].GetComponent<PlayerBase>();
                spawnedChars.Remove(pbase);
                Destroy(pbase.gameObject);
            } else
            {
                Destroy(myTransforms[selectedObject].gameObject);
            }
            myTransforms.RemoveAt(selectedObject);

            menuController.tabs[2].transform.Find("Content/Object").GetComponent<MenuSelection>().value = 0;
            menuController.tabs[2].transform.Find("Content/Object").GetComponent<MenuSelection>().labels.RemoveAt(selectedObject);
            menuController.tabs[2].transform.Find("Content/Object").GetComponent<MenuSelection>().UpdateLabel();
        }

        public void AddEnemy(Transform en)
        {
            myTransforms.Add(en);

            characterEffects.Add(false);
            superEffects.Add(false);
            terrainEffects.Add(false);
        }

        public void UpdateFrame()
        {
            myTransforms[selectedObject].gameObject.GetComponentInChildren<Animator>().Update(0.01f);
            if (myTransforms[selectedObject].name.Contains("Princess"))
            {
                myTransforms[selectedObject].Find("Mesh/sonic_Root/ch_princess01").GetComponent<Animator>().Update(0.01f);
            }
        }

        // Spawns The Selected Character
        private PlayerBase CreateCharacter(string character)
        {
            return (Instantiate(Resources.Load("DefaultPrefabs/Player/" + character), transform.position + (transform.forward * 2), Quaternion.identity) as GameObject).GetComponent<PlayerBase>();
        }

        // Returns The Original Character To A Playable State When Exiting Free Cam
        private void ReSetCharacter()
        {
            playerChar.enabled = true;
        }

        // Disable Each Character's Effects If The Player Turned Them Off For That Character
        private void ToggleCharacterEffects()
        {
            for (int i = 0; i < myTransforms.Count; i++)
            {
                if (characterEffects[i])
                {
                    ChangeLayersRecursively(myTransforms[i].Find("PlayerEffects"), 0);

                    if (myTransforms[i].Find("PlayerEffects/SuperFX"))
                    {
                        ChangeLayersRecursively(myTransforms[i].Find("PlayerEffects/SuperFX"), 8);
                    }
                }
            }
        }

        // Disable Each Character's Terrain Effects If The Player Turned Them Off For That Character
        private void ToggleCharacterTerrainEffects()
        {
            for (int i = 0; i < myTransforms.Count; i++)
            {
                if (terrainEffects[i])
                {
                    ChangeLayersRecursively(myTransforms[i].Find("CharacterTerrain(Clone)"), 0);
                }
            }
        }

        // Disable Each Character's Super Form Effects If The Player Turned Them Off For That Character
        private void ToggleCharacterSuperEffects()
        {
            for (int i = 0; i < myTransforms.Count; i++)
            {
                if (myTransforms[i].Find("PlayerEffects/SuperFX"))
                {
                    ChangeLayersRecursively(myTransforms[i].Find("PlayerEffects/SuperFX"), 0);
                }
            }
        }

        // Enable Each Character's Effects
        private void ResetAllPlayerLayers()
        {
            for (int i = 0; i < myTransforms.Count; i++)
            {
                ChangeLayersRecursively(myTransforms[i].Find("PlayerEffects"), 8);
                if (myTransforms[i].Find("PlayerEffects/SuperFX"))
                {
                    ChangeLayersRecursively(myTransforms[i].Find("PlayerEffects/SuperFX"), 8);
                }
                ChangeLayersRecursively(myTransforms[i].Find("CharacterTerrain(Clone)"), 8);
            }
        }

        // Set The Layer Of An Object, And All Children, Grandchildren, etc.
        private void ChangeLayersRecursively(Transform trans, int layer)
        {
            trans.gameObject.layer = layer;

            foreach (Transform child in trans)
                ChangeLayersRecursively(child, layer);
        }

        // Show Custom Characters In Isolated Renders
        private void ShowCustomModelIsolated()
        {
            if (GameObject.Find("MeshCustom"))
            {
                ChangeLayersRecursively(GameObject.Find("MeshCustom").transform, 8);
            }
        }
    }
}
