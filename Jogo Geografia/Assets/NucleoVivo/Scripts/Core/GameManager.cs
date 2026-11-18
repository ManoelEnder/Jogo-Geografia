using System;
using UnityEngine;

namespace NucleoVivo
{
    /// <summary>
    /// Ponto central do jogo: guarda o estado, avança a simulação e salva.
    /// Visual e interface só escutam os eventos daqui.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("Balanceamento de melhorias, eventos e Renascimento.")]
        [SerializeField] GameConfig config;
        [Tooltip("Nome do arquivo de save (útil para testes).")]
        [SerializeField] string saveFileName = SaveSystem.FileName;

        public GameConfig Config => config;
        public GameState State { get; private set; }
        public Economy Economy { get; private set; }
        public EventDirector Events { get; private set; }
        public RebirthSystem Rebirth { get; private set; }

        public event Action<double> Tapped;                 // valor ganho
        public event Action<int> UpgradeBought;             // índice
        public event Action<int, int> StructureTierChanged; // índice, nova etapa
        public event Action<int> Reborn;                    // núcleos ganhos
        public event Action StateReloaded;
        public event Action PermanentBought;
        public event Action<AreaDef> AreaOpened;

        float autosaveTimer;
        bool dirty;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 60;
#if UNITY_EDITOR
            if (config == null)
                config = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/NucleoVivo/Config/NucleoVivoConfig.asset");
#endif
            if (config == null)
            {
                Debug.LogWarning("[Núcleo Vivo] GameConfig não atribuído; usando valores padrão.");
                config = GameConfig.CreateDefault();
            }
            config.FillMissingDefaults();
            State = SaveSystem.Load(config, saveFileName);
            Economy = new Economy(config, () => State);
            Events = new EventDirector(config, Economy, () => State);
            Rebirth = new RebirthSystem(config, () => State);
            Events.EventResolved += (e, c, auto) => SaveNow();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            // Sem ganhos offline: limitamos o passo para que pausas longas não gerem saltos.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.5f);
            double passive = Economy.PassivePerSecond * dt;
            Economy.AddEnergy(passive);
            Events.Tick(dt);
            State.playSeconds += dt;
            State.campaignSeconds += dt;

            autosaveTimer += dt;
            if (autosaveTimer >= config.autosaveInterval || (dirty && autosaveTimer >= 1f))
            {
                SaveNow();
            }
        }

        public double Tap()
        {
            double v = Economy.ClickValue;
            Economy.AddEnergy(v);
            State.totalTaps++;
            Tapped?.Invoke(v);
            return v;
        }

        public bool BuyUpgrade(int index)
        {
            int before = Economy.VisualTier(index);
            if (!Economy.Buy(index)) return false;
            State.tutorialDone = true;
            UpgradeBought?.Invoke(index);
            int after = Economy.VisualTier(index);
            if (after != before) StructureTierChanged?.Invoke(index, after);
            SaveNow();
            return true;
        }

        public bool ChooseEvent(int choiceIndex)
        {
            return Events.Choose(choiceIndex); // salva via EventResolved
        }

        public int DoRebirth()
        {
            int gained = Rebirth.Perform();
            if (gained > 0)
            {
                SaveNow();
                Reborn?.Invoke(gained);
            }
            return gained;
        }

        public bool OpenArea(AreaDef a)
        {
            if (!Economy.OpenArea(a)) return false;
            AreaOpened?.Invoke(a);
            SaveNow();
            return true;
        }

        public bool BuyPermanent(PermanentUpgradeDef p)
        {
            if (!Rebirth.BuyPermanent(p)) return false;
            PermanentBought?.Invoke();
            SaveNow();
            return true;
        }

        public void SetSound(bool on)
        {
            State.soundOn = on;
            MarkDirty();
        }

        public void MarkDirty() => dirty = true;

        public void SaveNow()
        {
            SaveSystem.Save(State, saveFileName);
            autosaveTimer = 0;
            dirty = false;
        }

        /// <summary>Recarrega o save do disco (usado para testar a restauração).</summary>
        public void ReloadFromDisk()
        {
            State = SaveSystem.Load(config, saveFileName);
            StateReloaded?.Invoke();
        }

        public void ResetAllProgress()
        {
            bool sound = State.soundOn;
            SaveSystem.Delete(saveFileName);
            State = new GameState { reputation = config.startReputation, soundOn = sound };
            SaveNow();
            StateReloaded?.Invoke();
        }

        void OnApplicationPause(bool paused) { if (paused && State != null) SaveNow(); }
        void OnApplicationFocus(bool focus) { if (!focus && State != null) SaveNow(); }
        void OnApplicationQuit() { if (State != null) SaveNow(); }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ---------- atalhos de teste (somente Editor / build de desenvolvimento) ----------
        public void DevAddEnergy(double amount) => Economy.AddEnergy(amount);
        public void DevTriggerEvent()
        {
            if (!State.eventsEnabled) { State.eventsEnabled = true; }
            if (!State.HasPendingEvent) Events.TriggerRandomEvent();
        }
        public void DevTriggerEvent(string id)
        {
            var e = config.FindEvent(id);
            if (e == null) return;
            State.eventsEnabled = true;
            Events.StartEvent(e);
        }
#endif
    }
}
