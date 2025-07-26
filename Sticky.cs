using HPUI;
using Il2Cpp;
using Il2CppEekCharacterEngine;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Sticky.Sticky;

namespace Sticky
{
    public sealed class ZoneContainer : Dictionary<string, Dictionary<BodyPart, bool>> { }

    public class Sticky : MelonMod
    {

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

        private static bool builtUI = false;
        private static bool InGameMain = false;
        private static Canvas? canvas;
        private static Color defaultColor = new(0.1f, 0.1f, 0.1f);
        private static ColorBlock buttonColor = new() { normalColor = defaultColor, highlightedColor = defaultColor * 1.2f, pressedColor = defaultColor * 0.8f, colorMultiplier = 1 };
        private static Dropdown? dropdown;
        private static float timeAccumulated = 0;
        private static GameObject? CanvasGO;
        private static int characterUpdateTracker = 0;
        private static MelonPreferences_Entry<bool>? Persistance;
        private static MelonPreferences_Entry<bool>? UIVisible;
        private static PlayerCharacter? player = null;
        private static readonly Dictionary<BodyPart, Toggle> buttons = new();
        private static readonly Dictionary<string, Material> materials = new();
        private static readonly ZoneContainer config = new();
        private static readonly ZoneContainer gameState = new();
        private static string selectedCharacter = "Player";
        public static bool LockCursor => UIVisible?.Value ?? false;

        public override void OnUpdate()
        {
            if (UIVisible is null)
            {
                return;
            }

            if (InGameMain && Keyboard.current.altKey.isPressed && Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                UIVisible.Value = !UIVisible.Value;
                CanvasGO?.SetActive(UIVisible.Value);

                ToggleCursorPlayerLock();
            }
            else if (!InGameMain)
            {
                UIVisible.Value = false;
            }

            if (LockCursor)
            {
                ToggleCursorPlayerLock();
            }

            timeAccumulated += Time.deltaTime;
            if(timeAccumulated > 1)
            {
                timeAccumulated = 0;

                if (!InGameMain)
                {
                    return;
                }

                UpdateGameState();
                if (Persistance!.Value)
                {
                    TryAddGameToConfigState();
                }

                if (characterUpdateTracker < 0 || characterUpdateTracker >= dropdown!.options.Count)
                {
                    characterUpdateTracker = 0;
                }
                string character = dropdown!.options[characterUpdateTracker].text ?? "Player";

                characterUpdateTracker++;

                SetSingleConfigToGameState(character);
            }
        }

        private static void ToggleCursorPlayerLock()
        {
            if (player is null)
            {
                return;
            }

            if (player._controlManager is null)
            {
                return;
            }

            if (player._controlManager.PlayerInput is null)
            {
                return;
            }

            if (UIVisible?.Value ?? false)
            {
                player._controlManager.PlayerInput.DeactivateInput();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                player._controlManager.PlayerInput.ActivateInput();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public override void OnInitializeMelon()
        {
            var category = MelonPreferences.CreateCategory("Sticky");
            Persistance = category.CreateEntry("persistance", false, "Persistance", description: "Set this to True to keep game added cum present");
            UIVisible = category.CreateEntry("uivisible", false, "UI Visible", description: "Toggles the UI on or off");

            PlayerCharacter.OnPlayerLateStart += new Action(() => { GetAllCharactersMeshes(); });
            CharacterManager.OnCharacterEnabled += new Action<CharacterBase>((CharacterBase character) => { GetAllCharactersMeshes(); });
            CharacterManager.OnCharacterDisabled += new Action<CharacterBase>((CharacterBase character) => { GetAllCharactersMeshes(); });

            //load config
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            //MelonPreferences.Save();
            InGameMain = sceneName == "GameMain";
            if (!InGameMain)
            {
                return;
            }

            builtUI = false;
            materials.Clear();
            buttons.Clear();
            gameState.Clear();

            player = UnityEngine.Object.FindObjectOfType<PlayerCharacter>();
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
            canvas.sortingOrder = 100;
            CanvasScaler scaler = CanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;
            _ = CanvasGO.AddComponent<GraphicRaycaster>();

            _ = UIBuilder.CreatePanel("Sticky UI Container", CanvasGO, new(0.18f, 0.7f), new(0, 200), out GameObject contentHolder);
            UIBuilder.SetLayoutGroup<VerticalLayoutGroup>(contentHolder);

            UIBuilder.CreateToggle(contentHolder, "Cum Persistance", out Toggle persistanceToggle, out _);
            persistanceToggle.onValueChanged.AddListener(new Action<bool>((bool state) =>
            {
                Persistance!.Value = state;
            }));
            persistanceToggle.Set(Persistance!.Value);

            var resetButton = UIBuilder.CreateButton(contentHolder, "reset", "Reset", buttonColor);
            UIBuilder.SetLayoutElement(resetButton, minHeight: 16, minWidth: P(3), flexibleHeight: 0, flexibleWidth: 0);
            resetButton.GetComponent<Button>().colors = buttonColor;
            resetButton.GetComponent<Button>().onClick.AddListener(new Action(() => ResetStates()));

            var allButton = UIBuilder.CreateButton(contentHolder, "all", "Cum everywhere", buttonColor);
            UIBuilder.SetLayoutElement(allButton, minHeight: 16, minWidth: P(3), flexibleHeight: 0, flexibleWidth: 0);
            allButton.GetComponent<Button>().colors = buttonColor;
            allButton.GetComponent<Button>().onClick.AddListener(new Action(() => TurnOnAll()));

            _ = UIBuilder.CreateDropdown(contentHolder, "character selection", out dropdown, "Player", 14, (int index) =>
            {
                UpdateButtonsToSelectedCharacter(index);
            });
            UIBuilder.SetLayoutElement(dropdown.gameObject, minHeight: 23, minWidth: P(3), flexibleHeight: 0, flexibleWidth: 0);

            var zoneLabel = UIBuilder.CreateLabel(contentHolder, "Zones:", "Zones:");
            UIBuilder.SetLayoutElement(zoneLabel.gameObject, minHeight: 14, minWidth: P(3), flexibleHeight: 0, flexibleWidth: 0);
            zoneLabel.fontSize = 12;

            UpdateDropdownCharacters();

            foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
            {
                //MelonLogger.Msg("creating toggle for " + part);
                UIBuilder.CreateToggle(contentHolder, part.ToString()[1..].Replace('_', ' '), out Toggle toggle, out _);
                buttons.Add(part, toggle);
                toggle.SetIsOnWithoutNotify(gameState[selectedCharacter][part]);
                toggle.onValueChanged.AddListener(new Action<bool>((bool value) =>
                {
                    var localPart = part;
                    config[selectedCharacter][localPart] = value;
                    materials[selectedCharacter].SetFloat(localPart, value);
                }));
                UIBuilder.SetLayoutElement(toggle.gameObject, minHeight: 8, minWidth: P(3), flexibleHeight: 0, flexibleWidth: 0);
            }
        }

        private static void TurnOnAll()
        {
            if (dropdown is null)
            {
                return;
            }

            foreach (var key in config.Keys)
            {
                foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
                {
                    config[key][part] = true;
                }
            }
            SetConfigToGameState();
            UpdateButtonsToSelectedCharacter(dropdown.m_CurrentIndex);
        }

        private static void UpdateButtonsToSelectedCharacter(int index)
        {
            selectedCharacter = dropdown?.options[index].m_Text ?? "Player";
            //MelonLogger.Msg("selected " + selectedCharacter);
            foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
            {
                buttons[part].SetIsOnWithoutNotify(config[selectedCharacter][part]);
            }
        }

        private static void ResetStates()
        {
            if (dropdown is null)
            {
                return;
            }

            foreach (var key in config.Keys)
            {
                foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
                {
                    config[key][part] = false;
                }
            }
            SetConfigToGameState();
            UpdateButtonsToSelectedCharacter(dropdown.m_CurrentIndex);
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
                if (dropdown.options.Count > 0)
                {
                    selectedCharacter = dropdown.options[dropdown.value].text ?? "Player";
                }
                else
                {
                    selectedCharacter = "Player";
                }
            }
        }

        //we then have to either check regularly which are on and keep them on or just override from config
        private static void GetAllCharactersMeshes()
        {
            gameState.Clear();
            materials.Clear();

            if (!InGameMain)
            {
                return;
            }

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
                            //MelonLogger.Msg("added " + character.Name + "s material");
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
            SetGameToConfigState();

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
            //MelonLogger.Msg(s);
        }

        private static void SetGameToConfigState()
        {

            foreach (var key in gameState.Keys)
            {
                //MelonLogger.Msg("adding copnfig for " + key);
                config.TryAdd(key, new());
                foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
                {
                    config[key][part] = gameState[key][part];
                }
            }
        }

        private static void TryAddGameToConfigState()
        {
            bool changed = false;
            foreach (var mat in materials)
            {
                foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
                {
                    float val = mat.Value.GetFloat(part);
                    if (val > 0)
                    {
                        config[mat.Key][part] = true;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                UpdateButtonsToSelectedCharacter(dropdown!.m_CurrentIndex);
            }
        }

        private static void SetConfigToGameState()
        {
            //MelonLogger.Msg("pushing config to game");
            foreach (var mat in materials)
            {
                foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
                {
                    //MelonLogger.Msg($"{mat.Key} {part} -> {(config[mat.Key][part] ? "on" : "off")}");
                    mat.Value.SetFloat(part, config[mat.Key][part]);
                }
            }
        }

        private static void SetSingleConfigToGameState(string character)
        {
            //MelonLogger.Msg("pushing config to game");
            foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
            {
                //MelonLogger.Msg($"{character} {part} -> {(config[character][part] ? "on" : "off")}");
                materials[character].SetFloat(part, config[character][part]);
            }
        }
    }

    public static class Extender
    {
        public static void SetFloat(this Material mat, Sticky.BodyPart part, bool f)
        {
            mat.SetFloat(part.ToString(), f ? 1 : 0);
        }
        public static float GetFloat(this Material mat, Sticky.BodyPart part)
        {
            return mat.GetFloat(part.ToString());
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Cursor), "set_visible")]
    public static class CursorVisiblePatch
    {
        public static bool Prefix(ref bool value)
        {
            if (Sticky.LockCursor)
            {
                value = true;
            }
            return true;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Cursor), "set_lockState")]
    public static class CursorLockstatePatch
    {
        public static bool Prefix(ref CursorLockMode value)
        {
            if (Sticky.LockCursor)
            {
                value = CursorLockMode.None;
            }
            return true;
        }
    }
}
