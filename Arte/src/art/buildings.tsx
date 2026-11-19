import React from "react";
import { AssetDef, FoliageMarks, GroundShadow, darken, lighten, mix, rng, scallopPath } from "./core";

type Faces = { top: string; front: string; side: string };
const CONCRETE: Faces = { top: "#ddd3c1", front: "#bdb2a1", side: "#8e8576" };
const CONCRETE_W: Faces = { top: "#e6dcc8", front: "#c9bea9", side: "#9a907f" };
const ROOF_D: Faces = { top: "#6b6a64", front: "#4f4e49", side: "#3e3d39" };
const WARM_LIGHT = "#ffcf7a";
const WIN_OFF = "#3b3a3a";
const TEAL = "#2f5a5c";
const GLOW = "#9ff0e2";

/**
 * Caixa em vista elevada (projeção oblíqua): frente, topo inclinado para cima/direita e lateral.
 * (x, y) = canto inferior esquerdo da frente.
 */
const box = (key: string, x: number, y: number, w: number, h: number, depth: number, side: number, f: Faces) => (
  <g key={key}>
    <path d={`M ${x + w} ${y} L ${x + w} ${y - h} L ${x + w + side} ${y - h - depth} L ${x + w + side} ${y - depth} Z`} fill={f.side} />
    <rect x={x} y={y - h} width={w} height={h} fill={f.front} />
    <path d={`M ${x} ${y - h} L ${x + w} ${y - h} L ${x + w + side} ${y - h - depth} L ${x + side} ${y - h - depth} Z`} fill={f.top} />
    <rect x={x} y={y - h} width={w} height={3} fill={lighten(f.front, 0.25)} opacity={0.6} />
  </g>
);

const windows = (key: string, x: number, y: number, cols: number, rows: number, w: number, h: number, gx: number, gy: number, lit: boolean, color = WARM_LIGHT) => {
  const out: React.ReactNode[] = [];
  for (let r = 0; r < rows; r++)
    for (let c = 0; c < cols; c++) {
      const wx = x + c * (w + gx), wy = y + r * (h + gy);
      out.push(<rect key={`${key}${r}-${c}`} x={wx} y={wy} width={w} height={h} rx={2} fill={lit ? color : WIN_OFF} />);
      if (!lit) out.push(<rect key={`${key}f${r}-${c}`} x={wx} y={wy} width={w} height={h * 0.35} fill="#55524f" />);
    }
  return out;
};

/** Brilho suave atrás das janelas acesas (camada de luz). */
const glowRect = (key: string, x: number, y: number, w: number, h: number, id: string, color = WARM_LIGHT) => (
  <rect key={key} x={x - 6} y={y - 6} width={w + 12} height={h + 12} fill={color} opacity={0.45} filter={`url(#blur-${id})`} />
);

// ---------- usina ----------
const reactor = (lights: boolean): AssetDef => ({
  w: 380, h: 460,
  render: (id) => {
    const cx = 180, rx = 120, ry = 34, bodyTop = 230, bodyBot = 400;
    if (lights) {
      const out: React.ReactNode[] = [];
      for (let i = 0; i < 6; i++) {
        const a = Math.PI * (0.15 + i * 0.14);
        const x = cx - Math.cos(a) * rx * 0.95;
        out.push(<rect key={`g${i}`} x={x - 12} y={bodyTop + 28 + Math.sin(a) * ry - 6} width={24} height={16} fill={GLOW} opacity={0.5} filter={`url(#blur-${id})`} />);
        out.push(<rect key={`w${i}`} x={x - 9} y={bodyTop + 28 + Math.sin(a) * ry - 3} width={18} height={10} rx={2} fill={GLOW} />);
      }
      return out;
    }
    const grad = `rg-${id}`;
    return (
      <>
        <defs>
          <linearGradient id={grad} x1="0" x2="1">
            <stop offset="0" stopColor="#e9e0cf" />
            <stop offset="0.28" stopColor="#e9e0cf" />
            <stop offset="0.29" stopColor="#cdc3b1" />
            <stop offset="0.72" stopColor="#cdc3b1" />
            <stop offset="0.73" stopColor="#a1978a" />
            <stop offset="1" stopColor="#8a8175" />
          </linearGradient>
        </defs>
        <GroundShadow id={id} cx={cx + 70} cy={bodyBot + 10} rx={170} ry={42} opacity={0.4} />
        {/* plataforma */}
        <ellipse cx={cx} cy={bodyBot + 8} rx={rx + 34} ry={ry + 14} fill="#8c8273" />
        <ellipse cx={cx} cy={bodyBot} rx={rx + 34} ry={ry + 12} fill="#b3a996" />
        {/* corpo */}
        <path d={`M ${cx - rx} ${bodyTop} L ${cx - rx} ${bodyBot} A ${rx} ${ry} 0 0 0 ${cx + rx} ${bodyBot} L ${cx + rx} ${bodyTop} Z`} fill={`url(#${grad})`} />
        {[0.3, 0.5, 0.7].map((t, i) => (
          <path key={i} d={`M ${cx - rx * (1 - t * 2) } ${bodyTop + 50} L ${cx - rx * (1 - t * 2)} ${bodyBot + ry * Math.sqrt(1 - (1 - t * 2) ** 2) - 2}`} stroke="#9a907f" strokeWidth={2} opacity={0.5} />
        ))}
        {/* faixa com janelas */}
        <path d={`M ${cx - rx} ${bodyTop + 14} A ${rx} ${ry} 0 0 0 ${cx + rx} ${bodyTop + 14} L ${cx + rx} ${bodyTop + 46} A ${rx} ${ry} 0 0 1 ${cx - rx} ${bodyTop + 46} Z`} fill={TEAL} />
        {Array.from({ length: 6 }).map((_, i) => {
          const a = Math.PI * (0.15 + i * 0.14);
          const x = cx - Math.cos(a) * rx * 0.95;
          return <rect key={i} x={x - 9} y={bodyTop + 28 + Math.sin(a) * ry - 3} width={18} height={10} rx={2} fill="#3f7e7c" />;
        })}
        {/* cúpula */}
        <path d={`M ${cx - rx} ${bodyTop} A ${rx} ${rx * 0.95} 0 0 1 ${cx + rx} ${bodyTop} A ${rx} ${ry} 0 0 1 ${cx - rx} ${bodyTop} Z`} fill={`url(#${grad})`} />
        <path d={`M ${cx - rx * 0.86} ${bodyTop - 40} A ${rx * 0.8} ${rx * 0.75} 0 0 1 ${cx - rx * 0.1} ${bodyTop - rx * 0.9}`} stroke="#f6efe1" strokeWidth={14} fill="none" strokeLinecap="round" opacity={0.7} />
        <ellipse cx={cx + 6} cy={bodyTop - rx * 0.93} rx={18} ry={7} fill="#8a8175" />
        {/* porta e tubulação */}
        <path d={`M ${cx - 22} ${bodyBot + ry - 4} L ${cx - 22} ${bodyBot - 30} Q ${cx} ${bodyBot - 46} ${cx + 22} ${bodyBot - 30} L ${cx + 22} ${bodyBot + ry - 4} Z`} fill="#4a4743" />
        <rect x={cx + rx - 6} y={bodyBot - 60} width={70} height={18} rx={6} fill="#8e8576" />
        <rect x={cx + rx - 6} y={bodyBot - 60} width={70} height={6} rx={3} fill="#b6ad9c" />
      </>
    );
  },
});

const coolingTower: AssetDef = {
  w: 320, h: 470,
  render: (id) => {
    const cx = 150, top = 70, bot = 420;
    const half = (y: number) => { const t = (y - top) / (bot - top); return 70 + 70 * (t - 0.35) * (t - 0.35) * 3.2; };
    let left = "", right = "";
    for (let i = 0; i <= 20; i++) {
      const y = top + ((bot - top) * i) / 20;
      left += `${i === 0 ? "M" : "L"} ${cx - half(y)} ${y} `;
      right = `L ${cx + half(y)} ${y} ` + right;
    }
    const shape = left + right + "Z";
    const clip = `ct-${id}`;
    return (
      <>
        <GroundShadow id={id} cx={cx + 80} cy={bot + 6} rx={170} ry={36} opacity={0.4} />
        <clipPath id={clip}><path d={shape} /></clipPath>
        <path d={shape} fill="#c9bfae" />
        <g clipPath={`url(#${clip})`}>
          <rect x={cx - 200} y={0} width={150} height={500} fill="#e3dac8" />
          <rect x={cx + 45} y={0} width={200} height={500} fill="#a0978a" />
          <rect x={cx + 100} y={0} width={200} height={500} fill="#8a8175" />
          <rect x={0} y={top + 26} width={400} height={10} fill="#7e7569" opacity={0.6} />
          <path d={`M 0 300 Q 150 330 320 290 L 320 430 L 0 430 Z`} fill="#9a917f" opacity={0.25} />
        </g>
        {/* boca vista de cima */}
        <ellipse cx={cx} cy={top} rx={half(top)} ry={22} fill="#e3dac8" />
        <ellipse cx={cx} cy={top + 2} rx={half(top) - 8} ry={16} fill="#3a3632" />
        <ellipse cx={cx + 10} cy={top + 6} rx={half(top) - 30} ry={8} fill="#262320" />
        {/* pilares */}
        {Array.from({ length: 7 }).map((_, i) => (
          <rect key={i} x={cx - half(bot) + 10 + i * ((2 * half(bot) - 30) / 6)} y={bot - 4} width={10} height={20} fill="#6f675d" />
        ))}
        <ellipse cx={cx} cy={bot + 14} rx={half(bot) + 10} ry={16} fill="#8c8273" opacity={0.7} />
      </>
    );
  },
};

const turbineHall: AssetDef = {
  w: 470, h: 330,
  render: (id) => (
    <>
      <GroundShadow id={id} cx={260} cy={292} rx={220} ry={34} opacity={0.4} />
      {box("b", 30, 290, 360, 150, 80, 60, CONCRETE)}
      {/* faixa e portão */}
      <rect x={30} y={170} width={360} height={14} fill={TEAL} />
      <rect x={70} y={210} width={80} height={80} fill="#5a5753" />
      {[0, 1, 2, 3].map((i) => <rect key={i} x={70} y={214 + i * 19} width={80} height={3} fill="#77736d" />)}
      {windows("w", 180, 200, 5, 2, 30, 20, 12, 14, false)}
      {/* aberturas das turbinas no telhado */}
      {[0, 1, 2].map((i) => (
        <g key={i}>
          <ellipse cx={120 + i * 105} cy={100} rx={42} ry={22} fill="#8e8576" />
          <ellipse cx={120 + i * 105} cy={98} rx={36} ry={18} fill="#2e2b28" />
        </g>
      ))}
      <rect x={380} y={160} width={60} height={22} rx={8} fill="#8e8576" />
    </>
  ),
};

const fan: AssetDef = {
  w: 90, h: 90,
  render: () => (
    <>
      {[0, 1, 2, 3, 4].map((i) => (
        <path key={i} d={`M 45 45 Q ${45 + Math.cos(i * 1.2566) * 30} ${45 + Math.sin(i * 1.2566) * 30} ${45 + Math.cos(i * 1.2566 + 0.5) * 40} ${45 + Math.sin(i * 1.2566 + 0.5) * 40} Z`} fill="#c9d5cf" stroke="#9fb1a9" strokeWidth={2} />
      ))}
      <circle cx={45} cy={45} r={9} fill="#6f7a74" />
    </>
  ),
};

const controlRoom = (lights: boolean): AssetDef => ({
  w: 340, h: 280,
  render: (id) => {
    if (lights) return [0, 1, 2, 3].map((i) => (
      <g key={i}>{glowRect(`g${i}`, 58 + i * 52, 170, 34, 30, id)}<rect x={58 + i * 52} y={170} width={34} height={30} rx={2} fill={WARM_LIGHT} /></g>
    ));
    return (
      <>
        <GroundShadow id={id} cx={190} cy={248} rx={160} ry={28} opacity={0.4} />
        {box("b", 40, 245, 230, 115, 60, 45, CONCRETE_W)}
        {windows("w", 58, 170, 4, 1, 34, 30, 18, 0, false)}
        <rect x={232} y={180} width={26} height={65} fill="#4a4743" />
        <rect x={40} y={140} width={230} height={8} fill={TEAL} />
        {/* ar-condicionado no telhado */}
        {box("ac", 110, 112, 40, 14, 14, 10, ROOF_D)}
        {box("ac2", 180, 108, 32, 12, 12, 8, ROOF_D)}
      </>
    );
  },
});

const controlAnnex: AssetDef = {
  w: 220, h: 200,
  render: (id) => (
    <>
      <GroundShadow id={id} cx={120} cy={170} rx={100} ry={20} opacity={0.35} />
      {box("b", 30, 170, 130, 90, 40, 32, CONCRETE)}
      {windows("w", 42, 110, 3, 1, 26, 22, 12, 0, true)}
      <rect x={30} y={90} width={130} height={6} fill={TEAL} />
    </>
  ),
};

const antenna: AssetDef = {
  w: 80, h: 240,
  render: () => (
    <>
      <path d="M 40 230 L 34 40 L 46 40 Z" fill="#8e8576" />
      <path d="M 34 120 L 46 120 M 30 180 L 50 180 M 36 80 L 44 80" stroke="#6f675d" strokeWidth={3} />
      <circle cx={40} cy={34} r={8} fill="#c94f43" />
      <circle cx={38} cy={32} r={3} fill="#ffb3a3" />
    </>
  ),
};

const automation = (lights: boolean): AssetDef => ({
  w: 360, h: 230,
  render: (id) => {
    if (lights) return [0, 1, 2, 3, 4, 5].map((i) => (
      <g key={i}>{glowRect(`g${i}`, 60 + i * 38, 150, 28, 22, id, GLOW)}<rect x={60 + i * 38} y={150} width={28} height={22} rx={2} fill={GLOW} /></g>
    ));
    return (
      <>
        <GroundShadow id={id} cx={200} cy={210} rx={170} ry={26} opacity={0.4} />
        {box("b", 40, 205, 260, 80, 55, 45, CONCRETE_W)}
        <rect x={52} y={142} width={236} height={38} fill="#2f3335" />
        {[0, 1, 2, 3, 4, 5].map((i) => <rect key={i} x={60 + i * 38} y={150} width={28} height={22} rx={2} fill="#38585a" />)}
        <rect x={40} y={125} width={260} height={8} fill={TEAL} />
        {box("u", 200, 118, 60, 18, 16, 12, ROOF_D)}
      </>
    );
  },
});

const transformer: AssetDef = {
  w: 140, h: 150,
  render: (id) => (
    <>
      <GroundShadow id={id} cx={84} cy={128} rx={58} ry={14} opacity={0.4} />
      {box("b", 25, 128, 70, 55, 26, 22, { top: "#9aa39a", front: "#7f8a82", side: "#5f6862" })}
      {[0, 1, 2, 3, 4].map((i) => <rect key={i} x={30 + i * 13} y={80} width={6} height={44} fill="#69736c" />)}
      {[0, 1, 2].map((i) => (
        <g key={i}>
          <rect x={42 + i * 20} y={20 + (i % 2) * 6} width={8} height={40} fill="#b06a46" />
          {[0, 1, 2, 3].map((k) => <ellipse key={k} cx={46 + i * 20} cy={26 + (i % 2) * 6 + k * 9} rx={8} ry={3} fill="#c98a62" />)}
        </g>
      ))}
    </>
  ),
};

const pylon: AssetDef = {
  w: 160, h: 400,
  render: (id) => {
    const lines: React.ReactNode[] = [];
    const L = (x1: number, y1: number, x2: number, y2: number, k: string, w = 4) => lines.push(<line key={k} x1={x1} y1={y1} x2={x2} y2={y2} stroke="#4a4744" strokeWidth={w * 1.6} strokeLinecap="round" />);
    L(40, 390, 70, 60, "a"); L(120, 390, 90, 60, "b");
    L(10, 120, 150, 120, "c", 5); L(20, 170, 140, 170, "d", 5);
    for (let i = 0; i < 6; i++) {
      const y0 = 390 - i * 55, y1 = y0 - 55;
      const l0 = 40 + (30 * (390 - y0)) / 330, r0 = 120 - (30 * (390 - y0)) / 330;
      const l1 = 40 + (30 * (390 - y1)) / 330, r1 = 120 - (30 * (390 - y1)) / 330;
      L(l0, y0, r1, y1, `x${i}`, 2.5); L(r0, y0, l1, y1, `y${i}`, 2.5);
    }
    [10, 150, 20, 140].forEach((x, i) => lines.push(<rect key={`i${i}`} x={x - 3} y={(i < 2 ? 120 : 170) - 2} width={6} height={18} fill="#a9a091" />));
    return (
      <>
        <GroundShadow id={id} cx={110} cy={392} rx={60} ry={12} opacity={0.35} />
        {lines}
      </>
    );
  },
};

const constructionLot: AssetDef = {
  w: 320, h: 190,
  render: (id) => {
    const r = rng(8);
    return (
      <>
        <path d="M 30 150 L 260 150 L 300 60 L 70 60 Z" fill="#a47049" opacity={0.85} />
        <path d="M 50 140 L 250 140 L 282 70 L 84 70 Z" fill="#b8825a" opacity={0.7} />
        {[[30, 150], [260, 150], [300, 60], [70, 60]].map(([x, y], i) => (
          <g key={i}>
            <rect x={x - 4} y={y - 34} width={8} height={36} fill="#6b4b37" />
            <rect x={x - 4} y={y - 34} width={8} height={8} fill="#d9774f" />
          </g>
        ))}
        <path d="M 30 122 L 260 122 L 300 32 L 70 32 Z" stroke="#e9d9a8" strokeWidth={2} fill="none" strokeDasharray="10 6" />
        {Array.from({ length: 8 }).map((_, i) => <ellipse key={i} cx={90 + r() * 160} cy={80 + r() * 55} rx={6 + r() * 6} ry={3} fill="#8b5a3c" />)}
        <rect x={120} y={90} width={70} height={40} fill="#cdb78e" />
        <rect x={150} y={130} width={8} height={18} fill="#6b4b37" />
        <path d="M 128 104 L 182 104 M 128 116 L 170 116" stroke="#6b5a40" strokeWidth={3} />
        <GroundShadow id={id} cx={160} cy={160} rx={10} ry={2} opacity={0.01} />
      </>
    );
  },
};

// ---------- vila, porto, laboratório, centro ----------
const cabin = (seed: number, logs: string, roof: string, lights: boolean): AssetDef => ({
  w: 300, h: 300,
  render: (id) => {
    const r = rng(seed);
    const fx = 50, fy = 270, fw = 160, fh = 110, side = 60, depth = 50;
    if (lights) return (
      <>
        {glowRect("g1", fx + 26, fy - 78, 30, 34, id)}<rect x={fx + 26} y={fy - 78} width={30} height={34} rx={3} fill={WARM_LIGHT} />
        {glowRect("g3", fx + 68, fy - 178, 22, 26, id)}<rect x={fx + 68} y={fy - 178} width={22} height={26} fill={WARM_LIGHT} />
      </>
    );
    const logsOut: React.ReactNode[] = [];
    for (let i = 0; i < 8; i++) {
      const y = fy - fh + i * (fh / 8);
      logsOut.push(<rect key={`l${i}`} x={fx} y={y} width={fw} height={fh / 8 - 2} rx={6} fill={i % 2 ? logs : lighten(logs, 0.08)} />);
      logsOut.push(<circle key={`c${i}`} cx={fx + 2} cy={y + fh / 16} r={fh / 16 - 1} fill={lighten(logs, 0.25)} />);
    }
    // frente com empena (triângulo) e telhado em perspectiva
    const gableTop = fy - fh - 95;
    return (
      <>
        <GroundShadow id={id} cx={fx + fw / 2 + 60} cy={fy + 4} rx={150} ry={28} opacity={0.4} />
        <path d={`M ${fx + fw} ${fy} L ${fx + fw} ${fy - fh} L ${fx + fw + side} ${fy - fh - depth} L ${fx + fw + side} ${fy - depth} Z`} fill={darken(logs, 0.3)} />
        {logsOut}
        <path d={`M ${fx} ${fy - fh} L ${fx + fw / 2} ${gableTop} L ${fx + fw} ${fy - fh} Z`} fill={lighten(logs, 0.05)} />
        {[0, 1, 2, 3, 4].map((i) => <path key={`v${i}`} d={`M ${fx + 18 + i * 31} ${fy - fh} L ${fx + 18 + i * 31} ${fy - fh - Math.max(0, 80 - Math.abs(i - 2) * 38)}`} stroke={darken(logs, 0.15)} strokeWidth={2} />)}
        {/* telhado: plano direito visível */}
        <path d={`M ${fx + fw / 2} ${gableTop} L ${fx + fw + 10} ${fy - fh + 8} L ${fx + fw + side + 10} ${fy - fh - depth + 8} L ${fx + fw / 2 + side} ${gableTop - depth} Z`} fill={roof} />
        <path d={`M ${fx + fw / 2} ${gableTop} L ${fx - 10} ${fy - fh + 8} L ${fx - 4} ${fy - fh + 14} L ${fx + fw / 2} ${gableTop + 10} Z`} fill={darken(roof, 0.25)} />
        <path d={`M ${fx + fw / 2} ${gableTop} L ${fx + fw / 2 + side} ${gableTop - depth}`} stroke={lighten(roof, 0.3)} strokeWidth={5} />
        {/* musgo e telhas */}
        <path d={scallopPath(fx + fw * 0.75 + 20, fy - fh - 50, 40, 14, 7, 0.4, r)} fill="#6f7a3a" />
        {[0, 1, 2, 3].map((i) => <path key={`t${i}`} d={`M ${fx + fw / 2 + 8 + i * 14} ${gableTop + 16 + i * 22} L ${fx + fw / 2 + side + 8 + i * 14} ${gableTop - depth + 16 + i * 22}`} stroke={darken(roof, 0.2)} strokeWidth={2} opacity={0.7} />)}
        {/* chaminé */}
        {seed % 2 === 0 && box("ch", fx + fw / 2 + 50, gableTop + 30, 18, 40, 10, 8, { top: "#8a7a70", front: "#7b5d4e", side: "#5d463a" })}
        {/* janelas, porta e vaso */}
        <rect x={fx + 22} y={fy - 82} width={38} height={42} fill="#5a3f30" />
        <rect x={fx + 26} y={fy - 78} width={30} height={34} rx={3} fill={WIN_OFF} />
        <rect x={fx + 64} y={fy - 182} width={30} height={34} fill="#5a3f30" />
        <rect x={fx + 68} y={fy - 178} width={22} height={26} fill={WIN_OFF} />
        <path d={`M ${fx + fw} ${fy - 30} L ${fx + fw + 26} ${fy - 50} L ${fx + fw + 26} ${fy - 100} L ${fx + fw} ${fy - 80} Z`} fill="#4e3a2e" />
        <rect x={fx + 95} y={fy - 70} width={40} height={70} fill="#5a3f30" />
        <rect x={fx + 99} y={fy - 66} width={32} height={66} fill="#7a5640" />
        <circle cx={fx + 125} cy={fy - 32} r={3} fill="#d9b36a" />
        <path d={scallopPath(fx + 40, fy - 4, 34, 12, 6, 0.4, r)} fill="#5f7a3e" />
        <FoliageMarks cx={fx + 40} cy={fy - 6} rx={26} ry={6} n={3} color="#8fa75c" r={r} width={2} />
      </>
    );
  },
});

const crane: AssetDef = {
  w: 300, h: 420,
  render: (id) => {
    const c = "#b9523d", d = "#7e3528";
    const L = (k: string, x1: number, y1: number, x2: number, y2: number, w: number, col = c) => <line key={k} x1={x1} y1={y1} x2={x2} y2={y2} stroke={col} strokeWidth={w} strokeLinecap="round" />;
    const parts = [L("a", 70, 400, 80, 70, 10), L("b", 120, 400, 110, 70, 10, d)];
    for (let i = 0; i < 6; i++) parts.push(L(`x${i}`, 74 - i, 400 - i * 55, 116 + i, 345 - i * 55, 4));
    parts.push(L("j", 30, 70, 290, 70, 12), L("j2", 30, 82, 290, 82, 5, d), L("t1", 95, 20, 290, 70, 4, d), L("t2", 95, 20, 30, 70, 4, d));
    return (
      <>
        <GroundShadow id={id} cx={160} cy={402} rx={90} ry={16} opacity={0.35} />
        {parts}
        <rect x={20} y={60} width={44} height={36} fill="#5a5753" />
        {box("cab", 100, 110, 44, 36, 16, 12, { top: "#e6dcc8", front: "#c9bea9", side: "#9a907f" })}
        <rect x={108} y={82} width={26} height={14} fill="#38585a" />
        <line x1={250} y1={82} x2={250} y2={230} stroke="#2e2b28" strokeWidth={3} />
        <rect x={236} y={228} width={28} height={12} fill="#2e2b28" />
      </>
    );
  },
};

const container = (col: string): AssetDef => ({
  w: 170, h: 110,
  render: (id) => (
    <>
      <GroundShadow id={id} cx={95} cy={96} rx={80} ry={12} opacity={0.35} />
      {box("b", 15, 95, 120, 50, 26, 22, { top: lighten(col, 0.25), front: col, side: darken(col, 0.3) })}
      {Array.from({ length: 9 }).map((_, i) => <rect key={i} x={22 + i * 13} y={50} width={3} height={40} fill={darken(col, 0.18)} />)}
    </>
  ),
});

const barge: AssetDef = {
  w: 340, h: 140,
  render: (id) => (
    <>
      <GroundShadow id={id} cx={175} cy={112} rx={160} ry={18} opacity={0.3} />
      <path d="M 20 70 L 300 70 L 320 50 L 50 50 Z" fill="#5a5c58" />
      <path d="M 20 70 L 300 70 L 285 110 L 40 110 Z" fill="#3e403d" />
      <path d="M 20 70 L 300 70 L 298 78 L 24 78 Z" fill="#9a4533" />
      {box("c1", 70, 64, 60, 26, 14, 12, { top: "#c47a5a", front: "#9a4f37", side: "#6e3626" })}
      {box("c2", 135, 64, 60, 26, 14, 12, { top: "#7d9a8a", front: "#5a7466", side: "#3f5248" })}
      {box("cab", 230, 66, 40, 40, 16, 14, { top: "#e6dcc8", front: "#c9bea9", side: "#9a907f" })}
      <rect x={236} y={36} width={28} height={12} fill="#38585a" />
      <path d="M 20 110 Q 160 124 300 110" stroke="#9fc0b4" strokeWidth={3} fill="none" opacity={0.7} />
    </>
  ),
};

const dock: AssetDef = {
  w: 620, h: 170,
  render: (id) => (
    <>
      <GroundShadow id={id} cx={330} cy={150} rx={300} ry={18} opacity={0.3} />
      {Array.from({ length: 11 }).map((_, i) => <rect key={`p${i}`} x={30 + i * 54} y={80} width={12} height={78} fill="#4a3428" />)}
      <path d="M 10 80 L 590 80 L 610 40 L 30 40 Z" fill="#8a6247" />
      {Array.from({ length: 26 }).map((_, i) => <path key={`b${i}`} d={`M ${16 + i * 22} 80 L ${36 + i * 22} 40`} stroke="#6b4b37" strokeWidth={2} />)}
      <rect x={10} y={80} width={580} height={10} fill="#6b4b37" />
      {[80, 250, 420].map((x, i) => <g key={`bo${i}`}><rect x={x} y={44} width={12} height={20} fill="#3a3632" /><ellipse cx={x + 6} cy={44} rx={8} ry={4} fill="#5a5753" /></g>)}
    </>
  ),
};

const lab = (lights: boolean): AssetDef => ({
  w: 460, h: 360,
  render: (id) => {
    if (lights) return (
      <>
        {[0, 1].map((r) => [0, 1, 2, 3, 4].map((c) => (
          <g key={`${r}${c}`}>{glowRect(`g${r}${c}`, 60 + c * 40, 210 + r * 44, 26, 26, id)}<rect x={60 + c * 40} y={210 + r * 44} width={26} height={26} rx={2} fill={WARM_LIGHT} /></g>
        )))}
        <circle cx={330} cy={95} r={10} fill="#ff8a70" />
        <circle cx={330} cy={95} r={20} fill="#ff8a70" opacity={0.4} filter={`url(#blur-${id})`} />
      </>
    );
    return (
      <>
        <GroundShadow id={id} cx={250} cy={322} rx={210} ry={32} opacity={0.4} />
        {box("b", 40, 320, 230, 140, 60, 50, CONCRETE_W)}
        {windows("w", 60, 210, 5, 2, 26, 26, 14, 18, false)}
        <rect x={40} y={180} width={230} height={10} fill="#6c5a48" />
        {box("t", 270, 320, 110, 90, 60, 45, CONCRETE)}
        {/* cúpula do observatório */}
        <path d="M 290 230 A 60 56 0 0 1 410 230 A 60 16 0 0 1 290 230 Z" fill="#e9e0cf" />
        <path d="M 300 214 A 50 48 0 0 1 350 180" stroke="#f6efe1" strokeWidth={8} fill="none" opacity={0.8} />
        <path d="M 344 176 L 362 176 L 366 232 L 342 232 Z" fill="#3a3632" />
        {/* antena parabólica */}
        <path d="M 330 150 L 330 110" stroke="#6f675d" strokeWidth={5} />
        <ellipse cx={330} cy={102} rx={34} ry={14} fill="#d9d1c2" transform="rotate(-20 330 102)" />
        <ellipse cx={333} cy={104} rx={24} ry={8} fill="#b3aa99" transform="rotate(-20 333 104)" />
        <circle cx={330} cy={95} r={6} fill="#7a3a30" />
        <rect x={140} y={262} width={36} height={58} fill="#4a4743" />
      </>
    );
  },
});

const controlCenter = (lights: boolean): AssetDef => ({
  w: 500, h: 340,
  render: (id) => {
    if (lights) return [0, 1, 2, 3, 4, 5, 6, 7].map((i) => (
      <g key={i}>{glowRect(`g${i}`, 64 + i * 40, 196, 28, 34, id, "#a8e6b0")}<rect x={64 + i * 40} y={196} width={28} height={34} rx={2} fill="#a8e6b0" /></g>
    ));
    return (
      <>
        <GroundShadow id={id} cx={270} cy={300} rx={230} ry={34} opacity={0.4} />
        {box("b", 40, 300, 340, 150, 70, 60, CONCRETE)}
        <rect x={54} y={188} width={312} height={50} fill="#2f3335" />
        {Array.from({ length: 8 }).map((_, i) => <rect key={i} x={64 + i * 40} y={196} width={28} height={34} rx={2} fill="#3d5a4a" />)}
        <rect x={40} y={164} width={340} height={10} fill="#5a4a6a" />
        <rect x={190} y={250} width={44} height={50} fill="#4a4743" />
        {box("r", 120, 150, 60, 20, 22, 16, ROOF_D)}
        {box("r2", 250, 146, 80, 16, 20, 14, ROOF_D)}
        <path d="M 330 120 L 330 40" stroke="#6f675d" strokeWidth={4} />
        <circle cx={330} cy={36} r={6} fill="#c94f43" />
      </>
    );
  },
});

const warehouse: AssetDef = {
  w: 380, h: 280,
  render: (id) => (
    <>
      <GroundShadow id={id} cx={210} cy={250} rx={180} ry={28} opacity={0.4} />
      {box("b", 30, 248, 260, 120, 70, 55, { top: "#9a8a7a", front: "#8a6a5a", side: "#64493d" })}
      {Array.from({ length: 13 }).map((_, i) => <rect key={i} x={36 + i * 20} y={130} width={3} height={115} fill="#74574a" />)}
      <rect x={100} y={170} width={110} height={78} fill="#3e3a36" />
      {[0, 1, 2, 3].map((i) => <rect key={`d${i}`} x={100} y={176 + i * 18} width={110} height={3} fill="#5a5450" />)}
    </>
  ),
};

const lampPost = (lights: boolean): AssetDef => ({
  w: 70, h: 190,
  render: (id) => lights ? (
    <>
      <circle cx={35} cy={30} r={26} fill={WARM_LIGHT} opacity={0.45} filter={`url(#blur-${id})`} />
      <rect x={25} y={22} width={20} height={16} rx={3} fill={WARM_LIGHT} />
    </>
  ) : (
    <>
      <GroundShadow id={id} cx={44} cy={180} rx={18} ry={5} opacity={0.35} />
      <rect x={31} y={36} width={8} height={146} fill="#3a3632" />
      <path d="M 20 22 L 50 22 L 46 40 L 24 40 Z" fill="#3a3632" />
      <rect x={25} y={24} width={20} height={14} fill="#6b6255" />
      <rect x={26} y={176} width={18} height={8} fill="#3a3632" />
    </>
  ),
});

const fence: AssetDef = {
  w: 200, h: 90,
  render: (id) => (
    <>
      <GroundShadow id={id} cx={110} cy={80} rx={95} ry={8} opacity={0.3} />
      {[10, 70, 130, 186].map((x, i) => <rect key={i} x={x} y={24} width={10} height={58} fill={i % 2 ? "#7a5640" : "#8a6247"} />)}
      <rect x={4} y={36} width={196} height={9} fill="#9c7353" />
      <rect x={4} y={58} width={196} height={9} fill="#8a6247" />
    </>
  ),
};

const cart: AssetDef = {
  w: 190, h: 130,
  render: (id) => (
    <>
      <GroundShadow id={id} cx={100} cy={112} rx={80} ry={12} opacity={0.35} />
      {box("b", 30, 82, 110, 34, 26, 22, { top: "#3a3b3f", front: "#55575c", side: "#3a3b3f" })}
      <circle cx={70} cy={92} r={22} fill="none" stroke="#2e2b28" strokeWidth={6} />
      <path d="M 70 70 L 70 114 M 48 92 L 92 92" stroke="#2e2b28" strokeWidth={3} />
      <path d="M 150 64 L 186 84" stroke="#5a3f30" strokeWidth={6} strokeLinecap="round" />
      <ellipse cx={92} cy={50} rx={26} ry={10} fill="#8a6a3c" />
    </>
  ),
};

const signBoard: AssetDef = {
  w: 300, h: 170,
  render: (id) => (
    <>
      <GroundShadow id={id} cx={160} cy={160} rx={80} ry={8} opacity={0.35} />
      <rect x={60} y={80} width={12} height={84} fill="#4a3428" />
      <rect x={228} y={80} width={12} height={84} fill="#4a3428" />
      <rect x={20} y={20} width={260} height={84} rx={8} fill="#6b4b37" />
      <rect x={26} y={26} width={248} height={72} rx={6} fill="#8a6247" />
      {[0, 1, 2].map((i) => <path key={i} d={`M 30 ${46 + i * 22} L 270 ${44 + i * 22}`} stroke="#7a5640" strokeWidth={2} />)}
    </>
  ),
};

export const buildings: Record<string, AssetDef> = {
  reactor: reactor(false),
  reactor_lights: reactor(true),
  cooling_tower: coolingTower,
  turbine_hall: turbineHall,
  fan,
  control_room: controlRoom(false),
  control_room_lights: controlRoom(true),
  control_annex: controlAnnex,
  antenna,
  automation: automation(false),
  automation_lights: automation(true),
  transformer,
  pylon,
  construction_lot: constructionLot,
  cabin_1: cabin(2, "#8a7266", "#5e5a63", false),
  cabin_1_lights: cabin(2, "#8a7266", "#5e5a63", true),
  cabin_2: cabin(3, "#9a7a5a", "#6b4a3a", false),
  cabin_2_lights: cabin(3, "#9a7a5a", "#6b4a3a", true),
  cabin_3: cabin(4, "#7a6a5e", "#4f5a4a", false),
  cabin_3_lights: cabin(4, "#7a6a5e", "#4f5a4a", true),
  crane,
  container_red: container("#9a4f37"),
  container_green: container("#5a7466"),
  container_grey: container("#7d7a72"),
  container_ochre: container("#a8803f"),
  barge,
  dock,
  lab: lab(false),
  lab_lights: lab(true),
  control_center: controlCenter(false),
  control_center_lights: controlCenter(true),
  warehouse,
  lamp_post: lampPost(false),
  lamp_post_lights: lampPost(true),
  fence,
  cart,
  sign_board: signBoard,
};

export { mix };
