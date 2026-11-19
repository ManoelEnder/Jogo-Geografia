import React from "react";
import { AssetDef, FoliageMarks, GroundShadow, blobPath, darken, lighten, mix, rng, scallopPath } from "./core";

// Paletas próprias (fim de tarde, tons terrosos e musgo).
export const GREENS = {
  pine: ["#3f4a2c", "#55603a", "#6f7a48", "#8e9659"],
  olive: ["#4c5130", "#666b3d", "#82884c", "#a3a661"],
  green: ["#3d5233", "#4f6a3f", "#6a8650", "#8ea465"],
  autumnRed: ["#6e2c35", "#923d45", "#b45a57", "#cf8170"],
  autumnOrange: ["#7a3f2c", "#a35a3a", "#c47b4b", "#dca067"],
  rose: ["#5c2e45", "#7e4159", "#a05d72", "#c3848f"],
};

// ---------- árvores ----------
const pine = (seed: number, pal: string[], scale = 1): AssetDef => ({
  w: 260, h: 400,
  render: (id) => {
    const r = rng(seed);
    const cx = 130, base = 372;
    const tiers = 5;
    const out: React.ReactNode[] = [];
    out.push(<GroundShadow key="sh" id={id} cx={cx + 26} cy={base - 4} rx={86 * scale} ry={20} />);
    // tronco
    out.push(<path key="trunk" d={`M ${cx - 11} ${base - 70} L ${cx - 15} ${base} Q ${cx} ${base + 6} ${cx + 15} ${base} L ${cx + 11} ${base - 70} Z`} fill="#4a3428" />);
    out.push(<path key="trunkL" d={`M ${cx - 11} ${base - 70} L ${cx - 15} ${base} L ${cx - 4} ${base + 2} L ${cx - 3} ${base - 70} Z`} fill="#6b4b37" />);
    for (let i = 0; i < tiers; i++) {
      const t = i / (tiers - 1);
      const y = base - 70 - i * 56 * scale;
      const hw = (112 - i * 21) * scale;
      const top = y - 92 * scale;
      const n = 5 - Math.floor(i / 2);
      let d = `M ${cx - hw} ${y}`;
      for (let k = 0; k < n; k++) {
        const x0 = cx - hw + (2 * hw * k) / n, x1 = cx - hw + (2 * hw * (k + 1)) / n;
        d += ` Q ${(x0 + x1) / 2} ${y + 20 * scale + r() * 8} ${x1} ${y}`;
      }
      d += ` Q ${cx + hw * 0.45} ${y - 40 * scale} ${cx + 4} ${top} Q ${cx - hw * 0.45} ${y - 40 * scale} ${cx - hw} ${y} Z`;
      const clip = `pineclip-${id}-${i}`;
      out.push(<path key={`s${i}`} d={d} fill={pal[0]} transform="translate(5 9)" />);
      out.push(<clipPath key={`c${i}`} id={clip}><path d={d} /></clipPath>);
      out.push(<path key={`b${i}`} d={d} fill={pal[1]} />);
      out.push(
        <g key={`h${i}`} clipPath={`url(#${clip})`}>
          <ellipse cx={cx - hw * 0.5} cy={y - 40 * scale} rx={hw * 0.75} ry={60 * scale} fill={pal[2]} />
          <ellipse cx={cx - hw * 0.7} cy={y - 55 * scale} rx={hw * 0.35} ry={30 * scale} fill={mix(pal[3], "#f3d79a", 0.25 + t * 0.1)} opacity={0.85} />
          <FoliageMarks cx={cx} cy={y - 25 * scale} rx={hw * 0.75} ry={28} n={3} color={pal[3]} r={r} width={2.5} />
        </g>,
      );
    }
    return out;
  },
});

const roundTree = (seed: number, pal: string[], opts: { trunk?: string; bare?: boolean } = {}): AssetDef => ({
  w: 320, h: 420,
  render: (id) => {
    const r = rng(seed);
    const cx = 160, base = 395;
    const out: React.ReactNode[] = [];
    out.push(<GroundShadow key="sh" id={id} cx={cx + 30} cy={base - 2} rx={110} ry={26} />);
    const trunk = opts.trunk ?? "#3f2e25";
    out.push(<path key="t" d={`M ${cx - 12} ${base - 150} C ${cx - 10} ${base - 60} ${cx - 20} ${base - 20} ${cx - 22} ${base} L ${cx + 18} ${base} C ${cx + 12} ${base - 30} ${cx + 10} ${base - 80} ${cx + 12} ${base - 150} Z`} fill={trunk} />);
    out.push(<path key="tl" d={`M ${cx - 12} ${base - 150} C ${cx - 10} ${base - 60} ${cx - 20} ${base - 20} ${cx - 22} ${base} L ${cx - 8} ${base} C ${cx - 6} ${base - 60} ${cx - 4} ${base - 100} ${cx - 2} ${base - 150} Z`} fill={lighten(trunk, 0.18)} />);
    // galhos finos aparecendo abaixo da copa
    for (let i = 0; i < 6; i++) {
      const sx = cx + (r() - 0.5) * 16, sy = base - 90 - r() * 60;
      const ex = cx + (r() - 0.5) * 220, ey = sy - 40 - r() * 70;
      out.push(<path key={`g${i}`} d={`M ${sx} ${sy} Q ${(sx + ex) / 2} ${sy - 10} ${ex} ${ey}`} stroke={trunk} strokeWidth={3 + r() * 3} fill="none" strokeLinecap="round" />);
    }
    // copa: camadas do fundo (escuro) para a frente (claro)
    const clusters: { x: number; y: number; rx: number; ry: number; layer: number }[] = [];
    const cyc = base - 255;
    for (let i = 0; i < 9; i++) {
      const a = (i / 9) * Math.PI * 2 + r();
      clusters.push({ x: cx + Math.cos(a) * 70 + (r() - 0.5) * 20, y: cyc + Math.sin(a) * 60 + (r() - 0.5) * 20, rx: 62 + r() * 22, ry: 54 + r() * 18, layer: 0 });
    }
    clusters.push({ x: cx - 10, y: cyc - 20, rx: 90, ry: 80, layer: 1 });
    clusters.push({ x: cx - 45, y: cyc - 50, rx: 55, ry: 45, layer: 2 });
    clusters.push({ x: cx + 30, y: cyc - 70, rx: 45, ry: 38, layer: 2 });
    clusters.push({ x: cx - 70, y: cyc + 10, rx: 40, ry: 34, layer: 2 });
    const shapes = clusters.map((c) => scallopPath(c.x, c.y, c.rx, c.ry, 9, 0.38, r));
    // sombra interna (embaixo/direita)
    shapes.forEach((d, i) => clusters[i].layer === 0 && out.push(<path key={`d${i}`} d={d} fill={pal[0]} transform="translate(6 12)" />));
    shapes.forEach((d, i) => clusters[i].layer === 0 && out.push(<path key={`m${i}`} d={d} fill={pal[1]} />));
    shapes.forEach((d, i) => clusters[i].layer === 1 && out.push(<path key={`l${i}`} d={d} fill={pal[2]} />));
    shapes.forEach((d, i) => clusters[i].layer === 2 && out.push(<path key={`h${i}`} d={d} fill={pal[3]} opacity={0.9} />));
    out.push(<FoliageMarks key="fm" cx={cx - 10} cy={cyc - 10} rx={110} ry={90} n={16} color={lighten(pal[3], 0.15)} r={r} />);
    out.push(<FoliageMarks key="fm2" cx={cx + 20} cy={cyc + 30} rx={110} ry={70} n={10} color={pal[0]} r={r} />);
    if (opts.bare) {
      for (let i = 0; i < 5; i++) {
        const sx = cx + (r() - 0.5) * 100, sy = cyc + 40 + r() * 30;
        out.push(<path key={`b${i}`} d={`M ${sx} ${sy} l ${(r() - 0.5) * 80} ${-60 - r() * 60}`} stroke={darken(trunk, 0.2)} strokeWidth={2} fill="none" />);
      }
    }
    return out;
  },
});

const bush = (seed: number, pal: string[], berries?: string, flowers?: string): AssetDef => ({
  w: 200, h: 150,
  render: (id) => {
    const r = rng(seed);
    const out: React.ReactNode[] = [];
    out.push(<GroundShadow key="sh" id={id} cx={112} cy={130} rx={80} ry={14} />);
    const cl = [
      { x: 70, y: 95, rx: 50, ry: 38 }, { x: 130, y: 98, rx: 52, ry: 36 }, { x: 100, y: 70, rx: 58, ry: 44 },
      { x: 80, y: 62, rx: 30, ry: 24 },
    ];
    cl.forEach((c, i) => out.push(<path key={`s${i}`} d={scallopPath(c.x, c.y, c.rx, c.ry, 8, 0.35, r)} fill={pal[0]} transform="translate(4 8)" />));
    cl.slice(0, 3).forEach((c, i) => out.push(<path key={`m${i}`} d={scallopPath(c.x, c.y, c.rx, c.ry, 8, 0.35, r)} fill={i === 2 ? pal[2] : pal[1]} />));
    out.push(<path key="h" d={scallopPath(cl[3].x, cl[3].y, cl[3].rx, cl[3].ry, 7, 0.35, r)} fill={pal[3]} opacity={0.85} />);
    out.push(<FoliageMarks key="fm" cx={100} cy={85} rx={70} ry={35} n={7} color={pal[3]} r={r} width={2.5} />);
    const dot = berries ?? flowers;
    if (dot) {
      for (let i = 0; i < 16; i++) {
        const a = r() * Math.PI * 2, d = Math.sqrt(r());
        const x = 100 + Math.cos(a) * 75 * d, y = 85 + Math.sin(a) * 40 * d;
        out.push(<circle key={`d${i}`} cx={x} cy={y} r={berries ? 4 + r() * 2 : 5} fill={dot} />);
        out.push(<circle key={`dh${i}`} cx={x - 1.5} cy={y - 1.5} r={1.6} fill={lighten(dot, 0.5)} />);
      }
    }
    return out;
  },
});

// ---------- plantas baixas ----------
const grassTuft = (seed: number, cols: string[], h = 70, tips?: string): AssetDef => ({
  w: 120, h: h + 20,
  render: (id) => {
    const r = rng(seed);
    const out: React.ReactNode[] = [];
    const base = h + 12;
    out.push(<GroundShadow key="sh" id={id} cx={64} cy={base} rx={40} ry={7} opacity={0.25} />);
    for (let i = 0; i < 26; i++) {
      const x = 60 + (r() - 0.5) * 60;
      const len = h * (0.5 + r() * 0.5);
      const bend = (r() - 0.5) * 40;
      const c = cols[Math.floor(r() * cols.length)];
      out.push(<path key={i} d={`M ${x - 3} ${base} Q ${x + bend * 0.3} ${base - len * 0.6} ${x + bend} ${base - len} Q ${x + bend * 0.3 + 2} ${base - len * 0.55} ${x + 3} ${base} Z`} fill={c} />);
      if (tips && r() < 0.5) out.push(<ellipse key={`t${i}`} cx={x + bend} cy={base - len} rx={3} ry={7} fill={tips} transform={`rotate(${bend * 0.6} ${x + bend} ${base - len})`} />);
    }
    return out;
  },
});

const lavender: AssetDef = {
  w: 170, h: 130,
  render: (id) => {
    const r = rng(77);
    const out: React.ReactNode[] = [];
    out.push(<GroundShadow key="sh" id={id} cx={90} cy={118} rx={60} ry={9} opacity={0.25} />);
    for (let i = 0; i < 18; i++) {
      const x = 85 + (r() - 0.5) * 120, len = 60 + r() * 45, b = (r() - 0.5) * 20;
      out.push(<path key={`s${i}`} d={`M ${x} 118 Q ${x + b / 2} ${118 - len / 2} ${x + b} ${118 - len}`} stroke="#5b6a3a" strokeWidth={2.5} fill="none" />);
      for (let k = 0; k < 7; k++) {
        const t = 0.55 + k * 0.065;
        const px = x + b * t, py = 118 - len * t;
        out.push(<ellipse key={`f${i}-${k}`} cx={px + (k % 2 ? 3 : -3)} cy={py} rx={4.5} ry={3.5} fill={k > 4 ? "#a08bc7" : "#7a65a6"} />);
      }
    }
    return out;
  },
};

const flowers = (seed: number, petal: string): AssetDef => ({
  w: 140, h: 80,
  render: (id) => {
    const r = rng(seed);
    const out: React.ReactNode[] = [];
    for (let i = 0; i < 9; i++) {
      const x = 70 + (r() - 0.5) * 110, y = 40 + (r() - 0.5) * 50;
      out.push(<path key={`l${i}`} d={`M ${x} ${y + 12} l -8 -6 M ${x} ${y + 12} l 8 -7`} stroke="#5c6b38" strokeWidth={3} strokeLinecap="round" />);
      out.push(<path key={`st${i}`} d={`M ${x} ${y + 16} L ${x} ${y}`} stroke="#5c6b38" strokeWidth={2} />);
      for (let p = 0; p < 6; p++) {
        const a = (p / 6) * Math.PI * 2;
        out.push(<ellipse key={`p${i}-${p}`} cx={x + Math.cos(a) * 5} cy={y + Math.sin(a) * 4} rx={4} ry={3} fill={petal} transform={`rotate(${(a * 180) / Math.PI} ${x + Math.cos(a) * 5} ${y + Math.sin(a) * 4})`} />);
      }
      out.push(<circle key={`c${i}`} cx={x} cy={y} r={3} fill="#5a3a22" />);
    }
    void id;
    return out;
  },
});

const clover: AssetDef = {
  w: 160, h: 80,
  render: () => {
    const r = rng(12);
    const out: React.ReactNode[] = [];
    for (let i = 0; i < 16; i++) {
      const x = 80 + (r() - 0.5) * 140, y = 40 + (r() - 0.5) * 50;
      const c = r() < 0.5 ? "#5d7a3e" : "#6f8f48";
      for (let k = 0; k < 3; k++) {
        const a = (k / 3) * Math.PI * 2 - Math.PI / 2;
        out.push(<circle key={`${i}-${k}`} cx={x + Math.cos(a) * 6} cy={y + Math.sin(a) * 5} r={6} fill={c} />);
      }
      out.push(<circle key={`h${i}`} cx={x - 2} cy={y - 3} r={2.5} fill="#8fae5c" />);
    }
    return out;
  },
};

const rock = (seed: number, size: number): AssetDef => ({
  w: 160, h: 120,
  render: (id) => {
    const r = rng(seed);
    const out: React.ReactNode[] = [];
    out.push(<GroundShadow key="sh" id={id} cx={92} cy={96} rx={60 * size} ry={14 * size} />);
    const rx = 58 * size, ry = 38 * size;
    const d = blobPath(80, 75, rx, ry, 7, r, 0.35);
    const clip = `rock-${id}`;
    out.push(<clipPath key="c" id={clip}><path d={d} /></clipPath>);
    out.push(<path key="b" d={d} fill="#7d766c" />);
    out.push(
      <g key="g" clipPath={`url(#${clip})`}>
        <ellipse cx={70} cy={60} rx={rx * 0.9} ry={ry * 0.75} fill="#a39a8c" />
        <ellipse cx={60} cy={52} rx={rx * 0.45} ry={ry * 0.35} fill="#c4b8a5" />
        <ellipse cx={110} cy={100} rx={rx * 0.8} ry={ry * 0.5} fill="#5b554e" />
        <path d={`M ${80 - rx * 0.4} ${75} q 20 -6 40 4`} stroke="#5b554e" strokeWidth={2.5} fill="none" />
      </g>,
    );
    if (r() < 0.7) out.push(<path key="moss" d={scallopPath(70, 75 - ry * 0.75, rx * 0.4, 9, 6, 0.3, r)} fill="#6d7a3c" />);
    return out;
  },
});

const stump: AssetDef = {
  w: 140, h: 120,
  render: (id) => {
    const out: React.ReactNode[] = [];
    out.push(<GroundShadow key="sh" id={id} cx={82} cy={100} rx={52} ry={12} />);
    out.push(<path key="b" d="M 34 50 L 30 96 Q 70 112 112 96 L 106 50 Z" fill="#6b4a36" />);
    out.push(<path key="bl" d="M 34 50 L 30 96 Q 45 103 58 104 L 60 52 Z" fill="#8a6247" />);
    out.push(<path key="roots" d="M 30 96 q -14 6 -20 2 M 112 96 q 14 4 18 -2 M 70 106 q 0 8 -6 10" stroke="#5a3e2e" strokeWidth={8} strokeLinecap="round" fill="none" />);
    out.push(<ellipse key="top" cx={70} cy={50} rx={36} ry={14} fill="#c79a6e" />);
    out.push(<ellipse key="r1" cx={70} cy={50} rx={24} ry={9} fill="none" stroke="#a87b55" strokeWidth={2} />);
    out.push(<ellipse key="r2" cx={70} cy={50} rx={11} ry={4} fill="none" stroke="#a87b55" strokeWidth={2} />);
    return out;
  },
};

const fern: AssetDef = {
  w: 160, h: 120,
  render: (id) => {
    const r = rng(31);
    const out: React.ReactNode[] = [];
    out.push(<GroundShadow key="sh" id={id} cx={86} cy={106} rx={55} ry={9} opacity={0.25} />);
    for (let i = 0; i < 7; i++) {
      const a = -Math.PI / 2 + (i - 3) * 0.38;
      const len = 70 + r() * 25;
      const ex = 80 + Math.cos(a) * len, ey = 106 + Math.sin(a) * len * 0.8;
      out.push(<path key={`s${i}`} d={`M 80 106 Q ${(80 + ex) / 2} ${ey + 10} ${ex} ${ey}`} stroke="#4c6533" strokeWidth={3} fill="none" />);
      for (let k = 1; k < 8; k++) {
        const t = k / 8;
        const px = 80 + (ex - 80) * t, py = 106 + (ey - 106) * t + Math.sin(t * Math.PI) * 6;
        const w = 12 * (1 - t) + 3;
        out.push(<ellipse key={`l${i}-${k}`} cx={px} cy={py} rx={w} ry={3} fill={k % 2 ? "#5f7e3f" : "#71924a"} transform={`rotate(${(a * 180) / Math.PI + 90} ${px} ${py})`} />);
      }
    }
    return out;
  },
};

const mushrooms: AssetDef = {
  w: 90, h: 70,
  render: () => {
    const out: React.ReactNode[] = [];
    [[30, 50, 1], [52, 56, 0.8], [66, 48, 0.6]].forEach(([x, y, s], i) => {
      out.push(<rect key={`s${i}`} x={x - 4 * s} y={y - 14 * s} width={8 * s} height={16 * s} rx={3} fill="#e8dcc4" />);
      out.push(<path key={`c${i}`} d={`M ${x - 16 * s} ${y - 12 * s} Q ${x} ${y - 34 * s} ${x + 16 * s} ${y - 12 * s} Z`} fill="#b5523f" />);
      out.push(<circle key={`d${i}`} cx={x - 4 * s} cy={y - 20 * s} r={2.5 * s} fill="#f0e2c4" />);
    });
    return out;
  },
};

// ---------- chão e água ----------
/** Mancha de terra/caminho com borda irregular. */
const dirt = (seed: number, w: number, h: number): AssetDef => ({
  w, h,
  render: (id) => {
    const r = rng(seed);
    const out: React.ReactNode[] = [];
    out.push(<path key="b" d={blobPath(w / 2, h / 2, w * 0.46, h * 0.4, 12, r, 0.25)} fill="#9c6440" opacity={0.9} />);
    out.push(<path key="m" d={blobPath(w / 2 - 6, h / 2 - 4, w * 0.4, h * 0.32, 12, r, 0.3)} fill="#b77b50" />);
    out.push(<path key="l" d={blobPath(w / 2 - 20, h / 2 - 10, w * 0.22, h * 0.16, 9, r, 0.4)} fill="#c99064" opacity={0.8} />);
    for (let i = 0; i < 10; i++)
      out.push(<ellipse key={`p${i}`} cx={w * 0.15 + r() * w * 0.7} cy={h * 0.25 + r() * h * 0.5} rx={3 + r() * 4} ry={2 + r() * 2} fill={r() < 0.5 ? "#8b5a3c" : "#d4a07a"} />);
    void id;
    return out;
  },
});

/** Textura de grama que se repete sem emenda (512x512). */
const groundTile = (base: string, light: string, dark: string, seed: number): AssetDef => ({
  w: 512, h: 512,
  render: (id) => {
    const r = rng(seed);
    const blades: React.ReactNode[] = [];
    for (let i = 0; i < 260; i++) {
      const x = r() * 512, y = r() * 512, len = 6 + r() * 9, b = (r() - 0.5) * 6;
      const c = r() < 0.5 ? light : dark;
      for (const ox of [-512, 0, 512]) for (const oy of [-512, 0, 512]) {
        if (x + ox < -20 || x + ox > 532 || y + oy < -20 || y + oy > 532) continue;
        blades.push(<path key={`${i}-${ox}-${oy}`} d={`M ${x + ox} ${y + oy} q ${b} ${-len / 2} ${b * 1.5} ${-len}`} stroke={c} strokeWidth={2} fill="none" strokeLinecap="round" opacity={0.6} />);
      }
    }
    return (
      <>
        <defs>
          <filter id={`gt-${id}`} x="0" y="0" width="100%" height="100%">
            <feTurbulence type="fractalNoise" baseFrequency="0.0078125" numOctaves={3} stitchTiles="stitch" seed={seed} result="n" />
            <feColorMatrix in="n" type="matrix" values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  2.4 0 0 0 -1.05" result="mask" />
            <feFlood floodColor={light} result="lc" />
            <feComposite in="lc" in2="mask" operator="in" result="lp" />
            <feTurbulence type="fractalNoise" baseFrequency="0.015625" numOctaves={2} stitchTiles="stitch" seed={seed + 3} result="n2" />
            <feColorMatrix in="n2" type="matrix" values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  0 2.6 0 0 -1.25" result="mask2" />
            <feFlood floodColor={dark} result="dc" />
            <feComposite in="dc" in2="mask2" operator="in" result="dp" />
            <feMerge><feMergeNode in="lp" /><feMergeNode in="dp" /></feMerge>
          </filter>
        </defs>
        <rect width={512} height={512} fill={base} />
        <rect width={512} height={512} filter={`url(#gt-${id})`} />
        {blades}
      </>
    );
  },
});

/** Faixa de rio que se repete na horizontal, com margens. */
const riverTile: AssetDef = {
  w: 512, h: 260,
  render: (id) => {
    const r = rng(5);
    const out: React.ReactNode[] = [];
    out.push(<rect key="w" x={0} y={40} width={512} height={180} fill="#4a6b6a" />);
    out.push(<rect key="w2" x={0} y={70} width={512} height={120} fill="#55797a" />);
    out.push(<rect key="w3" x={0} y={110} width={512} height={40} fill="#5f8584" opacity={0.7} />);
    for (let i = 0; i < 26; i++) {
      const x = r() * 512, y = 60 + r() * 140, w = 20 + r() * 50;
      for (const ox of [-512, 0, 512])
        out.push(<path key={`r${i}${ox}`} d={`M ${x + ox} ${y} q ${w / 2} -4 ${w} 0`} stroke={r() < 0.6 ? "#8fb0a6" : "#3d5a5a"} strokeWidth={2.5} fill="none" strokeLinecap="round" opacity={0.8} />);
    }
    // margens de terra e grama pendendo (repetem a cada 512 px)
    const bank = (y: number, flip: number, key: string) => {
      let d = `M -40 ${y}`;
      const rr = rng(flip > 0 ? 9 : 19);
      for (let x = -40; x <= 552; x += 32) d += ` Q ${x + 16} ${y + flip * (8 + rr() * 6)} ${x + 32} ${y}`;
      return d;
    };
    out.push(<path key="mudTop" d={`${bank(46, 1, "t")} L 552 0 L -40 0 Z`} fill="#7a5a3e" />);
    out.push(<path key="grassTop" d={`${bank(34, 1, "t2")} L 552 0 L -40 0 Z`} fill="#5f6d34" />);
    out.push(<path key="mudBot" d={`${bank(214, -1, "b")} L 552 260 L -40 260 Z`} fill="#6e5038" />);
    out.push(<path key="grassBot" d={`${bank(226, -1, "b2")} L 552 260 L -40 260 Z`} fill="#56632f" />);
    void id;
    return out;
  },
};

/** Faixa larga de terra batida (estrada) que se repete na horizontal. */
const roadTile: AssetDef = {
  w: 512, h: 120,
  render: () => {
    const r = rng(41);
    const out: React.ReactNode[] = [];
    let top = "M -20 22", bot = "M -20 98";
    for (let x = -20; x <= 532; x += 24) { top += ` Q ${x + 12} ${16 + r() * 10} ${x + 24} ${22}`; bot += ` Q ${x + 12} ${104 - r() * 10} ${x + 24} ${98}`; }
    out.push(<path key="b" d={`${top} L 532 98 L -20 98 Z`} fill="#a86f47" />);
    out.push(<path key="b2" d={`${bot} L 532 22 L -20 22 Z`} fill="#a86f47" />);
    out.push(<rect key="l" x={0} y={40} width={512} height={30} fill="#bd8556" opacity={0.8} />);
    for (let i = 0; i < 40; i++) {
      const x = r() * 512, y = 28 + r() * 64;
      for (const ox of [-512, 0, 512]) out.push(<ellipse key={`p${i}${ox}`} cx={x + ox} cy={y} rx={2 + r() * 4} ry={1.5 + r() * 2} fill={r() < 0.5 ? "#8b5a3c" : "#d2a179"} />);
    }
    // marcas de roda
    out.push(<path key="w1" d="M 0 52 L 512 52" stroke="#93603e" strokeWidth={3} opacity={0.6} />);
    out.push(<path key="w2" d="M 0 78 L 512 78" stroke="#93603e" strokeWidth={3} opacity={0.6} />);
    return out;
  },
};

export const nature: Record<string, AssetDef> = {
  pine_1: pine(11, GREENS.pine),
  pine_2: pine(23, GREENS.olive, 0.9),
  pine_3: pine(37, [darken(GREENS.pine[0], 0.1), GREENS.pine[0], GREENS.pine[1], GREENS.pine[2]], 1.05),
  tree_green_1: roundTree(5, GREENS.green),
  tree_olive_1: roundTree(8, GREENS.olive),
  tree_red_1: roundTree(13, GREENS.autumnRed, { bare: true }),
  tree_orange_1: roundTree(17, GREENS.autumnOrange),
  tree_rose_1: roundTree(19, GREENS.rose, { bare: true }),
  bush_green: bush(3, GREENS.green),
  bush_berry: bush(4, GREENS.olive, "#3b2a3a"),
  bush_flower: bush(6, GREENS.green, undefined, "#d9775f"),
  bush_yellow: bush(9, GREENS.pine, undefined, "#e0b34f"),
  grass_tuft: grassTuft(1, ["#6d7a3c", "#85904a", "#5a6633"]),
  grass_tall: grassTuft(2, ["#8a8a46", "#a39c58", "#6d7a3c"], 110, "#c9a86a"),
  heather: grassTuft(4, ["#9a5a6a", "#b56f7f", "#7d4a5a"], 90, "#d48a96"),
  lavender,
  flowers_yellow: flowers(3, "#e6b54a"),
  flowers_white: flowers(8, "#efe4d0"),
  clover,
  rock_1: rock(2, 1),
  rock_2: rock(9, 0.75),
  rock_3: rock(14, 0.5),
  stump,
  fern,
  mushrooms,
  dirt_1: dirt(1, 320, 170),
  dirt_2: dirt(2, 240, 140),
  ground_grass: groundTile("#6c7535", "#828b45", "#59612c", 4),
  ground_meadow: groundTile("#7a7a3c", "#949150", "#636530", 8),
  river: riverTile,
  road: roadTile,
};
