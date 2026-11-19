import React from "react";

/** Gerador pseudoaleatório com semente (a arte sai igual a cada render). */
export const rng = (seed: number) => {
  let s = seed >>> 0 || 1;
  return () => {
    s ^= s << 13;
    s ^= s >>> 17;
    s ^= s << 5;
    return ((s >>> 0) % 100000) / 100000;
  };
};

export const lerp = (a: number, b: number, t: number) => a + (b - a) * t;

const hex = (c: string) => {
  const h = c.replace("#", "");
  return [0, 2, 4].map((i) => parseInt(h.slice(i, i + 2), 16));
};
const toHex = (r: number[]) =>
  "#" + r.map((v) => Math.max(0, Math.min(255, Math.round(v))).toString(16).padStart(2, "0")).join("");

/** Mistura duas cores em hex. */
export const mix = (a: string, b: string, t: number) => {
  const A = hex(a), B = hex(b);
  return toHex(A.map((v, i) => lerp(v, B[i], t)));
};
export const darken = (c: string, t: number) => mix(c, "#1a1410", t);
export const lighten = (c: string, t: number) => mix(c, "#fff3d6", t);

/** Luz quente do fim de tarde, vinda de cima à esquerda. */
export const LIGHT = "#ffe2a8";
export const SHADOW = "#2a2219";

/**
 * Contorno de "nuvem" com bordas recortadas (scallops): o bloco básico das copas e arbustos.
 * Pontos ao redor de uma elipse, ligados por arcos que se projetam para fora.
 */
export const scallopPath = (
  cx: number, cy: number, rx: number, ry: number, n: number, bulge: number, r: () => number, jitter = 0.18,
) => {
  const pts: [number, number][] = [];
  const off = r() * Math.PI * 2;
  for (let i = 0; i < n; i++) {
    const a = off + (i / n) * Math.PI * 2;
    const k = 1 - jitter / 2 + r() * jitter;
    pts.push([cx + Math.cos(a) * rx * k, cy + Math.sin(a) * ry * k]);
  }
  let d = `M ${pts[0][0].toFixed(1)} ${pts[0][1].toFixed(1)}`;
  for (let i = 0; i < n; i++) {
    const [x0, y0] = pts[i];
    const [x1, y1] = pts[(i + 1) % n];
    const mx = (x0 + x1) / 2, my = (y0 + y1) / 2;
    // controle empurrado para fora do centro
    const dx = mx - cx, dy = my - cy;
    const len = Math.hypot(dx, dy) || 1;
    const seg = Math.hypot(x1 - x0, y1 - y0);
    const b = seg * bulge * (0.8 + r() * 0.4);
    d += ` Q ${(mx + (dx / len) * b).toFixed(1)} ${(my + (dy / len) * b).toFixed(1)} ${x1.toFixed(1)} ${y1.toFixed(1)}`;
  }
  return d + " Z";
};

/** Contorno orgânico liso (pedras, manchas de terra). */
export const blobPath = (cx: number, cy: number, rx: number, ry: number, n: number, r: () => number, jitter = 0.3) => {
  const pts: [number, number][] = [];
  for (let i = 0; i < n; i++) {
    const a = (i / n) * Math.PI * 2;
    const k = 1 - jitter / 2 + r() * jitter;
    pts.push([cx + Math.cos(a) * rx * k, cy + Math.sin(a) * ry * k]);
  }
  let d = "";
  for (let i = 0; i < n; i++) {
    const p0 = pts[(i - 1 + n) % n], p1 = pts[i], p2 = pts[(i + 1) % n], p3 = pts[(i + 2) % n];
    if (i === 0) d += `M ${p1[0].toFixed(1)} ${p1[1].toFixed(1)}`;
    const c1 = [p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6];
    const c2 = [p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6];
    d += ` C ${c1[0].toFixed(1)} ${c1[1].toFixed(1)} ${c2[0].toFixed(1)} ${c2[1].toFixed(1)} ${p2[0].toFixed(1)} ${p2[1].toFixed(1)}`;
  }
  return d + " Z";
};

/**
 * Filtros que dão cara de pintura: bordas levemente tremidas e granulação de papel.
 * Cada SVG define os seus (ids únicos por asset).
 */
export const PaintFilters: React.FC<{ id: string; wobble?: number; grain?: number; seed?: number }> = ({ id, wobble = 3, grain = 0.12, seed = 3 }) => (
  <defs>
    <filter id={`paint-${id}`} x="-10%" y="-10%" width="120%" height="120%">
      <feTurbulence type="fractalNoise" baseFrequency="0.035" numOctaves={2} seed={seed} result="warp" />
      <feDisplacementMap in="SourceGraphic" in2="warp" scale={wobble} xChannelSelector="R" yChannelSelector="G" result="wobbly" />
      <feTurbulence type="fractalNoise" baseFrequency="0.85" numOctaves={2} seed={seed + 7} result="noise" />
      <feColorMatrix in="noise" type="matrix" values={`0 0 0 0 0.1  0 0 0 0 0.08  0 0 0 0 0.05  0 0 0 ${grain * 2.2} ${-grain * 0.6}`} result="grainAlpha" />
      <feComposite in="grainAlpha" in2="wobbly" operator="in" result="grainOnShape" />
      <feMerge>
        <feMergeNode in="wobbly" />
        <feMergeNode in="grainOnShape" />
      </feMerge>
    </filter>
    <filter id={`blur-${id}`} x="-50%" y="-50%" width="200%" height="200%">
      <feGaussianBlur stdDeviation="6" />
    </filter>
    <filter id={`softblur-${id}`} x="-50%" y="-50%" width="200%" height="200%">
      <feGaussianBlur stdDeviation="14" />
    </filter>
  </defs>
);

/** Sombra suave no chão (projetada para baixo e à direita). */
export const GroundShadow: React.FC<{ id: string; cx: number; cy: number; rx: number; ry: number; opacity?: number }> = ({ id, cx, cy, rx, ry, opacity = 0.35 }) => (
  <ellipse cx={cx} cy={cy} rx={rx} ry={ry} fill={SHADOW} opacity={opacity} filter={`url(#blur-${id})`} />
);

/** Riscos curtos em forma de "~" dentro das copas (detalhe pintado das referências de ilustração). */
export const FoliageMarks: React.FC<{ cx: number; cy: number; rx: number; ry: number; n: number; color: string; r: () => number; width?: number }> = ({ cx, cy, rx, ry, n, color, r, width = 3 }) => {
  const marks = [];
  for (let i = 0; i < n; i++) {
    const a = r() * Math.PI * 2, d = Math.sqrt(r());
    const x = cx + Math.cos(a) * rx * d, y = cy + Math.sin(a) * ry * d;
    const w = 10 + r() * 14;
    marks.push(
      <path key={i} d={`M ${x - w / 2} ${y} q ${w / 4} ${-w / 4} ${w / 2} 0 q ${w / 4} ${w / 4} ${w / 2} 0`} stroke={color} strokeWidth={width} fill="none" strokeLinecap="round" opacity={0.75} />,
    );
  }
  return <>{marks}</>;
};

export type AssetDef = { w: number; h: number; render: (id: string) => React.ReactNode; pivotY?: number };
