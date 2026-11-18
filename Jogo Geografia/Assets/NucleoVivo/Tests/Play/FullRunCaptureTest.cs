using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NucleoVivo.Tests
{
    /// <summary>
    /// Joga a cena principal de ponta a ponta (toques, compras, eventos, save, Renascimento)
    /// e salva capturas PNG em formato vertical em Logs/NucleoVivoCaptures.
    /// Qualquer erro no Console faz o teste falhar.
    /// </summary>
    public class FullRunCaptureTest
    {
        string outDir, savePath, backupPath;
        RenderTexture rt;
        GameManager gm;
        GameUI ui;
        PlantView plant;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "NucleoVivoCaptures"));
            Directory.CreateDirectory(outDir);
            foreach (var f in Directory.GetFiles(outDir, "*.png")) File.Delete(f);

            // Preserva o save real do jogador durante o teste.
            savePath = SaveSystem.PathFor();
            backupPath = savePath + ".testbackup";
            if (File.Exists(savePath)) File.Copy(savePath, backupPath, true);
            SaveSystem.Delete();

#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                "Assets/NucleoVivo/Scenes/NucleoVivo.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("NucleoVivo");
#endif
            yield return null;
            yield return null;
            gm = GameManager.Instance;
            ui = Object.FindAnyObjectByType<GameUI>();
            plant = Object.FindAnyObjectByType<PlantView>();
            Assert.IsNotNull(gm, "GameManager na cena");
            Assert.IsNotNull(ui, "GameUI na cena");
            Assert.IsNotNull(plant, "PlantView na cena");
            yield return SetResolution(1080, 1920);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (gm != null) gm.enabled = false; // evita salvar por cima ao sair
            SaveSystem.Delete();
            if (File.Exists(backupPath)) { File.Copy(backupPath, savePath, true); File.Delete(backupPath); }
            if (plant != null && plant.Cam != null) plant.PresentCamera.targetTexture = null;
            if (rt != null) rt.Release();
            yield return null;
        }

        IEnumerator SetResolution(int w, int h)
        {
            if (rt != null) { plant.PresentCamera.targetTexture = null; rt.Release(); }
            rt = new RenderTexture(w, h, 24);
            plant.PresentCamera.targetTexture = rt;
            var canvas = ui.GetComponentInChildren<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = plant.PresentCamera;
            canvas.planeDistance = 1f;
            for (int i = 0; i < 4; i++) yield return null;
        }

        IEnumerator Capture(string name)
        {
            yield return null;
            yield return null;
            plant.Cam.Render();
            plant.PresentCamera.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        object Call(string method, params object[] args)
        {
            var m = typeof(GameUI).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(m, method);
            return m.Invoke(ui, args);
        }

        IEnumerator Wait(float seconds)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end) yield return null;
        }

        void Tap(int times)
        {
            Vector2 center = new Vector2(plant.PresentCamera.pixelWidth * 0.5f, plant.PresentCamera.pixelHeight * 0.55f);
            for (int i = 0; i < times; i++) Call("DoTap", center);
        }

        int Idx(string id) => gm.Config.UpgradeIndex(id);

        void BuyUntil(string id, int level)
        {
            int i = Idx(id);
            while (gm.Economy.Level(i) < level)
            {
                if (!gm.Economy.CanBuy(i)) gm.DevAddEnergy(gm.Economy.Cost(i));
                Assert.IsTrue(gm.BuyUpgrade(i), "comprar " + id);
            }
        }

        [UnityTest]
        public IEnumerator PlaysFromFirstTapToSecondCampaign()
        {
            var s = gm.State;
            // 1. Cena abre e o toque gera Energia
            Assert.AreEqual(0, s.energy);
            yield return Capture("01_inicio");
            Tap(6);
            yield return Wait(0.3f);
            Assert.AreEqual(6, s.energy, 1e-6);
            yield return Capture("02_toques");

            // 2. Primeira melhoria e produção passiva
            Tap(4);
            Assert.IsTrue(gm.BuyUpgrade(Idx("painel")));
            Assert.IsTrue(s.tutorialDone);
            BuyUntil("turbina", 1);
            Assert.Greater(gm.Economy.PassivePerSecond, 0);
            double before = s.energy;
            yield return Wait(1.1f);
            Assert.Greater(s.energy, before, "produção passiva");
            yield return Capture("03_turbina");

            // 3. Ampliações aparecem no cenário (etapa expandida)
            gm.DevAddEnergy(6000);
            BuyUntil("subestacao", 1);
            BuyUntil("automacao", 1);
            BuyUntil("torre", 1);
            BuyUntil("turbina", 10);
            yield return Wait(2f);
            yield return Capture("04_expandida");

            // 4. Eventos: um negativo e um positivo, com efeitos e tempos
            Assert.IsTrue(s.eventsEnabled);
            gm.DevTriggerEvent("falha_turbina");
            Call("ShowTab", 1);
            yield return Wait(0.3f);
            yield return Capture("05_evento_pendente");
            double passiveBefore = gm.Economy.PassivePerSecond;
            Tap(3); // toques continuam valendo com decisão pendente
            Assert.IsTrue(gm.ChooseEvent(1));
            Assert.Less(gm.Economy.PassivePerSecond, passiveBefore);
            Assert.AreEqual(120, s.effects[0].remaining, 0.5f);

            gm.DevTriggerEvent("convenio");
            Assert.IsTrue(gm.ChooseEvent(0));
            Assert.AreEqual(2, s.effects.Count);
            yield return Wait(0.3f);
            yield return Capture("06_efeitos_ativos");

            // 5. Salvar e recarregar restaura saldo, melhorias e eventos
            gm.DevTriggerEvent("audiencia");
            gm.SaveNow();
            double energy = s.energy;
            int turb = gm.Economy.Level(Idx("turbina"));
            int fx = s.effects.Count;
            float fxRemaining = s.effects[0].remaining;
            gm.ReloadFromDisk();
            s = gm.State;
            Assert.AreEqual(energy, s.energy, 1e-6);
            Assert.AreEqual(turb, gm.Economy.Level(Idx("turbina")));
            Assert.AreEqual(fx, s.effects.Count);
            Assert.AreEqual(fxRemaining, s.effects[0].remaining, 0.01f);
            Assert.AreEqual("audiencia", s.pendingEventId);
            Assert.IsTrue(gm.ChooseEvent(1));

            // Etapa regional (mais bairros acesos)
            gm.DevAddEnergy(120000);
            BuyUntil("reator2", 1);
            BuyUntil("rede", 1);
            BuyUntil("torre", 10);
            Call("ShowTab", 0);
            yield return Wait(2f);
            yield return Capture("07_regional");

            // 6. Renascimento
            Assert.IsTrue(gm.Rebirth.CanRebirth);
            Call("ShowTab", 2);
            yield return Wait(0.3f);
            yield return Capture("08_renascimento_pronto");
            int expected = gm.Rebirth.PendingNucleos;
            Call("OpenConfirm");
            yield return Capture("09_confirmar");
            Call("ConfirmRebirth");
            yield return Wait(1.3f);
            yield return Capture("10_transicao");
            yield return Wait(3f);
            s = gm.State;
            Assert.AreEqual(expected, s.nucleos);
            Assert.AreEqual(0, gm.Economy.TotalUpgradeLevels());
            Assert.AreEqual(2, s.generation);
            yield return Capture("11_pos_renascimento");

            var click = gm.Config.permanentUpgrades.Find(p => p.kind == PermanentKind.ClickBonus);
            Assert.IsTrue(gm.BuyPermanent(click));
            Assert.AreEqual(2, gm.Economy.ClickValue, 1e-6);
            Call("ShowTab", 0);
            Tap(5);
            Assert.AreEqual(10, s.energy, 1e-6);
            yield return Wait(0.2f);
            yield return Capture("12_segunda_campanha");

            // 7. Layout em tela mais alongada (ex.: 9:19.5)
            yield return SetResolution(1080, 2340);
            yield return Capture("13_tela_alta");
        }
    }
}
