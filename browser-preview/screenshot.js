// Quick screenshot script — uses npx playwright screenshot (no node_modules needed)
const { execSync } = require('child_process');
const path = require('path');

const baseUrl = 'file:///C:/Users/PLANNER007/MiniMapGame/browser-preview/index.html';
const outDir = path.resolve(__dirname);

const types = ['Grid', 'Mountain', 'Rural'];

for (const type of types) {
  const url = `${baseUrl}#type=${type}`;
  const out = path.join(outDir, `preview-${type.toLowerCase()}.png`);
  console.log(`Capturing ${type}...`);
  execSync(`npx playwright screenshot --viewport-size "1280,800" --wait-for-timeout 1500 "${url}" "${out}"`, {
    cwd: 'C:/Users/PLANNER007/WritingPage',
    stdio: 'inherit',
  });
}
console.log('Done.');
