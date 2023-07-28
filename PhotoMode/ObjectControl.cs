using Photos_Plus.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Photos_Plus
{
    internal class ObjectControl : MonoBehaviour
    {
        public List<int> animations = new List<int>();

        public void SpawnCharacter()
        {
            Screenshot screenshot = gameObject.GetComponent<Screenshot>();
            MenuController menu = screenshot.GetMenuController();
            int value = menu.tabs[1].transform.Find("Content/SpawnCharacter").GetComponent<MenuSelection>().value;

            string charSpawn = "";

            switch(value)
            {
                case 0:
                    charSpawn += "Sonic_New";
                    break;
                case 1:
                    charSpawn += "Tails";
                    break;
                case 2:
                    charSpawn += "Knuckles";
                    break;
                case 3:
                    charSpawn += "Shadow";
                    break;
                case 4:
                    charSpawn += "Rouge";
                    break;
                case 5:
                    charSpawn += "Omega";
                    break;
                case 6:
                    charSpawn += "Silver";
                    break;
                case 7:
                    charSpawn += "Blaze";
                    break;
                case 8:
                    charSpawn += "Amy";
                    break;
                case 9:
                    charSpawn += "Princess";
                    break;
            }

            screenshot.AddChar(charSpawn);

            charSpawn = "< " + charSpawn + " >";
            animations.Add(0);

            menu.tabs[2].transform.Find("Content/Object").GetComponent<MenuSelection>().labels.Add(charSpawn);
        }

        public void SpawnEnemy()
        {
            Screenshot screenshot = gameObject.GetComponent<Screenshot>();
            MenuController menu = screenshot.GetMenuController();
            int value = menu.tabs[1].transform.Find("Content/SpawnEnemy").GetComponent<MenuSelection>().value;

            GameObject enemy = gameObject;

            string enemySpawn = "< ";

            switch (value)
            {
                case 0:
                    enemy = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("defaultprefabs/enemy/eGunner"), transform.position + (transform.forward * 2), Quaternion.identity);
                    enemy.GetComponent<eGunner>().SetParameters(false, true, "", 0f, "", Vector3.zero);

                    enemy.GetComponent<eGunner>().RobotMode = eGunner.Mode.Chase;

                    enemySpawn += "Gunner";
                    break;
                case 1:
                    enemy = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("defaultprefabs/enemy/ctaker"), transform.position + (transform.forward * 2), Quaternion.identity);
                    enemy.GetComponent<cTaker>().CreatureMode = cTaker.Mode.Normal;

                    enemySpawn += "Taker";
                    break;
                case 2:
                    enemy = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("defaultprefabs/enemy/cbiter"), transform.position + (transform.forward * 2), Quaternion.identity);
                    enemy.GetComponent<cBiter>().CreatureMode = cBiter.Mode.Normal;

                    enemySpawn += "Biter";
                    break;
                case 3:
                    enemy = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("defaultprefabs/enemy/cgolem"), transform.position + (transform.forward * 2), Quaternion.identity);
                    enemy.GetComponent<cGolem>().CreatureMode = cGolem.Mode.Normal;

                    enemySpawn += "Golem";
                    break;
                case 4:
                    enemy = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("defaultprefabs/enemy/ccrawler"), transform.position + (transform.forward * 2), Quaternion.identity);
                    enemy.GetComponent<cCrawler>().CreatureMode = cCrawler.Mode.Normal;

                    enemySpawn += "Crawler";
                    break;
                case 5:
                    enemy = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("defaultprefabs/enemy/eCannon"), transform.position + (transform.forward * 2), Quaternion.identity);
                    enemy.GetComponent<eCannon>().RobotMode = eCannon.Mode.Normal;

                    enemySpawn += "Cannon";
                    break;
                case 6:
                    enemy = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("defaultprefabs/enemy/eBomber"), transform.position + (transform.forward * 2), Quaternion.identity);
                    enemy.GetComponent<eBomber>().RobotMode = eBomber.Mode.Fix;

                    enemySpawn += "Bomber";
                    break;
                case 7:
                    enemy = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("defaultprefabs/enemy/eSweeper"), transform.position + (transform.forward * 2), Quaternion.identity);
                    enemy.GetComponent<eBomber>().RobotMode = eBomber.Mode.Fix;

                    enemySpawn += "Sweeper";
                    break;
                case 8:
                    enemy = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("defaultprefabs/enemy/eFlyer"), transform.position + (transform.forward * 2), Quaternion.identity);
                    enemy.GetComponent<eFlyer>().RobotMode = eFlyer.Mode.Fix_Rocket;

                    enemySpawn += "Flyer";
                    break;
                case 9:
                    enemy = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("defaultprefabs/enemy/ebluster"), transform.position + (transform.forward * 2), Quaternion.identity);
                    enemy.GetComponent<eFlyer>().RobotMode = eFlyer.Mode.Fix_Vulcan;

                    enemySpawn += "Bluster";
                    break;
            }

            animations.Add(0);

            enemySpawn += " >";

            screenshot.AddEnemy(enemy.transform);

            menu.tabs[2].transform.Find("Content/Object").GetComponent<MenuSelection>().labels.Add(enemySpawn);
        }

        public void AddEnemyList()
        {
            Screenshot screenshot = gameObject.GetComponent<Screenshot>();
            MenuSelection menu = screenshot.GetMenuController().tabs[1].transform.Find("Content/SpawnEnemy").GetComponent<MenuSelection>();

            menu.labels.RemoveAt(0);
            menu.labels.Add("Gunner");
            menu.labels.Add("Taker");
            menu.labels.Add("Biter");
            menu.labels.Add("Golem");
            menu.labels.Add("Crawler");
            menu.labels.Add("Cannon");
            menu.labels.Add("Bomber");
            menu.labels.Add("Sweeper");
            menu.labels.Add("Flyer");
            menu.labels.Add("Bluster");

            animations.Add(0);
        }

        public void ToggleAnimation(PlayerBase player, string Anim)
        {
            Animator animator = player.transform.Find("Mesh").GetComponentInChildren<Animator>();

            if (player.name.Contains("Omega")) PlayAnimation(animator, Anim, true);
            else PlayAnimation(animator, Anim, false);

            if (player.name.Contains("Princess"))
            {
                animator = player.transform.Find("Mesh/sonic_Root/ch_princess01").GetComponent<Animator>();
                PlayAnimation(animator, Anim, false);
            }
        }

        private void PlayAnimation(Animator animator, string Anim, bool omega)
        {
            int vicLayer = 2;
            if (omega) vicLayer = 5;

            switch (Anim)
            {
                case "Victory":
                    if (omega) animator.Play(1809627817, 5);
                    else animator.Play(-804951304, 2);
                    break;
                case "Idle":
                    animator.Play(-499022955, vicLayer);
                    animator.Play(-1916996070, 0);
                    animator.SetFloat("Speed", 0);
                    break;
                case "Run":
                    animator.Play(-499022955, vicLayer);
                    animator.Play(-1916996070, 0);
                    animator.SetFloat("Speed", 10);
                    break;
            }
        }
    }
}