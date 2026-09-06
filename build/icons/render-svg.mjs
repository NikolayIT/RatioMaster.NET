// Renders ../../icon.svg to PNG files at the sizes needed for the app icons.
// Usage: node render-svg.mjs <outDir> <size> [<size> ...]
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { Resvg } from "@resvg/resvg-js";

const here = dirname(fileURLToPath(import.meta.url));
const svg = readFileSync(join(here, "..", "..", "icon.svg"));
const [outDir, ...sizes] = process.argv.slice(2);
mkdirSync(outDir, { recursive: true });
for (const s of sizes) {
  const size = Number(s);
  const png = new Resvg(svg, { fitTo: { mode: "width", value: size } }).render().asPng();
  writeFileSync(join(outDir, `icon-${size}.png`), png);
  console.log(`icon-${size}.png`);
}
