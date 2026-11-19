import React from "react";
import { AssetDef, PaintFilters } from "./core";
import { nature } from "./nature";
import { buildings } from "./buildings";
import { world } from "./world";
import { ui } from "./ui";

export const ASSETS: Record<string, AssetDef> = {
  ...nature,
  ...buildings,
  ...world,
  ...ui,
};

/** Desenha uma peça isolada, com filtros de pintura e fundo transparente. */
export const AssetSvg: React.FC<{ name: string; flat?: boolean }> = ({ name, flat }) => {
  const a = ASSETS[name];
  if (!a) return null;
  flat = flat || name.startsWith("ui_") || name.startsWith("icon_") || ["light_shafts", "vignette", "soft_glow", "sky", "mist", "river", "road"].includes(name) || name.startsWith("ground_") || name.endsWith("_lights");
  const id = name.replace(/[^a-z0-9]/gi, "");
  const seed = [...name].reduce((s, c) => s + c.charCodeAt(0), 0) % 50;
  return (
    <svg width="100%" height="100%" viewBox={`0 0 ${a.w} ${a.h}`} xmlns="http://www.w3.org/2000/svg">
      <PaintFilters id={id} seed={seed} />
      <g filter={flat ? undefined : `url(#paint-${id})`}>{a.render(id)}</g>
    </svg>
  );
};
