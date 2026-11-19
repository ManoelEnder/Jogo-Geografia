import React from "react";
import { AbsoluteFill, Still } from "remotion";
import { ASSETS, AssetSvg } from "./art/registry";

const Peca: React.FC<{ name: string }> = ({ name }) => (
  <AbsoluteFill>
    <AssetSvg name={name} />
  </AbsoluteFill>
);

/** Folha de contato com todas as peças, para revisar o conjunto de uma vez. */
const Folha: React.FC = () => {
  const names = Object.keys(ASSETS);
  return (
    <AbsoluteFill style={{ backgroundColor: "#6c7535", display: "flex", flexWrap: "wrap", alignContent: "flex-start", padding: 20, gap: 16 }}>
      {names.map((n) => (
        <div key={n} style={{ width: 300, height: 300, display: "flex", flexDirection: "column", alignItems: "center", background: "rgba(0,0,0,0.12)", borderRadius: 8 }}>
          <div style={{ width: 280, height: 260 }}>
            <AssetSvg name={n} />
          </div>
          <div style={{ color: "#f1e6cf", fontFamily: "Georgia", fontSize: 16 }}>{n}</div>
        </div>
      ))}
    </AbsoluteFill>
  );
};

export const RemotionRoot: React.FC = () => {
  const names = Object.keys(ASSETS);
  const rows = Math.ceil(names.length / 6);
  return (
    <>
      <Still id="Folha" component={Folha} width={1920} height={40 + rows * 316} />
      {names.map((n) => (
        <Still key={n} id={n.replace(/_/g, "-")} component={Peca} width={ASSETS[n].w} height={ASSETS[n].h} defaultProps={{ name: n }} />
      ))}
    </>
  );
};
