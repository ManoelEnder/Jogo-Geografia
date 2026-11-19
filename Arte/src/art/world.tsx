import React from "react";
import { AssetDef, FoliageMarks, GroundShadow, darken, lighten, mix, rng, scallopPath } from "./core";
import { GREENS } from "./nature";

// ---------- luz e atmosfera ----------
const lightShafts: AssetDef = {
  w: 900, h: 1100,
  render: (id) => (
    <>
      <defs>
        <linearGradient id={`ls-${id}`} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#ffe2a8" stopOpacity={0.55} />
          <stop offset="1" stopColor="#ffe2a8" stopOpacity={0} />
        </linearGradient>
      </defs>
      {[0, 1, 2, 3].map((i) => (
        <path key={i} d={`M ${70 + i * 110} 60 L ${130 + i * 110 + (i % 2) * 30} 60 L ${520 + i * 80} 1000 L ${400 + i * 80} 1000 Z`} fill={`url(#ls-${id})`} opacity={0.5 + (i % 2) * 0.3} filter={`url(#softblur-${id})`} />
      ))}
    </>
  ),
};

const vignette: AssetDef = {
  w: 512, h: 512,
  render: (id) => (
    <>
      <defs>
        <radialGradient id={`vg-${id}`} cx="0.5" cy="0.5" r="0.72">
          <stop offset="0.55" stopColor="#1a120c" stopOpacity={0} />
          <stop offset="1" stopColor="#1a120c" stopOpacity={0.75} />
        </radialGradient>
      </defs>
      <rect width={512} height={512} fill={`url(#vg-${id})`} />
    </>
  ),
};

const mist: AssetDef = {
  w: 600, h: 260,
  render: (id) => {
    const r = rng(6);
    return (
      <g filter={`url(#softblur-${id})`}>
        {Array.from({ length: 9 }).map((_, i) => (
          <ellipse key={i} cx={210 + r() * 180} cy={120 + r() * 20} rx={70 + r() * 60} ry={28 + r() * 16} fill="#e8dfd0" opacity={0.35} />
        ))}
      </g>
    );
  },
};

const steam: AssetDef = {
  w: 160, h: 140,
  render: () => {
    const r = rng(2);
    return (
      <>
        <path d={scallopPath(80, 76, 62, 48, 9, 0.45, r)} fill="#d8d4cc" />
        <path d={scallopPath(66, 64, 38, 28, 7, 0.45, r)} fill="#f2eee6" />
      </>
    );
  },
};

const softGlow: AssetDef = {
  w: 256, h: 256,
  render: () => (
    <>
      <defs>
        <radialGradient id="sg">
          <stop offset="0" stopColor="#ffffff" stopOpacity={1} />
          <stop offset="1" stopColor="#ffffff" stopOpacity={0} />
        </radialGradient>
      </defs>
      <circle cx={128} cy={128} r={128} fill="url(#sg)" />
    </>
  ),
};

// ---------- pessoas e bichos ----------
const worker = (shirt: string, pants: string, frame: number, hat?: string): AssetDef => ({
  w: 60, h: 110,
  render: (id) => {
    const legA = frame === 0 ? 6 : -6;
    return (
      <>
        <GroundShadow id={id} cx={32} cy={102} rx={16} ry={4} opacity={0.4} />
        <path d={`M 24 72 L ${24 + legA} 100`} stroke={pants} strokeWidth={8} strokeLinecap="round" />
        <path d={`M 36 72 L ${36 - legA} 100`} stroke={darken(pants, 0.2)} strokeWidth={8} strokeLinecap="round" />
        <path d="M 16 46 Q 30 38 44 46 L 42 76 L 18 76 Z" fill={shirt} />
        <path d="M 16 46 Q 22 42 28 41 L 26 76 L 18 76 Z" fill={lighten(shirt, 0.18)} />
        <path d={`M 18 50 L ${12 - legA / 2} 70`} stroke={shirt} strokeWidth={6} strokeLinecap="round" />
        <path d={`M 42 50 L ${48 + legA / 2} 70`} stroke={darken(shirt, 0.15)} strokeWidth={6} strokeLinecap="round" />
        <circle cx={30} cy={32} r={10} fill="#c9946e" />
        <path d="M 20 30 Q 30 18 40 30 Q 36 24 30 24 Q 24 24 20 30 Z" fill="#4a3428" />
        {hat && <path d="M 18 28 Q 30 14 42 28 Z" fill={hat} />}
        {hat && <rect x={16} y={26} width={28} height={4} rx={2} fill={darken(hat, 0.2)} />}
      </>
    );
  },
});

const bird = (frame: number): AssetDef => ({
  w: 40, h: 24,
  render: () => (
    <path d={frame === 0 ? "M 2 6 Q 12 14 20 14 Q 28 14 38 6 Q 28 10 20 18 Q 12 10 2 6 Z" : "M 2 16 Q 12 10 20 14 Q 28 10 38 16 Q 28 14 20 20 Q 12 14 2 16 Z"} fill="#2e2a26" />
  ),
});

// ---------- horizonte ----------
const sky: AssetDef = {
  w: 64, h: 512,
  render: (id) => (
    <>
      <defs>
        <linearGradient id={`sk-${id}`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#5f6a7a" />
          <stop offset="0.55" stopColor="#c9a184" />
          <stop offset="1" stopColor="#f0c98f" />
        </linearGradient>
      </defs>
      <rect width={64} height={512} fill={`url(#sk-${id})`} />
    </>
  ),
};

const hillsFar: AssetDef = {
  w: 1600, h: 320,
  render: () => {
    const r = rng(17);
    const layer = (base: number, amp: number, col: string, k: string) => {
      let d = `M 0 ${base}`;
      for (let x = 0; x <= 1600; x += 80) d += ` Q ${x + 40} ${base - amp * (0.5 + r())} ${x + 80} ${base - amp * 0.4 * r()}`;
      return <path key={k} d={`${d} L 1600 320 L 0 320 Z`} fill={col} />;
    };
    return [layer(170, 70, "#9a8f86", "a"), layer(210, 60, "#7f7a68", "b"), layer(250, 50, "#636548", "c")];
  },
};

const city = (lit: boolean, seed: number): AssetDef => ({
  w: 800, h: 300,
  render: (id) => {
    const r = rng(seed);
    const out: React.ReactNode[] = [];
    let x = 0;
    while (x < 800) {
      const w = 40 + r() * 60, h = 60 + r() * 180, body = mix("#6e6a6c", "#8c8079", r());
      if (!lit) out.push(<rect key={`b${x}`} x={x} y={300 - h} width={w} height={h} fill={body} />);
      const tower = r() < 0.4;
      if (!lit && tower) out.push(<rect key={`t${x}`} x={x + w / 2 - 2} y={300 - h - 20} width={4} height={20} fill={body} />);
      for (let wy = 300 - h + 12; wy < 290; wy += 16)
        for (let wx = x + 8; wx < x + w - 10; wx += 14)
          if (r() < (lit ? 0.6 : 0.08)) {
            out.push(<rect key={`w${wx}-${wy}`} x={wx} y={wy} width={6} height={8} fill={lit ? "#ffcf7a" : "#d9b77a"} opacity={lit ? 1 : 0.6} />);
            if (lit) out.push(<rect key={`g${wx}-${wy}`} x={wx - 4} y={wy - 4} width={14} height={16} fill="#ffcf7a" opacity={0.3} filter={`url(#blur-${id})`} />);
          }
      x += w + 4;
    }
    return out;
  },
});

/** Muralha de floresta densa para a borda de cima do mapa. */
const forestWall: AssetDef = {
  w: 1600, h: 520,
  render: (id) => {
    const r = rng(29);
    const out: React.ReactNode[] = [];
    const pals = [GREENS.pine, GREENS.olive, GREENS.green, GREENS.pine, GREENS.autumnOrange];
    for (let row = 0; row < 3; row++) {
      for (let i = 0; i < 26; i++) {
        const x = i * 64 + r() * 40 - 20, base = 260 + row * 110 + r() * 30;
        const pal = pals[Math.floor(r() * (row === 2 ? 5 : 4))];
        const rx = 70 + r() * 30, ry = 110 + r() * 40;
        const shade = row === 0 ? 0.35 : row === 1 ? 0.18 : 0;
        out.push(<path key={`d${row}-${i}`} d={scallopPath(x, base - ry * 0.6, rx, ry, 10, 0.35, r)} fill={darken(pal[0], shade)} transform="translate(6 10)" />);
        out.push(<path key={`m${row}-${i}`} d={scallopPath(x, base - ry * 0.6, rx, ry, 10, 0.35, r)} fill={darken(pal[1], shade)} />);
        out.push(<path key={`h${row}-${i}`} d={scallopPath(x - rx * 0.3, base - ry * 0.85, rx * 0.5, ry * 0.4, 7, 0.35, r)} fill={darken(pal[2], shade)} />);
        out.push(<FoliageMarks key={`f${row}-${i}`} cx={x} cy={base - ry * 0.6} rx={rx * 0.7} ry={ry * 0.5} n={3} color={darken(pal[3], shade)} r={r} />);
      }
    }
    void id;
    return out;
  },
};

export const world: Record<string, AssetDef> = {
  light_shafts: lightShafts,
  vignette,
  mist,
  steam,
  soft_glow: softGlow,
  worker_a_0: worker("#6b7a4a", "#3a3a40", 0, "#c9a66a"),
  worker_a_1: worker("#6b7a4a", "#3a3a40", 1, "#c9a66a"),
  worker_b_0: worker("#8a4a3a", "#45403a", 0),
  worker_b_1: worker("#8a4a3a", "#45403a", 1),
  worker_c_0: worker("#e0d6c0", "#4a4a52", 0),
  worker_c_1: worker("#e0d6c0", "#4a4a52", 1),
  bird_0: bird(0),
  bird_1: bird(1),
  sky,
  hills_far: hillsFar,
  city_dark_1: city(false, 3),
  city_lit_1: city(true, 3),
  city_dark_2: city(false, 8),
  city_lit_2: city(true, 8),
  forest_wall: forestWall,
};
