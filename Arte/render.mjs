// Renderiza todas as peças em PNG transparente para o Unity.
// Uso: node render.mjs [nome-da-peça ...]   (sem nomes = todas; "Folha" = folha de contato)
import path from "node:path";
import fs from "node:fs";
import { bundle } from "@remotion/bundler";
import { getCompositions, renderStill } from "@remotion/renderer";

const outDir = path.resolve("../Jogo Geografia/Assets/NucleoVivo/Resources/Arte");
const sheetDir = path.resolve("out");
fs.mkdirSync(outDir, { recursive: true });
fs.mkdirSync(sheetDir, { recursive: true });

const serveUrl = await bundle({ entryPoint: path.resolve("src/index.ts") });
const comps = await getCompositions(serveUrl);
const wanted = process.argv.slice(2);
const list = comps.filter((c) => (wanted.length ? wanted.includes(c.id) : c.id !== "Folha"));
for (const c of list) {
  const file = c.id === "Folha" ? path.join(sheetDir, "folha.png") : path.join(outDir, c.id.replace(/-/g, "_") + ".png");
  await renderStill({ composition: c, serveUrl, output: file, imageFormat: "png", scale: 1, inputProps: c.defaultProps });
  console.log("ok", path.basename(file));
}
