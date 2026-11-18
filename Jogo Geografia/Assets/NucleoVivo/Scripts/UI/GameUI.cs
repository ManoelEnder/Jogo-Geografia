using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NucleoVivo
{
    /// <summary>
    /// Interface completa montada por código: topo com saldo, manchetes, abas
    /// (Melhorias, Eventos, Renascimento), modais e textos flutuantes.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GameUI : MonoBehaviour
    {
        [SerializeField] PlantView plant;
        [SerializeField] SfxPlayer sfx;
        [Tooltip("Fração da altura da tela ocupada pelo painel inferior.")]
        [Range(0.3f, 0.55f)] public float bottomPanelFraction = 0.41f;

        const float TopBarHeight = 270f;
        const int TabUpgrades = 0, TabEvents = 1, TabRebirth = 2;

        GameManager gm;
        Canvas canvas;
        CanvasScaler scaler;
        RectTransform safe, tapLayer, floatLayer, topBar, notices, bottomPanel, modalLayer;

        // topo
        PixelText balanceText, clickText, passiveText, repText;
        Image repFill;
        RectTransform headline;
        Image headlineBg, headlineIcon;
        PixelText headlineText;
        PixelText effectsText;
        GameObject effectsPill;

        // toque
        RectTransform tapRt;
        RectTransform tutorial;
        PixelText tutorialText;

        // abas
        readonly RectTransform[] pages = new RectTransform[3];
        readonly UIFactory.ButtonView[] tabButtons = new UIFactory.ButtonView[3];
        readonly Image[] tabBadges = new Image[3];
        int currentTab;

        // melhorias
        class UpgradeRow
        {
            public int index;
            public GameObject root;
            public Image bg, icon;
            public PixelText title, gain, status;
            public UIFactory.ButtonView buy;
        }
        readonly List<UpgradeRow> upgradeRows = new List<UpgradeRow>();

        // eventos
        PixelText repCardText, repCardHint;
        Image repCardFill;
        RectTransform eventSlot;
        string builtEventKey = null;
        PixelText eventStatusText, eventExpireText, effectsListText;
        readonly List<KeyValuePair<UIFactory.ButtonView, PixelText>> choiceViews = new List<KeyValuePair<UIFactory.ButtonView, PixelText>>();

        // renascimento
        PixelText rbProgressText, rbGainText, rbNextText, rbOwnedText, statsText;
        Image rbFill;
        UIFactory.ButtonView rbButton;
        class PermRow { public PermanentUpgradeDef def; public PixelText title, desc; public UIFactory.ButtonView buy; }
        readonly List<PermRow> permRows = new List<PermRow>();

        // modais e avisos
        GameObject settingsModal, confirmModal;
        PixelText confirmBody, soundLabel, resetLabel;
        UIFactory.ButtonView soundButton;
        bool resetArmed;
        PixelText captionText, toastText;
        Image toastBg;
        float toastTimer;
        float refreshTimer;
        bool busy; // durante a transição

        class Floater { public RectTransform rt; public PixelText text; public float age; public double value; public bool active; public float born; public Vector2 start; }
        readonly List<Floater> floaters = new List<Floater>();

        // ---------- inicialização ----------
        void Start()
        {
            gm = GameManager.Instance;
            if (plant == null) plant = FindAnyObjectByType<PlantView>();
            if (sfx == null) sfx = FindAnyObjectByType<SfxPlayer>();
            EnsureEventSystem();
            Build();

            gm.Events.EventStarted += e => { Play(Sfx.Event); builtEventKey = null; };
            gm.Events.EventResolved += (e, c, auto) =>
            {
                builtEventKey = null;
                Toast(auto ? "Sem resposta a tempo: \"" + c.label + "\" foi aplicada." : "Decisão registrada: " + c.label);
            };
            gm.StructureTierChanged += (i, tier) =>
            {
                Play(Sfx.Install);
                var u = gm.Config.upgrades[i];
                Toast(tier == 1 ? u.displayName + " entrou em operação!" : u.displayName + " evoluiu para a etapa " + tier + "!");
            };
            gm.StateReloaded += () => { builtEventKey = null; RefreshAll(); };
            RefreshAll();
        }

        static void EnsureEventSystem()
        {
            var existing = FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                var m = existing.GetComponent<InputSystemUIInputModule>();
                if (m != null && m.actionsAsset == null) m.AssignDefaultActions();
                return;
            }
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        void Play(Sfx s) { if (sfx != null) sfx.Play(s); }

        /// <summary>
        /// Mantém 1 pixel da interface (3 unidades) igual a um número inteiro de pixels da tela sempre que possível.
        /// </summary>
        void UpdateScale()
        {
            float w = PixelWidth, h = Mathf.Max(1, PixelHeight);
            float k; // pixels de tela por pixel da interface
            if (w / h > 0.75f) k = h / 640f;           // janela larga: guia pela altura
            else
            {
                k = Mathf.Min(w / 360f, h / 640f);
                if (k >= 1f) k = Mathf.Floor(k);
                if (w * PixelText.UiPixel / k > 1500f) k = w / 360f; // evita interface minúscula
            }
            scaler.scaleFactor = Mathf.Max(0.1f, k / PixelText.UiPixel);
        }

        // Funciona com o Canvas em Overlay (padrão) ou em modo câmera (usado nas capturas automáticas).
        Camera UiCam => canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        int PixelWidth => UiCam != null ? UiCam.pixelWidth : Screen.width;
        int PixelHeight => UiCam != null ? UiCam.pixelHeight : Screen.height;

        // ---------- construção ----------
        void Build()
        {
            var canvasGo = new GameObject("Interface", typeof(RectTransform));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            UpdateScale();
            canvasGo.AddComponent<GraphicRaycaster>();

            safe = UIFactory.Stretch(UIFactory.Rect("Área segura", canvasGo.transform));
            safe.gameObject.AddComponent<SafeArea>();

            tapLayer = UIFactory.Stretch(UIFactory.Rect("Camada de toque", safe));
            BuildTapArea();
            BuildTopBar();
            BuildNotices();
            BuildBottomPanel();
            floatLayer = UIFactory.Stretch(UIFactory.Rect("Textos flutuantes", safe));
            BuildFloaters();
            BuildCaptionAndToast();
            modalLayer = UIFactory.Stretch(UIFactory.Rect("Modais", safe));
            BuildSettingsModal();
            BuildConfirmModal();
            ShowTab(TabUpgrades);
        }

        void BuildTapArea()
        {
            tapRt = UIFactory.Rect("Reator (toque)", tapLayer);
            tapRt.anchorMin = tapRt.anchorMax = new Vector2(0.5f, 0.5f);
            var img = tapRt.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = true;
            var area = tapRt.gameObject.AddComponent<TapArea>();
            area.onDown = e => DoTap(e.position);

            tutorial = UIFactory.Rect("Dica inicial", tapLayer);
            tutorial.anchorMin = tutorial.anchorMax = new Vector2(0.5f, 0.5f);
            tutorial.sizeDelta = new Vector2(620, 110);
            UIFactory.Img(tutorial, Palette.UiCard.WithAlpha(0.92f));
            var border = UIFactory.Rect("borda", tutorial);
            UIFactory.Stretch(border);
            UIFactory.Img(border, Palette.UiText.WithAlpha(0.8f), Art.RoundRectOutline);
            tutorialText = UIFactory.Label(tutorial, "Toque no reator para gerar Energia", 36, Palette.UiText, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch((RectTransform)tutorialText.transform, 16, 8, 16, 8);
        }

        void BuildTopBar()
        {
            topBar = UIFactory.Rect("Topo", safe);
            UIFactory.Anchor(topBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(-24, TopBarHeight));
            UIFactory.Img(topBar, Palette.UiBackground.WithAlpha(0.9f));

            var icon = UIFactory.Icon(topBar, IconKind.Energy, 120, true);
            UIFactory.Anchor((RectTransform)icon.transform.parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -22), new Vector2(120, 120));

            balanceText = UIFactory.Label(topBar, "0", 84, Palette.UiText, TextAnchor.MiddleLeft, true);
            UIFactory.Anchor((RectTransform)balanceText.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(162, -12), new Vector2(560, 100));
            var sub = UIFactory.Label(topBar, "de Energia", 30, Palette.UiMuted);
            UIFactory.Anchor((RectTransform)sub.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(166, -108), new Vector2(400, 40));

            clickText = Chip(topBar, new Vector2(24, -170), 430, IconKind.Tap);
            passiveText = Chip(topBar, new Vector2(470, -170), 360, IconKind.Passive);

            var gear = UIFactory.Button(topBar, "", Palette.UiCardLight, OpenSettings);
            UIFactory.Anchor(gear.rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(104, 104));
            DestroyImmediate(gear.rt.GetComponent<VerticalLayoutGroup>());
            gear.label.gameObject.SetActive(false);
            var gi = UIFactory.Icon(gear.rt, IconKind.Settings, 80, false);
            UIFactory.Anchor((RectTransform)gi.transform.parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 80));

            repText = UIFactory.Label(topBar, "Reputação 50", 28, Palette.UiMuted, TextAnchor.MiddleRight);
            UIFactory.Anchor((RectTransform)repText.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-136, -26), new Vector2(240, 40));
            var barBg = UIFactory.Rect("barra reputação", topBar);
            UIFactory.Anchor(barBg, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-136, -76), new Vector2(200, 18));
            UIFactory.Img(barBg, Palette.UiCardLight);
            var fill = UIFactory.Rect("preenchimento", barBg);
            UIFactory.Stretch(fill);
            repFill = UIFactory.Img(fill, Palette.UiPositive, Art.Pixel);
            repFill.type = Image.Type.Filled;
            repFill.fillMethod = Image.FillMethod.Horizontal;
        }

        PixelText Chip(RectTransform parent, Vector2 pos, float width, IconKind kind)
        {
            var chip = UIFactory.Rect("chip", parent);
            UIFactory.Anchor(chip, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(width, 76));
            UIFactory.Img(chip, Palette.UiCardLight);
            var ic = UIFactory.Icon(chip, kind, 60, false);
            UIFactory.Anchor((RectTransform)ic.transform.parent, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(60, 60));
            var t = UIFactory.Label(chip, "", 32, Palette.UiText, TextAnchor.MiddleLeft, true);
            UIFactory.Stretch((RectTransform)t.transform, 78, 0, 10, 0);
            return t;
        }

        void BuildNotices()
        {
            notices = UIFactory.Rect("Avisos", safe);
            UIFactory.Anchor(notices, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -(TopBarHeight + 22)), new Vector2(-24, 0));
            var v = UIFactory.VLayout(notices.gameObject, 0, 10, TextAnchor.UpperCenter);
            var fit = notices.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Manchete do evento pendente
            headline = UIFactory.Rect("Manchete", notices);
            headlineBg = UIFactory.Img(headline, Palette.UiCard.WithAlpha(0.95f), Art.RoundRect, true);
            var btn = headline.gameObject.AddComponent<Button>();
            btn.targetGraphic = headlineBg;
            btn.onClick.AddListener(() => { Play(Sfx.Ui); ShowTab(TabEvents); });
            var h = UIFactory.HLayout(headline.gameObject, 12, 16);
            h.padding = new RectOffset(14, 20, 10, 10);
            headlineIcon = UIFactory.Icon(headline, IconKind.Press, 84, true);
            headlineText = UIFactory.Label(headline, "", 30, Palette.UiText);
            UIFactory.LE(headlineText.gameObject, -1, -1, 84, -1, 1);
            UIFactory.LE(headline.gameObject, -1, -1, 104, -1);

            effectsPill = UIFactory.Rect("Efeitos ativos", notices).gameObject;
            UIFactory.Img((RectTransform)effectsPill.transform, Palette.UiBackground.WithAlpha(0.8f));
            UIFactory.VLayout(effectsPill, 12, 0);
            effectsText = UIFactory.Label(effectsPill.transform, "", 28, Palette.UiText);
        }

        void BuildBottomPanel()
        {
            bottomPanel = UIFactory.Rect("Painel inferior", safe);
            bottomPanel.anchorMin = new Vector2(0, 0);
            bottomPanel.anchorMax = new Vector2(1, bottomPanelFraction);
            bottomPanel.offsetMin = new Vector2(0, 0);
            bottomPanel.offsetMax = new Vector2(0, 0);
            UIFactory.Img(bottomPanel, Palette.UiBackground, Art.RoundRect, true);

            var pagesRoot = UIFactory.Rect("Páginas", bottomPanel);
            UIFactory.Stretch(pagesRoot, 0, 150, 0, 8);

            var tabs = UIFactory.Rect("Abas", bottomPanel);
            UIFactory.Anchor(tabs, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(-20, 136));
            var h = UIFactory.HLayout(tabs.gameObject, 0, 12, TextAnchor.MiddleCenter);
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;

            string[] names = { "Melhorias", "Eventos", "Renascimento" };
            IconKind[] icons = { IconKind.ControlPanel, IconKind.Press, IconKind.Nucleo };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var bv = UIFactory.Button(tabs, names[i], Palette.UiCard, () => { Play(Sfx.Ui); ShowTab(idx); }, 32);
                UIFactory.LE(bv.rt.gameObject, -1, -1, 120, 136, 1, 1);
                var ic = UIFactory.Icon(bv.rt, icons[i], 56, false);
                ic.transform.parent.SetAsFirstSibling();
                var badge = UIFactory.Rect("alerta", bv.rt);
                var ble = badge.gameObject.AddComponent<LayoutElement>(); ble.ignoreLayout = true;
                UIFactory.Anchor(badge, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -12), new Vector2(26, 26));
                tabBadges[i] = UIFactory.Img(badge, Palette.UiNegative, Art.Circle);
                tabBadges[i].type = Image.Type.Simple;
                tabButtons[i] = bv;

                pages[i] = UIFactory.Rect(names[i], pagesRoot);
                UIFactory.Stretch(pages[i]);
            }
            BuildUpgradesPage(pages[TabUpgrades]);
            BuildEventsPage(pages[TabEvents]);
            BuildRebirthPage(pages[TabRebirth]);
        }

        // ---------- aba Melhorias ----------
        void BuildUpgradesPage(RectTransform page)
        {
            var content = UIFactory.Scroll(page, out _);
            var cfg = gm.Config;
            for (int i = 0; i < cfg.upgrades.Count; i++)
            {
                int idx = i;
                var u = cfg.upgrades[i];
                var row = new UpgradeRow { index = i };
                var bg = UIFactory.Panel("melhoria " + u.displayName, content, Palette.UiCard);
                row.root = bg.gameObject; row.bg = bg;
                var h = UIFactory.HLayout(bg.gameObject, 18, 18);
                h.childAlignment = TextAnchor.MiddleLeft;
                row.icon = UIFactory.Icon(bg.transform, u.icon, 128, true);

                var info = UIFactory.Rect("info", bg.transform);
                UIFactory.VLayout(info.gameObject, 0, 4);
                UIFactory.LE(info.gameObject, 200, 200, -1, -1, 1);
                row.title = UIFactory.Label(info, u.displayName, 38, Palette.UiText, TextAnchor.MiddleLeft, true);
                row.gain = UIFactory.Label(info, "", 30, Palette.UiText);
                row.status = UIFactory.Label(info, "", 28, Palette.UiMuted);

                row.buy = UIFactory.Button(bg.transform, "10", Palette.UiButton, () => Buy(idx), 40, "Comprar");
                UIFactory.LE(row.buy.rt.gameObject, 250, 250, 130, 130, 0);
                upgradeRows.Add(row);
            }
            var foot = UIFactory.Label(content, "Compras importantes instalam novas estruturas no cenário. Marcos de nível (1, 10 e 25) mudam o visual de cada estrutura.", 26, Palette.UiMuted, TextAnchor.MiddleCenter);
        }

        void Buy(int index)
        {
            if (busy) return;
            if (gm.BuyUpgrade(index)) Play(Sfx.Buy);
            else Play(Sfx.Error);
            RefreshUpgrades();
        }

        void RefreshUpgrades()
        {
            var cfg = gm.Config;
            var eco = gm.Economy;
            bool shownLocked = false;
            foreach (var row in upgradeRows)
            {
                var u = cfg.upgrades[row.index];
                int lv = eco.Level(row.index);
                bool unlocked = eco.IsUnlocked(row.index);
                bool visible = unlocked || !shownLocked;
                if (!unlocked) shownLocked = true;
                if (row.root.activeSelf != visible) row.root.SetActive(visible);
                if (!visible) continue;

                row.icon.sprite = Art.Icon(unlocked ? u.icon : IconKind.Lock);
                row.title.text = u.displayName + (lv > 0 ? "  <color=#9AA29E>Nv " + lv + "</color>" : "");
                if (!unlocked)
                {
                    double missing = u.unlockAtProduced - gm.State.campaignProduced;
                    row.gain.text = Gain(row.index);
                    row.status.text = "<color=#E3826F>Libera com " + NumberFormat.Short(u.unlockAtProduced) + " de Energia produzida na campanha (faltam " + NumberFormat.Short(missing) + ")</color>";
                    row.buy.rt.gameObject.SetActive(false);
                    row.bg.color = Palette.UiCard.WithAlpha(0.6f);
                    continue;
                }
                row.bg.color = Palette.UiCard;
                row.buy.rt.gameObject.SetActive(true);
                double cost = eco.Cost(row.index);
                bool can = eco.CanBuy(row.index);
                row.gain.text = Gain(row.index);
                string milestone = NextMilestoneText(u, lv);
                row.status.text = can ? milestone : "<color=#E3826F>Faltam " + NumberFormat.Short(cost - gm.State.energy) + " de Energia</color>" + (milestone.Length > 0 ? "  ·  " + milestone : "");
                row.buy.label.text = NumberFormat.Short(cost);
                row.buy.sub.text = lv == 0 ? "Instalar" : "Comprar";
                UIFactory.SetInteractable(row.buy, can, Palette.UiButton);
            }
        }

        string Gain(int i)
        {
            var u = gm.Config.upgrades[i];
            var eco = gm.Economy;
            var parts = new List<string>();
            if (u.clickGain > 0)
                parts.Add("+" + NumberFormat.Short(eco.ClickGainPreview(i)) + " por toque (total " + NumberFormat.Short(eco.ClickValue + eco.ClickGainPreview(i)) + ")");
            if (u.passiveGain > 0)
                parts.Add("+" + NumberFormat.Short(eco.PassiveGainPreview(i)) + "/s (total " + NumberFormat.Short(eco.PassivePerSecond + eco.PassiveGainPreview(i)) + "/s)");
            return string.Join("  ", parts);
        }

        static string NextMilestoneText(UpgradeDef u, int lv)
        {
            foreach (int m in u.visualMilestones)
                if (lv < m) return lv == 0 ? "Instala a estrutura no cenário" : "Nova etapa visual no Nv " + m;
            return "";
        }

        // ---------- aba Eventos ----------
        void BuildEventsPage(RectTransform page)
        {
            var content = UIFactory.Scroll(page, out _);

            var rep = UIFactory.Panel("reputação", content, Palette.UiCard);
            var h = UIFactory.HLayout(rep.gameObject, 18, 18);
            UIFactory.Icon(rep.transform, IconKind.Reputation, 110, true);
            var info = UIFactory.Rect("info", rep.transform);
            UIFactory.VLayout(info.gameObject, 0, 8);
            UIFactory.LE(info.gameObject, 200, 200, -1, -1, 1);
            repCardText = UIFactory.Label(info, "Reputação", 36, Palette.UiText, TextAnchor.MiddleLeft, true);
            var barBg = UIFactory.Rect("barra", info);
            UIFactory.LE(barBg.gameObject, -1, -1, 20, 20);
            UIFactory.Img(barBg, Palette.UiCardLight);
            var fill = UIFactory.Stretch(UIFactory.Rect("preenchimento", barBg));
            repCardFill = UIFactory.Img(fill, Palette.UiPositive, Art.Pixel);
            repCardFill.type = Image.Type.Filled; repCardFill.fillMethod = Image.FillMethod.Horizontal;
            repCardHint = UIFactory.Label(info, "", 26, Palette.UiMuted);

            eventSlot = UIFactory.Rect("evento", content);
            UIFactory.VLayout(eventSlot.gameObject, 0, 0);

            var eff = UIFactory.Panel("efeitos", content, Palette.UiCard);
            UIFactory.VLayout(eff.gameObject, 22, 8);
            UIFactory.Label(eff.transform, "Efeitos ativos", 34, Palette.UiText, TextAnchor.MiddleLeft, true);
            effectsListText = UIFactory.Label(eff.transform, "", 28, Palette.UiMuted);
        }

        void RebuildEventCard()
        {
            for (int i = eventSlot.childCount - 1; i >= 0; i--) Destroy(eventSlot.GetChild(i).gameObject);
            choiceViews.Clear();
            eventStatusText = null; eventExpireText = null;

            var e = gm.Events.Pending;
            var card = UIFactory.Panel("cartão", eventSlot, e == null ? Palette.UiCard : Palette.UiCardLight);
            UIFactory.VLayout(card.gameObject, 24, 14);

            if (e == null)
            {
                eventStatusText = UIFactory.Label(card.transform, "", 30, Palette.UiMuted);
                return;
            }

            var head = UIFactory.Rect("cabeçalho", card.transform);
            UIFactory.HLayout(head.gameObject, 0, 18);
            UIFactory.Icon(head, e.icon, 120, true);
            var titles = UIFactory.Rect("títulos", head);
            UIFactory.VLayout(titles.gameObject, 0, 4);
            UIFactory.LE(titles.gameObject, 200, 200, -1, -1, 1);
            string tag = e.tone == EventTone.Positive ? "<color=#86C995>OPORTUNIDADE</color>" : e.tone == EventTone.Negative ? "<color=#E3826F>PROBLEMA</color>" : "<color=#E3826F>DECISÃO</color>";
            UIFactory.Label(titles, tag, 26, Palette.UiText, TextAnchor.MiddleLeft, true);
            UIFactory.Label(titles, e.title, 38, Palette.UiText, TextAnchor.MiddleLeft, true);

            UIFactory.Label(card.transform, e.body, 30, Palette.UiText);
            eventExpireText = UIFactory.Label(card.transform, "", 26, Palette.UiMuted);

            for (int i = 0; i < e.choices.Count; i++)
            {
                int idx = i;
                var c = e.choices[i];
                var bv = UIFactory.Button(card.transform, c.label, Palette.UiButton, () => Choose(idx), 34, c.description);
                bv.sub.fontSize = 26;
                var effect = UIFactory.Label(bv.rt, "", 26, Palette.UiText, TextAnchor.MiddleCenter, true);
                choiceViews.Add(new KeyValuePair<UIFactory.ButtonView, PixelText>(bv, effect));
            }
        }

        void Choose(int idx)
        {
            if (busy) return;
            if (gm.ChooseEvent(idx)) Play(Sfx.Buy);
            else Play(Sfx.Error);
            RefreshEvents();
        }

        void RefreshEvents()
        {
            var ev = gm.Events;
            var s = gm.State;
            float rep = ev.EffectiveReputation;
            repCardText.text = "Reputação  " + Mathf.RoundToInt(rep) + "/100";
            repCardFill.fillAmount = rep / 100f;
            repCardFill.color = RepColor(rep);
            string hint = rep >= 65 ? "A imprensa local está de olho em boas notícias. Oportunidades positivas aparecem com mais frequência."
                : rep >= 35 ? "A cidade observa a usina com cautela. Reputação 65+ libera reportagens positivas."
                : "Moradores desconfiados: protestos e problemas ficam mais prováveis. Auditorias e visitas ajudam a recuperar a confiança.";
            float temp = rep - s.reputation;
            if (Mathf.Abs(temp) > 0.5f) hint += "\n<color=#E3826F>" + (temp > 0 ? "+" : "") + Mathf.RoundToInt(temp) + " temporário por eventos recentes.</color>";
            repCardHint.text = hint;

            string key = s.HasPendingEvent ? s.pendingEventId : (s.eventsEnabled ? "calmo" : "bloqueado");
            if (key != builtEventKey) { builtEventKey = key; RebuildEventCard(); }

            if (eventStatusText != null)
            {
                eventStatusText.text = !s.eventsEnabled
                    ? "<b>Eventos ainda não começaram.</b>\nEles aparecem depois que você instalar a primeira estrutura de produção passiva (Turbina auxiliar)."
                    : "<b>Tudo calmo em Nova Aurora.</b>\nA próxima notícia deve chegar em cerca de " + NumberFormat.Duration(Mathf.Max(1, s.eventTimer)) + ". Continue produzindo!";
            }

            var e = ev.Pending;
            if (e != null && eventExpireText != null)
            {
                var def = e.choices.Find(c => c.isDefaultIfIgnored);
                float left = gm.Config.pendingEventExpire - s.pendingEventAge;
                eventExpireText.text = "A usina continua operando enquanto você decide. Sem resposta em " + NumberFormat.Duration(left) +
                                       (def != null ? ", vale: \"" + def.label + "\"." : ".");
                for (int i = 0; i < choiceViews.Count && i < e.choices.Count; i++)
                {
                    var c = e.choices[i];
                    var bv = choiceViews[i].Key;
                    bool can = ev.CanAfford(c);
                    string desc = ev.DescribeChoice(c);
                    if (!can) desc += "\nFaltam " + NumberFormat.Short(ev.ChoiceCost(c) - s.energy) + " de Energia";
                    choiceViews[i].Value.text = desc;
                    bv.button.interactable = can;
                    bv.bg.color = can ? (i == 0 ? Palette.UiButton : Palette.UiCardHighlight) : Palette.UiButtonDisabled;
                    choiceViews[i].Value.color = can ? Palette.UiText : Palette.UiNegative;
                }
            }

            if (s.effects.Count == 0) effectsListText.text = "Nenhum efeito ativo.";
            else
            {
                var sb = new System.Text.StringBuilder();
                foreach (var fx in s.effects)
                {
                    bool good = fx.kind == EffectKind.ReputationTemp ? fx.value > 0 : fx.value >= 1f;
                    sb.Append(good ? "<color=#86C995>" : "<color=#E3826F>");
                    sb.Append(EventDirector.DescribeEffect(fx.kind, fx.value)).Append("</color>  ·  ")
                      .Append(NumberFormat.Duration(fx.remaining)).Append(" restantes  ·  <i>").Append(fx.label).Append("</i>\n");
                }
                effectsListText.text = sb.ToString().TrimEnd('\n');
            }
        }

        static Color RepColor(float rep) => rep >= 65 ? Palette.UiPositive : rep >= 35 ? Palette.ConcreteLight : Palette.UiNegative;

        // ---------- aba Renascimento ----------
        void BuildRebirthPage(RectTransform page)
        {
            var content = UIFactory.Scroll(page, out _);

            var card = UIFactory.Panel("renascimento", content, Palette.UiCardLight);
            UIFactory.VLayout(card.gameObject, 24, 12);
            var head = UIFactory.Rect("cabeçalho", card.transform);
            UIFactory.HLayout(head.gameObject, 0, 18);
            UIFactory.Icon(head, IconKind.Nucleo, 120, true);
            var t = UIFactory.Rect("títulos", head);
            UIFactory.VLayout(t.gameObject, 0, 4);
            UIFactory.LE(t.gameObject, 200, 200, -1, -1, 1);
            UIFactory.Label(t, "Renascimento", 40, Palette.UiText, TextAnchor.MiddleLeft, true);
            UIFactory.Label(t, "Inaugure uma nova geração da usina. A equipe leva a experiência desta campanha na forma de Núcleos.", 26, Palette.UiMuted);

            rbProgressText = UIFactory.Label(card.transform, "", 30, Palette.UiText);
            var barBg = UIFactory.Rect("barra", card.transform);
            UIFactory.LE(barBg.gameObject, -1, -1, 26, 26);
            UIFactory.Img(barBg, Palette.UiCard);
            var fill = UIFactory.Stretch(UIFactory.Rect("preenchimento", barBg));
            rbFill = UIFactory.Img(fill, Palette.UiPositive, Art.Pixel);
            rbFill.type = Image.Type.Filled; rbFill.fillMethod = Image.FillMethod.Horizontal;
            rbGainText = UIFactory.Label(card.transform, "", 32, Palette.UiText, TextAnchor.MiddleLeft, true);
            rbNextText = UIFactory.Label(card.transform, "", 26, Palette.UiMuted);
            rbButton = UIFactory.Button(card.transform, "Renascer", Palette.UiButton, OpenConfirm, 40, "Ver o que muda antes de confirmar");

            rbOwnedText = UIFactory.Label(content, "", 34, Palette.UiText, TextAnchor.MiddleLeft, true);
            foreach (var p in gm.Config.permanentUpgrades)
            {
                var def = p;
                var bg = UIFactory.Panel("permanente " + p.displayName, content, Palette.UiCard);
                UIFactory.HLayout(bg.gameObject, 18, 18);
                UIFactory.Icon(bg.transform, p.icon, 110, true);
                var info = UIFactory.Rect("info", bg.transform);
                UIFactory.VLayout(info.gameObject, 0, 4);
                UIFactory.LE(info.gameObject, 200, 200, -1, -1, 1);
                var row = new PermRow { def = p };
                row.title = UIFactory.Label(info, p.displayName, 36, Palette.UiText, TextAnchor.MiddleLeft, true);
                UIFactory.Label(info, p.description, 26, Palette.UiMuted);
                row.desc = UIFactory.Label(info, "", 28, Palette.UiText);
                row.buy = UIFactory.Button(bg.transform, "1", Palette.UiButton, () => BuyPermanent(def), 38, "Núcleo");
                UIFactory.LE(row.buy.rt.gameObject, 220, 220, 120, 120, 0);
                permRows.Add(row);
            }

            var stats = UIFactory.Panel("estatísticas", content, Palette.UiCard);
            UIFactory.VLayout(stats.gameObject, 22, 8);
            UIFactory.Label(stats.transform, "Estatísticas históricas", 34, Palette.UiText, TextAnchor.MiddleLeft, true);
            statsText = UIFactory.Label(stats.transform, "", 28, Palette.UiMuted);
        }

        void BuyPermanent(PermanentUpgradeDef p)
        {
            if (busy) return;
            if (gm.BuyPermanent(p)) { Play(Sfx.Install); Toast(p.displayName + " ativado para as próximas campanhas!"); }
            else Play(Sfx.Error);
            RefreshRebirth();
        }

        void RefreshRebirth()
        {
            var rb = gm.Rebirth;
            var s = gm.State;
            double th = gm.Config.rebirthThreshold;
            rbProgressText.text = "Energia produzida nesta campanha: <b>" + NumberFormat.Short(s.campaignProduced) + "</b> / " + NumberFormat.Short(th);
            rbFill.fillAmount = (float)rb.Progress01;
            int pending = rb.PendingNucleos;
            rbGainText.text = pending > 0 ? "Ao renascer agora: <color=#ECEEE9>+" + pending + " Núcleo" + (pending > 1 ? "s" : "") + "</color>"
                                          : "Renascimento libera com " + NumberFormat.Full(th) + " de Energia produzida (inclui o que já foi gasto).";
            rbNextText.text = pending > 0
                ? "Próximo Núcleo com " + NumberFormat.Short(rb.NextNucleoAt) + " produzidos (faltam " + NumberFormat.Short(rb.RemainingForNextNucleo) + ")."
                : "Faltam " + NumberFormat.Short(th - s.campaignProduced) + " de Energia produzida.";
            UIFactory.SetInteractable(rbButton, rb.CanRebirth && !busy, Palette.UiButton);

            rbOwnedText.text = "Núcleos disponíveis: <color=#ECEEE9>" + s.nucleos + "</color>   <size=26><color=#9AA29E>Melhorias permanentes</color></size>";
            foreach (var row in permRows)
            {
                int lv = rb.PermanentLevel(row.def);
                bool maxed = rb.PermanentMaxed(row.def);
                row.title.text = row.def.displayName + (lv > 0 ? "  <color=#9AA29E>Nv " + lv + "</color>" : "");
                row.desc.text = maxed ? RebirthSystem.DescribePermanent(row.def, lv) + " (máximo)"
                    : (lv > 0 ? RebirthSystem.DescribePermanent(row.def, lv) + "  →  " : "") + RebirthSystem.DescribePermanent(row.def, lv + 1);
                int cost = rb.PermanentCost(row.def);
                row.buy.label.text = maxed ? "Máx." : cost.ToString();
                row.buy.sub.text = maxed ? "" : cost == 1 ? "Núcleo" : "Núcleos";
                UIFactory.SetInteractable(row.buy, rb.CanBuyPermanent(row.def) && !busy, Palette.UiButton);
            }

            statsText.text =
                "Geração atual: " + PlantView.Roman(s.generation) + "\n" +
                "Energia produzida (total): " + NumberFormat.Short(s.totalProducedAllTime) + "\n" +
                "Toques no reator: " + NumberFormat.Full(s.totalTaps) + "\n" +
                "Renascimentos: " + s.rebirths + "   ·   Núcleos obtidos: " + s.nucleosEarnedTotal + "\n" +
                "Melhor campanha: " + NumberFormat.Short(s.bestCampaignProduced) + "\n" +
                "Renascimento mais rápido: " + (s.fastestRebirthSeconds > 0 ? NumberFormat.Duration((float)s.fastestRebirthSeconds) : "—") + "\n" +
                "Eventos resolvidos: " + s.eventsResolved + "\n" +
                "Tempo de jogo: " + NumberFormat.Duration((float)s.playSeconds) + "   ·   Campanha atual: " + NumberFormat.Duration((float)s.campaignSeconds);
        }

        // ---------- modais ----------
        GameObject Modal(string name, out RectTransform card)
        {
            var root = UIFactory.Stretch(UIFactory.Rect(name, modalLayer));
            UIFactory.Img(root, new Color(0, 0, 0, 0.7f), Art.Pixel, true);
            card = UIFactory.Rect("cartão", root);
            UIFactory.Anchor(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960, 0));
            UIFactory.Img(card, Palette.UiCardLight, Art.RoundRect, true);
            UIFactory.VLayout(card.gameObject, 36, 22);
            var fit = card.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        void BuildSettingsModal()
        {
            settingsModal = Modal("Configurações", out var card);
            UIFactory.Label(card, "Configurações", 44, Palette.UiText, TextAnchor.MiddleCenter, true);
            soundButton = UIFactory.Button(card, "Som: ligado", Palette.UiButton, ToggleSound, 36);
            soundLabel = soundButton.label;
            var reload = UIFactory.Button(card, "Salvar e recarregar", Palette.UiCardHighlight, () =>
            {
                gm.SaveNow(); gm.ReloadFromDisk(); plant?.RefreshAll(true);
                Toast("Progresso recarregado do save."); CloseModals();
            }, 34, "Confere se saldo, melhorias e eventos voltam como estavam");
            var reset = UIFactory.Button(card, "Reiniciar todo o progresso", Palette.UiNegative.WithAlpha(0.8f), ResetPressed, 34);
            resetLabel = reset.label;
            string help = "Toque no reator (ou aperte Espaço no PC) para gerar Energia. O jogo salva sozinho a cada poucos segundos e após compras, decisões e Renascimentos.";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            help += "\n\n<color=#E3826F>Atalhos de teste (Editor):</color> F1 +1 mil de Energia · F2 +25 mil · F3 sortear evento · F4 recarregar save";
#endif
            UIFactory.Label(card, help, 26, Palette.UiMuted, TextAnchor.MiddleCenter);
            UIFactory.Button(card, "Fechar", Palette.UiCard, CloseModals, 34);
        }

        void BuildConfirmModal()
        {
            confirmModal = Modal("Confirmar Renascimento", out var card);
            UIFactory.Label(card, "Inaugurar uma nova geração?", 42, Palette.UiText, TextAnchor.MiddleCenter, true);
            confirmBody = UIFactory.Label(card, "", 30, Palette.UiText);
            UIFactory.Button(card, "Renascer agora", Palette.UiButton, ConfirmRebirth, 38);
            UIFactory.Button(card, "Ainda não", Palette.UiCard, CloseModals, 34);
        }

        void OpenSettings()
        {
            Play(Sfx.Ui);
            resetArmed = false;
            resetLabel.text = "Reiniciar todo o progresso";
            settingsModal.SetActive(true);
            soundLabel.text = gm.State.soundOn ? "Som: ligado" : "Som: desligado";
        }

        void ToggleSound()
        {
            gm.SetSound(!gm.State.soundOn);
            soundLabel.text = gm.State.soundOn ? "Som: ligado" : "Som: desligado";
            Play(Sfx.Ui);
        }

        void ResetPressed()
        {
            if (!resetArmed)
            {
                resetArmed = true;
                resetLabel.text = "Toque de novo para apagar TUDO";
                return;
            }
            gm.ResetAllProgress();
            plant?.RefreshAll(true);
            CloseModals();
            Toast("Progresso reiniciado.");
        }

        void OpenConfirm()
        {
            if (!gm.Rebirth.CanRebirth || busy) return;
            Play(Sfx.Ui);
            int n = gm.Rebirth.PendingNucleos;
            int total = gm.State.nucleos + n;
            var sb = new System.Text.StringBuilder();
            sb.Append("Você receberá <color=#ECEEE9><b>").Append(n).Append(" Núcleo").Append(n > 1 ? "s" : "").Append("</b></color> (total: ").Append(total).Append(").\n\n");
            sb.Append("<b>Com eles você pode comprar:</b>\n");
            foreach (var p in gm.Config.permanentUpgrades)
            {
                int lv = gm.Rebirth.PermanentLevel(p);
                if (gm.Rebirth.PermanentMaxed(p)) continue;
                int cost = gm.Rebirth.PermanentCost(p);
                sb.Append(cost <= total ? "• " : "<color=#9AA29E>• ").Append(p.displayName).Append(": ").Append(RebirthSystem.DescribePermanent(p, lv + 1))
                  .Append(" (").Append(cost).Append(cost == 1 ? " Núcleo)" : " Núcleos)").Append(cost <= total ? "" : "</color>").Append("\n");
            }
            sb.Append("\n<color=#E3826F><b>Será reiniciado:</b></color> saldo de Energia (").Append(NumberFormat.Short(gm.State.energy))
              .Append("), Energia produzida na campanha (").Append(NumberFormat.Short(gm.State.campaignProduced))
              .Append("), níveis das melhorias (").Append(gm.Economy.TotalUpgradeLevels()).Append(" níveis), reputação e eventos ativos.\n");
            sb.Append("<color=#86C995><b>Será mantido:</b></color> Núcleos, melhorias permanentes, estatísticas e configurações.");
            confirmBody.text = sb.ToString();
            confirmModal.SetActive(true);
        }

        void ConfirmRebirth()
        {
            CloseModals();
            if (!gm.Rebirth.CanRebirth || busy) return;
            busy = true;
            Play(Sfx.Rebirth);
            int gained = 0;
            if (plant != null)
                StartCoroutine(RebirthRoutine());
            else
            {
                gained = gm.DoRebirth();
                busy = false;
                AfterRebirth(gained);
            }

            System.Collections.IEnumerator RebirthRoutine()
            {
                yield return plant.RebirthSequence(() => { gained = gm.DoRebirth(); builtEventKey = null; RefreshAll(); }, SetCaption);
                busy = false;
                AfterRebirth(gained);
            }
        }

        void AfterRebirth(int gained)
        {
            ShowTab(TabRebirth);
            Toast("+" + gained + " Núcleo" + (gained > 1 ? "s" : "") + "! Gaste-os nas melhorias permanentes.");
            RefreshAll();
        }

        void CloseModals()
        {
            settingsModal.SetActive(false);
            confirmModal.SetActive(false);
        }

        // ---------- avisos ----------
        void BuildCaptionAndToast()
        {
            captionText = UIFactory.Label(safe, "", 46, Palette.UiText, TextAnchor.MiddleCenter, true);
            UIFactory.Anchor((RectTransform)captionText.transform, new Vector2(0, 0.62f), new Vector2(1, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-80, 200));
            captionText.gameObject.SetActive(false);

            var toast = UIFactory.Rect("Aviso", safe);
            UIFactory.Anchor(toast, new Vector2(0.5f, bottomPanelFraction), new Vector2(0.5f, bottomPanelFraction), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(940, 0));
            toastBg = UIFactory.Img(toast, Palette.UiCardHighlight.WithAlpha(0.96f));
            UIFactory.VLayout(toast.gameObject, 20, 0, TextAnchor.MiddleCenter);
            var fit = toast.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            toastText = UIFactory.Label(toast, "", 30, Palette.UiText, TextAnchor.MiddleCenter, true);
            toast.gameObject.SetActive(false);
        }

        void SetCaption(string text)
        {
            captionText.gameObject.SetActive(!string.IsNullOrEmpty(text));
            captionText.text = text ?? "";
        }

        public void Toast(string msg)
        {
            toastText.text = msg;
            toastBg.gameObject.SetActive(true);
            toastTimer = 3.2f;
        }

        // ---------- textos flutuantes ----------
        void BuildFloaters()
        {
            for (int i = 0; i < 14; i++)
            {
                var t = UIFactory.Label(floatLayer, "", 56, Palette.UiText, TextAnchor.MiddleCenter, true);
                var rt = (RectTransform)t.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(420, 90);
                t.gameObject.SetActive(false);
                floaters.Add(new Floater { rt = rt, text = t });
            }
        }

        void SpawnFloater(Vector2 screenPos, double value)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(floatLayer, screenPos, UiCam, out var local);
            float now = Time.unscaledTime;
            // Toques rápidos: soma no texto mais recente em vez de criar outro.
            Floater recent = null;
            foreach (var f in floaters)
                if (f.active && now - f.born < 0.25f && (f.start - local).sqrMagnitude < 220f * 220f && (recent == null || f.born > recent.born))
                    recent = f;
            if (recent != null)
            {
                recent.value += value;
                recent.age = Mathf.Min(recent.age, 0.1f);
                recent.born = now;
                recent.text.text = "+" + NumberFormat.Short(recent.value);
                return;
            }
            Floater use = null;
            foreach (var f in floaters) if (!f.active) { use = f; break; }
            if (use == null) { use = floaters[0]; foreach (var f in floaters) if (f.born < use.born) use = f; }
            use.active = true; use.age = 0; use.value = value; use.born = now;
            use.start = local + new Vector2(Random.Range(-40f, 40f), 40f);
            use.text.text = "+" + NumberFormat.Short(value);
            use.text.gameObject.SetActive(true);
            use.rt.SetAsLastSibling();
        }

        void UpdateFloaters(float dt)
        {
            foreach (var f in floaters)
            {
                if (!f.active) continue;
                f.age += dt;
                float t = f.age / 0.9f;
                if (t >= 1f) { f.active = false; f.text.gameObject.SetActive(false); continue; }
                float rise = Mathf.Round(170f * (1f - (1f - t) * (1f - t)) / PixelText.UiPixel) * PixelText.UiPixel;
                f.rt.anchoredPosition = f.start + new Vector2(0, rise);
                f.rt.localScale = Vector3.one;
                f.text.color = t < 0.75f ? Palette.UiText : Palette.UiMuted;
            }
        }

        // ---------- toque ----------
        void DoTap(Vector2 screenPos)
        {
            if (busy) return;
            double v = gm.Tap();
            Play(Sfx.Tap);
            SpawnFloater(screenPos, v);
        }

        // ---------- loop ----------
        void Update()
        {
            if (gm == null) return;
            float dt = Time.unscaledDeltaTime;

            UpdateScale();

            HandleKeyboard();
            UpdateHud();
            UpdateFloaters(dt);

            if (toastTimer > 0)
            {
                toastTimer -= dt;
                if (toastTimer <= 0) toastBg.gameObject.SetActive(false);
            }

            refreshTimer -= dt;
            if (refreshTimer <= 0)
            {
                refreshTimer = 0.1f;
                RefreshCurrentTab();
                UpdateBadges();
            }
        }

        void LateUpdate()
        {
            if (gm == null || plant == null || plant.Cam == null) return;
            PositionTapArea();
            // Informa à câmera qual faixa da tela está livre para o cenário.
            var corners = new Vector3[4];
            bottomPanel.GetWorldCorners(corners);
            float bottom = RectTransformUtility.WorldToScreenPoint(UiCam, corners[1]).y / Mathf.Max(1, PixelHeight);
            topBar.GetWorldCorners(corners);
            float top = RectTransformUtility.WorldToScreenPoint(UiCam, corners[0]).y / Mathf.Max(1, PixelHeight);
            plant.viewportBottom = Mathf.Clamp01(bottom);
            plant.viewportTop = Mathf.Clamp(top, plant.viewportBottom + 0.2f, 1f);
        }

        void PositionTapArea()
        {
            Rect wr = plant.ReactorWorldRect;
            Vector2 va = plant.WorldToViewport(new Vector3(wr.xMin, wr.yMin, 0));
            Vector2 vb = plant.WorldToViewport(new Vector3(wr.xMax, wr.yMax, 0));
            Vector2 a = new Vector2(va.x * PixelWidth, va.y * PixelHeight);
            Vector2 b = new Vector2(vb.x * PixelWidth, vb.y * PixelHeight);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tapLayer, a, UiCam, out var la);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tapLayer, b, UiCam, out var lb);
            Vector2 center = (la + lb) * 0.5f - tapLayer.rect.center;
            tapRt.anchoredPosition = center;
            tapRt.sizeDelta = new Vector2(Mathf.Abs(lb.x - la.x), Mathf.Abs(lb.y - la.y));

            bool showTutorial = !gm.State.tutorialDone && !busy;
            if (tutorial.gameObject.activeSelf != showTutorial) tutorial.gameObject.SetActive(showTutorial);
            if (showTutorial)
            {
                bool canBuyFirst = gm.Economy.CanBuy(0);
                tutorialText.text = canBuyFirst ? "Compre o " + gm.Config.upgrades[0].displayName + " em Melhorias, logo abaixo"
                                                : "Toque no reator para gerar Energia";
                float bob = Mathf.Sin(Time.unscaledTime * 3f) * 10f;
                float y = canBuyFirst ? center.y - tapRt.sizeDelta.y * 0.5f - 40f : center.y + tapRt.sizeDelta.y * 0.5f + 150f;
                tutorial.anchoredPosition = new Vector2(center.x, y + bob);
            }
        }

        void HandleKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame && !settingsModal.activeSelf && !confirmModal.activeSelf)
            {
                Vector2 c = RectTransformUtility.WorldToScreenPoint(UiCam, tapRt.position);
                DoTap(new Vector2(c.x, c.y));
            }
            if (kb.escapeKey.wasPressedThisFrame) CloseModals();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (kb.f1Key.wasPressedThisFrame) { gm.DevAddEnergy(1000); Toast("[Teste] +1 mil de Energia"); }
            if (kb.f2Key.wasPressedThisFrame) { gm.DevAddEnergy(25000); Toast("[Teste] +25 mil de Energia"); }
            if (kb.f3Key.wasPressedThisFrame) { gm.DevTriggerEvent(); Toast("[Teste] Evento sorteado"); }
            if (kb.f4Key.wasPressedThisFrame) { gm.SaveNow(); gm.ReloadFromDisk(); plant?.RefreshAll(true); Toast("[Teste] Save recarregado"); }
#endif
        }

        void UpdateHud()
        {
            var s = gm.State;
            var eco = gm.Economy;
            balanceText.text = NumberFormat.Short(s.energy);
            clickText.text = "+" + NumberFormat.Short(eco.ClickValue) + " por toque";
            passiveText.text = "+" + NumberFormat.Short(eco.PassivePerSecond) + "/s";
            float rep = gm.Events.EffectiveReputation;
            repText.text = "Reputação " + Mathf.RoundToInt(rep);
            repFill.fillAmount = rep / 100f;
            repFill.color = RepColor(rep);

            var e = gm.Events.Pending;
            bool showHead = e != null && !busy;
            if (headline.gameObject.activeSelf != showHead) headline.gameObject.SetActive(showHead);
            if (showHead)
            {
                headlineIcon.sprite = Art.Icon(e.icon);
                headlineText.text = "<b>" + e.headline + "</b>\n<color=#9AA29E>Toque para ler e decidir</color>";
                float pulse = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f ? 1f : 0f;
                Color tone = e.tone == EventTone.Positive ? Palette.UiPositive : e.tone == EventTone.Negative ? Palette.UiNegative : Palette.UiNegative;
                headlineBg.color = Color.Lerp(Palette.UiCard, Color.Lerp(Palette.UiCard, tone, 0.35f), pulse);
            }

            bool showFx = s.effects.Count > 0 && !busy;
            if (effectsPill.activeSelf != showFx) effectsPill.SetActive(showFx);
            if (showFx)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < s.effects.Count; i++)
                {
                    var fx = s.effects[i];
                    bool good = fx.kind == EffectKind.ReputationTemp ? fx.value > 0 : fx.value >= 1f;
                    if (i > 0) sb.Append("\n");
                    sb.Append(good ? "<color=#86C995>" : "<color=#E3826F>").Append(EventDirector.DescribeEffect(fx.kind, fx.value))
                      .Append("</color>  ·  ").Append(NumberFormat.Duration(fx.remaining));
                }
                effectsText.text = sb.ToString();
            }
        }

        void UpdateBadges()
        {
            tabBadges[TabUpgrades].enabled = AnyAffordableUpgrade() && currentTab != TabUpgrades;
            tabBadges[TabEvents].enabled = gm.State.HasPendingEvent && currentTab != TabEvents;
            bool canSpend = false;
            foreach (var p in gm.Config.permanentUpgrades) if (gm.Rebirth.CanBuyPermanent(p)) canSpend = true;
            tabBadges[TabRebirth].enabled = (gm.Rebirth.CanRebirth || canSpend) && currentTab != TabRebirth;
        }

        bool AnyAffordableUpgrade()
        {
            for (int i = 0; i < gm.Config.upgrades.Count; i++) if (gm.Economy.CanBuy(i)) return true;
            return false;
        }

        void ShowTab(int tab)
        {
            currentTab = tab;
            for (int i = 0; i < 3; i++)
            {
                pages[i].gameObject.SetActive(i == tab);
                tabButtons[i].bg.color = i == tab ? Palette.UiCardHighlight : Palette.UiCard;
                tabButtons[i].label.color = i == tab ? Palette.UiText : Palette.UiMuted;
            }
            RefreshCurrentTab();
        }

        void RefreshCurrentTab()
        {
            if (currentTab == TabUpgrades) RefreshUpgrades();
            else if (currentTab == TabEvents) RefreshEvents();
            else RefreshRebirth();
        }

        void RefreshAll()
        {
            RefreshUpgrades();
            RefreshEvents();
            RefreshRebirth();
        }
    }
}
