// Quick screenshot script — uses npx playwright screenshot (no local node_modules required)
const { execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const { pathToFileURL } = require('url');

const previewDir = path.resolve(__dirname);
const baseUrl = pathToFileURL(path.join(previewDir, 'index.html')).href;

const cliArgs = process.argv.slice(2);
const outputDirIndex = cliArgs.indexOf('--output-dir');
const channelIndex = cliArgs.indexOf('--channel');
const updateBaselines = cliArgs.includes('--update-baselines');
if (outputDirIndex >= 0 && updateBaselines) {
  throw new Error('Use either --output-dir or --update-baselines, not both.');
}
if (outputDirIndex >= 0 && !cliArgs[outputDirIndex + 1]) {
  throw new Error('--output-dir requires a path.');
}
if (channelIndex >= 0 && !cliArgs[channelIndex + 1]) {
  throw new Error('--channel requires a Playwright Chromium channel name.');
}

const browserChannel = channelIndex >= 0
  ? cliArgs[channelIndex + 1]
  : process.env.PLAYWRIGHT_CHANNEL || 'chrome';

const outDir = updateBaselines
  ? previewDir
  : outputDirIndex >= 0
    ? path.resolve(process.cwd(), cliArgs[outputDirIndex + 1])
    : path.resolve(previewDir, '..', 'Logs', 'browser-preview');
fs.mkdirSync(outDir, { recursive: true });

function runNpx(args) {
  if (process.platform === 'win32') {
    const npxCommand = execFileSync('where.exe', ['npx.cmd'], { encoding: 'utf8' })
      .trim()
      .split(/\r?\n/)[0];
    execFileSync(process.env.ComSpec || 'cmd.exe', ['/d', '/c', 'call', npxCommand, ...args], {
      cwd: previewDir,
      stdio: 'inherit',
    });
    return;
  }

  execFileSync('npx', args, {
    cwd: previewDir,
    stdio: 'inherit',
  });
}

const types = ['Organic', 'Grid', 'Mountain', 'Rural'];

for (const type of types) {
  const url = `${baseUrl}#type=${type}`;
  const out = path.join(outDir, `preview-${type.toLowerCase()}.png`);
  console.log(`Capturing ${type}...`);
  runNpx([
    '--no-install',
    'playwright',
    'screenshot',
    '--channel', browserChannel,
    '--viewport-size', '1280,800',
    '--wait-for-timeout', '1500',
    url,
    out,
  ]);
}
console.log('Done.');
