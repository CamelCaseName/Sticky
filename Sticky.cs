using Il2Cpp;
using Il2CppEekCharacterEngine;
using Il2CppEekEvents;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

namespace Sticky
{
    public class Sticky : MelonMod
    {
        MelonPreferences_Entry<bool>? ManualOverride;

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

        static readonly Dictionary<string, Dictionary<BodyPart, bool>> config = new();
        static readonly Dictionary<string, Dictionary<BodyPart, bool>> gameState = new();
        static readonly Dictionary<string, Material> materials = new();

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

            PlayerCharacter.OnPlayerLateStart += new Action(() => { GetAllCharactersMeshes(); });
            CharacterManager.OnCharacterEnabled += new Action<CharacterBase>((CharacterBase character) => { GetAllCharactersMeshes(); });
        }

        //todo add global list to track all materials and states of cum zones
        //we then have to either check regularly which are on and keep them on or just override from config
        private static void GetAllCharactersMeshes()
        {
            foreach (var character in CharacterManager.GetCharacters())
            {
                MelonLogger.Msg(character.Name + " " + character.name);
                MelonLogger.Msg("transform name " + character.transform.name);
                var renderer = character.transform.GetChild(0).GetChild(0).GetChild(1).FindChild(character.name.Replace(" ", "") + "_LOD0")?.GetComponent<SkinnedMeshRenderer>();
                renderer ??= character.transform.GetChild(0).GetChild(0).GetChild(1).FindChild(character.name.Replace(" ", "") + "_Body")?.GetComponent<SkinnedMeshRenderer>();

                if (renderer is null)
                {
                    continue;
                }
                else
                {
                    MelonLogger.Msg(renderer.gameObject.name);
                }

                foreach (var material in renderer.sharedMaterials)
                {
                    if (material is not null && material.name.Contains("Skin", StringComparison.InvariantCultureIgnoreCase) && !material.name.Contains("Lashes", StringComparison.InvariantCultureIgnoreCase))
                    {
                        materials.Add(character.Name, material);
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
                        break;
                    }
                }
            }
        }

        private static void UpdateGameState()
        {
            foreach (var mat in materials)
            {
                foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
                {
                    float val = mat.Value.GetFloat(part);
                    gameState[mat.Key][part] = val > 0;
                }
            }
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
