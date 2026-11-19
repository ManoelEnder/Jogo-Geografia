using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NucleoVivo
{
    /// <summary>
    /// Interface para PC (tela horizontal): Energia no canto, painel lateral com abas,
    /// barra do mapa, dicas ao passar o mouse e controle da câmera com o mouse.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GameUI : MonoBehaviour
    {
        [SerializeField] PlantView plant;
        [SerializeField] SfxPlayer sfx;
        [Tooltip("Largura do painel lateral (unidades do Canvas; 1 pixel da interface = 3).")]
        public float panelWidth = 630f;

        const int TabUpgrades = 0, TabAreas = 1, TabEvents = 2, TabRebirth = 3;
        static readonly string[] TabNames = { "Melhorias", "Áreas do mapa", "Eventos", "Renascimento" };
        static readonly IconKind[] TabIcons = { IconKind.ControlPanel, IconKind.Map, IconKind.Press, IconKind.Nucleo };

        GameManager gm;
        Canvas canvas;
        CanvasScaler scaler;
        RectTransform safe, worldLayer, floatLayer, hud, notices, panel, modalLayer, mapBar, tooltip;

        // topo
        Text balanceText, clickText, passiveText, repText;
        Image repFill;
        RectTransform headline;
        Image headlineBg, headlineIcon;
        Text headlineText, effectsText;
        GameObject effectsPill;

        // toque no reator
        RectTransform tutorial;
        Text tutorialText;

        // abas
        readonly RectTransform[] pages = new RectTransform[4];
        readonly UIFactory.ButtonView[] tabButtons = new UIFactory.ButtonView[4];
        readonly Image[] tabBadges = new Image[4];
        Text tabTitle;
        int currentTab;

        class UpgradeRow { public int index; public GameObject root; public Image bg, icon; public Text title, gain; public UIFactory.ButtonView buy; }
        readonly List<UpgradeRow> upgradeRows = new List<UpgradeRow>();

        class AreaRow { public AreaDef def; public Image bg, icon; public Text title, info; public UIFactory.ButtonView open, go; }
        readonly List<AreaRow> areaRows = new List<AreaRow>();
        readonly List<KeyValuePair<AreaDef, UIFactory.ButtonView>> mapButtons = new List<KeyValuePair<AreaDef, UIFactory.ButtonView>>();
        AreaDef selectedArea;

        // eventos
        Text repCardText, repCardHint, eventStatusText, eventExpireText, effectsListText;
        Image repCardFill;
        RectTransform eventSlot;
        string builtEventKey;
        readonly List<KeyValuePair<UIFactory.ButtonView, Text>> choiceViews = new List<KeyValuePair<UIFactory.ButtonView, Text>>();

        // renascimento
        Text rbProgressText, rbGainText, rbNextText, rbOwnedText, statsText;
        Image rbFill;
        UIFactory.ButtonView rbButton;
        class PermRow { public PermanentUpgradeDef def; public Text title, desc; public UIFactory.ButtonView buy; }
        readonly List<PermRow> permRows = new List<PermRow>();

        // modais, avisos, dica
        GameObject settingsModal, confirmModal;
        Text confirmBody, soundLabel, resetLabel, captionText, toastText, tooltipText;
        Image toastBg;
        float toastTimer, refreshTimer;
        bool busy, resetArmed;
        Func<string> hoverText;

        class Floater { public RectTransform rt; public Text text; public float age; public double value; public bool active; public float born; public Vector2 start; }
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
                Toast(auto ? "Sem resposta: \"" + c.label + "\"" : "Decisão: " + c.label);
            };
            gm.StructureTierChanged += (i, tier) =>
            {
                Play(Sfx.Install);
                var u = gm.Config.upgrades[i];
                Toast(tier == 1 ? u.displayName + " em operação" : u.displayName + " · etapa " + tier);
            };
            gm.AreaOpened += a => { Play(Sfx.Install); Toast(a.displayName + " aberta"); };
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
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        void Play(Sfx s) { if (sfx != null) sfx.Play(s); }

        Camera UiCam => canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        int PixelWidth => UiCam != null ? UiCam.pixelWidth : Screen.width;
        int PixelHeight => UiCam != null ? UiCam.pixelHeight : Screen.height;

        /// <summary>1 pixel da interface (3 unidades) = número inteiro de pixels da tela sempre que possível.</summary>
        void UpdateScale()
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

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

            BuildWorldLayer();
            BuildHud();
            BuildNotices();
            BuildMapBar();
            BuildPanel();
            floatLayer = UIFactory.Stretch(UIFactory.Rect("Textos flutuantes", safe));
            BuildFloaters();
            BuildCaptionAndToast();
            modalLayer = UIFactory.Stretch(UIFactory.Rect("Modais", safe));
            BuildSettingsModal();
            BuildConfirmModal();
            BuildTooltip();
            ShowTab(TabUpgrades);
        }

        void BuildWorldLayer()
        {
            worldLayer = UIFactory.Stretch(UIFactory.Rect("Cenário (mouse)", safe));
            var img = worldLayer.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            var input = worldLayer.gameObject.AddComponent<WorldInput>();
            input.ui = this;

            tutorial = UIFactory.Rect("Dica inicial", worldLayer);
            tutorial.anchorMin = tutorial.anchorMax = Vector2.zero;
            tutorial.pivot = new Vector2(0.5f, 0f);
            tutorial.sizeDelta = new Vector2(450, 60);
            UIFactory.Img(tutorial, Palette.UiBackground.WithAlpha(0.95f));
            tutorialText = UIFactory.Label(tutorial, "Clique no reator", 30, Palette.UiText, TextAnchor.MiddleCenter);
            UIFactory.Stretch((RectTransform)tutorialText.transform, 12, 6, 12, 6);
        }

        void BuildHud()
        {
            hud = UIFactory.Rect("Energia", safe);
            UIFactory.Anchor(hud, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -18), new Vector2(660, 192));
            UIFactory.Img(hud, Palette.UiBackground.WithAlpha(0.92f), Art.RoundRect, true);

            var icon = UIFactory.Icon(hud, IconKind.Energy, 54, false);
            UIFactory.Anchor((RectTransform)icon.transform.parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(21, -24), new Vector2(54, 54));
            balanceText = UIFactory.Label(hud, "0", 50, Palette.UiText);
            UIFactory.Anchor((RectTransform)balanceText.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -21), new Vector2(420, 66));
            var sub = UIFactory.Label(hud, "Energia", 30, Palette.UiMuted);
            UIFactory.Anchor((RectTransform)sub.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -87), new Vector2(300, 39));

            clickText = UIFactory.Label(hud, "", 30, Palette.UiText);
            UIFactory.Anchor((RectTransform)clickText.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -135), new Vector2(300, 39));
            passiveText = UIFactory.Label(hud, "", 30, Palette.UiText);
            UIFactory.Anchor((RectTransform)passiveText.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(330, -135), new Vector2(300, 39));

            repText = UIFactory.Label(hud, "", 30, Palette.UiMuted, TextAnchor.MiddleRight);
            UIFactory.Anchor((RectTransform)repText.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(240, 39));
            var barBg = UIFactory.Rect("barra", hud);
            UIFactory.Anchor(barBg, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -72), new Vector2(150, 12));
            UIFactory.Img(barBg, Palette.UiCardLight, Art.Pixel);
            var fill = UIFactory.Stretch(UIFactory.Rect("nível", barBg));
            repFill = UIFactory.Img(fill, Palette.UiPositive, Art.Pixel);
            repFill.type = Image.Type.Filled; repFill.fillMethod = Image.FillMethod.Horizontal;
            AddTooltip(hud.gameObject, () =>
                "Energia por clique no reator e por segundo.\nReputação " + Mathf.RoundToInt(gm.Events.EffectiveReputation) + "/100: muda os eventos que aparecem.");
        }

        void BuildNotices()
        {
            notices = UIFactory.Rect("Avisos", safe);
            UIFactory.Anchor(notices, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -222), new Vector2(660, 0));
            UIFactory.VLayout(notices.gameObject, 0, 9);
            notices.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            headline = UIFactory.Rect("Manchete", notices);
            headlineBg = UIFactory.Img(headline, Palette.UiCard.WithAlpha(0.95f), Art.RoundRect, true);
            var btn = headline.gameObject.AddComponent<Button>();
            btn.targetGraphic = headlineBg;
            btn.onClick.AddListener(() => { Play(Sfx.Ui); ShowTab(TabEvents); });
            var h = UIFactory.HLayout(headline.gameObject, 12, 12);
            headlineIcon = UIFactory.Icon(headline, IconKind.Press, 54, false);
            headlineText = UIFactory.Label(headline, "", 30, Palette.UiText);
            UIFactory.LE(headlineText.gameObject, 300, 300, -1, -1, 1);

            effectsPill = UIFactory.Rect("Efeitos ativos", notices).gameObject;
            UIFactory.Img((RectTransform)effectsPill.transform, Palette.UiBackground.WithAlpha(0.85f));
            UIFactory.VLayout(effectsPill, 15, 0);
            effectsText = UIFactory.Label(effectsPill.transform, "", 30, Palette.UiText);
        }

        void BuildMapBar()
        {
            mapBar = UIFactory.Rect("Mapa", safe);
            UIFactory.Anchor(mapBar, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 18), new Vector2(0, 90));
            UIFactory.Img(mapBar, Palette.UiBackground.WithAlpha(0.92f), Art.RoundRect, true);
            var h = UIFactory.HLayout(mapBar.gameObject, 12, 9);
            h.padding = new RectOffset(15, 15, 12, 12);
            mapBar.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var list = new List<AreaDef>(gm.Config.areas);
            list.Sort((a, b) => a.mapX.CompareTo(b.mapX));
            bool usinaAdded = false;
            foreach (var a in list)
            {
                if (!usinaAdded && a.mapX > 0) { AddMapButton(null); usinaAdded = true; }
                AddMapButton(a);
            }
            if (!usinaAdded) AddMapButton(null);
            var hint = UIFactory.Label(mapBar, "Arraste: mover\nRoda: zoom", 30, Palette.UiMuted);
            UIFactory.LE(hint.gameObject, 270, 270, 66, 66);
        }

        void AddMapButton(AreaDef a)
        {
            var bv = UIFactory.Button(mapBar, "", Palette.UiCardLight, () =>
            {
                Play(Sfx.Ui);
                if (a != null) plant.FocusOn(plant.Areas.CenterOf(a)); else plant.FocusX(0f);
                if (a != null) { selectedArea = a; ShowTab(TabAreas); }
            }, 30);
            DestroyImmediate(bv.rt.GetComponent<VerticalLayoutGroup>());
            bv.label.gameObject.SetActive(false);
            UIFactory.LE(bv.rt.gameObject, 66, 66, 66, 66);
            var ic = UIFactory.Icon(bv.rt, a != null ? a.icon : IconKind.Reactor, 54, false);
            var hold = (RectTransform)ic.transform.parent;
            hold.anchorMin = hold.anchorMax = new Vector2(0.5f, 0.5f);
            hold.sizeDelta = new Vector2(54, 54);
            AddTooltip(bv.rt.gameObject, () => a == null ? "Usina Núcleo Vivo" : a.displayName + "\n" + AreaStatus(a));
            mapButtons.Add(new KeyValuePair<AreaDef, UIFactory.ButtonView>(a, bv));
        }

        void BuildPanel()
        {
            panel = UIFactory.Rect("Painel", safe);
            panel.anchorMin = new Vector2(1, 0); panel.anchorMax = new Vector2(1, 1); panel.pivot = new Vector2(1, 0.5f);
            panel.sizeDelta = new Vector2(panelWidth, -36);
            panel.anchoredPosition = new Vector2(-18, 0);
            UIFactory.Img(panel, Palette.UiBackground, Art.RoundRect, true);

            var tabs = UIFactory.Rect("Abas", panel);
            UIFactory.Anchor(tabs, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -15), new Vector2(-30, 78));
            var h = UIFactory.HLayout(tabs.gameObject, 0, 9);
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var bv = UIFactory.Button(tabs, "", Palette.UiCard, () => { Play(Sfx.Ui); ShowTab(idx); }, 30);
                DestroyImmediate(bv.rt.GetComponent<VerticalLayoutGroup>());
                bv.label.gameObject.SetActive(false);
                UIFactory.LE(bv.rt.gameObject, 96, 96, 78, 78);
                var ic = UIFactory.Icon(bv.rt, TabIcons[i], 54, false);
                var hold = (RectTransform)ic.transform.parent;
                hold.anchorMin = hold.anchorMax = new Vector2(0.5f, 0.5f);
                hold.sizeDelta = new Vector2(54, 54);
                var badge = UIFactory.Rect("alerta", bv.rt);
                UIFactory.Anchor(badge, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-6, -6), new Vector2(12, 12));
                tabBadges[i] = UIFactory.Img(badge, Palette.UiNegative, Art.Pixel);
                AddTooltip(bv.rt.gameObject, () => TabNames[idx]);
                tabButtons[i] = bv;
            }
            var gear = UIFactory.Button(tabs, "", Palette.UiCard, OpenSettings, 30);
            DestroyImmediate(gear.rt.GetComponent<VerticalLayoutGroup>());
            gear.label.gameObject.SetActive(false);
            UIFactory.LE(gear.rt.gameObject, 78, 78, 78, 78);
            var gi = UIFactory.Icon(gear.rt, IconKind.Settings, 54, false);
            var gh = (RectTransform)gi.transform.parent;
            gh.anchorMin = gh.anchorMax = new Vector2(0.5f, 0.5f); gh.sizeDelta = new Vector2(54, 54);
            AddTooltip(gear.rt.gameObject, () => "Configurações");

            tabTitle = UIFactory.Label(panel, "", 50, Palette.UiText);
            UIFactory.Anchor((RectTransform)tabTitle.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -108), new Vector2(-48, 60));

            var pagesRoot = UIFactory.Rect("Páginas", panel);
            UIFactory.Stretch(pagesRoot, 6, 9, 6, 174);
            for (int i = 0; i < 4; i++) { pages[i] = UIFactory.Stretch(UIFactory.Rect(TabNames[i], pagesRoot)); }
            BuildUpgradesPage(pages[TabUpgrades]);
            BuildAreasPage(pages[TabAreas]);
            BuildEventsPage(pages[TabEvents]);
            BuildRebirthPage(pages[TabRebirth]);
        }

        RectTransform Content(RectTransform page)
        {
            var content = UIFactory.Scroll(page, out _);
            var v = content.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 6, 24);
            v.spacing = 9;
            return content;
        }

        // ---------- dica ao passar o mouse ----------
        void BuildTooltip()
        {
            tooltip = UIFactory.Rect("Dica", safe);
            tooltip.anchorMin = tooltip.anchorMax = Vector2.zero;
            tooltip.pivot = new Vector2(0, 1);
            UIFactory.Img(tooltip, Palette.UiCardHighlight.WithAlpha(0.98f));
            UIFactory.VLayout(tooltip.gameObject, 12, 0);
            var fit = tooltip.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            tooltipText = UIFactory.Label(tooltip, "", 30, Palette.UiText);
            tooltip.gameObject.SetActive(false);
            foreach (var g in tooltip.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
        }

        public void AddTooltip(GameObject go, Func<string> text)
        {
            var t = go.AddComponent<HoverTip>();
            t.onEnter = () => hoverText = text;
            t.onExit = () => { if (hoverText == text) hoverText = null; };
        }

        void UpdateTooltip()
        {
            string txt = hoverText != null ? hoverText() : null;
            bool show = !string.IsNullOrEmpty(txt) && Mouse.current != null;
            if (tooltip.gameObject.activeSelf != show) tooltip.gameObject.SetActive(show);
            if (!show) return;
            tooltipText.text = txt;
            var le = tooltipText.GetComponent<LayoutElement>();
            if (le == null) le = tooltipText.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = Mathf.Min(tooltipText.preferredWidth, 540);
            Vector2 mouse = Mouse.current.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, mouse, UiCam, out var local);
            Vector2 size = safe.rect.size;
            Vector2 pos = local + size * 0.5f + new Vector2(24, -24);
            Vector2 tip = tooltip.rect.size;
            if (pos.x + tip.x > size.x - 6) pos.x = size.x - 6 - tip.x;
            if (pos.y - tip.y < 6) pos.y = 6 + tip.y;
            tooltip.anchoredPosition = new Vector2(Mathf.Round(pos.x / 3) * 3, Mathf.Round(pos.y / 3) * 3);
            tooltip.SetAsLastSibling();
        }

        // ---------- aba Melhorias ----------
        void BuildUpgradesPage(RectTransform page)
        {
            var content = Content(page);
            for (int i = 0; i < gm.Config.upgrades.Count; i++)
            {
                int idx = i;
                var u = gm.Config.upgrades[i];
                var row = new UpgradeRow { index = i };
                row.bg = UIFactory.Panel("melhoria " + u.displayName, content, Palette.UiCard);
                row.root = row.bg.gameObject;
                UIFactory.HLayout(row.root, 12, 12);
                row.icon = UIFactory.Icon(row.root.transform, u.icon, 54, true);
                var info = UIFactory.Rect("info", row.root.transform);
                UIFactory.VLayout(info.gameObject, 0, 3);
                UIFactory.LE(info.gameObject, 200, 200, -1, -1, 1);
                row.title = UIFactory.Label(info, u.displayName, 30, Palette.UiText);
                row.gain = UIFactory.Label(info, "", 30, Palette.UiMuted);
                row.buy = UIFactory.Button(row.root.transform, "10", Palette.UiButton, () => Buy(idx), 30);
                UIFactory.LE(row.buy.rt.gameObject, 150, 150, 72, 72, 0);
                AddTooltip(row.root, () => UpgradeTip(idx));
                upgradeRows.Add(row);
            }
        }

        string UpgradeTip(int i)
        {
            var u = gm.Config.upgrades[i];
            var eco = gm.Economy;
            int lv = eco.Level(i);
            string s = u.displayName + (lv > 0 ? "  Nv " + lv : "") + "\n" + u.description;
            if (!eco.IsUnlocked(i)) return s + "\n\nLibera com " + NumberFormat.Short(u.unlockAtProduced) + " de Energia produzida.";
            s += "\n\nPróximo nível: " + Gain(i);
            foreach (int m in u.visualMilestones)
                if (lv < m) { s += "\n" + (lv == 0 ? "Instala a estrutura no cenário." : "Muda o visual no Nv " + m + "."); break; }
            double cost = eco.Cost(i);
            if (gm.State.energy < cost) s += "\n<color=#E3826F>Faltam " + NumberFormat.Short(cost - gm.State.energy) + "</color>";
            return s;
        }

        void Buy(int index)
        {
            if (busy) return;
            if (gm.BuyUpgrade(index)) Play(Sfx.Buy); else Play(Sfx.Error);
            RefreshUpgrades();
        }

        void RefreshUpgrades()
        {
            var eco = gm.Economy;
            bool shownLocked = false;
            foreach (var row in upgradeRows)
            {
                var u = gm.Config.upgrades[row.index];
                int lv = eco.Level(row.index);
                bool unlocked = eco.IsUnlocked(row.index);
                bool visible = unlocked || !shownLocked;
                if (!unlocked) shownLocked = true;
                if (row.root.activeSelf != visible) row.root.SetActive(visible);
                if (!visible) continue;
                row.icon.sprite = Pinturas.Icon(unlocked ? u.icon : IconKind.Lock);
                row.title.text = u.displayName + (lv > 0 ? " <color=#9AA29E>" + lv + "</color>" : "");
                if (!unlocked)
                {
                    row.gain.text = "libera com " + NumberFormat.Short(u.unlockAtProduced);
                    row.buy.rt.gameObject.SetActive(false);
                    UIFactory.Tint(row.bg, Palette.UiCard.WithAlpha(0.6f));
                    continue;
                }
                UIFactory.Tint(row.bg, Palette.UiCard);
                row.buy.rt.gameObject.SetActive(true);
                row.gain.text = Gain(row.index);
                row.buy.label.text = NumberFormat.Short(eco.Cost(row.index));
                UIFactory.SetInteractable(row.buy, eco.CanBuy(row.index), Palette.UiButton);
            }
        }

        string Gain(int i)
        {
            var u = gm.Config.upgrades[i];
            var eco = gm.Economy;
            if (u.clickGain > 0) return "+" + NumberFormat.Short(eco.ClickGainPreview(i)) + "/clique";
            return "+" + NumberFormat.Short(eco.PassiveGainPreview(i)) + "/s";
        }

        // ---------- aba Áreas ----------
        void BuildAreasPage(RectTransform page)
        {
            var content = Content(page);
            foreach (var a in gm.Config.areas)
            {
                var def = a;
                var row = new AreaRow { def = a };
                row.bg = UIFactory.Panel("área " + a.displayName, content, Palette.UiCard);
                UIFactory.HLayout(row.bg.gameObject, 12, 12);
                row.icon = UIFactory.Icon(row.bg.transform, a.icon, 54, true);
                var info = UIFactory.Rect("info", row.bg.transform);
                UIFactory.VLayout(info.gameObject, 0, 3);
                UIFactory.LE(info.gameObject, 200, 200, -1, -1, 1);
                row.title = UIFactory.Label(info, a.displayName, 30, Palette.UiText);
                row.info = UIFactory.Label(info, "", 30, Palette.UiMuted);
                var buttons = UIFactory.Rect("botões", row.bg.transform);
                UIFactory.VLayout(buttons.gameObject, 0, 6);
                UIFactory.LE(buttons.gameObject, 150, 150, -1, -1, 0);
                row.open = UIFactory.Button(buttons, "Abrir", Palette.UiButton, () => OpenArea(def), 30);
                UIFactory.LE(row.open.rt.gameObject, 150, 150, 60, 60);
                row.go = UIFactory.Button(buttons, "Ver", Palette.UiCardLight, () => { Play(Sfx.Ui); selectedArea = def; plant.FocusOn(plant.Areas.CenterOf(def)); }, 30);
                UIFactory.LE(row.go.rt.gameObject, 150, 150, 60, 60);
                AddTooltip(row.bg.gameObject, () => def.displayName + "\n" + def.description + "\n\n" + BonusText(def) + " (permanente)\n" + AreaStatus(def));
                areaRows.Add(row);
            }
            UIFactory.Label(content, "Áreas abertas ficam abertas depois do Renascimento.", 30, Palette.UiMuted);
        }

        static string BonusText(AreaDef a)
        {
            string v = "+" + NumberFormat.Percent(a.bonusValue);
            switch (a.bonus)
            {
                case AreaBonus.ClickPercent: return v + " Energia por clique";
                case AreaBonus.PassivePercent: return v + " produção por segundo";
                default: return "−" + NumberFormat.Percent(a.bonusValue) + " no custo das melhorias";
            }
        }

        string AreaStatus(AreaDef a)
        {
            var eco = gm.Economy;
            if (eco.IsAreaOpen(a)) return "Aberta";
            if (!eco.IsAreaAvailable(a)) return "Libera com " + NumberFormat.Short(a.unlockAtProduced) + " de Energia produzida";
            return eco.CanOpenArea(a) ? "Pronta para abrir: " + NumberFormat.Short(a.cost) : "Abrir custa " + NumberFormat.Short(a.cost) + " (faltam " + NumberFormat.Short(a.cost - gm.State.energy) + ")";
        }

        void OpenArea(AreaDef a)
        {
            if (busy) return;
            if (gm.OpenArea(a)) plant.FocusOn(plant.Areas.CenterOf(a)); else Play(Sfx.Error);
            RefreshAreas();
        }

        void RefreshAreas()
        {
            var eco = gm.Economy;
            foreach (var row in areaRows)
            {
                bool open = eco.IsAreaOpen(row.def), avail = eco.IsAreaAvailable(row.def);
                row.icon.sprite = Pinturas.Icon(avail ? row.def.icon : IconKind.Lock);
                row.info.text = open ? BonusText(row.def) : avail ? BonusText(row.def) : "libera com " + NumberFormat.Short(row.def.unlockAtProduced);
                row.open.rt.gameObject.SetActive(!open);
                row.open.label.text = NumberFormat.Short(row.def.cost);
                UIFactory.SetInteractable(row.open, eco.CanOpenArea(row.def), Palette.UiButton);
                UIFactory.Tint(row.bg, row.def == selectedArea ? Palette.UiCardHighlight : Palette.UiCard);
                row.title.text = row.def.displayName + (open ? " <color=#86C995>aberta</color>" : "");
            }
        }

        /// <summary>Clique numa área do mapa.</summary>
        public void SelectArea(AreaDef a)
        {
            Play(Sfx.Ui);
            selectedArea = a;
            ShowTab(TabAreas);
        }

        // ---------- aba Eventos ----------
        void BuildEventsPage(RectTransform page)
        {
            var content = Content(page);
            var rep = UIFactory.Panel("reputação", content, Palette.UiCard);
            UIFactory.VLayout(rep.gameObject, 15, 6);
            repCardText = UIFactory.Label(rep.transform, "", 30, Palette.UiText);
            var barBg = UIFactory.Rect("barra", rep.transform);
            UIFactory.LE(barBg.gameObject, -1, -1, 12, 12);
            UIFactory.Img(barBg, Palette.UiCardLight, Art.Pixel);
            var fill = UIFactory.Stretch(UIFactory.Rect("nível", barBg));
            repCardFill = UIFactory.Img(fill, Palette.UiPositive, Art.Pixel);
            repCardFill.type = Image.Type.Filled; repCardFill.fillMethod = Image.FillMethod.Horizontal;
            repCardHint = UIFactory.Label(rep.transform, "", 30, Palette.UiMuted);

            eventSlot = UIFactory.Rect("evento", content);
            UIFactory.VLayout(eventSlot.gameObject, 0, 0);

            var eff = UIFactory.Panel("efeitos", content, Palette.UiCard);
            UIFactory.VLayout(eff.gameObject, 15, 6);
            UIFactory.Label(eff.transform, "Efeitos ativos", 30, Palette.UiText);
            effectsListText = UIFactory.Label(eff.transform, "", 30, Palette.UiMuted);
        }

        void RebuildEventCard()
        {
            for (int i = eventSlot.childCount - 1; i >= 0; i--) Destroy(eventSlot.GetChild(i).gameObject);
            choiceViews.Clear();
            eventStatusText = null; eventExpireText = null;
            var e = gm.Events.Pending;
            var card = UIFactory.Panel("cartão", eventSlot, e == null ? Palette.UiCard : Palette.UiCardLight);
            UIFactory.VLayout(card.gameObject, 15, 9);
            if (e == null) { eventStatusText = UIFactory.Label(card.transform, "", 30, Palette.UiMuted); return; }

            var head = UIFactory.Rect("cabeçalho", card.transform);
            UIFactory.HLayout(head.gameObject, 0, 12);
            UIFactory.Icon(head, e.icon, 54, true);
            var titles = UIFactory.Rect("títulos", head);
            UIFactory.VLayout(titles.gameObject, 0, 3);
            UIFactory.LE(titles.gameObject, 200, 200, -1, -1, 1);
            string tag = e.tone == EventTone.Positive ? "<color=#86C995>OPORTUNIDADE</color>" : e.tone == EventTone.Negative ? "<color=#E3826F>PROBLEMA</color>" : "<color=#C9CCC6>DECISÃO</color>";
            UIFactory.Label(titles, tag, 30, Palette.UiText);
            UIFactory.Label(titles, e.title, 30, Palette.UiText);
            UIFactory.Label(card.transform, e.body, 30, Palette.UiMuted);
            eventExpireText = UIFactory.Label(card.transform, "", 30, Palette.UiMuted);
            for (int i = 0; i < e.choices.Count; i++)
            {
                int idx = i;
                var c = e.choices[i];
                var bv = UIFactory.Button(card.transform, c.label, Palette.UiButton, () => Choose(idx), 30);
                var effect = UIFactory.Label(bv.rt, "", 30, Palette.UiText, TextAnchor.MiddleCenter);
                AddTooltip(bv.rt.gameObject, () => c.description);
                choiceViews.Add(new KeyValuePair<UIFactory.ButtonView, Text>(bv, effect));
            }
        }

        void Choose(int idx)
        {
            if (busy) return;
            if (gm.ChooseEvent(idx)) Play(Sfx.Buy); else Play(Sfx.Error);
            RefreshEvents();
        }

        void RefreshEvents()
        {
            var ev = gm.Events;
            var s = gm.State;
            float rep = ev.EffectiveReputation;
            repCardText.text = "Reputação " + Mathf.RoundToInt(rep) + "/100";
            repCardFill.fillAmount = rep / 100f;
            repCardFill.color = RepColor(rep);
            string hint = rep >= 65 ? "A imprensa está do seu lado." : rep >= 35 ? "A cidade observa com cautela. 65+ atrai a imprensa." : "Protestos e problemas ficam mais prováveis.";
            float temp = rep - s.reputation;
            if (Mathf.Abs(temp) > 0.5f) hint += "\n<color=#E3826F>" + (temp > 0 ? "+" : "") + Mathf.RoundToInt(temp) + " temporário</color>";
            repCardHint.text = hint;

            string key = s.HasPendingEvent ? s.pendingEventId : (s.eventsEnabled ? "calmo" : "bloqueado");
            if (key != builtEventKey) { builtEventKey = key; RebuildEventCard(); }
            if (eventStatusText != null)
                eventStatusText.text = !s.eventsEnabled ? "Os eventos começam com a primeira turbina." : "Tudo calmo. Próxima notícia em ~" + NumberFormat.Duration(Mathf.Max(1, s.eventTimer)) + ".";

            var e = ev.Pending;
            if (e != null && eventExpireText != null)
            {
                var def = e.choices.Find(c => c.isDefaultIfIgnored);
                eventExpireText.text = "Sem resposta em " + NumberFormat.Duration(gm.Config.pendingEventExpire - s.pendingEventAge) + (def != null ? ": \"" + def.label + "\"" : "");
                for (int i = 0; i < choiceViews.Count && i < e.choices.Count; i++)
                {
                    var c = e.choices[i];
                    var bv = choiceViews[i].Key;
                    bool can = ev.CanAfford(c);
                    string desc = ev.DescribeChoice(c);
                    if (!can) desc += "\nFaltam " + NumberFormat.Short(ev.ChoiceCost(c) - s.energy);
                    choiceViews[i].Value.text = desc;
                    bv.button.interactable = can;
                    UIFactory.Tint(bv.bg, can ? (i == 0 ? Palette.UiButton : Palette.UiCardHighlight) : Palette.UiButtonDisabled);
                    choiceViews[i].Value.color = can ? Palette.UiText : Palette.UiNegative;
                }
            }

            if (s.effects.Count == 0) effectsListText.text = "Nenhum.";
            else
            {
                var sb = new System.Text.StringBuilder();
                foreach (var fx in s.effects)
                {
                    bool good = fx.kind == EffectKind.ReputationTemp ? fx.value > 0 : fx.value >= 1f;
                    sb.Append(good ? "<color=#86C995>" : "<color=#E3826F>").Append(EventDirector.DescribeEffect(fx.kind, fx.value))
                      .Append("</color> · ").Append(NumberFormat.Duration(fx.remaining)).Append("\n");
                }
                effectsListText.text = sb.ToString().TrimEnd('\n');
            }
        }

        static Color RepColor(float rep) => rep >= 65 ? Palette.UiPositive : rep >= 35 ? Palette.ConcreteLight : Palette.UiNegative;

        // ---------- aba Renascimento ----------
        void BuildRebirthPage(RectTransform page)
        {
            var content = Content(page);
            var card = UIFactory.Panel("renascimento", content, Palette.UiCardLight);
            UIFactory.VLayout(card.gameObject, 15, 9);
            UIFactory.Label(card.transform, "Nova geração da usina: a campanha recomeça e a experiência vira Núcleos.", 30, Palette.UiMuted);
            rbProgressText = UIFactory.Label(card.transform, "", 30, Palette.UiText);
            var barBg = UIFactory.Rect("barra", card.transform);
            UIFactory.LE(barBg.gameObject, -1, -1, 15, 15);
            UIFactory.Img(barBg, Palette.UiCard, Art.Pixel);
            var fill = UIFactory.Stretch(UIFactory.Rect("nível", barBg));
            rbFill = UIFactory.Img(fill, Palette.UiPositive, Art.Pixel);
            rbFill.type = Image.Type.Filled; rbFill.fillMethod = Image.FillMethod.Horizontal;
            rbGainText = UIFactory.Label(card.transform, "", 30, Palette.UiText);
            rbNextText = UIFactory.Label(card.transform, "", 30, Palette.UiMuted);
            rbButton = UIFactory.Button(card.transform, "Renascer", Palette.UiButton, OpenConfirm, 30);

            rbOwnedText = UIFactory.Label(content, "", 30, Palette.UiText);
            foreach (var p in gm.Config.permanentUpgrades)
            {
                var def = p;
                var bg = UIFactory.Panel("permanente " + p.displayName, content, Palette.UiCard);
                UIFactory.HLayout(bg.gameObject, 12, 12);
                UIFactory.Icon(bg.transform, p.icon, 54, true);
                var info = UIFactory.Rect("info", bg.transform);
                UIFactory.VLayout(info.gameObject, 0, 3);
                UIFactory.LE(info.gameObject, 200, 200, -1, -1, 1);
                var row = new PermRow { def = p };
                row.title = UIFactory.Label(info, p.displayName, 30, Palette.UiText);
                row.desc = UIFactory.Label(info, "", 30, Palette.UiMuted);
                row.buy = UIFactory.Button(bg.transform, "1", Palette.UiButton, () => BuyPermanent(def), 30);
                UIFactory.LE(row.buy.rt.gameObject, 120, 120, 72, 72, 0);
                AddTooltip(bg.gameObject, () => def.displayName + "\n" + def.description);
                permRows.Add(row);
            }
            var stats = UIFactory.Panel("estatísticas", content, Palette.UiCard);
            UIFactory.VLayout(stats.gameObject, 15, 6);
            UIFactory.Label(stats.transform, "Histórico", 30, Palette.UiText);
            statsText = UIFactory.Label(stats.transform, "", 30, Palette.UiMuted);
        }

        void BuyPermanent(PermanentUpgradeDef p)
        {
            if (busy) return;
            if (gm.BuyPermanent(p)) { Play(Sfx.Install); Toast(p.displayName + " ativado"); } else Play(Sfx.Error);
            RefreshRebirth();
        }

        void RefreshRebirth()
        {
            var rb = gm.Rebirth;
            var s = gm.State;
            double th = gm.Config.rebirthThreshold;
            rbProgressText.text = "Produzido: " + NumberFormat.Short(s.campaignProduced) + " / " + NumberFormat.Short(th);
            rbFill.fillAmount = (float)rb.Progress01;
            int pending = rb.PendingNucleos;
            rbGainText.text = pending > 0 ? "Renascer agora: +" + pending + " Núcleo" + (pending > 1 ? "s" : "") : "Libera com " + NumberFormat.Full(th) + " produzidos";
            rbNextText.text = pending > 0 ? "Próximo Núcleo em " + NumberFormat.Short(rb.NextNucleoAt) + " (faltam " + NumberFormat.Short(rb.RemainingForNextNucleo) + ")" : "Faltam " + NumberFormat.Short(th - s.campaignProduced);
            UIFactory.SetInteractable(rbButton, rb.CanRebirth && !busy, Palette.UiButton);
            rbOwnedText.text = "Núcleos: " + s.nucleos;
            foreach (var row in permRows)
            {
                int lv = rb.PermanentLevel(row.def);
                bool maxed = rb.PermanentMaxed(row.def);
                row.title.text = row.def.displayName + (lv > 0 ? " <color=#9AA29E>" + lv + "</color>" : "");
                row.desc.text = maxed ? RebirthSystem.DescribePermanent(row.def, lv) : RebirthSystem.DescribePermanent(row.def, lv + 1);
                int cost = rb.PermanentCost(row.def);
                row.buy.label.text = maxed ? "Máx." : cost + " N";
                UIFactory.SetInteractable(row.buy, rb.CanBuyPermanent(row.def) && !busy, Palette.UiButton);
            }
            statsText.text =
                "Geração " + PlantView.Roman(s.generation) + "\n" +
                "Total produzido: " + NumberFormat.Short(s.totalProducedAllTime) + "\n" +
                "Cliques: " + NumberFormat.Full(s.totalTaps) + "\n" +
                "Renascimentos: " + s.rebirths + " · Núcleos: " + s.nucleosEarnedTotal + "\n" +
                "Melhor campanha: " + NumberFormat.Short(s.bestCampaignProduced) + "\n" +
                "Mais rápido: " + (s.fastestRebirthSeconds > 0 ? NumberFormat.Duration((float)s.fastestRebirthSeconds) : "—") + "\n" +
                "Tempo de jogo: " + NumberFormat.Duration((float)s.playSeconds);
        }

        // ---------- modais ----------
        GameObject Modal(string name, out RectTransform card)
        {
            var root = UIFactory.Stretch(UIFactory.Rect(name, modalLayer));
            UIFactory.Img(root, new Color(0, 0, 0, 0.7f), Art.Pixel, true);
            card = UIFactory.Rect("cartão", root);
            UIFactory.Anchor(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 0));
            UIFactory.Img(card, Palette.UiCardLight, Art.RoundRect, true);
            UIFactory.VLayout(card.gameObject, 24, 12);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        void BuildSettingsModal()
        {
            settingsModal = Modal("Configurações", out var card);
            UIFactory.Label(card, "Configurações", 50, Palette.UiText, TextAnchor.MiddleCenter);
            soundLabel = UIFactory.Button(card, "Som: ligado", Palette.UiButton, ToggleSound, 30).label;
            UIFactory.Button(card, "Salvar e recarregar", Palette.UiCardHighlight, () =>
            {
                gm.SaveNow(); gm.ReloadFromDisk(); plant?.RefreshAll(true);
                Toast("Progresso recarregado"); CloseModals();
            }, 30);
            resetLabel = UIFactory.Button(card, "Apagar progresso", Palette.UiNegative.WithAlpha(0.8f), ResetPressed, 30).label;
            string help = "Clique no reator ou aperte Espaço. Arraste o cenário para andar pelo mapa; roda do mouse para zoom; A/D ou setas também movem.";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            help += "\nTeste: F1 +1 mil · F2 +25 mil · F3 evento · F4 recarregar";
#endif
            UIFactory.Label(card, help, 30, Palette.UiMuted);
            UIFactory.Button(card, "Fechar", Palette.UiCard, CloseModals, 30);
        }

        void BuildConfirmModal()
        {
            confirmModal = Modal("Confirmar Renascimento", out var card);
            UIFactory.Label(card, "Nova geração?", 50, Palette.UiText, TextAnchor.MiddleCenter);
            confirmBody = UIFactory.Label(card, "", 30, Palette.UiText);
            UIFactory.Button(card, "Renascer agora", Palette.UiButton, ConfirmRebirth, 30);
            UIFactory.Button(card, "Ainda não", Palette.UiCard, CloseModals, 30);
        }

        void OpenSettings()
        {
            Play(Sfx.Ui);
            resetArmed = false;
            resetLabel.text = "Apagar progresso";
            soundLabel.text = gm.State.soundOn ? "Som: ligado" : "Som: desligado";
            settingsModal.SetActive(true);
        }

        void ToggleSound()
        {
            gm.SetSound(!gm.State.soundOn);
            soundLabel.text = gm.State.soundOn ? "Som: ligado" : "Som: desligado";
            Play(Sfx.Ui);
        }

        void ResetPressed()
        {
            if (!resetArmed) { resetArmed = true; resetLabel.text = "Clique de novo para apagar tudo"; return; }
            gm.ResetAllProgress();
            plant?.RefreshAll(true);
            CloseModals();
            Toast("Progresso apagado");
        }

        void OpenConfirm()
        {
            if (!gm.Rebirth.CanRebirth || busy) return;
            Play(Sfx.Ui);
            int n = gm.Rebirth.PendingNucleos;
            int total = gm.State.nucleos + n;
            var sb = new System.Text.StringBuilder();
            sb.Append("Você recebe ").Append(n).Append(" Núcleo").Append(n > 1 ? "s" : "").Append(" (total ").Append(total).Append(").\n\n");
            foreach (var p in gm.Config.permanentUpgrades)
            {
                if (gm.Rebirth.PermanentMaxed(p)) continue;
                int cost = gm.Rebirth.PermanentCost(p);
                sb.Append(cost <= total ? "• " : "<color=#9AA29E>• ").Append(p.displayName).Append(": ").Append(RebirthSystem.DescribePermanent(p, gm.Rebirth.PermanentLevel(p) + 1))
                  .Append(" (").Append(cost).Append(" N)").Append(cost <= total ? "" : "</color>").Append("\n");
            }
            sb.Append("\n<color=#E3826F>Recomeça:</color> Energia (").Append(NumberFormat.Short(gm.State.energy)).Append("), produção da campanha, ")
              .Append(gm.Economy.TotalUpgradeLevels()).Append(" níveis de melhorias, reputação e eventos.\n");
            sb.Append("<color=#86C995>Fica:</color> Núcleos, melhorias permanentes, áreas abertas e histórico.");
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
            plant.FocusX(0f);
            StartCoroutine(Routine());

            System.Collections.IEnumerator Routine()
            {
                yield return plant.RebirthSequence(() => { gained = gm.DoRebirth(); builtEventKey = null; RefreshAll(); }, SetCaption);
                busy = false;
                ShowTab(TabRebirth);
                Toast("+" + gained + " Núcleo" + (gained > 1 ? "s" : ""));
                RefreshAll();
            }
        }

        void CloseModals()
        {
            settingsModal.SetActive(false);
            confirmModal.SetActive(false);
        }

        // ---------- avisos ----------
        void BuildCaptionAndToast()
        {
            captionText = UIFactory.Label(safe, "", 50, Palette.UiText, TextAnchor.MiddleCenter);
            UIFactory.Anchor((RectTransform)captionText.transform, new Vector2(0, 0.6f), new Vector2(1, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(-panelWidth * 0.5f, 0), new Vector2(-panelWidth - 60, 120));
            var cs = captionText.gameObject.AddComponent<Outline>();
            cs.effectColor = new Color(0.1f, 0.07f, 0.04f, 0.9f);
            cs.effectDistance = new Vector2(2f, -2f);
            captionText.gameObject.SetActive(false);

            var toast = UIFactory.Rect("Aviso", safe);
            UIFactory.Anchor(toast, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-panelWidth * 0.5f, 126), new Vector2(0, 0));
            toastBg = UIFactory.Img(toast, Palette.UiCardHighlight.WithAlpha(0.96f));
            UIFactory.VLayout(toast.gameObject, 15, 0, TextAnchor.MiddleCenter);
            var fit = toast.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            toastText = UIFactory.Label(toast, "", 30, Palette.UiText, TextAnchor.MiddleCenter);
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
            toastTimer = 3f;
        }

        // ---------- textos flutuantes ----------
        void BuildFloaters()
        {
            for (int i = 0; i < 14; i++)
            {
                var t = UIFactory.Label(floatLayer, "", 50, Palette.UiText, TextAnchor.MiddleCenter);
                var ol = t.gameObject.AddComponent<Outline>();
                ol.effectColor = new Color(0.12f, 0.08f, 0.05f, 0.85f);
                ol.effectDistance = new Vector2(2f, -2f);
                var rt = (RectTransform)t.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(420, 66);
                t.gameObject.SetActive(false);
                floaters.Add(new Floater { rt = rt, text = t });
            }
        }

        void SpawnFloater(Vector2 screenPos, double value)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(floatLayer, screenPos, UiCam, out var local);
            float now = Time.unscaledTime;
            Floater recent = null;
            foreach (var f in floaters)
                if (f.active && now - f.born < 0.25f && (f.start - local).sqrMagnitude < 200f * 200f && (recent == null || f.born > recent.born))
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
            use.start = local + new Vector2(UnityEngine.Random.Range(-30f, 30f), 30f);
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
                float rise = Mathf.Round(150f * (1f - (1f - t) * (1f - t)) / 3f) * 3f;
                f.rt.anchoredPosition = f.start + new Vector2(0, rise);
                f.text.color = t < 0.75f ? Palette.UiText : Palette.UiMuted;
            }
        }

        // ---------- mouse no cenário ----------
        Vector2 ScreenToViewport(Vector2 screen) => new Vector2(screen.x / Mathf.Max(1, PixelWidth), screen.y / Mathf.Max(1, PixelHeight));

        /// <summary>Retorna true se o ponto da tela está sobre o reator.</summary>
        public bool IsOverReactor(Vector2 screen)
        {
            if (plant == null || plant.Cam == null) return false;
            Vector3 w = plant.ViewportToWorld(ScreenToViewport(screen));
            return plant.ReactorWorldRect.Contains(new Vector2(w.x, w.y));
        }

        public void WorldPointerDown(Vector2 screen)
        {
            if (IsOverReactor(screen)) DoTap(screen);
        }

        public void WorldClick(Vector2 screen)
        {
            if (IsOverReactor(screen) || plant.Areas == null) return;
            Vector3 w = plant.ViewportToWorld(ScreenToViewport(screen));
            var area = plant.Areas.AreaAt(new Vector2(w.x, w.y));
            if (area != null) SelectArea(area);
        }

        public void WorldDrag(Vector2 screenDelta) => plant.PanByScreen(screenDelta, PixelHeight);
        public void WorldScroll(float delta) { if (Mathf.Abs(delta) > 0.01f) plant.ZoomBy(Mathf.Sign(delta)); }

        public string WorldHover(Vector2 screen)
        {
            if (IsOverReactor(screen)) return "Reator 1 · clique para gerar Energia";
            if (plant.Areas == null) return null;
            Vector3 w = plant.ViewportToWorld(ScreenToViewport(screen));
            var a = plant.Areas.AreaAt(new Vector2(w.x, w.y));
            return a == null ? null : a.displayName + "\n" + AreaStatus(a);
        }

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
            HandleKeyboard(dt);
            UpdateHud();
            UpdateFloaters(dt);
            UpdateTooltip();
            if (toastTimer > 0) { toastTimer -= dt; if (toastTimer <= 0) toastBg.gameObject.SetActive(false); }
            refreshTimer -= dt;
            if (refreshTimer <= 0)
            {
                refreshTimer = 0.1f;
                RefreshCurrentTab();
                UpdateBadges();
                UpdateMapBar();
            }
        }

        void LateUpdate()
        {
            if (gm == null || plant == null || plant.Cam == null) return;
            // fração da tela coberta pelo painel, para centralizar as áreas no espaço livre
            var corners = new Vector3[4];
            panel.GetWorldCorners(corners);
            float left = RectTransformUtility.WorldToScreenPoint(UiCam, corners[0]).x;
            plant.rightOccluded = Mathf.Clamp01(1f - left / Mathf.Max(1, PixelWidth));

            bool showTutorial = !gm.State.tutorialDone && !busy;
            if (tutorial.gameObject.activeSelf != showTutorial) tutorial.gameObject.SetActive(showTutorial);
            if (showTutorial)
            {
                Rect wr = plant.ReactorWorldRect;
                Vector2 v = plant.WorldToViewport(new Vector3(wr.center.x, wr.yMax + 0.2f, 0));
                Vector2 screen = new Vector2(v.x * PixelWidth, v.y * PixelHeight);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(worldLayer, screen, UiCam, out var local);
                tutorialText.text = gm.Economy.CanBuy(0) ? "Compre o " + gm.Config.upgrades[0].displayName + " no painel →" : "Clique no reator";
                float bob = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f ? 0 : 6;
                tutorial.anchoredPosition = local + worldLayer.rect.size * 0.5f + new Vector2(0, bob);
            }
        }

        void HandleKeyboard(float dt)
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            bool modal = settingsModal.activeSelf || confirmModal.activeSelf;
            if (kb.spaceKey.wasPressedThisFrame && !modal)
            {
                Rect wr = plant.ReactorWorldRect;
                Vector2 v = plant.WorldToViewport(wr.center);
                DoTap(new Vector2(v.x * PixelWidth, v.y * PixelHeight));
            }
            float pan = 0;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) pan -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) pan += 1;
            if (pan != 0 && !modal) plant.PanBy(new Vector2(pan * 14f * dt, 0));
            if (kb.escapeKey.wasPressedThisFrame) CloseModals();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (kb.f1Key.wasPressedThisFrame) { gm.DevAddEnergy(1000); Toast("[Teste] +1 mil"); }
            if (kb.f2Key.wasPressedThisFrame) { gm.DevAddEnergy(25000); Toast("[Teste] +25 mil"); }
            if (kb.f3Key.wasPressedThisFrame) { gm.DevTriggerEvent(); Toast("[Teste] evento"); }
            if (kb.f4Key.wasPressedThisFrame) { gm.SaveNow(); gm.ReloadFromDisk(); plant?.RefreshAll(true); Toast("[Teste] save recarregado"); }
#endif
        }

        void UpdateHud()
        {
            var s = gm.State;
            var eco = gm.Economy;
            balanceText.text = NumberFormat.Short(s.energy);
            clickText.text = "+" + NumberFormat.Short(eco.ClickValue) + "/clique";
            passiveText.text = "+" + NumberFormat.Short(eco.PassivePerSecond) + "/s";
            float rep = gm.Events.EffectiveReputation;
            repText.text = "Rep. " + Mathf.RoundToInt(rep);
            repFill.fillAmount = rep / 100f;
            repFill.color = RepColor(rep);

            var e = gm.Events.Pending;
            bool showHead = e != null && !busy;
            if (headline.gameObject.activeSelf != showHead) headline.gameObject.SetActive(showHead);
            if (showHead)
            {
                headlineIcon.sprite = Pinturas.Icon(e.icon);
                headlineText.text = e.headline;
                bool blink = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f;
                UIFactory.Tint(headlineBg, blink ? Palette.UiCardHighlight : Palette.UiCard);
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
                    sb.Append(good ? "<color=#86C995>" : "<color=#E3826F>").Append(EventDirector.DescribeEffect(fx.kind, fx.value)).Append("</color> · ").Append(NumberFormat.Duration(fx.remaining));
                }
                effectsText.text = sb.ToString();
            }
        }

        void UpdateMapBar()
        {
            foreach (var kv in mapButtons)
            {
                var a = kv.Key;
                if (a == null) continue;
                bool open = gm.Economy.IsAreaOpen(a), avail = gm.Economy.IsAreaAvailable(a);
                var img = kv.Value.rt.GetComponentsInChildren<Image>()[1];
                img.sprite = Pinturas.Icon(open || avail ? a.icon : IconKind.Lock);
                UIFactory.Tint(kv.Value.bg, open ? Palette.UiCardLight : avail ? Palette.UiButton : Palette.UiCard);
            }
        }

        void UpdateBadges()
        {
            bool anyUpgrade = false;
            for (int i = 0; i < gm.Config.upgrades.Count; i++) if (gm.Economy.CanBuy(i)) anyUpgrade = true;
            bool anyArea = false;
            foreach (var a in gm.Config.areas) if (gm.Economy.CanOpenArea(a)) anyArea = true;
            bool canSpend = false;
            foreach (var p in gm.Config.permanentUpgrades) if (gm.Rebirth.CanBuyPermanent(p)) canSpend = true;
            tabBadges[TabUpgrades].enabled = anyUpgrade && currentTab != TabUpgrades;
            tabBadges[TabAreas].enabled = anyArea && currentTab != TabAreas;
            tabBadges[TabEvents].enabled = gm.State.HasPendingEvent && currentTab != TabEvents;
            tabBadges[TabRebirth].enabled = (gm.Rebirth.CanRebirth || canSpend) && currentTab != TabRebirth;
        }

        void ShowTab(int tab)
        {
            currentTab = tab;
            tabTitle.text = TabNames[tab];
            for (int i = 0; i < 4; i++)
            {
                pages[i].gameObject.SetActive(i == tab);
                UIFactory.Tint(tabButtons[i].bg, i == tab ? Palette.UiCardHighlight : Palette.UiCard);
            }
            RefreshCurrentTab();
        }

        void RefreshCurrentTab()
        {
            switch (currentTab)
            {
                case TabUpgrades: RefreshUpgrades(); break;
                case TabAreas: RefreshAreas(); break;
                case TabEvents: RefreshEvents(); break;
                default: RefreshRebirth(); break;
            }
        }

        void RefreshAll()
        {
            RefreshUpgrades();
            RefreshAreas();
            RefreshEvents();
            RefreshRebirth();
        }

        // usado pelo componente de entrada do cenário
        public void SetWorldHover(Func<string> provider) => hoverText = provider;
        public void ClearWorldHover(Func<string> provider) { if (hoverText == provider) hoverText = null; }
    }

    /// <summary>Dispara callbacks quando o mouse entra ou sai de um elemento.</summary>
    public class HoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action onEnter, onExit;
        public void OnPointerEnter(PointerEventData e) => onEnter?.Invoke();
        public void OnPointerExit(PointerEventData e) => onExit?.Invoke();
        void OnDisable() => onExit?.Invoke();
    }

    /// <summary>
    /// Camada invisível sobre o cenário: clique no reator gera Energia, arrastar move a câmera,
    /// roda faz zoom e clicar numa área abre os detalhes dela.
    /// </summary>
    public class WorldInput : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IScrollHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        public GameUI ui;
        bool inside;
        Func<string> hover;

        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) ui.WorldPointerDown(e.position); }
        public void OnPointerClick(PointerEventData e) { if (!e.dragging && e.button == PointerEventData.InputButton.Left) ui.WorldClick(e.position); }
        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e) => ui.WorldDrag(e.delta);
        public void OnScroll(PointerEventData e) => ui.WorldScroll(e.scrollDelta.y);

        public void OnPointerEnter(PointerEventData e)
        {
            inside = true;
            hover = () => inside && Mouse.current != null ? ui.WorldHover(Mouse.current.position.ReadValue()) : null;
            ui.SetWorldHover(hover);
        }

        public void OnPointerExit(PointerEventData e) { inside = false; ui.ClearWorldHover(hover); }
    }
}
