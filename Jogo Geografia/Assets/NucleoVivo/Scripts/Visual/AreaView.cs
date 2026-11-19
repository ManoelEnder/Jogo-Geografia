using System.Collections.Generic;
using UnityEngine;

namespace NucleoVivo
{
    /// <summary>
    /// Áreas do mapa ao redor da usina (vila, porto, laboratório, centro de distribuição)
    /// e a vida que circula por elas: moradores, carroças na estrada, pássaros e a barcaça.
    /// </summary>
    public class AreaView : MonoBehaviour
    {
        public const float HalfWidth = 5.5f;

        class Area
        {
            public AreaDef def;
            public Transform root;
            public readonly List<SpriteRenderer> parts = new List<SpriteRenderer>();
            public readonly List<SpriteRenderer> lights = new List<SpriteRenderer>();
            public readonly List<Walker> walkers = new List<Walker>();
            public readonly List<GameObject> lockedOnly = new List<GameObject>();
            public TextMesh title, status;
            public string statusText;
            public bool? open;
            public Rect rect;
        }

        class Walker { public SpriteRenderer sr; public string look; public float speed, min, max, t; public int frame; }
        class Mover { public SpriteRenderer sr; public float speed, min, max, y; public string look; public int frame; public float t; }

        PlantView plant;
        Transform root;
        GameManager gm;
        readonly List<Area> areas = new List<Area>();
        readonly List<Mover> travelers = new List<Mover>();
        readonly List<Mover> birds = new List<Mover>();
        Mover barge;
        Area porto;
        readonly System.Random rnd = new System.Random(21);
        float statusTimer;

        float R() => (float)rnd.NextDouble();

        public void Init(PlantView plant, Transform worldRoot, Material material)
        {
            this.plant = plant;
            gm = GameManager.Instance;
            root = new GameObject("Áreas").transform;
            root.SetParent(worldRoot, false);
            BuildTravelers();
            BuildBirds();
            if (gm == null) return;
            foreach (var def in gm.Config.areas) areas.Add(BuildArea(def));
            gm.AreaOpened += _ => Refresh();
            gm.StateReloaded += Refresh;
            Refresh();
        }

        SpriteRenderer Put(Area a, string sprite, Vector2 local, float scale = 1f, bool flip = false)
        {
            var sr = plant.Put(sprite, a.root, local, scale, null, flip);
            a.parts.Add(sr);
            return sr;
        }

        SpriteRenderer Light(Area a, string sprite, Vector2 local, float scale = 1f)
        {
            var sr = plant.Put(sprite, a.root, local, scale);
            sr.sortingOrder += 1;
            a.lights.Add(sr);
            return sr;
        }

        // ---------- vida comum ----------
        void BuildTravelers()
        {
            string[] looks = { "worker_a", "worker_b", "worker_c" };
            for (int i = 0; i < 8; i++)
            {
                bool right = i % 2 == 0;
                string look = looks[i % looks.Length];
                float y = right ? -5.75f : -5.3f;
                var m = new Mover { look = look, speed = (right ? 1 : -1) * (0.55f + R() * 0.35f), min = -40f, max = 40f, y = y };
                m.sr = plant.Put(look + "_0", root, new Vector2(-38f + R() * 76f, y), 0.9f);
                m.sr.flipX = !right;
                travelers.Add(m);
            }
            var cart = new Mover { look = null, speed = 0.8f, min = -40f, max = 40f, y = -5.6f };
            cart.sr = plant.Put("cart", root, new Vector2(-20f, -5.6f), 0.8f);
            travelers.Add(cart);
        }

        void BuildBirds()
        {
            for (int i = 0; i < 9; i++)
            {
                var m = new Mover { speed = 1.1f + R() * 0.7f, min = -44f, max = 44f, y = 5f + R() * 7f };
                m.sr = plant.Put("bird_0", root, new Vector2(-40f + R() * 80f, m.y), 0.7f + R() * 0.3f, 31500);
                birds.Add(m);
            }
        }

        // ---------- áreas ----------
        Area BuildArea(AreaDef def)
        {
            var a = new Area { def = def };
            a.root = new GameObject(def.displayName).transform;
            a.root.SetParent(root, false);
            a.root.localPosition = new Vector2(def.mapX, 0);
            switch (def.kind)
            {
                case AreaKind.Vila: BuildVila(a); a.rect = new Rect(def.mapX - HalfWidth, -5f, HalfWidth * 2, 9f); break;
                case AreaKind.Porto: BuildPorto(a); porto = a; a.rect = new Rect(def.mapX - 6f, -11.5f, 12f, 7.5f); break;
                case AreaKind.Laboratorio: BuildLab(a); a.rect = new Rect(def.mapX - HalfWidth, -5f, HalfWidth * 2, 9f); break;
                case AreaKind.Distribuicao: BuildCentro(a); a.rect = new Rect(def.mapX - HalfWidth, -5f, HalfWidth * 2, 9f); break;
            }

            // névoa, cerca e placas enquanto a área está fechada
            Vector2 c = a.rect.center - new Vector2(def.mapX, 0);
            for (int i = 0; i < 7; i++)
            {
                var m = plant.Put("mist", a.root, c + new Vector2((i - 3) * 1.4f, (i % 3 - 1) * 1.5f), 1.4f, 29000);
                m.color = new Color(1, 1, 1, 0.92f);
                a.lockedOnly.Add(m.gameObject);
            }
            for (int i = 0; i < 5; i++)
                a.lockedOnly.Add(plant.Put("fence", a.root, new Vector2(a.rect.xMin - def.mapX + 0.8f + i * 2.1f, a.rect.yMin + 0.6f), 1f, 29100).gameObject);
            var board = plant.Put("sign_board", a.root, new Vector2(c.x, a.rect.yMin + 1.4f), 1f, 29200);
            a.lockedOnly.Add(board.gameObject);
            a.title = Label(board.transform, new Vector2(0.1f, 1.68f), 40, 0.11f, 29201);
            a.title.text = def.displayName;
            a.status = Label(board.transform, new Vector2(0.1f, 1.18f), 36, 0.1f, 29201);
            return a;
        }

        TextMesh Label(Transform parent, Vector2 pos, int size, float charSize, int order)
        {
            var go = new GameObject("texto");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, -0.01f);
            var t = go.AddComponent<TextMesh>();
            t.font = UIFactory.DefaultFont;
            t.fontSize = size;
            t.characterSize = charSize;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.color = new Color32(0xf1, 0xe6, 0xcf, 255);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = t.font.material;
            mr.sortingOrder = order;
            return t;
        }

        void BuildVila(Area a)
        {
            Vector2[] homes = { new Vector2(-3.4f, 1.4f), new Vector2(0.2f, 2.2f), new Vector2(3.4f, 1.2f), new Vector2(-2.2f, -1.8f), new Vector2(2.0f, -2.0f) };
            for (int i = 0; i < homes.Length; i++)
            {
                string v = (i % 3 + 1).ToString();
                Put(a, "cabin_" + v, homes[i], 1f);
                Light(a, "cabin_" + v + "_lights", homes[i], 1f);
            }
            for (int i = 0; i < 6; i++) plant.Put(i % 2 == 0 ? "dirt_1" : "dirt_2", a.root, new Vector2(-4f + i * 1.6f, -0.4f + (i % 2) * 0.3f), 0.8f, -30700);
            foreach (var x in new[] { -4.6f, 0f, 4.4f })
            {
                Put(a, "lamp_post", new Vector2(x, -0.6f));
                Light(a, "lamp_post_lights", new Vector2(x, -0.6f));
            }
            Put(a, "cart", new Vector2(4.2f, -2.8f), 0.9f);
            Put(a, "bush_flower", new Vector2(-4.6f, -2.6f), 0.9f);
            Put(a, "tree_orange_1", new Vector2(5.0f, 2.6f), 0.9f);
            Put(a, "flowers_yellow", new Vector2(-1f, -3.4f), 0.9f);
            Put(a, "lavender", new Vector2(0.8f, 0.6f), 0.8f);
            string[] looks = { "worker_a", "worker_b", "worker_c", "worker_b" };
            for (int i = 0; i < looks.Length; i++)
            {
                var sr = Put(a, looks[i] + "_0", new Vector2(-4f + i * 2.4f, -0.5f + (i % 2) * 0.4f), 0.9f);
                a.walkers.Add(new Walker { sr = sr, look = looks[i], speed = (i % 2 == 0 ? 1 : -1) * (0.35f + R() * 0.25f), min = -4.8f, max = 4.8f });
            }
        }

        void BuildPorto(Area a)
        {
            Put(a, "dock", new Vector2(0, -9.6f), 1.4f);
            Put(a, "crane", new Vector2(-3.0f, -8.4f), 1f);
            Put(a, "crane", new Vector2(2.6f, -8.3f), 1f, true);
            string[] cols = { "container_red", "container_green", "container_ochre", "container_grey" };
            int k = 0;
            for (int col = 0; col < 4; col++)
                for (int row = 0; row < 1 + (col % 3); row++)
                {
                    var sr = Put(a, cols[k++ % cols.Length], new Vector2(-1.4f + col * 1.25f + row * 0.15f, -7.2f + row * 0.55f), 0.95f);
                    sr.sortingOrder = PlantView.Depth(-7.2f + col * 0.05f) + row * 2;
                }
            Put(a, "warehouse", new Vector2(4.4f, -6.6f), 0.95f);
            Put(a, "lamp_post", new Vector2(-4.8f, -7.0f));
            Light(a, "lamp_post_lights", new Vector2(-4.8f, -7.0f));
            Put(a, "worker_c_0", new Vector2(0.4f, -8.9f), 0.85f);
            var b = plant.Put("barge", a.root, new Vector2(-3f, -10.9f), 1.1f, PlantView.Depth(-10.9f));
            a.parts.Add(b);
            barge = new Mover { sr = b, speed = 0.45f, min = -5.5f, max = 5.5f, y = -10.9f };
        }

        void BuildLab(Area a)
        {
            Put(a, "lab", new Vector2(-0.4f, 0.4f), 1f);
            Light(a, "lab_lights", new Vector2(-0.4f, 0.4f), 1f);
            Put(a, "cart", new Vector2(3.2f, -2.4f), 0.85f);
            Put(a, "lamp_post", new Vector2(-3.6f, -1.6f));
            Light(a, "lamp_post_lights", new Vector2(-3.6f, -1.6f));
            for (int i = 0; i < 4; i++) Put(a, i % 2 == 0 ? "tree_green_1" : "pine_2", new Vector2(-4.8f + i * 0.9f, 2.4f + (i % 2) * 0.5f), 0.85f);
            Put(a, "flowers_white", new Vector2(1.2f, -2.2f), 0.9f);
            Put(a, "worker_c_0", new Vector2(1.6f, -1.5f), 0.9f);
        }

        void BuildCentro(Area a)
        {
            Put(a, "control_center", new Vector2(1.2f, 0.6f), 1f);
            Light(a, "control_center_lights", new Vector2(1.2f, 0.6f), 1f);
            for (int i = 0; i < 6; i++) Put(a, "transformer", new Vector2(-4.4f + (i % 3) * 1.25f, -2.2f + (i / 3) * 0.7f), 0.95f);
            Put(a, "pylon", new Vector2(-1.4f, 2.4f), 0.9f);
            Put(a, "pylon", new Vector2(4.6f, -2.6f), 0.85f);
            Put(a, "lamp_post", new Vector2(-0.6f, -2.6f));
            Light(a, "lamp_post_lights", new Vector2(-0.6f, -2.6f));
        }

        // ---------- estado ----------
        public void Refresh()
        {
            if (gm == null) return;
            foreach (var a in areas)
            {
                bool open = gm.Economy.IsAreaOpen(a.def);
                if (a.open != open)
                {
                    a.open = open;
                    foreach (var go in a.lockedOnly) go.SetActive(!open);
                    foreach (var p in a.parts) p.color = open ? Color.white : new Color(0.45f, 0.45f, 0.5f, 1f);
                    foreach (var l in a.lights) l.gameObject.SetActive(open);
                    foreach (var w in a.walkers) w.sr.gameObject.SetActive(open);
                }
                if (!open)
                {
                    bool available = gm.Economy.IsAreaAvailable(a.def);
                    string text = available ? "Abrir: " + NumberFormat.Short(a.def.cost) : "Libera com " + NumberFormat.Short(a.def.unlockAtProduced);
                    if (text != a.statusText)
                    {
                        a.statusText = text;
                        a.status.text = text;
                        a.status.color = available ? new Color32(0xc9, 0xe0, 0x9a, 255) : new Color32(0xd9, 0xc8, 0xa8, 255);
                    }
                }
            }
            if (barge != null && porto != null) barge.sr.gameObject.SetActive(porto.open == true);
        }

        /// <summary>Centro de uma área no mapa (para a câmera).</summary>
        public Vector2 CenterOf(AreaDef def)
        {
            foreach (var a in areas) if (a.def == def) return a.rect.center;
            return new Vector2(def.mapX, -0.5f);
        }

        /// <summary>Área sob um ponto do mundo (ou null).</summary>
        public AreaDef AreaAt(Vector2 world)
        {
            foreach (var a in areas) if (a.rect.Contains(world)) return a.def;
            return null;
        }

        void Update()
        {
            float dt = Time.deltaTime, time = Time.time;
            statusTimer -= dt;
            if (statusTimer <= 0) { statusTimer = 0.5f; Refresh(); }

            foreach (var m in travelers)
            {
                var p = m.sr.transform.localPosition;
                p.x += m.speed * dt;
                if (p.x > m.max) p.x = m.min; else if (p.x < m.min) p.x = m.max;
                m.sr.transform.localPosition = new Vector3(p.x, m.y, 0);
                if (m.look != null) Step(m.sr, m.look, ref m.t, ref m.frame, dt);
            }
            foreach (var m in birds)
            {
                var p = m.sr.transform.localPosition;
                p.x += m.speed * dt;
                if (p.x > m.max) p.x = m.min;
                m.sr.transform.localPosition = new Vector3(p.x, m.y + Mathf.Sin(time * 0.7f + m.min + p.x * 0.3f) * 0.3f, 0);
                m.sr.sprite = Pinturas.Get(Mathf.FloorToInt(time * 4f + m.y) % 2 == 0 ? "bird_0" : "bird_1");
            }
            if (barge != null && barge.sr.gameObject.activeSelf)
            {
                var p = barge.sr.transform.localPosition;
                p.x += barge.speed * dt;
                if (p.x > barge.max || p.x < barge.min) { barge.speed = -barge.speed; barge.sr.flipX = barge.speed < 0; }
                barge.sr.transform.localPosition = new Vector3(p.x, barge.y + Mathf.Sin(time * 1.6f) * 0.04f, 0);
            }
            foreach (var a in areas)
            {
                if (a.open != true) continue;
                foreach (var w in a.walkers)
                {
                    var p = w.sr.transform.localPosition;
                    p.x += w.speed * dt;
                    if (p.x > w.max || p.x < w.min) w.speed = -w.speed;
                    w.sr.flipX = w.speed < 0;
                    w.sr.transform.localPosition = p;
                    Step(w.sr, w.look, ref w.t, ref w.frame, dt);
                }
                for (int i = 0; i < a.lights.Count; i++)
                    a.lights[i].color = new Color(1, 1, 1, 0.7f + 0.3f * Mathf.PerlinNoise(i * 3.1f + a.def.mapX, time * 0.5f));
            }
        }

        static void Step(SpriteRenderer sr, string look, ref float t, ref int frame, float dt)
        {
            t += dt;
            if (t < 0.28f) return;
            t = 0;
            frame ^= 1;
            sr.sprite = Pinturas.Get(look + "_" + frame);
        }
    }
}
