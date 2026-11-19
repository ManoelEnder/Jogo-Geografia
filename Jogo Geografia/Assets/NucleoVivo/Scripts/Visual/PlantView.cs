using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NucleoVivo
{
    /// <summary>
    /// Cenário pintado em vista elevada: horizonte com a cidade, floresta, rio, estrada, a usina e suas
    /// estruturas (com etapas visuais) e a câmera que o jogador arrasta pelo mapa.
    /// As ilustrações vêm de Resources/Arte (projeto Remotion em /Arte).
    /// </summary>
    public class PlantView : MonoBehaviour
    {
        [Header("Câmera")]
        public Rect mapBounds = Rect.MinMaxRect(-40f, -14f, 40f, 17f);
        [Tooltip("Meia altura visível com zoom 1.")] public float baseHalfHeight = 9f;
        [Range(0.4f, 1f)] public float minZoom = 0.6f;
        [Range(1f, 2.5f)] public float maxZoom = 1.5f;
        [Tooltip("Fração da tela coberta à direita pelo painel (a interface ajusta).")] [Range(0, 0.6f)] public float rightOccluded = 0.33f;

        public Camera Cam { get; private set; }
        public Camera PresentCamera => Cam;
        public AreaView Areas { get; private set; }
        public float Zoom { get; private set; } = 1f;

        public static readonly Vector2 ReactorPos = new Vector2(0f, -1.6f);
        public Rect ReactorWorldRect => new Rect(ReactorPos.x - 1.4f, ReactorPos.y - 0.3f, 2.8f, 4.4f);

        // ---------- interno ----------
        class Structure
        {
            public GameObject root;
            public readonly List<GameObject>[] tierObjects = { new List<GameObject>(), new List<GameObject>(), new List<GameObject>(), new List<GameObject>() };
            public readonly List<SpriteRenderer> lights = new List<SpriteRenderer>();
            public GameObject lot;
            public int tier = -1;
            public bool unlocked;
            public float pop;
        }

        GameManager gm;
        Transform worldRoot;
        Material spriteMat;
        Structure[] structures;
        SpriteRenderer reactorLights, reactorGlow, darkOverlay, flashOverlay, vignette;
        Transform reactorTf;
        readonly List<Transform> fans = new List<Transform>();
        readonly List<SpriteRenderer> wires = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> regionalWires = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> beacons = new List<SpriteRenderer>();
        readonly List<Transform> steamSources = new List<Transform>();
        readonly List<Puff> steam = new List<Puff>();
        readonly List<SpriteRenderer> cityLit = new List<SpriteRenderer>();
        readonly List<float> cityTarget = new List<float>();
        TextMesh signText;
        int signGeneration = -1;

        class Puff { public SpriteRenderer sr; public float life, maxLife, size; public Vector2 vel; public bool active; }

        Vector2 camTarget = new Vector2(0f, -0.5f), camPos = new Vector2(0f, -0.5f);
        bool camInitialized, transitioning;
        float tapFlash, installFlash, shake, darkness;
        System.Random rnd = new System.Random(7);

        // ---------- ciclo de vida ----------
        void Start()
        {
            gm = GameManager.Instance;
            spriteMat = gm != null ? gm.Config.spriteMaterial : null;
            Cam = Camera.main;
            if (Cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                Cam = camGo.AddComponent<Camera>();
            }
            Cam.orthographic = true;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color32(0x5f, 0x6a, 0x7a, 255);

            Build();
            Areas = gameObject.AddComponent<AreaView>();
            Areas.Init(this, worldRoot, spriteMat);
            if (gm != null)
            {
                gm.Tapped += _ => tapFlash = 1f;
                gm.StructureTierChanged += OnTierChanged;
                gm.StateReloaded += () => RefreshAll(true);
                gm.PermanentBought += () => RefreshAll(false);
                gm.AreaOpened += a => { RefreshCity(); installFlash = 1f; shake = 0.4f; };
            }
            RefreshAll(true);
        }

        void LateUpdate()
        {
            UpdateCamera();
            Animate(Time.deltaTime);
            if (gm != null && !transitioning) RefreshLots();
        }

        // ---------- câmera ----------
        public float ViewHalfHeight => baseHalfHeight * Zoom;
        public float ViewHalfWidth => ViewHalfHeight * Mathf.Max(0.3f, Cam.aspect);

        void UpdateCamera()
        {
            if (Cam == null) return;
            Cam.orthographicSize = ViewHalfHeight;
            if (!camInitialized) { camInitialized = true; FocusX(0f, true); }
            camTarget = Clamp(camTarget);
            camPos = Vector2.Lerp(camPos, camTarget, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 10f));
            Vector2 sh = shake > 0 ? UnityEngine.Random.insideUnitCircle * shake * 0.12f : Vector2.zero;
            Cam.transform.position = new Vector3(camPos.x + sh.x, camPos.y + sh.y, -10f);
            float h = ViewHalfHeight * 2f, w = ViewHalfWidth * 2f;
            darkOverlay.transform.localScale = new Vector3(w * 1.1f, h * 1.1f, 1);
            flashOverlay.transform.localScale = new Vector3(w * 1.1f, h * 1.1f, 1);
            vignette.transform.localScale = new Vector3(w / 5.12f, h / 5.12f, 1);
        }

        Vector2 Clamp(Vector2 c)
        {
            float hh = ViewHalfHeight, hw = ViewHalfWidth;
            c.x = mapBounds.width > hw * 2 ? Mathf.Clamp(c.x, mapBounds.xMin + hw, mapBounds.xMax - hw) : mapBounds.center.x;
            c.y = mapBounds.height > hh * 2 ? Mathf.Clamp(c.y, mapBounds.yMin + hh, mapBounds.yMax - hh) : mapBounds.center.y;
            return c;
        }

        /// <summary>Arrasta a câmera. <paramref name="screenDelta"/> em pixels de tela.</summary>
        public void PanByScreen(Vector2 screenDelta, float screenHeight)
        {
            float worldPerPixel = 2f * ViewHalfHeight / Mathf.Max(1f, screenHeight);
            camTarget = Clamp(camTarget - screenDelta * worldPerPixel);
            camPos = camTarget;
        }

        public void PanBy(Vector2 world) => camTarget = Clamp(camTarget + world);

        public void ZoomBy(float steps) => Zoom = Mathf.Clamp(Zoom * Mathf.Pow(0.88f, steps), minZoom, maxZoom);

        /// <summary>Centraliza um ponto do mapa na parte da tela que o painel não cobre.</summary>
        public void FocusX(float x, bool instant = false) => FocusOn(new Vector2(x, 0.5f), instant);

        public void FocusOn(Vector2 world, bool instant = false)
        {
            camTarget = Clamp(new Vector2(world.x + ViewHalfWidth * rightOccluded, world.y));
            if (instant) camPos = camTarget;
        }

        public Vector2 WorldToViewport(Vector3 world) => Cam.WorldToViewportPoint(world);
        public Vector3 ViewportToWorld(Vector2 viewport) => Cam.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, 10f));

        // ---------- construção ----------
        /// <summary>Ordem de desenho pela profundidade: quanto mais acima (longe), mais atrás.</summary>
        public static int Depth(float y) => 5000 - Mathf.RoundToInt(y * 100f);

        public SpriteRenderer Put(string sprite, Transform parent, Vector2 pos, float scale = 1f, int? order = null, bool flip = false)
        {
            var go = new GameObject(sprite);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(scale, scale, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Pinturas.Get(sprite);
            sr.flipX = flip;
            sr.sortingOrder = order ?? Depth(go.transform.position.y);
            if (spriteMat != null) sr.sharedMaterial = spriteMat;
            return sr;
        }

        SpriteRenderer Tiled(string sprite, Transform parent, Vector2 center, Vector2 size, int order)
        {
            var sr = Put(sprite, parent, center, 1f, order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            return sr;
        }

        /// <summary>Linha fina entre dois pontos (fios de alta tensão).</summary>
        SpriteRenderer Wire(Transform parent, Vector2 a, Vector2 b, int order)
        {
            var go = new GameObject("fio");
            go.transform.SetParent(parent, false);
            Vector2 d = b - a;
            go.transform.localPosition = (a + b) * 0.5f;
            go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            go.transform.localScale = new Vector3(d.magnitude, 0.035f, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Pixel;
            sr.color = new Color32(0x3a, 0x36, 0x32, 255);
            sr.sortingOrder = order;
            if (spriteMat != null) sr.sharedMaterial = spriteMat;
            return sr;
        }

        float R() => (float)rnd.NextDouble();

        void Build()
        {
            worldRoot = new GameObject("Mundo").transform;
            worldRoot.SetParent(transform, false);
            BuildBackdrop();
            BuildGround();
            BuildDecor();
            BuildPlant();
            BuildOverlays();
        }

        void BuildBackdrop()
        {
            var t = new GameObject("Horizonte").transform;
            t.SetParent(worldRoot, false);
            var sky = Put("sky", t, new Vector2(0, 11.5f), 1f, -32000);
            sky.transform.localScale = new Vector3(140f, 3.2f, 1);
            for (int i = -3; i <= 3; i++) Put("hills_far", t, new Vector2(i * 15.9f, 12.4f), 1f, -31900);
            var lits = new List<SpriteRenderer>();
            for (int i = 0; i < 10; i++)
            {
                float x = -36f + i * 8f;
                string v = i % 2 == 0 ? "1" : "2";
                Put("city_dark_" + v, t, new Vector2(x, 12.7f), 1f, -31800);
                var lit = Put("city_lit_" + v, t, new Vector2(x, 12.7f), 1f, -31799);
                lit.color = new Color(1, 1, 1, 0);
                lits.Add(lit);
            }
            // bairros acendem do centro para fora
            var order = new List<int>();
            for (int i = 0; i < lits.Count; i++) order.Add(i);
            order.Sort((a, b) => Mathf.Abs(a - 4.5f).CompareTo(Mathf.Abs(b - 4.5f)));
            foreach (int i in order) { cityLit.Add(lits[i]); cityTarget.Add(0); }
            for (int i = -3; i <= 3; i++) Put("forest_wall", t, new Vector2(i * 15.9f + (i & 1) * 4f, 8.6f), 1f, -31700);
        }

        void BuildGround()
        {
            var t = new GameObject("Chão").transform;
            t.SetParent(worldRoot, false);
            Tiled("ground_grass", t, new Vector2(0, -2f), new Vector2(90f, 26f), -31000);
            Tiled("river", t, new Vector2(0, -10.2f), new Vector2(90f, 2.6f), -30900);
            Tiled("road", t, new Vector2(0, -5.5f), new Vector2(90f, 1.2f), -30800);
            foreach (float x in new[] { -30f, -16f, 0f, 30f })
                for (float y = -5f; y < -0.5f; y += 0.9f)
                    Put(R() < 0.5f ? "dirt_1" : "dirt_2", t, new Vector2(x + (R() - 0.5f) * 0.6f, y), 0.45f + R() * 0.15f, -30700).color = new Color(1, 1, 1, 0.7f);
            for (int i = 0; i < 9; i++)
                Put(R() < 0.5f ? "dirt_1" : "dirt_2", t, new Vector2(-6f + R() * 13f, -4.2f + R() * 3.5f), 0.7f + R() * 0.3f, -30710).color = new Color(1, 1, 1, 0.55f);
            for (int i = -6; i <= 6; i++)
            {
                var m = Put("mist", t, new Vector2(i * 6.5f + R(), -9.8f), 1.2f, -30600);
                m.color = new Color(1, 1, 1, 0.5f);
            }
        }

        static readonly string[] Trees = { "pine_1", "pine_2", "pine_3", "tree_green_1", "tree_olive_1", "pine_1", "pine_3" };
        static readonly string[] AutumnTrees = { "tree_red_1", "tree_orange_1", "tree_rose_1" };
        static readonly string[] Bushes = { "bush_green", "bush_berry", "bush_flower", "bush_yellow" };
        static readonly string[] Lows = { "grass_tuft", "grass_tuft", "grass_tall", "heather", "lavender", "flowers_yellow", "flowers_white", "clover", "fern", "mushrooms", "rock_2", "rock_3" };

        /// <summary>Lugares sem vegetação (usina, estrada, rio, centros das áreas).</summary>
        bool Blocked(Vector2 p)
        {
            if (p.y > -6.6f && p.y < -4.4f) return true;
            if (p.y < -8.6f && p.y > -11.8f) return true;
            if (p.x > -9f && p.x < 12f && p.y > -5f && p.y < 4.5f) return true;
            foreach (float ax in new[] { -30f, -16f, 30f }) if (Mathf.Abs(p.x - ax) < 5.2f && p.y > -5f && p.y < 3.5f) return true;
            if (Mathf.Abs(p.x - 16f) < 6f && p.y < -4f) return true;
            return false;
        }

        void BuildDecor()
        {
            var t = new GameObject("Vegetação").transform;
            t.SetParent(worldRoot, false);
            for (int row = 0; row < 4; row++)
                for (float x = -41f; x < 41f; x += 1.3f + R() * 0.6f)
                {
                    var p = new Vector2(x + R() * 0.5f, 4.2f + row * 1.25f + R() * 0.5f);
                    if (Blocked(p)) continue;
                    string s = R() < 0.18f ? AutumnTrees[(int)(R() * AutumnTrees.Length)] : Trees[(int)(R() * Trees.Length)];
                    Put(s, t, p, 0.9f + R() * 0.35f);
                }
            for (int i = 0; i < 90; i++)
            {
                var p = new Vector2(-41f + R() * 82f, -13.5f + R() * 17.5f);
                if (Blocked(p)) continue;
                float roll = R();
                string s = roll < 0.35f ? Bushes[(int)(R() * Bushes.Length)] : roll < 0.6f ? AutumnTrees[(int)(R() * AutumnTrees.Length)] : roll < 0.8f ? Trees[(int)(R() * Trees.Length)] : "rock_1";
                Put(s, t, p, 0.75f + R() * 0.35f);
            }
            for (int i = 0; i < 420; i++)
            {
                var p = new Vector2(-41f + R() * 82f, -13.8f + R() * 18f);
                if (Blocked(p)) continue;
                string s = Lows[(int)(R() * Lows.Length)];
                Put(s, t, p, 0.6f + R() * 0.4f, null, R() < 0.5f && !s.StartsWith("rock"));
            }
            // vegetação baixa dentro do terreno da usina, longe dos prédios
            Vector2[] keepOut = { new Vector2(-4.4f, -2.7f), new Vector2(4.4f, -2.6f), new Vector2(7.6f, -4.2f), new Vector2(-3.4f, -4.5f),
                                  new Vector2(7.2f, 1.6f), new Vector2(-6.4f, 1.4f), ReactorPos, new Vector2(9.8f, 2.5f), new Vector2(1.9f, -4.6f) };
            for (int i = 0; i < 160; i++)
            {
                var p = new Vector2(-9f + R() * 21f, -4.8f + R() * 9.2f);
                bool near = false;
                foreach (var k in keepOut) if ((p - k).sqrMagnitude < 4.4f) near = true;
                if (near) continue;
                string s = Lows[(int)(R() * Lows.Length)];
                Put(s, t, p, 0.55f + R() * 0.35f, null, R() < 0.5f && !s.StartsWith("rock"));
            }
            for (int i = 0; i < 10; i++)
            {
                var p = new Vector2(-9f + R() * 21f, 2.6f + R() * 1.8f);
                bool near = false;
                foreach (var k in keepOut) if ((p - k).sqrMagnitude < 6f) near = true;
                if (!near) Put(Bushes[(int)(R() * Bushes.Length)], t, p, 0.8f + R() * 0.3f);
            }
            for (float x = -41f; x < 41f; x += 0.9f + R())
            {
                if (Mathf.Abs(x - 16f) < 6f) continue;
                Put(R() < 0.5f ? "grass_tall" : "heather", t, new Vector2(x, -8.55f - R() * 0.2f), 0.7f + R() * 0.3f);
                if (R() < 0.6f) Put(R() < 0.5f ? "grass_tall" : "fern", t, new Vector2(x + 0.4f, -11.95f), 0.8f + R() * 0.3f);
            }
        }

        // ---------- usina ----------
        Structure NewStructure(string name, Vector2 pos, float lotScale = 0.9f)
        {
            var s = new Structure { root = new GameObject(name) };
            s.root.transform.SetParent(worldRoot, false);
            s.root.transform.localPosition = pos;
            var l = Put("construction_lot", worldRoot, pos + new Vector2(0, -0.1f), lotScale, Depth(pos.y) + 50);
            l.gameObject.name = "canteiro " + name;
            s.lot = l.gameObject;
            return s;
        }

        SpriteRenderer Part(Structure s, string sprite, Vector2 local, float scale, int tier, bool light = false)
        {
            var sr = Put(sprite, s.root.transform, local, scale);
            s.tierObjects[Mathf.Clamp(tier, 1, 3)].Add(sr.gameObject);
            if (light) { sr.sortingOrder += 1; s.lights.Add(sr); }
            return sr;
        }

        void BuildPlant()
        {
            structures = new Structure[7];

            var cr = NewStructure("Sala de controle", new Vector2(-4.4f, -2.7f));
            Part(cr, "control_room", Vector2.zero, 1f, 1);
            Part(cr, "control_room_lights", Vector2.zero, 1f, 1, true);
            Part(cr, "control_annex", new Vector2(-2.1f, 1.3f), 0.95f, 2);
            Part(cr, "antenna", new Vector2(0.55f, 1.35f), 1f, 3).sortingOrder = Depth(-2.7f) + 2;
            structures[(int)StructureKind.ControlRoom] = cr;

            var th = NewStructure("Casa de turbinas", new Vector2(4.4f, -2.6f));
            Part(th, "turbine_hall", Vector2.zero, 1f, 1);
            float[] fx = { -1.15f, -0.1f, 0.95f };
            for (int i = 0; i < 3; i++)
            {
                var f = Part(th, "fan", new Vector2(fx[i] - 0.05f, 2.12f), 0.85f, i + 1);
                f.sortingOrder = Depth(-2.6f) + 2;
                f.transform.localScale = new Vector3(0.8f, 0.4f, 1);
                fans.Add(f.transform);
            }
            structures[(int)StructureKind.Turbine] = th;

            var subPos = new Vector2(7.6f, -4.2f);
            var sub = NewStructure("Subestação", subPos);
            Part(sub, "transformer", Vector2.zero, 1f, 1);
            Part(sub, "transformer", new Vector2(1.2f, 0.25f), 1f, 1);
            Part(sub, "transformer", new Vector2(-1.1f, 0.3f), 0.95f, 2);
            Part(sub, "transformer", new Vector2(2.3f, 0.5f), 0.95f, 3);
            Vector2[] towers = { new Vector2(10.4f, -2.2f), new Vector2(12.6f, 0.6f), new Vector2(14.8f, 3.4f) };
            Vector2 prev = new Vector2(8.2f, -3.2f);
            foreach (var p in towers)
            {
                Part(sub, "pylon", p - subPos, 0.9f, 1);
                Vector2 top = p + new Vector2(0.05f, 2.6f * 0.9f);
                var wire = Wire(sub.root.transform, prev - subPos, top - subPos, Depth(p.y) + 3);
                sub.tierObjects[1].Add(wire.gameObject);
                wires.Add(wire);
                prev = top;
            }
            structures[(int)StructureKind.Substation] = sub;

            var au = NewStructure("Sala de automação", new Vector2(-3.4f, -4.5f));
            Part(au, "automation", Vector2.zero, 1f, 1);
            Part(au, "automation_lights", Vector2.zero, 1f, 1, true);
            Part(au, "container_grey", new Vector2(2.3f, 0.2f), 0.9f, 2);
            Part(au, "antenna", new Vector2(-1.1f, 1.0f), 0.7f, 3).sortingOrder = Depth(-4.5f) + 2;
            structures[(int)StructureKind.Automation] = au;

            var ct = NewStructure("Torres de resfriamento", new Vector2(7.2f, 1.6f));
            Part(ct, "cooling_tower", Vector2.zero, 1f, 1);
            Part(ct, "cooling_tower", new Vector2(2.6f, 0.9f), 0.82f, 2);
            var b1 = Part(ct, "soft_glow", new Vector2(-0.1f, 3.9f), 0.12f, 3);
            b1.color = new Color(1f, 0.35f, 0.3f);
            beacons.Add(b1);
            var src1 = new GameObject("vapor 1").transform; src1.SetParent(ct.root.transform, false); src1.localPosition = new Vector2(-0.1f, 3.7f); steamSources.Add(src1);
            var src2 = new GameObject("vapor 2").transform; src2.SetParent(ct.root.transform, false); src2.localPosition = new Vector2(2.52f, 0.9f + 3.7f * 0.82f); steamSources.Add(src2);
            structures[(int)StructureKind.CoolingTower] = ct;

            var r2 = NewStructure("Segundo reator", new Vector2(-6.4f, 1.4f));
            Part(r2, "reactor", Vector2.zero, 0.8f, 1);
            Part(r2, "reactor_lights", Vector2.zero, 0.8f, 1, true);
            Part(r2, "control_annex", new Vector2(2.0f, -0.4f), 0.8f, 3);
            structures[(int)StructureKind.SecondReactor] = r2;

            var rgPos = new Vector2(-9.5f, -2.6f);
            var rg = NewStructure("Rede regional", rgPos, 0.6f);
            Vector2[] west = { new Vector2(-9.5f, -2.6f), new Vector2(-11.6f, 0f), new Vector2(-13.6f, 2.6f), new Vector2(-12f, 5.6f) };
            Vector2 wprev = new Vector2(-6.6f, -2.0f);
            for (int i = 0; i < west.Length; i++)
            {
                int tier = i < 2 ? 1 : i;
                Part(rg, "pylon", west[i] - rgPos, 0.9f, tier);
                Vector2 top = west[i] + new Vector2(0.05f, 2.6f * 0.9f);
                var w = Wire(rg.root.transform, wprev - rgPos, top - rgPos, Depth(west[i].y) + 3);
                rg.tierObjects[tier].Add(w.gameObject);
                regionalWires.Add(w);
                wprev = top;
            }
            structures[(int)StructureKind.RegionalGrid] = rg;

            // reator principal (sempre presente; é o botão do jogo)
            reactorTf = new GameObject("Reator principal").transform;
            reactorTf.SetParent(worldRoot, false);
            reactorTf.localPosition = ReactorPos;
            reactorGlow = Put("soft_glow", reactorTf, new Vector2(0, 2.0f), 3.2f, Depth(ReactorPos.y) - 1);
            reactorGlow.color = new Color(0.6f, 1f, 0.9f, 0f);
            Put("reactor", reactorTf, Vector2.zero, 1f, Depth(ReactorPos.y));
            reactorLights = Put("reactor_lights", reactorTf, Vector2.zero, 1f, Depth(ReactorPos.y) + 1);

            // letreiro na entrada
            var sign = Put("sign_board", worldRoot, new Vector2(1.9f, -4.6f), 0.75f);
            var go = new GameObject("texto do letreiro");
            go.transform.SetParent(sign.transform, false);
            go.transform.localPosition = new Vector3(0.1f, 1.02f, -0.01f);
            signText = go.AddComponent<TextMesh>();
            signText.font = UIFactory.DefaultFont;
            signText.fontSize = 48;
            signText.characterSize = 0.1f;
            signText.anchor = TextAnchor.MiddleCenter;
            signText.alignment = TextAlignment.Center;
            signText.color = new Color32(0xf1, 0xe6, 0xcf, 255);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = signText.font.material;
            mr.sortingOrder = sign.sortingOrder + 1;

            for (int i = 0; i < 6; i++) Put("fence", worldRoot, new Vector2(-8.2f + i * 1.9f + (i > 2 ? 3.6f : 0), -5.05f), 0.95f);
            Put("lamp_post", worldRoot, new Vector2(-1.6f, -4.7f), 1f);
            Put("lamp_post_lights", worldRoot, new Vector2(-1.6f, -4.7f), 1f, Depth(-4.7f) + 1);
            Put("cart", worldRoot, new Vector2(-6.8f, -4.3f), 0.9f);
        }

        void BuildOverlays()
        {
            var camT = Cam.transform;
            vignette = MakeCamOverlay("vignette", camT, 32000, Color.white);
            darkOverlay = MakeCamOverlay(null, camT, 32100, new Color(0.05f, 0.03f, 0.02f, 0));
            flashOverlay = MakeCamOverlay(null, camT, 32200, new Color(0.7f, 1f, 0.9f, 0));
            for (int i = -5; i <= 5; i++)
            {
                var ls = Put("light_shafts", worldRoot, new Vector2(i * 8f + R() * 2f, 2f), 1.3f, 31000);
                ls.color = new Color(1, 1, 1, 0.35f);
            }
            for (int i = 0; i < 24; i++)
            {
                var sr = Put("steam", worldRoot, Vector2.zero, 1f, 30000);
                sr.gameObject.SetActive(false);
                steam.Add(new Puff { sr = sr });
            }
        }

        SpriteRenderer MakeCamOverlay(string sprite, Transform cam, int order, Color c)
        {
            var go = new GameObject(sprite ?? "camada");
            go.transform.SetParent(cam, false);
            go.transform.localPosition = new Vector3(0, 0, 10f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? Pinturas.Get(sprite) : Art.Pixel;
            sr.color = c;
            sr.sortingOrder = order;
            if (spriteMat != null) sr.sharedMaterial = spriteMat;
            return sr;
        }

        // ---------- estado -> visual ----------
        public void RefreshAll(bool instant)
        {
            if (gm == null || structures == null) return;
            var cfg = gm.Config;
            for (int i = 0; i < cfg.upgrades.Count; i++)
            {
                var s = structures[(int)cfg.upgrades[i].structure];
                if (s != null) ApplyTier(s, gm.Economy.VisualTier(i), gm.Economy.IsUnlocked(i), instant);
            }
            RefreshCity();
            RefreshGeneration();
        }

        void OnTierChanged(int index, int tier)
        {
            var s = structures[(int)gm.Config.upgrades[index].structure];
            if (s == null) return;
            ApplyTier(s, tier, true, false);
            RefreshCity();
            installFlash = tier == 1 ? 1f : 0.6f;
            shake = tier == 1 ? 0.5f : 0.3f;
        }

        void RefreshLots()
        {
            var cfg = gm.Config;
            for (int i = 0; i < cfg.upgrades.Count; i++)
            {
                var s = structures[(int)cfg.upgrades[i].structure];
                if (s == null || s.tier > 0) continue;
                bool unlocked = gm.Economy.IsUnlocked(i);
                if (unlocked != s.unlocked) ApplyTier(s, 0, unlocked, true);
            }
        }

        void ApplyTier(Structure s, int tier, bool unlocked, bool instant)
        {
            if (s.tier == tier && s.unlocked == unlocked) return;
            bool grew = tier > s.tier && s.tier >= 0 && !instant;
            s.tier = tier;
            s.unlocked = unlocked;
            s.root.SetActive(tier > 0);
            s.lot.SetActive(tier == 0 && unlocked);
            for (int t = 1; t <= 3; t++)
                foreach (var go in s.tierObjects[t]) go.SetActive(tier >= t);
            if (grew) s.pop = 1f;
        }

        void RefreshCity()
        {
            if (gm == null) return;
            var cfg = gm.Config;
            int lit = 1;
            foreach (var kind in new[] { StructureKind.Substation, StructureKind.CoolingTower, StructureKind.SecondReactor })
            {
                int idx = cfg.upgrades.FindIndex(u => u.structure == kind);
                if (idx >= 0 && gm.Economy.Level(idx) > 0) lit++;
            }
            int grid = cfg.upgrades.FindIndex(u => u.structure == StructureKind.RegionalGrid);
            if (grid >= 0) lit += gm.Economy.VisualTier(grid);
            lit += gm.State.openAreas.Count;
            for (int i = 0; i < cityTarget.Count; i++) cityTarget[i] = i < lit ? 1f : 0f;
        }

        void RefreshGeneration()
        {
            int gen = gm.State.generation;
            if (signGeneration == gen) return;
            signGeneration = gen;
            signText.text = "NÚCLEO VIVO\nGeração " + Roman(gen);
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
        void Animate(float dt)
        {
            float time = Time.time;
            double passive = gm != null ? gm.Economy.PassivePerSecond : 0;
            float activity = Mathf.Clamp01((float)Math.Log10(1 + passive) / 3f);
            tapFlash = Mathf.MoveTowards(tapFlash, 0, dt * 3f);
            installFlash = Mathf.MoveTowards(installFlash, 0, dt * 1.5f);
            shake = Mathf.MoveTowards(shake, 0, dt * 2f);

            float idle = 0.35f + 0.25f * activity + 0.15f * Mathf.Sin(time * 2.2f);
            reactorLights.color = new Color(1, 1, 1, Mathf.Clamp01(idle + tapFlash) * (1f - darkness));
            reactorGlow.color = new Color(0.6f, 1f, 0.9f, 0.35f * tapFlash + 0.25f * installFlash);
            reactorTf.localScale = new Vector3(1f + 0.015f * tapFlash, 1f - 0.02f * tapFlash, 1);

            foreach (var s in structures)
            {
                if (s == null) continue;
                for (int i = 0; i < s.lights.Count; i++)
                {
                    float f = 0.75f + 0.25f * Mathf.PerlinNoise(i * 2.3f + s.root.transform.position.x, time * 0.8f);
                    s.lights[i].color = new Color(1, 1, 1, f * (1f - darkness));
                }
                if (s.pop > 0)
                {
                    s.pop = Mathf.MoveTowards(s.pop, 0, dt * 2.5f);
                    float k = 1f + Mathf.Sin(s.pop * Mathf.PI) * 0.06f;
                    s.root.transform.localScale = new Vector3(k, k, 1);
                }
            }

            float rot = 90f + 720f * activity;
            foreach (var f in fans) if (f.gameObject.activeInHierarchy) f.Rotate(0, 0, -rot * dt);

            Color wireOn = Color.Lerp(new Color32(0x3a, 0x36, 0x32, 255), new Color(0.55f, 0.95f, 0.85f), 0.35f + 0.25f * Mathf.Sin(time * 3f));
            foreach (var w in wires) w.color = wireOn;
            foreach (var w in regionalWires) w.color = wireOn;
            foreach (var b in beacons) b.color = new Color(1f, 0.35f, 0.3f, Mathf.Repeat(time, 1.4f) < 0.4f ? 0.9f : 0.15f);

            for (int i = 0; i < cityLit.Count; i++)
            {
                var c = cityLit[i].color;
                c.a = Mathf.MoveTowards(c.a, cityTarget[i] * (1f - darkness), dt * 0.8f);
                cityLit[i].color = c;
            }

            var tower = structures[(int)StructureKind.CoolingTower];
            if (tower != null && tower.tier > 0)
                for (int i = 0; i < steamSources.Count; i++)
                {
                    if (i == 1 && tower.tier < 2) continue;
                    if (UnityEngine.Random.value < (1.6f + tower.tier) * dt) Emit(steamSources[i].position, i == 0 ? 1f : 0.82f);
                }
            foreach (var p in steam)
            {
                if (!p.active) continue;
                p.life += dt;
                float t = p.life / p.maxLife;
                if (t >= 1f) { p.active = false; p.sr.gameObject.SetActive(false); continue; }
                p.sr.transform.position += (Vector3)(p.vel * dt);
                float sz = p.size * (0.5f + t * 1.6f);
                p.sr.transform.localScale = new Vector3(sz, sz, 1);
                p.sr.color = new Color(1, 1, 1, 0.75f * Mathf.Sin(t * Mathf.PI) * (1f - darkness));
            }

            flashOverlay.color = new Color(0.8f, 1f, 0.92f, 0.22f * installFlash * installFlash);
            darkOverlay.color = new Color(0.05f, 0.03f, 0.02f, darkness * 0.88f);
        }

        void Emit(Vector3 pos, float scale)
        {
            foreach (var p in steam)
            {
                if (p.active) continue;
                p.active = true;
                p.life = 0;
                p.maxLife = 2.6f + UnityEngine.Random.value * 1.4f;
                p.size = (0.55f + UnityEngine.Random.value * 0.4f) * scale;
                p.vel = new Vector2(0.25f + UnityEngine.Random.value * 0.2f, 0.55f + UnityEngine.Random.value * 0.3f) * scale;
                p.sr.transform.position = pos + new Vector3((UnityEngine.Random.value - 0.5f) * 0.5f, 0, 0);
                p.sr.gameObject.SetActive(true);
                return;
            }
        }

        // ---------- Renascimento ----------
        public IEnumerator RebirthSequence(Action atMidpoint, Action<string> caption)
        {
            transitioning = true;
            float t = 0;
            caption?.Invoke("A cidade se prepara para uma nova geração…");
            while (t < 1.4f) { t += Time.deltaTime; darkness = Mathf.SmoothStep(0, 1, t / 1.4f); yield return null; }
            atMidpoint?.Invoke();
            RefreshAll(true);
            for (int i = 0; i < cityLit.Count; i++) cityLit[i].color = new Color(1, 1, 1, 0);
            caption?.Invoke("Geração " + Roman(gm.State.generation) + ": o reator reacende");
            yield return new WaitForSeconds(0.4f);
            installFlash = 1f; tapFlash = 1f; shake = 0.6f;
            t = 0;
            while (t < 1.4f) { t += Time.deltaTime; darkness = 1f - Mathf.SmoothStep(0, 1, t / 1.4f); tapFlash = Mathf.Max(tapFlash, 0.6f * (1f - t / 1.4f)); yield return null; }
            darkness = 0;
            caption?.Invoke(null);
            transitioning = false;
        }
    }
}
