using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NucleoVivo
{
    /// <summary>
    /// Monta e anima o cenário 2D: céu, cidade em bairros, colinas, rio, usina e estruturas.
    /// Cada melhoria corresponde a uma estrutura com até 3 etapas visuais.
    /// </summary>
    public class PlantView : MonoBehaviour
    {
        [Header("Câmera")]
        [Tooltip("Meia largura do mundo que deve caber na tela.")] public float halfWorldWidth = 5.3f;
        [Tooltip("Ponto do mundo que fica no centro da área livre entre o topo e o painel.")] public float focusY = 0.35f;
        [Range(0, 1)] public float viewportBottom = 0.42f; // a interface ajusta em tempo real
        [Range(0, 1)] public float viewportTop = 0.86f;

        public Camera Cam { get; private set; }
        public Rect ReactorWorldRect => new Rect(reactorPos.x - 1.25f, reactorPos.y, 2.5f, 2.9f);

        // ---------- interno ----------
        class Structure
        {
            public GameObject root;
            public readonly List<KeyValuePair<SpriteRenderer, Color>> parts = new List<KeyValuePair<SpriteRenderer, Color>>();
            public readonly List<GameObject>[] tierObjects = { new List<GameObject>(), new List<GameObject>(), new List<GameObject>(), new List<GameObject>() };
            public int tier = -1;
            public bool ghost;
            public float pop; // animação de instalação
            public Vector3 baseScale = Vector3.one;
        }

        GameManager gm;
        Transform worldRoot;
        Material spriteMat;
        readonly Vector2 reactorPos = new Vector2(0f, -1.55f);

        Structure[] structures;
        SpriteRenderer reactorHalo, reactorStripe, riverGlow, darkOverlay, flashOverlay;
        readonly List<SpriteRenderer> reactorLights = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> domeLights = new List<SpriteRenderer>();
        Transform reactorTf;
        SpriteRenderer signText;
        int signGeneration = -1;
        SpriteRenderer signBoard;

        readonly List<Transform> rotors = new List<Transform>();
        readonly List<SpriteRenderer> monitors = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> blinkers = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> substationLines = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> regionalLines = new List<SpriteRenderer>();
        readonly List<Transform> steamSources = new List<Transform>();
        readonly List<Steam> steam = new List<Steam>();
        readonly List<SpriteRenderer> ripples = new List<SpriteRenderer>();
        readonly List<List<Window>> districts = new List<List<Window>>();
        readonly List<SpriteRenderer> stars = new List<SpriteRenderer>();

        class Steam { public SpriteRenderer sr; public float life, maxLife; public Vector2 vel; public float size; public bool active; }
        class Window { public SpriteRenderer sr; public bool lit; public bool stray; public float phase; public float target; }

        float tapFlash, installFlash, shake;
        int litDistricts = -1;
        float darkness; // 0..1 para a transição de Renascimento
        bool transitioning;
        System.Random rnd = new System.Random(7);

        // ---------- ciclo de vida ----------
        void Start()
        {
            gm = GameManager.Instance;
            spriteMat = gm != null ? gm.Config.spriteMaterial : null;
            Cam = Camera.main;
            if (Cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                Cam = camGo.AddComponent<Camera>();
            }
            Cam.orthographic = true;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = Palette.SkyTop;
            Cam.cullingMask &= ~(1 << 5); // a câmera do mundo não desenha a interface
            BuildPresenter();

            Build();
            if (gm != null)
            {
                gm.Tapped += _ => Pulse();
                gm.StructureTierChanged += OnTierChanged;
                gm.StateReloaded += () => RefreshAll(true);
                gm.PermanentBought += () => RefreshAll(false);
            }
            RefreshAll(true);
        }

        // ---------- pixel art: o mundo é desenhado numa textura pequena e ampliado sem filtro ----------
        [Header("Pixel art")]
        [Tooltip("Largura alvo, em pixels, da imagem do cenário (o resto é ampliação inteira).")]
        public int targetPixelWidth = 270;

        Camera presentCam;
        RawImage presentImage;
        RenderTexture lowRes;

        /// <summary>Câmera que desenha a imagem ampliada na tela (e a interface nas capturas).</summary>
        public Camera PresentCamera => presentCam;

        void BuildPresenter()
        {
            var go = new GameObject("Câmera de apresentação");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(5000, 5000, -10);
            presentCam = go.AddComponent<Camera>();
            presentCam.orthographic = true;
            presentCam.clearFlags = CameraClearFlags.SolidColor;
            presentCam.backgroundColor = Color.black;
            presentCam.cullingMask = 1 << 5;
            presentCam.depth = Cam.depth + 10;
            presentCam.nearClipPlane = 0.1f; presentCam.farClipPlane = 200f;

            var canvasGo = new GameObject("Imagem do cenário", typeof(RectTransform));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = presentCam;
            canvas.planeDistance = 100f;
            canvas.sortingOrder = -100;
            var imgGo = new GameObject("pixels", typeof(RectTransform));
            imgGo.layer = 5;
            imgGo.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            presentImage = imgGo.AddComponent<RawImage>();
            presentImage.raycastTarget = false;
            EnsureLowRes();
        }

        Vector2Int OutputSize()
        {
            var t = presentCam != null ? presentCam.targetTexture : null;
            return t != null ? new Vector2Int(t.width, t.height) : new Vector2Int(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        }

        void EnsureLowRes()
        {
            var o = OutputSize();
            int scale = Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(o.x / (float)targetPixelWidth, o.y / (targetPixelWidth * 16f / 9f))));
            int w = Mathf.CeilToInt(o.x / (float)scale), h = Mathf.CeilToInt(o.y / (float)scale);
            if (lowRes != null && lowRes.width == w && lowRes.height == h) return;
            if (lowRes != null) { Cam.targetTexture = null; lowRes.Release(); Destroy(lowRes); }
            lowRes = new RenderTexture(w, h, 16) { filterMode = FilterMode.Point, name = "cenário em pixels" };
            Cam.targetTexture = lowRes;
            presentImage.texture = lowRes;
        }

        /// <summary>Converte um ponto do mundo para a fração (0..1) da tela.</summary>
        public Vector2 WorldToViewport(Vector3 world) => Cam.WorldToViewportPoint(world);

        void OnDestroy()
        {
            if (lowRes != null) lowRes.Release();
        }

        void LateUpdate()
        {
            if (presentCam != null) EnsureLowRes();
            FitCamera();
            Animate(Time.deltaTime);
            if (gm != null && !transitioning) RefreshUnlockGhosts();
        }

        void FitCamera()
        {
            if (Cam == null) return;
            float aspect = Mathf.Max(0.3f, Cam.aspect);
            float visibleFrac = Mathf.Max(0.25f, viewportTop - viewportBottom);
            // garante que a largura caiba e que a área livre mostre ~8 unidades de altura
            float sizeByWidth = halfWorldWidth / aspect;
            float sizeByHeight = 4.4f / visibleFrac;
            float size = Mathf.Max(sizeByWidth, Mathf.Min(sizeByHeight, sizeByWidth * 1.6f));
            Cam.orthographicSize = size;
            float centerFrac = (viewportBottom + viewportTop) * 0.5f;
            float y = focusY - (centerFrac - 0.5f) * 2f * size;
            Vector2 sh = shake > 0 ? UnityEngine.Random.insideUnitCircle * shake * 0.08f : Vector2.zero;
            float unit = 2f * size / Mathf.Max(1, Cam.pixelHeight); // tamanho de 1 pixel no mundo
            Vector2 pos = new Vector2(sh.x, y + sh.y);
            pos = new Vector2(Mathf.Round(pos.x / unit) * unit, Mathf.Round(pos.y / unit) * unit);
            Cam.transform.position = new Vector3(pos.x, pos.y, -10f);
        }

        // ---------- construção ----------
        SpriteRenderer Make(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 scale, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (spriteMat != null) sr.sharedMaterial = spriteMat;
            return sr;
        }

        SpriteRenderer Part(Structure s, string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 scale, Color color, int order, int tier = 1)
        {
            var sr = Make(name, parent, sprite, pos, scale, color, order);
            s.parts.Add(new KeyValuePair<SpriteRenderer, Color>(sr, color));
            s.tierObjects[Mathf.Clamp(tier, 1, 3)].Add(sr.gameObject);
            return sr;
        }

        Structure NewStructure(string name, Vector2 pos)
        {
            var s = new Structure { root = new GameObject(name) };
            s.root.transform.SetParent(worldRoot, false);
            s.root.transform.localPosition = pos;
            return s;
        }

        /// <summary>Prédio simples com fachada, telhado em perspectiva e lateral.</summary>
        void BuildingBlock(Structure s, Transform t, Vector2 pos, float w, float h, int order, int tier, Color front, out float roofY)
        {
            Part(s, "fachada", t, Art.PixelBottom, pos, new Vector2(w, h), front, order, tier);
            Part(s, "lateral", t, Art.PixelBottom, pos + new Vector2(w * 0.5f + 0.08f, 0.06f), new Vector2(0.16f, h), Color.Lerp(front, Color.black, 0.35f), order - 1, tier);
            Part(s, "telhado", t, Art.PixelBottom, pos + new Vector2(0.08f, h), new Vector2(w + 0.16f, 0.14f), Palette.Roof, order + 1, tier);
            roofY = pos.y + h + 0.14f;
        }

        void Build()
        {
            worldRoot = new GameObject("Mundo").transform;
            worldRoot.SetParent(transform, false);

            // Céu e estrelas
            Make("Céu", worldRoot, Art.SkyGradient, new Vector2(0, -4.5f), Vector2.one, Color.white, -100);
            Make("Céu baixo", worldRoot, Art.PixelBottom, new Vector2(0, -24f), new Vector2(40, 19.6f), Palette.SkyHorizon, -101);
            for (int i = 0; i < 40; i++)
            {
                float x = (float)(rnd.NextDouble() * 12 - 6), y = 5.2f + (float)rnd.NextDouble() * 6f;
                float sz = rnd.NextDouble() < 0.2 ? 2f / Art.WorldPPU : 1f / Art.WorldPPU;
                stars.Add(Make("estrela", worldRoot, Art.Pixel, new Vector2(x, y), new Vector2(sz, sz), Color.white, -99));
            }

            BuildCity();

            // Colinas
            Make("Colinas distantes", worldRoot, Art.Hills(3, Palette.HillFar, Color.Lerp(Palette.HillFar, Palette.SkyHorizon, 0.6f)), new Vector2(0, 1.15f), Vector2.one, Color.white, -80);
            Make("Colinas", worldRoot, Art.Hills(11, Palette.HillMid, Palette.GreenLight), new Vector2(0.6f, 0.3f), Vector2.one, Color.white, -75);
            for (int i = 0; i < 22; i++)
            {
                float x = -6.5f + i * 0.62f + (float)rnd.NextDouble() * 0.3f;
                if (Mathf.Abs(x) < 3.5f && rnd.NextDouble() < 0.6) continue;
                float sc = rnd.NextDouble() < 0.5 ? 0.5f : 0.75f;
                Make("árvore", worldRoot, Art.Tree, new Vector2(x, 1.0f + (float)rnd.NextDouble() * 0.5f), new Vector2(sc, sc), Color.white, -74);
            }

            // Terreno, rio e margem
            Make("Terreno", worldRoot, Art.PixelBottom, new Vector2(0, -2.7f), new Vector2(16, 3.9f), Palette.GreenDark, -70);
            Make("Plataforma", worldRoot, Art.PixelBottom, new Vector2(0, -2.25f), new Vector2(9.6f, 2.75f), Palette.ConcreteShadow, -69);
            Make("Plataforma topo", worldRoot, Art.PixelBottom, new Vector2(0, -2.1f), new Vector2(9.4f, 2.5f), Color.Lerp(Palette.ConcreteDark, Palette.Petrol, 0.25f), -68);
            for (int i = -4; i <= 4; i++)
                Make("faixa", worldRoot, Art.Pixel, new Vector2(i * 1.05f, -2.0f), new Vector2(0.5f, 0.04f), Palette.Amber.WithAlpha(0.25f), -67);

            Make("Rio", worldRoot, Art.PixelBottom, new Vector2(0, -3.85f), new Vector2(16, 1.25f), Palette.River, -12);
            Make("Margem", worldRoot, Art.PixelBottom, new Vector2(0, -2.62f), new Vector2(16, 0.1f), Palette.RiverLight.WithAlpha(0.6f), -11);
            riverGlow = Make("Reflexo do reator", worldRoot, Art.Glow, new Vector2(reactorPos.x, -3.2f), new Vector2(3.2f, 0.7f), Palette.Cyan.WithAlpha(0.12f), -10);
            for (int i = 0; i < 14; i++)
            {
                var r = Make("ondulação", worldRoot, Art.Pixel, new Vector2((float)(rnd.NextDouble() * 12 - 6), -2.85f - (float)rnd.NextDouble() * 0.9f),
                    new Vector2(Mathf.Round((0.3f + (float)rnd.NextDouble() * 0.5f) * Art.WorldPPU) / Art.WorldPPU, 1f / Art.WorldPPU), Palette.RiverLight, -9);
                ripples.Add(r);
            }
            Make("Margem próxima", worldRoot, Art.PixelBottom, new Vector2(0, -6.2f), new Vector2(16, 2.4f), Palette.GreenDark, -5);
            for (int i = 0; i < 8; i++)
            {
                float x = -5.6f + i * 1.6f + (float)rnd.NextDouble() * 0.5f;
                float sc = rnd.NextDouble() < 0.5 ? 1f : 1.25f;
                Make("árvore próxima", worldRoot, Art.Tree, new Vector2(x, -4.1f), new Vector2(sc, sc), new Color(0.7f, 0.7f, 0.7f, 1f), -4);
            }

            structures = new Structure[7];
            structures[(int)StructureKind.SecondReactor] = BuildSecondReactor();
            structures[(int)StructureKind.CoolingTower] = BuildCoolingTower();
            structures[(int)StructureKind.RegionalGrid] = BuildRegionalGrid();
            structures[(int)StructureKind.Substation] = BuildSubstation();
            structures[(int)StructureKind.ControlRoom] = BuildControlRoom();
            structures[(int)StructureKind.Turbine] = BuildTurbineHall();
            BuildReactor();
            structures[(int)StructureKind.Automation] = BuildAutomation();
            BuildSign();

            // Efeitos de tela
            flashOverlay = Make("Clarão", worldRoot, Art.Pixel, Vector2.zero, new Vector2(60, 60), Palette.Cyan.WithAlpha(0), 40);
            darkOverlay = Make("Escuridão", worldRoot, Art.Pixel, Vector2.zero, new Vector2(60, 60), new Color(0.01f, 0.03f, 0.05f, 0), 30);
            for (int i = 0; i < 18; i++)
            {
                var sr = Make("vapor", worldRoot, Art.Circle, Vector2.zero, Vector2.one, Color.white.WithAlpha(0), -44);
                sr.gameObject.SetActive(false);
                steam.Add(new Steam { sr = sr });
            }
        }

        void BuildCity()
        {
            var cityRoot = new GameObject("Cidade").transform;
            cityRoot.SetParent(worldRoot, false);
            int[] order = { 2, 1, 3, 0, 4 }; // bairros acendem do centro para fora
            var temp = new List<Window>[5];
            for (int d = 0; d < 5; d++)
            {
                temp[d] = new List<Window>();
                float x0 = -5.6f + d * 2.24f;
                float x = x0;
                while (x < x0 + 2.1f)
                {
                    float w = 0.32f + (float)rnd.NextDouble() * 0.35f;
                    float h = 0.5f + (float)rnd.NextDouble() * (d == 2 ? 1.5f : 1.0f);
                    float baseY = 2.95f;
                    Color body = Color.Lerp(Palette.PetrolDark, Palette.SkyMid, (float)rnd.NextDouble() * 0.5f);
                    Make("prédio", cityRoot, Art.PixelBottom, new Vector2(x + w * 0.5f, baseY), new Vector2(w, h), body, -92);
                    int cols = Mathf.Max(1, Mathf.FloorToInt(w / 0.11f));
                    int rows = Mathf.Max(1, Mathf.FloorToInt((h - 0.1f) / 0.14f));
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                        {
                            if (rnd.NextDouble() < 0.25) continue;
                            float wx = x + 0.07f + c * (w - 0.1f) / Mathf.Max(1, cols - 0.5f);
                            float wy = baseY + 0.12f + r * 0.14f;
                            if (wx > x + w - 0.04f) continue;
                            var sr = Make("janela", cityRoot, Art.Pixel, new Vector2(wx, wy), new Vector2(0.045f, 0.06f), Palette.WindowOff, -91);
                            temp[d].Add(new Window { sr = sr, stray = rnd.NextDouble() < 0.12, phase = (float)rnd.NextDouble() * 10f });
                        }
                    x += w + 0.04f + (float)rnd.NextDouble() * 0.08f;
                }
            }
            foreach (int d in order) districts.Add(temp[d]);
        }

        Structure BuildControlRoom()
        {
            var s = NewStructure("Sala de controle", new Vector2(-2.45f, -1.35f));
            var t = s.root.transform;
            BuildingBlock(s, t, Vector2.zero, 1.5f, 0.85f, -36, 1, Palette.Concrete, out float roof);
            for (int i = 0; i < 4; i++)
                blinkersWindow(s, t, new Vector2(-0.5f + i * 0.33f, 0.45f), 1);
            Part(s, "porta", t, Art.PixelBottom, new Vector2(0.45f, 0f), new Vector2(0.22f, 0.32f), Palette.PetrolDark, -35, 1);
            // etapa 2: segundo andar
            BuildingBlock(s, t, new Vector2(-0.2f, roof), 0.9f, 0.5f, -34, 2, Palette.ConcreteLight, out float roof2);
            for (int i = 0; i < 3; i++)
                blinkersWindow(s, t, new Vector2(-0.5f + i * 0.3f, roof + 0.25f), 2);
            // etapa 3: antena com luz de sinalização
            Part(s, "antena", t, Art.PixelBottom, new Vector2(0.05f, roof2), new Vector2(0.04f, 0.6f), Palette.ConcreteDark, -33, 3);
            var light = Part(s, "sinal", t, Art.Circle, new Vector2(0.05f, roof2 + 0.62f), new Vector2(0.1f, 0.1f), Palette.UiNegative, -32, 3);
            blinkers.Add(light);
            return s;

            void blinkersWindow(Structure st, Transform tr, Vector2 p, int tier)
            {
                Part(st, "janela", tr, Art.Pixel, p, new Vector2(0.2f, 0.22f), Palette.Amber, -35, tier);
            }
        }

        Structure BuildTurbineHall()
        {
            var s = NewStructure("Casa de turbinas", new Vector2(2.45f, -1.35f));
            var t = s.root.transform;
            BuildingBlock(s, t, Vector2.zero, 1.9f, 1.05f, -36, 1, Palette.Concrete, out float roof);
            Part(s, "faixa", t, Art.PixelBottom, new Vector2(0, 0.86f), new Vector2(1.9f, 0.08f), Palette.Petrol, -35, 1);
            for (int i = 0; i < 3; i++)
            {
                int tier = i + 1;
                var pos = new Vector2(-0.6f + i * 0.6f, 0.45f);
                Part(s, "janela turbina", t, Art.Circle, pos, new Vector2(0.5f, 0.5f), Palette.PetrolDark, -35, tier);
                var rotor = Part(s, "rotor", t, Art.Rotor, pos, new Vector2(0.4f, 0.4f), Palette.Cyan, -34, tier);
                rotors.Add(rotor.transform);
                Part(s, "eixo", t, Art.Circle, pos, new Vector2(0.1f, 0.1f), Palette.ConcreteLight, -33, tier);
            }
            // tubulação até o reator
            Part(s, "tubo", t, Art.Pixel, new Vector2(-1.25f, 0.55f), new Vector2(0.7f, 0.12f), Palette.ConcreteDark, -37, 1);
            return s;
        }

        Structure BuildSubstation()
        {
            var s = NewStructure("Subestação", new Vector2(3.55f, -2.2f));
            var t = s.root.transform;
            Part(s, "base", t, Art.PixelBottom, new Vector2(0, -0.05f), new Vector2(1.7f, 0.12f), Palette.ConcreteShadow, -27, 1);
            for (int i = 0; i < 4; i++)
            {
                int tier = i < 2 ? 1 : i - 0;
                tier = Mathf.Clamp(tier, 1, 3);
                float x = -0.6f + i * 0.4f;
                Part(s, "transformador", t, Art.PixelBottom, new Vector2(x, 0), new Vector2(0.3f, 0.36f), Palette.ConcreteLight, -26, tier);
                Part(s, "aletas", t, Art.PixelBottom, new Vector2(x, 0.06f), new Vector2(0.34f, 0.04f), Palette.ConcreteDark, -25, tier);
                Part(s, "isolador", t, Art.PixelBottom, new Vector2(x, 0.36f), new Vector2(0.05f, 0.2f), Palette.Amber.WithAlpha(0.9f), -25, tier);
            }
            // torres de transmissão até a cidade (lado direito)
            var p1 = Part(s, "torre 1", t, Art.Pylon, new Vector2(1.15f, 0.3f), new Vector2(1f, 1f), Palette.ConcreteLight, -40, 1);
            var p2 = Part(s, "torre 2", t, Art.Pylon, new Vector2(0.55f, 2.5f), new Vector2(0.65f, 0.65f), Palette.Concrete, -78, 1);
            var p3 = Part(s, "torre 3", t, Art.Pylon, new Vector2(-0.35f, 4.05f), new Vector2(0.4f, 0.4f), Palette.ConcreteDark, -82, 1);
            Vector2 top1 = new Vector2(1.15f, 0.3f + 2.4f * 0.78f), top2 = new Vector2(0.55f, 2.5f + 2.4f * 0.65f * 0.78f), top3 = new Vector2(-0.35f, 4.05f + 2.4f * 0.4f * 0.78f);
            substationLines.Add(Line(s, t, new Vector2(0, 0.5f), top1, -39, 1));
            substationLines.Add(Line(s, t, top1, top2, -77, 1));
            substationLines.Add(Line(s, t, top2, top3, -81, 1));
            return s;
        }

        SpriteRenderer Line(Structure s, Transform t, Vector2 a, Vector2 b, int order, int tier)
        {
            Vector2 d = b - a;
            var sr = Part(s, "cabo", t, Art.Pixel, (a + b) * 0.5f, new Vector2(d.magnitude, 1.2f / Art.WorldPPU), Palette.ConcreteDark, order, tier);
            sr.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            return sr;
        }

        Structure BuildAutomation()
        {
            var s = NewStructure("Sala de automação", new Vector2(-3.05f, -2.3f));
            var t = s.root.transform;
            BuildingBlock(s, t, Vector2.zero, 1.6f, 0.62f, -24, 1, Palette.ConcreteLight, out float roof);
            Part(s, "faixa", t, Art.PixelBottom, new Vector2(0, 0.0f), new Vector2(1.6f, 0.1f), Palette.Petrol, -23, 1);
            for (int i = 0; i < 6; i++)
            {
                int tier = i < 2 ? 1 : i < 4 ? 2 : 3;
                var m = Part(s, "monitor", t, Art.Pixel, new Vector2(-0.6f + i * 0.24f, 0.35f), new Vector2(0.17f, 0.15f), Palette.Cyan, -23, tier);
                monitors.Add(m);
            }
            // antenas parabólicas na etapa 3
            Part(s, "parabólica", t, Art.Circle, new Vector2(0.45f, roof + 0.12f), new Vector2(0.28f, 0.18f), Palette.ConcreteLight, -22, 3);
            Part(s, "suporte", t, Art.PixelBottom, new Vector2(0.45f, roof - 0.02f), new Vector2(0.04f, 0.1f), Palette.ConcreteDark, -22, 3);
            return s;
        }

        Structure BuildCoolingTower()
        {
            var s = NewStructure("Torres de resfriamento", new Vector2(3.35f, -0.55f));
            var t = s.root.transform;
            Part(s, "torre", t, Art.CoolingTower, Vector2.zero, new Vector2(1f, 1f), Color.white, -52, 1);
            var src = new GameObject("saída de vapor").transform; src.SetParent(t, false); src.localPosition = new Vector2(0, 2.5f);
            steamSources.Add(src);
            Part(s, "torre 2", t, Art.CoolingTower, new Vector2(-1.25f, 0.35f), new Vector2(0.7f, 0.7f), Color.Lerp(Color.white, Palette.SkyMid, 0.25f), -55, 2);
            var src2 = new GameObject("saída de vapor 2").transform; src2.SetParent(t, false); src2.localPosition = new Vector2(-1.25f, 0.35f + 2.5f * 0.7f);
            steamSources.Add(src2);
            // luzes de aviso no topo (etapa 3)
            blinkers.Add(Part(s, "luz", t, Art.Circle, new Vector2(-0.42f, 2.45f), new Vector2(0.09f, 0.09f), Palette.UiNegative, -51, 3));
            blinkers.Add(Part(s, "luz", t, Art.Circle, new Vector2(0.42f, 2.45f), new Vector2(0.09f, 0.09f), Palette.UiNegative, -51, 3));
            Part(s, "faixa ciano", t, Art.Pixel, new Vector2(0, 1.2f), new Vector2(0.95f, 0.05f), Palette.Cyan.WithAlpha(0.8f), -51, 3);
            return s;
        }

        Structure BuildSecondReactor()
        {
            var s = NewStructure("Segundo reator", new Vector2(-2.75f, -0.35f));
            var t = s.root.transform;
            Part(s, "halo", t, Art.Glow, new Vector2(0, 1.2f), new Vector2(3.5f, 3.5f), Palette.Cyan.WithAlpha(0.18f), -58, 3);
            Part(s, "reator", t, Art.Reactor, Vector2.zero, new Vector2(0.72f, 0.72f), Color.Lerp(Color.white, Palette.SkyMid, 0.2f), -56, 1);
            for (int i = 0; i < 5; i++)
                Part(s, "luz", t, Art.Pixel, new Vector2(-0.56f + i * 0.28f, 3f * 0.72f * 0.435f), new Vector2(0.16f, 0.1f), Palette.Cyan, -55, 2);
            Part(s, "tubo", t, Art.Pixel, new Vector2(1.1f, 0.25f), new Vector2(0.9f, 0.12f), Palette.ConcreteDark, -57, 1);
            return s;
        }

        Structure BuildRegionalGrid()
        {
            var s = NewStructure("Rede regional", new Vector2(-4.55f, -1.0f));
            var t = s.root.transform;
            var p1 = Part(s, "torre 1", t, Art.Pylon, new Vector2(0, 0), new Vector2(0.95f, 0.95f), Palette.ConcreteLight, -45, 1);
            var p2 = Part(s, "torre 2", t, Art.Pylon, new Vector2(0.75f, 2.6f), new Vector2(0.6f, 0.6f), Palette.Concrete, -78, 1);
            var p3 = Part(s, "torre 3", t, Art.Pylon, new Vector2(1.6f, 3.95f), new Vector2(0.38f, 0.38f), Palette.ConcreteDark, -82, 2);
            var p4 = Part(s, "torre 4", t, Art.Pylon, new Vector2(3.0f, 4.35f), new Vector2(0.3f, 0.3f), Palette.ConcreteDark, -83, 3);
            Vector2 a = new Vector2(0, 2.4f * 0.95f * 0.78f), b = new Vector2(0.75f, 2.6f + 2.4f * 0.6f * 0.78f),
                    c = new Vector2(1.6f, 3.95f + 2.4f * 0.38f * 0.78f), d = new Vector2(3.0f, 4.35f + 2.4f * 0.3f * 0.78f);
            regionalLines.Add(Line(s, t, new Vector2(1.6f, 0.4f), a, -44, 1));
            regionalLines.Add(Line(s, t, a, b, -77, 1));
            regionalLines.Add(Line(s, t, b, c, -81, 2));
            regionalLines.Add(Line(s, t, c, d, -82, 3));
            return s;
        }

        void BuildReactor()
        {
            reactorTf = new GameObject("Reator principal").transform;
            reactorTf.SetParent(worldRoot, false);
            reactorTf.localPosition = reactorPos;
            reactorHalo = Make("halo", reactorTf, Art.Glow, new Vector2(0, 1.5f), new Vector2(5.5f, 5.5f), Palette.Cyan.WithAlpha(0.08f), -31);
            Make("reator", reactorTf, Art.Reactor, Vector2.zero, Vector2.one, Color.white, -30);
            float bandY = 3f * 0.435f;
            reactorStripe = Make("faixa da geração", reactorTf, Art.Pixel, new Vector2(0, bandY), new Vector2(1.98f, 0.21f), Palette.PetrolDark.WithAlpha(0), -29);
            for (int i = 0; i < 6; i++)
                reactorLights.Add(Make("luz", reactorTf, Art.Pixel, new Vector2(-0.8f + i * 0.32f, bandY), new Vector2(0.2f, 0.11f), Palette.CyanSoft, -28));
            // luzes da cúpula (a partir da 2ª geração)
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.Lerp(20f, 160f, i / 6f) * Mathf.Deg2Rad;
                var sr = Make("luz da cúpula", reactorTf, Art.Circle, new Vector2(Mathf.Cos(a) * 0.86f, 1.68f + Mathf.Sin(a) * 0.86f), new Vector2(0.08f, 0.08f), Palette.Amber, -28);
                domeLights.Add(sr);
            }
        }

        void BuildSign()
        {
            var root = new GameObject("Letreiro").transform;
            root.SetParent(worldRoot, false);
            root.localPosition = new Vector2(0.15f, -2.42f);
            signBoard = Make("placa", root, Art.Pixel, Vector2.zero, new Vector2(4.6f, 0.62f), Palette.PetrolDark, -20);
            Make("borda", root, Art.Pixel, new Vector2(0, -0.31f), new Vector2(4.6f, 1f / Art.WorldPPU), Palette.Cyan, -19);
            Make("borda topo", root, Art.Pixel, new Vector2(0, 0.31f), new Vector2(4.6f, 1f / Art.WorldPPU), Palette.PetrolLight, -19);
            signText = Make("texto", root, null, new Vector2(0, 0.02f), Vector2.one, Color.white, -18);
        }

        // ---------- estado -> visual ----------
        public void RefreshAll(bool instant)
        {
            if (gm == null || structures == null) return;
            var cfg = gm.Config;
            for (int i = 0; i < cfg.upgrades.Count; i++)
            {
                var kind = cfg.upgrades[i].structure;
                var s = structures[(int)kind];
                if (s == null) continue;
                ApplyTier(s, gm.Economy.VisualTier(i), gm.Economy.IsUnlocked(i), instant);
            }
            RefreshCity(instant);
            RefreshGeneration();
        }

        void OnTierChanged(int index, int tier)
        {
            var s = structures[(int)gm.Config.upgrades[index].structure];
            if (s == null) return;
            ApplyTier(s, tier, true, false);
            RefreshCity(false);
            installFlash = tier == 1 ? 1f : 0.6f;
            shake = tier == 1 ? 0.5f : 0.3f;
        }

        void RefreshUnlockGhosts()
        {
            var cfg = gm.Config;
            for (int i = 0; i < cfg.upgrades.Count; i++)
            {
                var s = structures[(int)cfg.upgrades[i].structure];
                if (s == null || s.tier > 0) continue;
                bool unlocked = gm.Economy.IsUnlocked(i);
                if (unlocked != s.ghost) ApplyTier(s, 0, unlocked, true);
            }
        }

        void ApplyTier(Structure s, int tier, bool unlocked, bool instant)
        {
            bool ghost = tier == 0 && unlocked;
            if (s.tier == tier && s.ghost == ghost) return;
            bool grew = tier > s.tier && s.tier >= 0 && !instant;
            s.tier = tier;
            s.ghost = ghost;
            s.root.SetActive(tier > 0 || ghost);
            for (int t = 1; t <= 3; t++)
                foreach (var go in s.tierObjects[t]) go.SetActive(tier >= t || (ghost && t == 1));
            foreach (var kv in s.parts)
                kv.Key.color = ghost ? Palette.ConcreteLight.WithAlpha(0.16f) : kv.Value;
            if (grew) s.pop = 1f;
        }

        void RefreshCity(bool instant)
        {
            if (gm == null) return;
            var cfg = gm.Config;
            int lit = 0;
            foreach (var kind in new[] { StructureKind.Substation, StructureKind.CoolingTower, StructureKind.SecondReactor })
            {
                int idx = cfg.upgrades.FindIndex(u => u.structure == kind);
                if (idx >= 0 && gm.Economy.Level(idx) > 0) lit++;
            }
            int grid = cfg.upgrades.FindIndex(u => u.structure == StructureKind.RegionalGrid);
            if (grid >= 0) lit += gm.Economy.VisualTier(grid);
            lit = Mathf.Clamp(lit, 0, districts.Count);
            if (lit == litDistricts) return;
            litDistricts = lit;
            for (int d = 0; d < districts.Count; d++)
                foreach (var w in districts[d])
                {
                    w.lit = d < lit;
                    w.target = w.lit ? 0.65f + 0.35f * Mathf.PerlinNoise(w.phase, 0) : (w.stray ? 0.35f : 0f);
                    if (instant) SetWindow(w, w.target);
                }
        }

        static void SetWindow(Window w, float level)
        {
            // três tons fixos, sem degradê
            w.sr.color = level > 0.7f ? Palette.Amber : level > 0.25f ? Palette.AmberDim : Palette.WindowOff;
        }

        void RefreshGeneration()
        {
            int gen = gm.State.generation;
            Color[] stripe = { Palette.PetrolDark, Palette.CyanSoft, Palette.Amber, Palette.UiPositive, new Color(0.75f, 0.55f, 1f) };
            Color sc = stripe[(gen - 1) % stripe.Length];
            reactorStripe.color = gen > 1 ? sc.WithAlpha(0.55f) : sc.WithAlpha(0f);
            for (int i = 0; i < domeLights.Count; i++) domeLights[i].gameObject.SetActive(gen > 1 && i < 3 + gen * 2);
            if (signGeneration != gen)
            {
                signGeneration = gen;
                signText.sprite = Art.WorldText("NÚCLEO VIVO · GERAÇÃO " + Roman(gen), Palette.ConcreteLight);
            }
        }

        public static string Roman(int n)
        {
            if (n <= 0) return n.ToString();
            string[] r = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            int[] v = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < v.Length; i++) while (n >= v[i]) { sb.Append(r[i]); n -= v[i]; }
            return sb.ToString();
        }

        // ---------- animação ----------
        public void Pulse()
        {
            tapFlash = 1f;
        }

        void Animate(float dt)
        {
            float time = Time.time;
            double passive = gm != null ? gm.Economy.PassivePerSecond : 0;
            float activity = Mathf.Clamp01((float)Math.Log10(1 + passive) / 3f);

            tapFlash = Mathf.MoveTowards(tapFlash, 0, dt * 4f);
            installFlash = Mathf.MoveTowards(installFlash, 0, dt * 1.5f);
            shake = Mathf.MoveTowards(shake, 0, dt * 2f);

            // Reator: pulsa suave com a produção e acende no toque
            float idle = 0.35f + 0.15f * Mathf.Sin(time * 2f) * activity;
            for (int i = 0; i < reactorLights.Count; i++)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(time * 3f - i * 0.6f);
                float k = Mathf.Clamp01(idle + wave * 0.2f * activity + tapFlash);
                reactorLights[i].color = k > 0.7f ? Palette.Cyan : k > 0.45f ? Palette.CyanSoft : Palette.PetrolLight;
            }
            reactorHalo.color = Palette.Cyan.WithAlpha(0.06f + 0.05f * activity + 0.3f * tapFlash + 0.25f * installFlash);
            float squash = 1f - 0.025f * tapFlash;
            reactorTf.localScale = new Vector3(1f + 0.012f * tapFlash, squash, 1);
            riverGlow.color = Palette.Cyan.WithAlpha(0.08f + 0.06f * activity + 0.2f * tapFlash);
            for (int i = 0; i < domeLights.Count; i++)
                domeLights[i].color = Palette.Amber.WithAlpha(0.55f + 0.45f * Mathf.Sin(time * 2.2f + i));

            // Turbinas giram conforme a produção passiva
            float rotSpeed = 60f + 600f * activity;
            foreach (var r in rotors) if (r.gameObject.activeInHierarchy) r.Rotate(0, 0, -rotSpeed * dt);

            foreach (var m in monitors)
                if (m.gameObject.activeInHierarchy && !IsGhost(m))
                    m.color = Mathf.PerlinNoise(m.transform.position.x * 3f, time * 2f) > 0.5f ? Palette.Cyan : Palette.CyanSoft;

            foreach (var b in blinkers)
                if (b.gameObject.activeInHierarchy && !IsGhost(b))
                    b.color = Palette.UiNegative.WithAlpha(Mathf.Sin(time * 4f + b.transform.position.x) > 0.3f ? 1f : 0.15f);

            AnimateLines(substationLines, StructureKind.Substation, time);
            AnimateLines(regionalLines, StructureKind.RegionalGrid, time);

            // Rio
            foreach (var r in ripples)
            {
                var p = r.transform.localPosition;
                p.x += dt * 0.25f;
                if (p.x > 6.5f) p.x = -6.5f;
                r.transform.localPosition = p;
                r.color = Palette.RiverLight.WithAlpha(0.35f + 0.3f * Mathf.Sin(time * 1.5f + p.x * 2f));
            }

            // Estrelas e janelas da cidade
            for (int i = 0; i < stars.Count; i++)
                stars[i].color = Color.white.WithAlpha((0.25f + 0.25f * Mathf.Sin(time * 0.8f + i * 1.7f)) * (1f - darkness * 0.3f));
            foreach (var d in districts)
                foreach (var w in d)
                {
                    float cur = w.sr.color.r; // aproximação para interpolar
                    float level = Mathf.InverseLerp(Palette.WindowOff.r, Palette.Amber.r, cur);
                    float target = w.target * (1f - darkness);
                    if (w.lit) target *= 0.9f + 0.1f * Mathf.Sin(time * 0.7f + w.phase);
                    if (Mathf.Abs(level - target) > 0.01f) SetWindow(w, Mathf.MoveTowards(level, target, dt * 1.5f));
                }

            // Vapor das torres
            var tower = structures[(int)StructureKind.CoolingTower];
            if (tower != null && tower.tier > 0)
            {
                for (int i = 0; i < steamSources.Count; i++)
                {
                    if (!steamSources[i].gameObject.activeInHierarchy || (i == 1 && tower.tier < 2)) continue;
                    float rate = 2.5f + tower.tier;
                    if (UnityEngine.Random.value < rate * dt) Emit(steamSources[i].position, i == 0 ? 1f : 0.7f);
                }
            }
            foreach (var s in steam)
            {
                if (!s.active) continue;
                s.life += dt;
                float t = s.life / s.maxLife;
                if (t >= 1f) { s.active = false; s.sr.gameObject.SetActive(false); continue; }
                s.sr.transform.position += (Vector3)(s.vel * dt);
                float sz = s.size * (0.6f + t * 1.4f);
                s.sr.transform.localScale = new Vector3(sz, sz, 1);
                s.sr.color = darkness > 0.5f ? Color.clear : (t < 0.6f ? new Color(0.86f, 0.9f, 0.9f, 1f) : new Color(0.55f, 0.66f, 0.7f, 1f)); // dois tons, sem transparência gradual
            }

            // Pop de instalação
            foreach (var s in structures)
            {
                if (s == null || s.pop <= 0) continue;
                s.pop = Mathf.MoveTowards(s.pop, 0, dt * 2.5f);
                float k = 1f + Mathf.Sin(s.pop * Mathf.PI) * 0.08f;
                s.root.transform.localScale = s.baseScale * k;
            }

            flashOverlay.color = Palette.Cyan.WithAlpha(0.18f * installFlash * installFlash);
            darkOverlay.color = new Color(0.01f, 0.03f, 0.05f, darkness * 0.85f);
        }

        void AnimateLines(List<SpriteRenderer> lines, StructureKind kind, float time)
        {
            var s = structures[(int)kind];
            if (s == null || s.tier <= 0) return;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].gameObject.activeInHierarchy) continue;
                float pulse = 0.55f + 0.45f * Mathf.Sin(time * 3f - i * 1.3f);
                lines[i].color = pulse > 0.6f ? Palette.Cyan : Palette.CyanSoft;
            }
        }

        bool IsGhost(SpriteRenderer sr)
        {
            return sr.color.a < 0.2f && Mathf.Abs(sr.color.b - Palette.ConcreteLight.b) < 0.01f;
        }

        void Emit(Vector3 pos, float scale)
        {
            foreach (var s in steam)
            {
                if (s.active) continue;
                s.active = true;
                s.life = 0;
                s.maxLife = 2.2f + UnityEngine.Random.value * 1.2f;
                s.size = (0.25f + UnityEngine.Random.value * 0.2f) * scale;
                s.vel = new Vector2(0.12f + UnityEngine.Random.value * 0.1f, 0.35f + UnityEngine.Random.value * 0.2f) * scale;
                s.sr.transform.position = pos + new Vector3((UnityEngine.Random.value - 0.5f) * 0.4f * scale, 0, 0);
                s.sr.gameObject.SetActive(true);
                return;
            }
        }

        // ---------- Renascimento ----------
        /// <summary>A cidade apaga, o estado é reiniciado no meio e a primeira ala reacende.</summary>
        public IEnumerator RebirthSequence(Action atMidpoint, Action<string> caption)
        {
            transitioning = true;
            float t = 0;
            caption?.Invoke("A cidade se prepara para uma nova geração…");
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                darkness = Mathf.SmoothStep(0, 1, t / 1.4f);
                yield return null;
            }
            atMidpoint?.Invoke();
            litDistricts = -1;
            RefreshAll(true);
            foreach (var d in districts) foreach (var w in d) SetWindow(w, 0);
            caption?.Invoke("Geração " + Roman(gm.State.generation) + " — o reator reacende");
            yield return new WaitForSeconds(0.4f);
            installFlash = 1f; tapFlash = 1f; shake = 0.6f;
            t = 0;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                darkness = 1f - Mathf.SmoothStep(0, 1, t / 1.4f);
                tapFlash = Mathf.Max(tapFlash, 0.6f * (1f - t / 1.4f));
                yield return null;
            }
            darkness = 0;
            caption?.Invoke(null);
            transitioning = false;
        }
    }
}
