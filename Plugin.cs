using BepInEx;
using BepInEx.Configuration;
using GorillaNetworking;
using GorillaTagScripts;
using MusicVisualiser.SCripts;
using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using Utilla;

namespace MicReactor
{
    [BepInDependency("org.legoandmars.gorillatag.utilla")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        GameObject anc;

        GorillaSpeakerLoudness Speakerloudness;

        Spinner2 spinner;

        Shader GTUber;

        VRRig rig;


        int hSliderValue;

        float radius;

        bool showSlider;
        bool ready;


        List<Renderer> rends = new List<Renderer>();

        List<GameObject> Cubes = new List<GameObject>();

        Color Orange = new Color(1, 0.3288f, 0, 1);

        ConfigEntry<int> Ammount;
        ConfigEntry<bool> MusicMode;
        public Plugin()
        {
            HarmonyPatches.ApplyHarmonyPatches();
            Events.GameInitialized += OnGameInitialized;
            Ammount = Config.Bind("Settings", "Ammount", 5);
            MusicMode = Config.Bind("Settings", "Mode", false);
        }
        void OnGameInitialized(object sender, EventArgs e)
        {
            Speakerloudness = GorillaTagger.Instance.offlineVRRig.GetComponent<GorillaSpeakerLoudness>();
            GTUber = FindObjectOfType<Renderer>().material.shader;
            anc = new GameObject("Visualiser Ancor");
            spinner = anc.AddComponent<Spinner2>();
            FindObjectOfType<GorillaCameraFollow>().transform.SetParent(anc.transform, false);
            anc.transform.SetParent(GorillaTagger.Instance.offlineVRRig.transform, false);
            rig = GorillaTagger.Instance.offlineVRRig;
        }

        private void OnGUI()
        {
            if (showSlider)
            {
                if (PhotonNetwork.InRoom)
                {
                    hSliderValue = (int)GUI.HorizontalSlider(new Rect(25f, 25f, 100f, 30f), (float)hSliderValue, 0f, 100f);
                    GUI.Label(new Rect(35f, 35f, 100f, 30f), Speakerloudness.SmoothedLoudness.ToString());
                    GUI.Label(new Rect(35f, 55f, 100f, 30f), hSliderValue.ToString());
                    GUI.Label(new Rect(65f, 55f, 100f, 30f), Cubes.Count.ToString());
                    if (GUI.Button(new Rect(130, 35, 115, 50), Mode()))
                    {
                        MusicMode.Value = !MusicMode.Value;
                    }
                    Ammount.Value = hSliderValue;
                }
                else
                {
                    GUI.Label(new Rect(35f, 35f, 100f, 50f), "Please Join a Room");
                }
            }
        }

        public string Mode()
        {
            if (MusicMode.Value == true)
            {
                return " In: Music Mode";
            }
            else
            {
                return "In: Voice Mode";
            }
        }
        public void PositionObjectsEvenly()
        {
            int count = Cubes.Count;
            float num = 360f / (float)count;
            float currentLoudness = Speakerloudness.SmoothedLoudness;
            Vector3 position = anc.transform.position;
            for (int i = 0; i < count; i++)
            {
                float num2 = (float)i * num;
                float x = radius * Mathf.Cos(num2 * 0.017453292f);
                float z = radius * Mathf.Sin(num2 * 0.017453292f);
                Vector3 vector = position + new Vector3(x, 0.2f, z);
                Cubes[i].transform.position = vector;
                Cubes[i].transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                float y = vector.y + currentLoudness;
                Vector3 position2 = new Vector3(vector.x, y, vector.z);
                Cubes[i].transform.position = position2;
            }
        }

        void CreateVisCube()
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(c.GetComponent<Collider>());
            c.GetComponent<Renderer>().material.shader = GTUber;
            c.transform.parent = anc.transform;
            c.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
            rends.Add(c.GetComponent<Renderer>());
            Cubes.Add(c);
        }


        float IfMusicMode()
        {
            if (MusicMode.Value == true)
            {
                return Speakerloudness.SmoothedLoudness;
            }
            else
            {
                return Speakerloudness.Loudness;
            }
        }

        private void Update()
        {
            if (Keyboard.current.altKey.wasPressedThisFrame)
            {
                showSlider = !showSlider;
            }
            PositionObjectsEvenly();
            radius = IfMusicMode();
            spinner.Speed = IfMusicMode() * 100f;
            if (Cubes.Count < hSliderValue)
            {
                CreateVisCube();
            }

            if (Cubes.Count > hSliderValue)
            {
                foreach (GameObject cube in Cubes)
                {
                    Destroy(cube);
                    Cubes.Remove(cube);
                    rends.Remove(cube.GetComponent<Renderer>());
                }
            }

            if (!ready)
            {
                hSliderValue = Ammount.Value;
                ready = true;
            }

            if (IfMusicMode() < 0.001f)
            {
                foreach (Renderer rend in rends)
                {
                    rend.enabled = false;
                }

                return;
            }

            foreach (Renderer rend2 in rends)
            {
                rend2.enabled = true;
            }
        }

        void FixedUpdate()
        {
            foreach (Renderer r in rends)
            {
                r.material.color = ColourHandling();
            }
        }
        Color ColourHandling()
        {
            switch (rig.setMatIndex)
            {
                default:
                    return new Color(
                    rig.materialsToChangeTo[rig.tempMatIndex].color.r,
                    rig.materialsToChangeTo[rig.tempMatIndex].color.g,
                    rig.materialsToChangeTo[rig.tempMatIndex].color.b,
                        1
                    );
                case 2:
                case 11:
                    return Orange;
                case 3:
                case 7:
                    return Color.blue;
                case 12:
                    return Color.green;
            }
        }
    }
    public class Spinner2 : MonoBehaviour
    {
        public float Speed;

        public void Update()
        {
            float angle = Speed * Time.deltaTime;
            transform.RotateAround(transform.position, Vector3.up, angle);
        }
    }
}