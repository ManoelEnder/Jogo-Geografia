using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace NucleoVivo.EditorTools
{
    /// <summary>
    /// Cria a configuração de balanceamento e a cena principal com todos os sistemas conectados.
    /// Roda sozinho na primeira vez que o projeto compila (se a cena não existir)
    /// e pode ser chamado pelo menu "Núcleo Vivo".
    /// </summary>
    [InitializeOnLoad]
    public static class NucleoVivoSetup
    {
        public const string ScenePath = "Assets/NucleoVivo/Scenes/NucleoVivo.unity";
        public const string ConfigPath = "Assets/NucleoVivo/Config/NucleoVivoConfig.asset";
        const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        static NucleoVivoSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(ScenePath) && File.Exists(ConfigPath)) return;
            if (SessionState.GetBool("NucleoVivo.AutoSetupDone", false)) return;
            SessionState.SetBool("NucleoVivo.AutoSetupDone", true);
            if (SceneManager.GetActiveScene().isDirty)
            {
                // Não descartamos alterações não salvas do usuário.
                EnsureConfig();
                Debug.Log("[Núcleo Vivo] Salve a cena atual e use o menu Núcleo Vivo > Criar ou recriar cena principal.");
                return;
            }
            CreateEverything(true);
            Debug.Log("[Núcleo Vivo] Cena principal criada em " + ScenePath + ". Aperte Play para jogar.");
        }

        [MenuItem("Núcleo Vivo/Criar ou recriar cena principal", priority = 0)]
        static void MenuCreate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateEverything(true);
        }

        [MenuItem("Núcleo Vivo/Abrir cena principal", priority = 1)]
        static void MenuOpen()
        {
            if (!File.Exists(ScenePath)) { MenuCreate(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Núcleo Vivo/Selecionar balanceamento (Inspector)", priority = 20)]
        static void MenuSelectConfig()
        {
            Selection.activeObject = EnsureConfig();
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        [MenuItem("Núcleo Vivo/Configurar projeto para PC (16:9)", priority = 30)]
        static void MenuConfigurePc()
        {
            ConfigurePlayer();
            var cfg = EnsureConfig();
            Debug.Log("[Núcleo Vivo] Projeto configurado para PC; áreas do mapa: " + cfg.areas.Count);
        }

        [MenuItem("Núcleo Vivo/Apagar save local", priority = 40)]
        static void MenuDeleteSave()
        {
            SaveSystem.Delete();
            Debug.Log("[Núcleo Vivo] Save apagado: " + SaveSystem.PathFor());
        }

        public static GameConfig EnsureConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (cfg == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                cfg = ScriptableObject.CreateInstance<GameConfig>();
                cfg.ResetToDefaults();
                AssetDatabase.CreateAsset(cfg, ConfigPath);
            }
            if (cfg.areas == null || cfg.areas.Count == 0) { cfg.FillMissingDefaults(); EditorUtility.SetDirty(cfg); }
            if (cfg.spriteMaterial == null)
            {
                cfg.spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
                if (cfg.spriteMaterial == null) Debug.LogWarning("[Núcleo Vivo] Material Sprite-Unlit-Default não encontrado; os sprites usarão o material padrão.");
                EditorUtility.SetDirty(cfg);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ConfigPath);
            return AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
        }

        public static void CreateEverything(bool openScene)
        {
            var cfg = EnsureConfig();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 10;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.SkyTop;
            cam.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();

            var core = new GameObject("Núcleo Vivo");
            var gm = core.AddComponent<GameManager>();
            var so = new SerializedObject(gm);
            so.FindProperty("config").objectReferenceValue = cfg;
            so.ApplyModifiedPropertiesWithoutUndo();

            var sceneryGo = new GameObject("Cenário");
            var plant = sceneryGo.AddComponent<PlantView>();

            var soundGo = new GameObject("Som");
            soundGo.AddComponent<AudioSource>();
            var sfx = soundGo.AddComponent<SfxPlayer>();

            var uiGo = new GameObject("Interface");
            var ui = uiGo.AddComponent<GameUI>();
            var uiSo = new SerializedObject(ui);
            uiSo.FindProperty("plant").objectReferenceValue = plant;
            uiSo.FindProperty("sfx").objectReferenceValue = sfx;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            var module = esGo.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null) module.AssignDefaultActions();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            ConfigurePlayer();

            if (!openScene && !string.IsNullOrEmpty(previous) && File.Exists(previous))
                EditorSceneManager.OpenScene(previous);
            AssetDatabase.Refresh();
        }

        static void AddToBuildSettings()
        {
            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void ConfigurePlayer()
        {
            // Jogo de PC em tela horizontal (16:9).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
        }
    }
}
