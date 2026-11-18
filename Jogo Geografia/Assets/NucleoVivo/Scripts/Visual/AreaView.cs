using System.Collections.Generic;
using UnityEngine;

namespace NucleoVivo
{
    /// <summary>
    /// Áreas do mapa ao redor da usina (vila, porto, laboratório, centro de distribuição)
    /// e a vida que circula por elas: carros, pessoas, pássaros e barcaças.
    /// </summary>
    public class AreaView : MonoBehaviour
    {
        public const float HalfWidth = 5.5f;

        class Area
        {
            public AreaDef def;
            public Transform root;
            public readonly List<KeyValuePair<SpriteRenderer, Color>> parts = new List<KeyValuePair<SpriteRenderer, Color>>();
            public readonly List<SpriteRenderer> lights = new List<SpriteRenderer>();
            public readonly List<Walker> walkers = new List<Walker>();
            public SpriteRenderer fog, fence, status;
            public string statusText;
            public bool? open;
            public Rect rect;
        }

        class Walker { public SpriteRenderer sr; public float speed, min, max; public int frame; public float t; }
        class Mover { public SpriteRenderer sr; public float speed, min, max, y; public bool flip; }

        PlantView plant;
        Transform root;
        Material mat;
        GameManager gm;
        readonly List<Area> areas = new List<Area>();
        readonly List<Mover> cars = new List<Mover>();
        readonly List<Mover> birds = new List<Mover>();
        Mover barge;
        Area porto;
        System.Random rnd = new System.Random(21);
        float statusTimer;

        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        public void Init(PlantView plant, Transform worldRoot, Material material)
        {
            this.plant = plant;
            mat = material;
            gm = GameManager.Instance;
            root = new GameObject("Áreas").transform;
            root.SetParent(worldRoot, false);
            BuildRoad();
            BuildBirds();
            if (gm == null) return;
            foreach (var def in gm.Config.areas) areas.Add(BuildArea(def));
            gm.AreaOpened += _ => Refresh(false);
            gm.StateReloaded += () => Refresh(true);
            Refresh(true);
        }

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
            if (mat != null) sr.sharedMaterial = mat;
            return sr;
        }

        SpriteRenderer Part(Area a, string name, Sprite sprite, Vector2 pos, Vector2 scale, Color color, int order)
        {
            var sr = Make(name, a.root, sprite, pos, scale, color, order);
            a.parts.Add(new KeyValuePair<SpriteRenderer, Color>(sr, color));
            return sr;
        }

        SpriteRenderer Light(Area a, Vector2 pos, Vector2 size, int order)
        {
            var sr = Make("luz", a.root, Art.Pixel, pos, size, Palette.Amber, order);
            a.lights.Add(sr);
            return sr;
        }

        // ---------- vida comum a todo o mapa ----------
        void BuildRoad()
        {
            Make("Estrada", root, Art.PixelBottom, new Vector2(0, -2.6f), new Vector2(90, 0.3f), C("#2B2F30"), -13);
            for (int i = -40; i <= 40; i++)
                Make("faixa da estrada", root, Art.Pixel, new Vector2(i * 1f, -2.45f), new Vector2(0.33f, 1f / Art.WorldPPU), C("#77786F"), -13);
            Color[] paint = { C("#8A4B3C"), C("#8C8F8A"), C("#4F6B52"), C("#C9CCC6"), C("#59646A") };
            for (int i = 0; i < 9; i++)
            {
                bool right = i % 2 == 0;
                var m = new Mover
                {
                    sr = Make("carro", root, Art.Car, Vector2.zero, Vector2.one, paint[i % paint.Length], -8),
                    speed = (right ? 1 : -1) * (1.6f + (float)rnd.NextDouble() * 1.2f),
                    min = -38f, max = 38f, y = right ? -2.58f : -2.47f,
                };
                m.sr.transform.localPosition = new Vector2((float)(rnd.NextDouble() * 76 - 38), m.y);
                m.sr.flipX = !right;
                cars.Add(m);
            }
        }

        void BuildBirds()
        {
            for (int i = 0; i < 7; i++)
            {
                var m = new Mover
                {
                    sr = Make("pássaro", root, Art.Bird(0), Vector2.zero, Vector2.one, Color.white, -60),
                    speed = 0.6f + (float)rnd.NextDouble() * 0.5f, min = -40f, max = 40f, y = 3.6f + (float)rnd.NextDouble() * 2.5f,
                };
                m.sr.transform.localPosition = new Vector2((float)(rnd.NextDouble() * 80 - 40), m.y);
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
            a.rect = new Rect(def.mapX - HalfWidth, -2.7f, HalfWidth * 2, 4.6f);
            switch (def.kind)
            {
                case AreaKind.Vila: BuildVila(a); break;
                case AreaKind.Porto: BuildPorto(a); porto = a; break;
                case AreaKind.Laboratorio: BuildLab(a); break;
                case AreaKind.Distribuicao: BuildCentro(a); break;
            }

            a.fog = Make("névoa", a.root, Art.Checker, new Vector2(0, -0.3f), Vector2.one, Palette.SkyTop.WithAlpha(0.9f), -6);
            a.fog.drawMode = SpriteDrawMode.Tiled;
            a.fog.size = new Vector2(HalfWidth * 2, 4.8f);
            a.fence = Make("cerca", a.root, Art.Fence, new Vector2(0, -2.28f), Vector2.one, Color.white, -5);
            a.fence.drawMode = SpriteDrawMode.Tiled;
            a.fence.size = new Vector2(HalfWidth * 2 - 0.5f, 8f / Art.WorldPPU);
            Make("placa", a.root, Art.Sign(def.displayName.ToUpperInvariant(), Palette.ConcreteLight), new Vector2(0, 1.7f), Vector2.one, Color.white, -3);
            a.status = Make("situação", a.root, null, new Vector2(0, 0.3f), Vector2.one, Color.white, -3);
            return a;
        }

        void BuildVila(Area a)
        {
            Part(a, "rua", Art.PixelBottom, new Vector2(0, -2.0f), new Vector2(10.5f, 0.28f), C("#6B5A48"), -40);
            for (int i = 0; i < 4; i++)
            {
                float x = -3.9f + i * 2.5f + (i % 2) * 0.3f;
                var h = Part(a, "casa fundo", Art.House(i + 1), new Vector2(x, -0.7f), new Vector2(0.85f, 0.85f), new Color(0.8f, 0.8f, 0.8f), -42);
                Light(a, new Vector2(x - 0.25f, -0.35f), new Vector2(2f / Art.WorldPPU, 2f / Art.WorldPPU), -41);
            }
            for (int i = 0; i < 5; i++)
            {
                float x = -4.4f + i * 2.2f;
                Part(a, "casa", Art.House(i), new Vector2(x, -1.75f), Vector2.one, Color.white, -38);
                Light(a, new Vector2(x - 0.32f, -1.3f), new Vector2(2f / Art.WorldPPU, 3f / Art.WorldPPU), -37);
                if (i % 2 == 1) Light(a, new Vector2(x + 0.3f, -1.3f), new Vector2(2f / Art.WorldPPU, 3f / Art.WorldPPU), -37);
            }
            for (int i = 0; i < 3; i++)
            {
                float x = -3.3f + i * 3.3f;
                Part(a, "poste", Art.Lamp, new Vector2(x, -2.0f), Vector2.one, Color.white, -36);
                Light(a, new Vector2(x, -2.0f + 0.78f), new Vector2(4f / Art.WorldPPU, 1f / Art.WorldPPU), -35);
            }
            Part(a, "árvore", Art.Tree, new Vector2(4.9f, -1.9f), new Vector2(1.2f, 1.2f), Color.white, -39);
            Color[] shirts = { C("#8A4B3C"), C("#C9CCC6"), C("#4F6B52"), C("#59646A"), C("#A0705C") };
            for (int i = 0; i < 5; i++)
            {
                var sr = Part(a, "pessoa", Art.Person(0), new Vector2(-4f + i * 2f, -1.98f), Vector2.one, shirts[i], -34);
                a.walkers.Add(new Walker { sr = sr, speed = (i % 2 == 0 ? 1 : -1) * (0.25f + (float)rnd.NextDouble() * 0.2f), min = -5f, max = 5f });
            }
        }

        void BuildPorto(Area a)
        {
            Part(a, "cais", Art.PixelBottom, new Vector2(0, -2.95f), new Vector2(9.5f, 0.32f), C("#5E4436"), -11);
            for (int i = 0; i < 10; i++)
                Part(a, "estaca", Art.PixelBottom, new Vector2(-4.5f + i, -3.35f), new Vector2(2f / Art.WorldPPU, 0.42f), C("#3E2C24"), -11);
            Part(a, "pátio", Art.PixelBottom, new Vector2(0, -2.3f), new Vector2(10f, 0.9f), C("#5D6361"), -41);
            Part(a, "guindaste", Art.Crane, new Vector2(-3f, -2.35f), Vector2.one, Color.white, -36);
            Part(a, "guindaste", Art.Crane, new Vector2(2.2f, -2.35f), new Vector2(-1f, 1f), new Color(0.9f, 0.9f, 0.9f), -37);
            Color[] cont = { C("#8A4B3C"), C("#59646A"), C("#4F6B52"), C("#8C8F8A"), C("#6E4A3A") };
            int k = 0;
            for (int col = 0; col < 4; col++)
                for (int row = 0; row < 1 + (col % 3); row++)
                    Part(a, "contêiner", Art.Container, new Vector2(-1.6f + col * 0.8f, -1.95f + row * 8f / Art.WorldPPU), Vector2.one, cont[k++ % cont.Length], -38 + row);
            Part(a, "armazém", Art.ControlCenter, new Vector2(3.9f, -1.95f), new Vector2(0.75f, 0.9f), new Color(0.85f, 0.82f, 0.78f), -39);
            Light(a, new Vector2(3.9f, -1.2f), new Vector2(6f / Art.WorldPPU, 1f / Art.WorldPPU), -38);
            barge = new Mover { sr = Part(a, "barcaça", Art.Barge, new Vector2(-3f, -3.6f), Vector2.one, Color.white, -10), speed = 0.5f, min = -4.5f, max = 4.5f, y = -3.6f };
        }

        void BuildLab(Area a)
        {
            Part(a, "gramado", Art.PixelBottom, new Vector2(0, -2.15f), new Vector2(10f, 0.5f), C("#2E5A3B"), -41);
            Part(a, "laboratório", Art.LabBuilding, new Vector2(-0.6f, -1.95f), Vector2.one, Color.white, -38);
            for (int i = 0; i < 6; i++)
                Light(a, new Vector2(-1.73f + (i % 3) * 0.25f, -1.6f + (i / 3) * 0.3f), new Vector2(2f / Art.WorldPPU, 2f / Art.WorldPPU), -37);
            var beacon = Light(a, new Vector2(-1.0f, -1.95f + 48f / Art.WorldPPU * 0.86f), new Vector2(2f / Art.WorldPPU, 2f / Art.WorldPPU), -37);
            beacon.name = "farol";
            Part(a, "carro", Art.Car, new Vector2(2.5f, -2.25f), Vector2.one, C("#8C8F8A"), -36);
            Part(a, "carro", Art.Car, new Vector2(3.2f, -2.25f), Vector2.one, C("#8A4B3C"), -36);
            for (int i = 0; i < 4; i++)
                Part(a, "árvore", Art.Tree, new Vector2(-4.6f + i * 0.6f, -2.0f), Vector2.one, Color.white, -39);
        }

        void BuildCentro(Area a)
        {
            Part(a, "pátio", Art.PixelBottom, new Vector2(0, -2.3f), new Vector2(10f, 0.8f), C("#5D6361"), -41);
            Part(a, "centro de controle", Art.ControlCenter, new Vector2(1.6f, -1.95f), Vector2.one, Color.white, -38);
            for (int i = 0; i < 8; i++)
                Light(a, new Vector2(0.4f + i * 0.33f, -1.4f), new Vector2(3f / Art.WorldPPU, 2f / Art.WorldPPU), -37);
            for (int i = 0; i < 6; i++)
                Part(a, "transformador", Art.Transformer, new Vector2(-4.4f + (i % 3) * 0.75f, -2.25f + (i / 3) * 0.35f), Vector2.one, Color.white, -39 + i / 3 * -1);
            Part(a, "torre", Art.Pylon, new Vector2(-1.2f, -1.9f), Vector2.one, Palette.ConcreteLight, -40);
            Part(a, "torre", Art.Pylon, new Vector2(4.6f, -1.6f), new Vector2(0.8f, 0.8f), Palette.Concrete, -43);
        }

        // ---------- estado ----------
        public void Refresh(bool instant)
        {
            if (gm == null) return;
            foreach (var a in areas)
            {
                bool open = gm.Economy.IsAreaOpen(a.def);
                if (a.open != open)
                {
                    a.open = open;
                    a.fog.gameObject.SetActive(!open);
                    a.fence.gameObject.SetActive(!open);
                    a.status.gameObject.SetActive(!open);
                    foreach (var kv in a.parts) kv.Key.color = open ? kv.Value : Color.Lerp(kv.Value, Palette.SkyTop, 0.6f);
                    foreach (var l in a.lights) l.gameObject.SetActive(open);
                    foreach (var w in a.walkers) w.sr.gameObject.SetActive(open);
                }
                if (!open) UpdateStatus(a);
            }
            if (barge != null && porto != null) barge.sr.gameObject.SetActive(porto.open == true);
        }

        void UpdateStatus(Area a)
        {
            bool available = gm.Economy.IsAreaAvailable(a.def);
            string text = available ? "ABRIR: " + NumberFormat.Short(a.def.cost) : "LIBERA COM " + NumberFormat.Short(a.def.unlockAtProduced);
            if (text == a.statusText) return;
            a.statusText = text;
            a.status.sprite = Art.Sign(text, available ? Palette.UiPositive : Palette.UiMuted);
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
            if (statusTimer <= 0) { statusTimer = 0.5f; Refresh(false); }

            foreach (var m in cars)
            {
                var p = m.sr.transform.localPosition;
                p.x += m.speed * dt;
                // os carros passam por baixo da usina sem parar
                if (p.x > m.max) p.x = m.min; else if (p.x < m.min) p.x = m.max;
                m.sr.transform.localPosition = Snap(new Vector2(p.x, m.y));
            }
            foreach (var m in birds)
            {
                var p = m.sr.transform.localPosition;
                p.x += m.speed * dt;
                if (p.x > m.max) p.x = m.min;
                float y = m.y + Mathf.Sin(time * 0.7f + m.min + p.x * 0.3f) * 0.15f;
                m.sr.transform.localPosition = Snap(new Vector2(p.x, y));
                m.sr.sprite = Art.Bird(Mathf.FloorToInt(time * 5f + p.x) % 2);
            }
            if (barge != null && barge.sr.gameObject.activeSelf)
            {
                var p = barge.sr.transform.localPosition;
                p.x += barge.speed * dt;
                if (p.x > barge.max || p.x < barge.min) { barge.speed = -barge.speed; barge.sr.flipX = barge.speed < 0; }
                barge.sr.transform.localPosition = Snap(new Vector2(p.x, barge.y + (Mathf.Sin(time * 2f) > 0 ? 1f / Art.WorldPPU : 0f)));
            }
            foreach (var a in areas)
            {
                if (a.open != true) continue;
                foreach (var w in a.walkers)
                {
                    var p = w.sr.transform.localPosition;
                    p.x += w.speed * dt;
                    if (p.x > w.max || p.x < w.min) { w.speed = -w.speed; }
                    w.sr.flipX = w.speed < 0;
                    w.t += dt;
                    if (w.t > 0.25f) { w.t = 0; w.frame ^= 1; w.sr.sprite = Art.Person(w.frame); }
                    w.sr.transform.localPosition = Snap(new Vector2(p.x, p.y));
                }
                for (int i = 0; i < a.lights.Count; i++)
                {
                    var l = a.lights[i];
                    bool on = l.name == "farol" ? Mathf.Repeat(time, 1.2f) < 0.3f : Mathf.PerlinNoise(i * 3.1f, time * 0.3f) > 0.22f;
                    l.color = on ? (a.def.kind == AreaKind.Distribuicao ? Palette.UiPositive : Palette.Amber) : Palette.AmberDim;
                }
            }
        }

        static Vector2 Snap(Vector2 p) => new Vector2(Mathf.Round(p.x * Art.WorldPPU) / Art.WorldPPU, Mathf.Round(p.y * Art.WorldPPU) / Art.WorldPPU);
    }
}
