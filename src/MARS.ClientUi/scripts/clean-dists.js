import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const targets = ["dist", "dists"].map(p => path.resolve(__dirname, "..", p));

for (const t of targets) {
  try {
    if (fs.existsSync(t)) {
      fs.rmSync(t, { recursive: true, force: true });
      console.log(`Removed ${t}`);
    } else {
      console.log(`Not found: ${t}`);
    }
  } catch (err) {
    console.error(`Failed to remove ${t}:`, err);
    process.exitCode = 1;
  }
}
