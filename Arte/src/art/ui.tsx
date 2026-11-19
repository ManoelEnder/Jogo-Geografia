import React from "react";
import { AssetDef, darken, lighten } from "./core";

const CREAM = "#efe3c8", AMBER = "#e8a94a", MOSS = "#8a9a52", RUST = "#c2604a", INK = "#2b231d", TEAL = "#7fc9bd", STONE = "#b8ad9a";

// ---------- molduras (9 fatias) ----------
const panel = (fill: string, rim: string): AssetDef => ({
  w: 96, h: 96,
  render: () => (
    <>
      <rect x={2} y={4} width={92} height={90} rx={22} fill="#120d09" opacity={0.45} />
      <rect x={2} y={2} width={92} height={90} rx={22} fill={fill} />
      <rect x={5} y={5} width={86} height={84} rx={19} fill="none" stroke={rim} strokeWidth={2} />
      <path d="M 24 7 L 72 7" stroke={lighten(rim, 0.4)} strokeWidth={2} strokeLinecap="round" opacity={0.6} />
    </>
  ),
});

const button = (fill: string): AssetDef => ({
  w: 96, h: 64,
  render: () => (
    <>
      <rect x={2} y={6} width={92} height={56} rx={16} fill={darken(fill, 0.45)} />
      <rect x={2} y={2} width={92} height={54} rx={16} fill={fill} />
      <rect x={6} y={5} width={84} height={18} rx={9} fill={lighten(fill, 0.18)} opacity={0.6} />
      <rect x={2} y={2} width={92} height={54} rx={16} fill="none" stroke={darken(fill, 0.35)} strokeWidth={2} />
    </>
  ),
});

const badge: AssetDef = {
  w: 160, h: 160,
  render: () => (
    <>
      <circle cx={80} cy={84} r={74} fill="#120d09" opacity={0.4} />
      <circle cx={80} cy={80} r={74} fill="#5a4030" />
      <circle cx={80} cy={80} r={64} fill="#2b231d" />
      <circle cx={80} cy={80} r={70} fill="none" stroke="#7a5a42" strokeWidth={3} />
      <path d="M 30 50 A 60 60 0 0 1 80 18" stroke="#9a7a5a" strokeWidth={4} fill="none" strokeLinecap="round" />
    </>
  ),
};

// ---------- ícones (128x128) ----------
const icon = (draw: () => React.ReactNode): AssetDef => ({ w: 128, h: 128, render: () => <>{draw()}</> });

const icons: Record<string, AssetDef> = {
  icon_energy: icon(() => (
    <path d="M 72 10 L 30 70 L 58 70 L 48 118 L 98 50 L 68 50 L 80 10 Z" fill={AMBER} stroke={darken(AMBER, 0.4)} strokeWidth={4} strokeLinejoin="round" />
  )),
  icon_control_panel: icon(() => (
    <>
      <rect x={30} y={14} width={68} height={44} rx={6} fill={INK} stroke={STONE} strokeWidth={4} />
      <path d="M 40 44 L 54 32 L 64 40 L 86 24" stroke={TEAL} strokeWidth={5} fill="none" strokeLinecap="round" />
      <rect x={58} y={58} width={12} height={12} fill={STONE} />
      <path d="M 14 110 L 26 70 L 102 70 L 114 110 Z" fill={STONE} />
      <path d="M 14 110 L 26 70 L 102 70 L 114 110 Z" fill="none" stroke={darken(STONE, 0.4)} strokeWidth={3} />
      <circle cx={42} cy={90} r={7} fill={AMBER} /><circle cx={64} cy={90} r={7} fill={TEAL} /><circle cx={86} cy={90} r={7} fill={RUST} />
    </>
  )),
  icon_turbine: icon(() => (
    <>
      <circle cx={64} cy={64} r={52} fill={INK} stroke={STONE} strokeWidth={6} />
      {[0, 1, 2, 3, 4].map((i) => (
        <path key={i} d={`M 64 64 Q ${64 + Math.cos(i * 1.2566) * 34} ${64 + Math.sin(i * 1.2566) * 34} ${64 + Math.cos(i * 1.2566 + 0.55) * 44} ${64 + Math.sin(i * 1.2566 + 0.55) * 44} Z`} fill={TEAL} />
      ))}
      <circle cx={64} cy={64} r={10} fill={CREAM} />
    </>
  )),
  icon_substation: icon(() => (
    <>
      <path d="M 10 22 L 118 22" stroke={TEAL} strokeWidth={5} />
      <path d="M 34 22 L 34 44 M 94 22 L 94 44" stroke={STONE} strokeWidth={4} />
      <rect x={14} y={44} width={40} height={60} rx={4} fill={STONE} /><rect x={74} y={44} width={40} height={60} rx={4} fill={STONE} />
      {[0, 1, 2].map((i) => <rect key={i} x={20 + i * 11} y={54} width={5} height={42} fill={darken(STONE, 0.3)} />)}
      {[0, 1, 2].map((i) => <rect key={`b${i}`} x={80 + i * 11} y={54} width={5} height={42} fill={darken(STONE, 0.3)} />)}
      <path d="M 64 50 L 54 78 L 66 78 L 60 104 L 76 70 L 64 70 L 70 50 Z" fill={AMBER} />
    </>
  )),
  icon_automation: icon(() => (
    <>
      <rect x={12} y={18} width={104} height={70} rx={8} fill={INK} stroke={STONE} strokeWidth={5} />
      <path d="M 24 70 L 46 48 L 62 60 L 90 32 L 104 40" stroke={TEAL} strokeWidth={6} fill="none" strokeLinecap="round" strokeLinejoin="round" />
      <rect x={54} y={88} width={20} height={14} fill={STONE} />
      <rect x={34} y={102} width={60} height={10} rx={4} fill={STONE} />
    </>
  )),
  icon_cooling_tower: icon(() => (
    <>
      <path d="M 44 112 Q 54 64 44 34 L 84 34 Q 74 64 84 112 Z" fill={STONE} />
      <path d="M 44 112 Q 54 64 44 34 L 58 34 Q 62 64 56 112 Z" fill={lighten(STONE, 0.3)} />
      <ellipse cx={64} cy={34} rx={20} ry={6} fill={INK} />
      <circle cx={56} cy={20} r={10} fill={CREAM} /><circle cx={72} cy={14} r={12} fill={CREAM} /><circle cx={84} cy={22} r={8} fill={CREAM} />
    </>
  )),
  icon_reactor: icon(() => (
    <>
      <path d="M 22 60 A 42 40 0 0 1 106 60 L 106 106 A 42 12 0 0 1 22 106 Z" fill={STONE} />
      <path d="M 22 60 A 42 40 0 0 1 64 20 L 64 118 A 42 12 0 0 1 22 106 Z" fill={lighten(STONE, 0.3)} />
      <path d="M 22 70 A 42 12 0 0 0 106 70 L 106 82 A 42 12 0 0 1 22 82 Z" fill={TEAL} />
    </>
  )),
  icon_grid: icon(() => (
    <>
      <path d="M 8 30 Q 64 46 120 22" stroke={TEAL} strokeWidth={4} fill="none" />
      {[[12, 60, 26, 52], [42, 46, 30, 66], [76, 56, 36, 56]].map(([x, y, w, h], i) => (
        <g key={i}>
          <rect x={x} y={y} width={w} height={h} fill={i === 1 ? STONE : darken(STONE, 0.2)} />
          {Array.from({ length: 6 }).map((_, k) => <rect key={k} x={x + 5 + (k % 2) * (w / 2)} y={y + 8 + Math.floor(k / 2) * 14} width={7} height={8} fill={AMBER} />)}
        </g>
      ))}
    </>
  )),
  icon_heat: icon(() => (
    <>
      <circle cx={54} cy={60} r={24} fill={AMBER} />
      {Array.from({ length: 8 }).map((_, i) => <path key={i} d={`M ${54 + Math.cos(i * 0.785) * 32} ${60 + Math.sin(i * 0.785) * 32} L ${54 + Math.cos(i * 0.785) * 44} ${60 + Math.sin(i * 0.785) * 44}`} stroke={AMBER} strokeWidth={6} strokeLinecap="round" />)}
      <rect x={94} y={30} width={14} height={62} rx={7} fill={CREAM} />
      <circle cx={101} cy={98} r={13} fill={RUST} /><rect x={98} y={56} width={6} height={42} fill={RUST} />
    </>
  )),
  icon_university: icon(() => (
    <>
      <path d="M 10 46 L 64 14 L 118 46 Z" fill={CREAM} />
      {[0, 1, 2, 3].map((i) => <rect key={i} x={20 + i * 26} y={52} width={14} height={46} fill={STONE} />)}
      <rect x={10} y={98} width={108} height={14} fill={CREAM} />
      <circle cx={64} cy={34} r={6} fill={TEAL} />
    </>
  )),
  icon_wrench: icon(() => (
    <>
      <path d="M 30 98 L 76 52" stroke={STONE} strokeWidth={16} strokeLinecap="round" />
      <path d="M 70 30 A 22 22 0 1 0 98 58 L 86 54 L 76 44 Z" fill={STONE} />
      <circle cx={30} cy={98} r={6} fill={INK} />
      <path d="M 28 20 L 28 44" stroke={RUST} strokeWidth={8} strokeLinecap="round" /><circle cx={28} cy={58} r={5} fill={RUST} />
    </>
  )),
  icon_protest: icon(() => (
    <>
      <rect x={58} y={60} width={10} height={58} fill="#7a5640" />
      <rect x={14} y={14} width={100} height={56} rx={6} fill={CREAM} />
      <path d="M 34 42 Q 54 24 76 42 Q 54 60 34 42 Z" fill={TEAL} /><path d="M 76 42 L 92 30 L 92 54 Z" fill={TEAL} />
      <circle cx={46} cy={40} r={3} fill={INK} />
    </>
  )),
  icon_hearing: icon(() => (
    <>
      <circle cx={38} cy={58} r={14} fill={CREAM} /><path d="M 16 110 Q 16 80 38 78 Q 60 80 60 110 Z" fill={CREAM} />
      <circle cx={84} cy={64} r={12} fill={STONE} /><path d="M 64 112 Q 64 86 84 84 Q 104 86 104 112 Z" fill={STONE} />
      <rect x={60} y={10} width={58} height={34} rx={10} fill={TEAL} /><path d="M 70 44 L 66 56 L 82 44 Z" fill={TEAL} />
    </>
  )),
  icon_press: icon(() => (
    <>
      <rect x={14} y={16} width={100} height={96} rx={6} fill={CREAM} />
      <rect x={24} y={26} width={80} height={14} fill={INK} />
      <rect x={24} y={50} width={36} height={30} fill={TEAL} />
      {[0, 1, 2, 3].map((i) => <rect key={i} x={68} y={50 + i * 9} width={36} height={4} fill={STONE} />)}
      {[0, 1].map((i) => <rect key={`l${i}`} x={24} y={88 + i * 10} width={80} height={4} fill={STONE} />)}
    </>
  )),
  icon_nucleo: icon(() => (
    <>
      <ellipse cx={64} cy={64} rx={54} ry={20} fill="none" stroke={TEAL} strokeWidth={5} />
      <ellipse cx={64} cy={64} rx={54} ry={20} fill="none" stroke={TEAL} strokeWidth={5} transform="rotate(60 64 64)" />
      <ellipse cx={64} cy={64} rx={54} ry={20} fill="none" stroke={AMBER} strokeWidth={5} transform="rotate(-60 64 64)" />
      <circle cx={64} cy={64} r={13} fill={CREAM} />
    </>
  )),
  icon_tap: icon(() => (
    <>
      <circle cx={64} cy={56} r={40} fill="none" stroke={TEAL} strokeWidth={4} opacity={0.5} />
      <circle cx={64} cy={56} r={24} fill="none" stroke={TEAL} strokeWidth={5} />
      <path d="M 56 46 L 56 100 L 66 90 L 76 112 L 86 106 L 76 86 L 90 84 Z" fill={CREAM} stroke={INK} strokeWidth={3} strokeLinejoin="round" />
    </>
  )),
  icon_passive: icon(() => (
    <>
      <path d="M 104 64 A 40 40 0 1 1 84 29" stroke={TEAL} strokeWidth={9} fill="none" strokeLinecap="round" />
      <path d="M 78 12 L 102 26 L 78 42 Z" fill={TEAL} />
      <path d="M 70 40 L 50 70 L 64 70 L 58 94 L 80 60 L 66 60 L 74 40 Z" fill={AMBER} />
    </>
  )),
  icon_discount: icon(() => (
    <>
      <path d="M 14 64 L 50 22 L 112 22 L 112 106 L 50 106 Z" fill={CREAM} />
      <circle cx={40} cy={64} r={7} fill={INK} />
      <path d="M 64 92 L 96 36" stroke={RUST} strokeWidth={7} strokeLinecap="round" />
      <circle cx={68} cy={44} r={9} fill="none" stroke={RUST} strokeWidth={6} /><circle cx={92} cy={84} r={9} fill="none" stroke={RUST} strokeWidth={6} />
    </>
  )),
  icon_sound: icon(() => (
    <>
      <path d="M 14 48 L 38 48 L 66 22 L 66 106 L 38 80 L 14 80 Z" fill={CREAM} />
      <path d="M 80 44 Q 92 64 80 84" stroke={TEAL} strokeWidth={7} fill="none" strokeLinecap="round" />
      <path d="M 94 30 Q 116 64 94 98" stroke={TEAL} strokeWidth={7} fill="none" strokeLinecap="round" />
    </>
  )),
  icon_reputation: icon(() => (
    <>
      <path d="M 64 10 L 110 26 L 104 72 Q 96 104 64 120 Q 32 104 24 72 L 18 26 Z" fill={MOSS} />
      <path d="M 64 10 L 64 120 Q 32 104 24 72 L 18 26 Z" fill={lighten(MOSS, 0.2)} />
      <path d="M 42 64 L 58 80 L 88 44" stroke={CREAM} strokeWidth={10} fill="none" strokeLinecap="round" strokeLinejoin="round" />
    </>
  )),
  icon_settings: icon(() => (
    <>
      {Array.from({ length: 8 }).map((_, i) => <rect key={i} x={56} y={8} width={16} height={28} rx={4} fill={STONE} transform={`rotate(${i * 45} 64 64)`} />)}
      <circle cx={64} cy={64} r={38} fill={STONE} />
      <circle cx={64} cy={64} r={16} fill={INK} />
    </>
  )),
  icon_lock: icon(() => (
    <>
      <path d="M 40 58 L 40 40 A 24 24 0 0 1 88 40 L 88 58" stroke={STONE} strokeWidth={10} fill="none" />
      <rect x={24} y={56} width={80} height={60} rx={10} fill={AMBER} />
      <rect x={24} y={56} width={80} height={14} rx={6} fill={lighten(AMBER, 0.25)} />
      <circle cx={64} cy={84} r={8} fill={INK} /><rect x={60} y={86} width={8} height={16} fill={INK} />
    </>
  )),
  icon_house: icon(() => (
    <>
      <path d="M 12 62 L 64 18 L 116 62 Z" fill={RUST} />
      <rect x={24} y={62} width={80} height={52} fill={CREAM} />
      <rect x={56} y={80} width={18} height={34} fill="#7a5640" />
      <rect x={32} y={72} width={16} height={14} fill={AMBER} />
      <rect x={88} y={26} width={12} height={22} fill={STONE} />
    </>
  )),
  icon_port: icon(() => (
    <>
      <path d="M 26 100 L 26 16 L 112 16" stroke={RUST} strokeWidth={9} fill="none" />
      <path d="M 96 16 L 96 50" stroke={INK} strokeWidth={3} />
      <rect x={86} y={50} width={20} height={12} fill={TEAL} />
      <path d="M 8 92 L 120 92 L 108 116 L 20 116 Z" fill="#5a5c58" />
      <rect x={40} y={74} width={26} height={18} fill={RUST} /><rect x={66} y={74} width={26} height={18} fill={TEAL} />
    </>
  )),
  icon_map: icon(() => (
    <>
      <path d="M 10 26 L 44 14 L 84 26 L 118 14 L 118 102 L 84 114 L 44 102 L 10 114 Z" fill={CREAM} />
      <path d="M 44 14 L 44 102 M 84 26 L 84 114" stroke={STONE} strokeWidth={3} />
      <path d="M 22 84 Q 48 60 62 70 Q 80 82 104 44" stroke={RUST} strokeWidth={5} fill="none" strokeDasharray="8 7" />
      <circle cx={104} cy={44} r={7} fill={RUST} />
      <path d="M 20 40 l 8 -10 l 8 10 Z M 92 92 l 8 -10 l 8 10 Z" fill={MOSS} />
    </>
  )),
};

export const ui: Record<string, AssetDef> = {
  ui_panel: panel("#2b231d", "#6b5440"),
  ui_panel_light: panel("#3a3027", "#8a6c52"),
  ui_button: button("#6a7a3e"),
  ui_button_off: button("#4a423a"),
  ui_button_alt: button("#7a5a42"),
  ui_badge: badge,
  ...icons,
};
