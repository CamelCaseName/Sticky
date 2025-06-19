using HPUI;
using Il2Cpp;
using Il2CppEekCharacterEngine;
using MelonLoader;
using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace Sticky
{
    public class Sticky : MelonMod
    {
        MelonPreferences_Entry<bool>? ManualOverride;
        static private GameObject? CanvasGO;
        static private Canvas? canvas;
        static private Dropdown? dropdown;

        public enum BodyPart
        {
            _Face,
            _Chest,
            _Stomach,
            _Butt,
            _Back,
            _Upper_Arm_Left,
            _Lower_Arm_Left,
            _Upper_Arm_Right,
            _Lower_Arm_Right,
            _Upper_Leg_Front_Left,
            _Upper_Leg_Back_Left,
            _Lower_Leg_Front_Left,
            _Lower_Leg_Back_Left,
            _Upper_Leg_Front_Right,
            _Upper_Leg_Back_Right,
            _Lower_Leg_Front_Right,
            _Lower_Leg_Back_Right

        }

        private static readonly Dictionary<string, Dictionary<BodyPart, bool>> config = new();
        private static readonly Dictionary<string, Dictionary<BodyPart, bool>> gameState = new();
        private static readonly Dictionary<BodyPart, Toggle> buttons = new();
        private static readonly Dictionary<string, Material> materials = new();
        private static string selectedCharacter = "Player";
        private static bool builtUI = false;

        public override void OnInitializeMelon()
        {
            var category = MelonPreferences.CreateCategory("Sticky");
            ManualOverride = category.CreateEntry("manual_override", false, "Manual Override", description: "Set this to True so you can decide what parts are covered in jizz");
            MelonPreferences.Save();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "GameMain")
            {
                return;
            }

            builtUI = false;
            materials.Clear();
            buttons.Clear();
            gameState.Clear();

            PlayerCharacter.OnPlayerLateStart += new Action(() => { GetAllCharactersMeshes(); });
            CharacterManager.OnCharacterEnabled += new Action<CharacterBase>((CharacterBase character) => { GetAllCharactersMeshes(); });
        }

        private static int P(int percentage)
        {
            Math.Clamp(percentage, 0, 100);
            var f = (percentage / 100);
            return Screen.width * f;
        }

        private static void BuildUI()
        {
            // Canvas
            CanvasGO = new()
            {
                name = "Sticky UI"
            };
            canvas = CanvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = CanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _ = CanvasGO.AddComponent<GraphicRaycaster>();
            _ = UIBuilder.CreatePanel("Sticky UI Container", CanvasGO, new(0.2f, 0.3f), new(0, Screen.height * 0.2f), out GameObject contentHolder);

            var layout = UIBuilder.CreateUIObject("Control buttons", contentHolder);
            //set lower min height on layout group
            _ = UIBuilder.SetLayoutGroup<HorizontalLayoutGroup>(layout, true, true, padTop: 2, padLeft: 2, padRight: 2, padBottom: 2);

            _ = UIBuilder.CreateDropdown(layout, "character selection", out dropdown, "Player", 14, (int index) =>
            {
                selectedCharacter = dropdown?.options[index].m_Text ?? "Player";
                MelonLogger.Msg("selected " + selectedCharacter);
                foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
                {
                    buttons[part].SetIsOnWithoutNotify(gameState[selectedCharacter][part]);
                }
            });

            var zoneLabel = UIBuilder.CreateLabel(layout, "zones", "");
            zoneLabel.fontSize = 14;

            UpdateDropdownCharacters();

            foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
            {
                MelonLogger.Msg("creating toggle for " + part);
                UIBuilder.CreateToggle(contentHolder, part.ToString()[1..].Replace('_', ' '), out Toggle toggle, out _);
                buttons.Add(part, toggle);
                toggle.SetIsOnWithoutNotify(gameState[selectedCharacter][part]);
                toggle.onValueChanged.AddListener(new Action<bool>((bool value) =>
                {
                    var localPart = part;
                    gameState[selectedCharacter][localPart] = value;
                    MelonLogger.Msg($"toggled {selectedCharacter}'s {localPart} {(value ? "on" : "off")}");
                    materials[selectedCharacter].SetFloat(localPart, value ? 1 : 0);
                }));
            }
        }

        private static void UpdateDropdownCharacters()
        {
            if (dropdown is not null)
            {
                dropdown.options.Clear();
                foreach (var character in gameState.Keys)
                {
                    dropdown.options.Add(new(character));
                }

                if (dropdown.value >= dropdown.options.Count || dropdown.value < 0)
                {
                    dropdown.value = 0;
                }
                dropdown.RefreshShownValue();
                selectedCharacter = dropdown.options[dropdown.value].text ?? "Player";
            }
        }

        //we then have to either check regularly which are on and keep them on or just override from config
        private static void GetAllCharactersMeshes()
        {
            gameState.Clear();
            materials.Clear();

            foreach (var character in CharacterManager.GetCharacters())
            {
                //MelonLogger.Msg(character.Name + " " + character.name);
                //MelonLogger.Msg("transform name " + character.transform.name);
                var renderer = character.transform.GetChild(0).GetChild(0).GetChild(1).FindChild(character.name.Replace(" ", "") + "_LOD0")?.GetComponent<SkinnedMeshRenderer>();
                renderer ??= character.transform.GetChild(0).GetChild(0).GetChild(1).FindChild(character.name.Replace(" ", "") + "_Body")?.GetComponent<SkinnedMeshRenderer>();

                if (renderer is null)
                {
                    continue;
                }
                else
                {
                    //MelonLogger.Msg(renderer.gameObject.name);
                }

                foreach (var material in renderer.sharedMaterials)
                {
                    if (material is not null && material.name.Contains("Skin", StringComparison.InvariantCultureIgnoreCase) && !material.name.Contains("Lashes", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (materials.TryAdd(character.Name, material))
                        {
                            MelonLogger.Msg("added " + character.Name + "s material");
                        }
                        break;
                        //material.SetFloat(BodyPart._Face, 1.0f);
                        //material.SetFloat(BodyPart._Chest, 1.0f);
                        //material.SetFloat(BodyPart._Stomach, 1.0f);
                        //material.SetFloat(BodyPart._Butt, 1.0f);
                        //material.SetFloat(BodyPart._Back, 1.0f);
                        //material.SetFloat(BodyPart._Upper_Arm_Left, 1.0f);
                        //material.SetFloat(BodyPart._Lower_Arm_Left, 1.0f);
                        //material.SetFloat(BodyPart._Upper_Arm_Right, 1.0f);
                        //material.SetFloat(BodyPart._Lower_Arm_Right, 1.0f);
                        //material.SetFloat(BodyPart._Upper_Leg_Front_Left, 1.0f);
                        //material.SetFloat(BodyPart._Upper_Leg_Back_Left, 1.0f);
                        //material.SetFloat(BodyPart._Lower_Leg_Front_Left, 1.0f);
                        //material.SetFloat(BodyPart._Lower_Leg_Back_Left, 1.0f);
                        //material.SetFloat(BodyPart._Upper_Leg_Front_Right, 1.0f);
                        //material.SetFloat(BodyPart._Upper_Leg_Back_Right, 1.0f);
                        //material.SetFloat(BodyPart._Lower_Leg_Front_Right, 1.0f);
                        //material.SetFloat(BodyPart._Lower_Leg_Back_Right, 1.0f);
                    }
                }
            }

            UpdateGameState();

            if (!builtUI)
            {
                builtUI = true;
                BuildUI();
            }
            else
            {
                UpdateDropdownCharacters();
            }
        }

        private static void UpdateGameState()
        {
            foreach (var mat in materials)
            {
                gameState.TryAdd(mat.Key, new());
                foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
                {
                    float val = mat.Value.GetFloat(part);
                    gameState[mat.Key][part] = val > 0;
                }
            }
            string s = "got states for ";
            foreach (var character in gameState.Keys)
            {
                s += character + ", ";
            }
            MelonLogger.Msg(s);
        }

        private static void SetGameToConfigState()
        {
            //todo
            foreach (var mat in materials)
            {
                foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
                {
                    float val = mat.Value.GetFloat(part);
                    gameState[mat.Key][part] = val > 0;
                }
            }
        }
    }

    public static class Extender
    {
        public static void SetFloat(this Material mat, Sticky.BodyPart part, float f)
        {
            mat.SetFloat(part.ToString(), f);
        }
        public static float GetFloat(this Material mat, Sticky.BodyPart part)
        {
            return mat.GetFloat(part.ToString());
        }
    }
}
