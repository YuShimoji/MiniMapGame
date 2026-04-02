// MiniMapGame Browser Preview — Map Generation Engine
// Direct port of C# Assets/Scripts/{Core,MapGen,Data}/ to JavaScript
// Phase A: Road network + Buildings
// Phase B: Terrain (hills/elevation) + Water (rivers/coasts)

// ============================================================
// Data Types
// ============================================================

/** @returns {{position:{x:number,y:number}, degree:number, label:string, type:string, elevation:number}} */
function makeNode(x, y, label = '', type = 'None', elevation = 0) {
  return { position: { x, y }, degree: 0, label, type, elevation };
}

/** @returns {{nodeA:number, nodeB:number, tier:number, controlPoint:{x:number,y:number}, layer:number}} */
function makeEdge(nodeA, nodeB, tier, controlPoint) {
  return { nodeA, nodeB, tier, controlPoint, layer: 0 };
}

/** @returns MapBuilding */
function makeBuilding(pos, width, height, angle, tier, isLandmark, floors, shapeType, id) {
  return { position: pos, width, height, angle, tier, isLandmark, floors, shapeType, id };
}

// ============================================================
// Preset Defaults
// ============================================================

const PRESETS = {
  Organic: {
    displayName: 'Organic Urban',
    generatorType: 'Organic',
    arterialRange: { x: 3, y: 6 },
    hasRingRoad: true,
    curveAmount: 0.7,
    buildingDensity: 0.6,
    hasCoast: true,
    hasRiver: true,
    hillDensity: 0.4,
    worldWidth: 860,
    worldHeight: 580,
    borderPadding: 50,
    maxElevation: 15,
    elevationScale: 1,
    steepnessBias: 0.5,
    decorationDensity: 0.5,
  },
  Grid: {
    displayName: 'Grid City',
    generatorType: 'Grid',
    arterialRange: { x: 4, y: 7 },
    hasRingRoad: false,
    curveAmount: 0.15,
    buildingDensity: 0.7,
    hasCoast: true,
    hasRiver: true,
    hillDensity: 0.2,
    worldWidth: 860,
    worldHeight: 580,
    borderPadding: 50,
    maxElevation: 10,
    elevationScale: 1,
    steepnessBias: 0.3,
    decorationDensity: 0.5,
  },
  Mountain: {
    displayName: 'Mountain Trail',
    generatorType: 'Mountain',
    arterialRange: { x: 2, y: 4 },
    hasRingRoad: false,
    curveAmount: 0.9,
    buildingDensity: 0.3,
    hasCoast: false,
    hasRiver: true,
    hillDensity: 0.8,
    worldWidth: 860,
    worldHeight: 580,
    borderPadding: 50,
    maxElevation: 20,
    elevationScale: 1.2,
    steepnessBias: 0.8,
    decorationDensity: 0.4,
  },
  Rural: {
    displayName: 'Rural Village',
    generatorType: 'Rural',
    arterialRange: { x: 4, y: 7 },
    hasRingRoad: false,
    curveAmount: 0.5,
    buildingDensity: 0.4,
    hasCoast: false,
    hasRiver: true,
    hillDensity: 0.3,
    worldWidth: 860,
    worldHeight: 580,
    borderPadding: 50,
    maxElevation: 12,
    elevationScale: 1,
    steepnessBias: 0.4,
    decorationDensity: 0.6,
  },
};

// ============================================================
// SeededRng — XOR-shift PRNG (port of C# SeededRng / JSX mkRng)
// ============================================================

class SeededRng {
  constructor(seed) {
    this._state = (seed >>> 0) || 1; // uint32, non-zero
  }
  next() {
    this._state ^= (this._state << 13) >>> 0;
    this._state ^= (this._state >>> 17);
    this._state ^= (this._state << 5) >>> 0;
    this._state = this._state >>> 0; // keep uint32
    return this._state / 0x100000000;
  }
  rangeInt(min, max) {
    return min + Math.floor(this.next() * (max - min));
  }
  rangeFloat(min, max) {
    return min + this.next() * (max - min);
  }
}

// ============================================================
// MapGenUtils — Core utility functions
// ============================================================

function vec2(x, y) { return { x, y }; }
function dist(a, b) { return Math.sqrt((a.x - b.x) ** 2 + (a.y - b.y) ** 2); }
function dir(from, to) {
  const d = dist(from, to);
  if (d < 0.0001) return { x: 1, y: 0 };
  return { x: (to.x - from.x) / d, y: (to.y - from.y) / d };
}
function perp(d) { return { x: -d.y, y: d.x }; }

function bezierPoint(a, ctrl, b, t) {
  const u = 1 - t;
  return {
    x: u * u * a.x + 2 * u * t * ctrl.x + t * t * b.x,
    y: u * u * a.y + 2 * u * t * ctrl.y + t * t * b.y,
  };
}

function clampToPreset(p, preset) {
  const pad = preset.borderPadding;
  return {
    x: Math.max(pad, Math.min(preset.worldWidth - pad, p.x)),
    y: Math.max(pad * 0.8, Math.min(preset.worldHeight - pad * 0.8, p.y)),
  };
}

function addNode(nodes, x, y, preset, label = '', type = 'None') {
  const pos = clampToPreset(vec2(x, y), preset);
  nodes.push(makeNode(pos.x, pos.y, label, type));
  return nodes.length - 1;
}

function addEdge(nodes, edges, a, b, tier, rng, curveAmount = 0.5) {
  const na = nodes[a];
  const nb = nodes[b];
  const mx = lerp(na.position.x, nb.position.x, 0.45 + (rng.next() - 0.5) * 0.1)
    + (rng.next() - 0.5) * 30 * curveAmount * (1 - tier * 0.25);
  const my = lerp(na.position.y, nb.position.y, 0.45 + (rng.next() - 0.5) * 0.1)
    + (rng.next() - 0.5) * 30 * curveAmount * (1 - tier * 0.25);

  edges.push(makeEdge(a, b, tier, vec2(mx, my)));
  na.degree++;
  nb.degree++;
}

function lerp(a, b, t) { return a + (b - a) * t; }
function clamp(v, min, max) { return Math.max(min, Math.min(max, v)); }

// ============================================================
// SpatialHash — 2D collision detection
// ============================================================

class SpatialHash {
  constructor(cellSize = 40) {
    this._cellSize = cellSize;
    this._cells = new Map();
  }
  _bounds(item) {
    const cos = Math.abs(Math.cos(item.angle));
    const sin = Math.abs(Math.sin(item.angle));
    const hw = (item.width * cos + item.height * sin) / 2 + 3;
    const hh = (item.width * sin + item.height * cos) / 2 + 3;
    return { x: item.position.x - hw, y: item.position.y - hh, w: hw * 2, h: hh * 2 };
  }
  _key(x, y) { return `${x},${y}`; }
  insert(item) {
    const bd = this._bounds(item);
    const x0 = Math.floor(bd.x / this._cellSize);
    const x1 = Math.floor((bd.x + bd.w) / this._cellSize);
    const y0 = Math.floor(bd.y / this._cellSize);
    const y1 = Math.floor((bd.y + bd.h) / this._cellSize);
    for (let x = x0; x <= x1; x++) {
      for (let y = y0; y <= y1; y++) {
        const k = this._key(x, y);
        if (!this._cells.has(k)) this._cells.set(k, []);
        this._cells.get(k).push(item);
      }
    }
  }
  overlaps(item) {
    const bd = this._bounds(item);
    const x0 = Math.floor(bd.x / this._cellSize);
    const x1 = Math.floor((bd.x + bd.w) / this._cellSize);
    const y0 = Math.floor(bd.y / this._cellSize);
    const y1 = Math.floor((bd.y + bd.h) / this._cellSize);
    const seen = new Set();
    for (let x = x0; x <= x1; x++) {
      for (let y = y0; y <= y1; y++) {
        const list = this._cells.get(this._key(x, y));
        if (!list) continue;
        for (const other of list) {
          if (seen.has(other)) continue;
          seen.add(other);
          const ob = this._bounds(other);
          if (!(bd.x + bd.w < ob.x || ob.x + ob.w < bd.x ||
                bd.y + bd.h < ob.y || ob.y + ob.h < bd.y))
            return true;
        }
      }
    }
    return false;
  }
}

// ============================================================
// Generators
// ============================================================

function generateOrganic(rng, center, preset) {
  const nodes = [];
  const edges = [];
  const ca = preset.curveAmount;

  const C = addNode(nodes, center.x, center.y, preset, '\u5e02\u4e2d\u5fc3', 'Hub');

  const numA = preset.arterialRange.x + rng.rangeInt(0, preset.arterialRange.y - preset.arterialRange.x + 1);
  const arterialTips = [];

  for (let i = 0; i < numA; i++) {
    let angle = (i / numA) * Math.PI * 2 + rng.next() * 0.2;
    let prev = C;
    const totalLen = 150 + rng.next() * 130;
    const steps = 3 + Math.floor(rng.next() * 3);
    const slen = totalLen / steps;

    for (let j = 0; j < steps; j++) {
      angle += (rng.next() - 0.5) * 0.4 * ca;
      const nx = nodes[prev].position.x + Math.cos(angle) * slen * (0.7 + rng.next() * 0.6);
      const ny = nodes[prev].position.y + Math.sin(angle) * slen * (0.7 + rng.next() * 0.6);

      let snapped = -1;
      for (let k = 0; k < nodes.length; k++) {
        if (k !== prev && dist(nodes[k].position, vec2(nx, ny)) < 20) {
          snapped = k; break;
        }
      }

      const n = snapped >= 0
        ? snapped
        : addNode(nodes, nx, ny, preset, j === steps - 1 ? `\u9580${i + 1}` : '');
      addEdge(nodes, edges, prev, n, 0, rng, ca);
      if (j === steps - 1) arterialTips.push(n);
      prev = n;
    }
  }

  // Ring road
  if (preset.hasRingRoad && arterialTips.length >= 3) {
    arterialTips.sort((a, b) => {
      const angA = Math.atan2(nodes[a].position.y - center.y, nodes[a].position.x - center.x);
      const angB = Math.atan2(nodes[b].position.y - center.y, nodes[b].position.x - center.x);
      return angA - angB;
    });
    for (let i = 0; i < arterialTips.length; i++) {
      const j = (i + 1) % arterialTips.length;
      if (dist(nodes[arterialTips[i]].position, nodes[arterialTips[j]].position) < 280)
        addEdge(nodes, edges, arterialTips[i], arterialTips[j], 0, rng, ca * 0.6);
    }
  }

  // Secondary / tertiary
  const t0Count = edges.length;
  for (let ei = 0; ei < t0Count; ei++) {
    const seg = edges[ei];
    if (seg.tier !== 0) continue;
    const numB = Math.floor(rng.next() * 3);

    for (let b = 0; b < numB; b++) {
      if (rng.next() > 0.7) continue;
      const na = nodes[seg.nodeA];
      const nb = nodes[seg.nodeB];
      const t = 0.2 + rng.next() * 0.6;
      const bx = lerp(na.position.x, nb.position.x, t) + (rng.next() - 0.5) * 10;
      const by = lerp(na.position.y, nb.position.y, t) + (rng.next() - 0.5) * 10;

      let tooClose = false;
      for (let k = 0; k < nodes.length; k++) {
        if (dist(nodes[k].position, vec2(bx, by)) < 13) { tooClose = true; break; }
      }
      if (tooClose) continue;

      const bRoot = addNode(nodes, bx, by, preset);
      addEdge(nodes, edges, seg.nodeA, bRoot, 1, rng, ca);

      let brAngle = Math.atan2(nb.position.y - na.position.y, nb.position.x - na.position.x)
        + (rng.next() > 0.5 ? 1 : -1) * (Math.PI * 0.4 + rng.next() * 0.3);
      let prev = bRoot;
      const brSteps = 2 + Math.floor(rng.next() * 4);

      for (let j = 0; j < brSteps; j++) {
        brAngle += (rng.next() - 0.5) * 0.35 * ca;
        const len = 28 + rng.next() * 60;
        const nnx = nodes[prev].position.x + Math.cos(brAngle) * len;
        const nny = nodes[prev].position.y + Math.sin(brAngle) * len;

        let ok = true;
        for (let k = 0; k < nodes.length; k++) {
          if (dist(nodes[k].position, vec2(nnx, nny)) < 11) { ok = false; break; }
        }
        if (!ok) break;

        const nn = addNode(nodes, nnx, nny, preset);
        addEdge(nodes, edges, prev, nn, 1, rng, ca);

        // Tertiary sub-branch
        if (rng.next() > 0.6) {
          const aa = brAngle + (rng.next() > 0.5 ? 1 : -1) * (0.4 + rng.next() * 0.5);
          const ax = nodes[prev].position.x + Math.cos(aa) * (18 + rng.next() * 38);
          const ay = nodes[prev].position.y + Math.sin(aa) * (18 + rng.next() * 38);
          let ok2 = true;
          for (let k = 0; k < nodes.length; k++) {
            if (dist(nodes[k].position, vec2(ax, ay)) < 10) { ok2 = false; break; }
          }
          if (ok2) {
            const an = addNode(nodes, ax, ay, preset);
            addEdge(nodes, edges, prev, an, 2, rng, ca);
          }
        }
        prev = nn;
      }
    }
  }

  return { nodes, edges };
}

function generateGrid(rng, center, preset) {
  const nodes = [];
  const edges = [];

  const spacing = 42 + rng.next() * 10;
  const cols = Math.floor((preset.worldWidth - 100) / spacing);
  const rows = Math.floor((preset.worldHeight - 80) / spacing);
  const startX = (preset.worldWidth - cols * spacing) / 2;
  const startY = (preset.worldHeight - rows * spacing) / 2;

  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const jx = (rng.next() - 0.5) * preset.curveAmount * 12;
      const jy = (rng.next() - 0.5) * preset.curveAmount * 12;
      const label = (r === Math.floor(rows / 2) && c === Math.floor(cols / 2)) ? '\u4e2d\u5fc3\u8857' : '';
      nodes.push(makeNode(startX + c * spacing + jx, startY + r * spacing + jy, label));
    }
  }

  // Horizontal
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols - 1; c++) {
      const a = r * cols + c, b = r * cols + c + 1;
      let tier;
      if (r === Math.floor(rows / 2) || r === 1 || r === rows - 2) tier = 0;
      else if (c % 4 === 0) tier = 1;
      else tier = 2;
      addEdge(nodes, edges, a, b, tier, rng, preset.curveAmount);
    }
  }
  // Vertical
  for (let c = 0; c < cols; c++) {
    for (let r = 0; r < rows - 1; r++) {
      const a = r * cols + c, b = (r + 1) * cols + c;
      let tier;
      if (c === Math.floor(cols / 2) || c === 1 || c === cols - 2) tier = 0;
      else if (r % 4 === 0) tier = 1;
      else tier = 2;
      addEdge(nodes, edges, a, b, tier, rng, preset.curveAmount);
    }
  }

  // Diagonal avenues
  if (rows > 4 && cols > 4) {
    createDiagonalAvenue(nodes, edges, rng, rows, cols, 0, Math.floor(cols * 0.3), 1, 1);
    if (rows > 6 && cols > 6) {
      createDiagonalAvenue(nodes, edges, rng, rows, cols, 0, Math.floor(cols * 0.7), 1, -1);
    }
  }

  // Mark center
  const centerNode = Math.floor(rows / 2) * cols + Math.floor(cols / 2);
  if (centerNode < nodes.length) nodes[centerNode].type = 'Hub';

  return { nodes, edges };
}

function createDiagonalAvenue(nodes, edges, rng, rows, cols, startRow, startCol, rowDir, colDir) {
  let row = startRow, col = startCol;
  while (row >= 0 && row < rows - 1 && col >= 0 && col < cols) {
    const cur = row * cols + col;
    let nextRow = row + rowDir;
    let nextCol = col;
    if (rng.next() > 0.4) nextCol = col + colDir;
    nextCol = clamp(nextCol, 0, cols - 1);
    nextRow = clamp(nextRow, 0, rows - 1);
    if (nextRow === row && nextCol === col) break;
    const next = nextRow * cols + nextCol;
    addEdge(nodes, edges, cur, next, 0, rng, 0.12);
    row = nextRow;
    col = nextCol;
  }
}

function generateMountain(rng, center, preset) {
  const nodes = [];
  const edges = [];
  const ca = preset.curveAmount;
  const w = preset.worldWidth, h = preset.worldHeight;

  // Spine
  let x = center.x + (rng.next() - 0.5) * 80;
  let y = 30;
  const spine = [];

  while (y < h - 30) {
    const label = y < 60 ? '\u767b\u5c71\u53e3' : (y > h - 60 ? '\u5c71\u9802' : '');
    spine.push(addNode(nodes, x, y, preset, label));
    x = clamp(x + (rng.next() - 0.5) * 80 * ca, 80, w - 80);
    y += 38 + rng.next() * 28;
  }

  for (let i = 0; i < spine.length - 1; i++)
    addEdge(nodes, edges, spine[i], spine[i + 1], 0, rng, ca);

  // Elevation profile
  const maxElev = preset.maxElevation;
  const peakPos = 0.5 + (rng.next() - 0.5) * 0.3;
  for (let i = 0; i < spine.length; i++) {
    const t = spine.length > 1 ? i / (spine.length - 1) : 0;
    const d = Math.abs(t - peakPos) / Math.max(peakPos, 1 - peakPos);
    nodes[spine[i]].elevation = maxElev * Math.exp(-d * d * 3);
  }

  // Secondary ridges
  const numRidges = 1 + Math.floor(rng.next() * 2);
  for (let r = 0; r < numRidges; r++) {
    let branchIdx = Math.floor(spine.length * (0.4 + rng.next() * 0.4));
    branchIdx = clamp(branchIdx, 1, spine.length - 2);
    const branchFrom = spine[branchIdx];
    const parentElev = nodes[branchFrom].elevation;

    const nextSpine = spine[Math.min(branchIdx + 1, spine.length - 1)];
    const spineAngle = Math.atan2(
      nodes[nextSpine].position.y - nodes[branchFrom].position.y,
      nodes[nextSpine].position.x - nodes[branchFrom].position.x);
    let ridgeAngle = spineAngle + (rng.next() > 0.5 ? 1 : -1) * (Math.PI * 0.3 + rng.next() * 0.4);

    const ridgeSteps = 3 + Math.floor(rng.next() * 3);
    let prev = branchFrom;

    for (let s = 0; s < ridgeSteps; s++) {
      ridgeAngle += (rng.next() - 0.5) * 0.4 * ca;
      const stepLen = 30 + rng.next() * 40;
      const rx = nodes[prev].position.x + Math.cos(ridgeAngle) * stepLen;
      const ry = nodes[prev].position.y + Math.sin(ridgeAngle) * stepLen;
      if (rx < 50 || rx > w - 50 || ry < 30 || ry > h - 30) break;

      const rLabel = s === ridgeSteps - 1 ? '\u526f\u5cf0' : '';
      const rn = addNode(nodes, rx, ry, preset, rLabel);
      const ridgeT = (s + 1) / ridgeSteps;
      nodes[rn].elevation = parentElev * (0.7 - ridgeT * 0.3);
      addEdge(nodes, edges, prev, rn, 1, rng, ca);
      prev = rn;
    }
  }

  // Dead-end branches
  for (const si of spine) {
    if (rng.next() >= 0.4) continue;
    const angle = (rng.next() - 0.5) * Math.PI * 0.7 + Math.PI / 2;
    const len = 35 + rng.next() * 65;
    const bx = nodes[si].position.x + Math.cos(angle) * len;
    const by = nodes[si].position.y + Math.sin(angle) * len;
    if (bx < 50 || bx > w - 50 || by < 30 || by > h - 30) continue;

    const bLabel = rng.next() > 0.8 ? '\u907f\u96e3\u5c0f\u5c4b' : '';
    const bn = addNode(nodes, bx, by, preset, bLabel, 'Shelter');
    nodes[bn].elevation = nodes[si].elevation * (0.6 + rng.next() * 0.3);
    addEdge(nodes, edges, si, bn, 1, rng, ca);

    if (rng.next() > 0.6) {
      const bx2 = bx + (rng.next() - 0.5) * 45;
      const by2 = by + (rng.next() - 0.5) * 45;
      if (bx2 > 50 && bx2 < w - 50 && by2 > 30 && by2 < h - 30) {
        const bn2 = addNode(nodes, bx2, by2, preset);
        nodes[bn2].elevation = nodes[bn].elevation * (0.5 + rng.next() * 0.3);
        addEdge(nodes, edges, bn, bn2, 2, rng, ca);
      }
    }
  }

  return { nodes, edges };
}

function generateRural(rng, center, preset) {
  const nodes = [];
  const edges = [];
  const ca = preset.curveAmount;
  const w = preset.worldWidth, h = preset.worldHeight;

  const C = addNode(nodes, center.x, center.y, preset, '\u6751', 'Hub');
  const numA = preset.arterialRange.x + Math.floor(rng.next() * (preset.arterialRange.y - preset.arterialRange.x + 1));

  const spokeNodes = [];

  for (let i = 0; i < numA; i++) {
    const baseAngle = (i / numA) * Math.PI * 2 + rng.next() * 0.4;
    let prev = C;
    const steps = 4 + Math.floor(rng.next() * 4);
    const len = (200 + rng.next() * 120) / steps;
    const spoke = [];

    for (let j = 0; j < steps; j++) {
      const na = baseAngle + (rng.next() - 0.5) * 0.25 * ca;
      const nx = nodes[prev].position.x + Math.cos(na) * len * (0.6 + rng.next() * 0.8);
      const ny = nodes[prev].position.y + Math.sin(na) * len * (0.6 + rng.next() * 0.8);

      let ok = true;
      for (let k = 0; k < nodes.length; k++) {
        if (dist(nodes[k].position, vec2(nx, ny)) < 18) { ok = false; break; }
      }
      if (!ok) break;

      const label = (j === steps - 1 && rng.next() > 0.4) ? '\u8fb2\u5834' : '';
      const tier = j < 2 ? 0 : 1;
      const n = addNode(nodes, nx, ny, preset, label, label === '\u8fb2\u5834' ? 'Farm' : 'None');
      addEdge(nodes, edges, prev, n, tier, rng, ca);
      spoke.push(n);

      if (j > 0 && rng.next() > 0.75) {
        const bAng = na + (rng.next() > 0.5 ? 1 : -1) * (0.5 + rng.next() * 0.6);
        const bLen = 25 + rng.next() * 55;
        const bx = nodes[n].position.x + Math.cos(bAng) * bLen;
        const by = nodes[n].position.y + Math.sin(bAng) * bLen;
        if (bx > 50 && bx < w - 50 && by > 40 && by < h - 40) {
          const bn = addNode(nodes, bx, by, preset);
          addEdge(nodes, edges, n, bn, 2, rng, ca);
        }
      }
      prev = n;
    }
    spokeNodes.push(spoke);
  }

  // Cross-links
  for (let i = 0; i < spokeNodes.length; i++) {
    const j = (i + 1) % spokeNodes.length;
    const sA = spokeNodes[i], sB = spokeNodes[j];
    if (sA.length < 2 || sB.length < 2) continue;
    const idxA = Math.min(1, sA.length - 1);
    const idxB = Math.min(1, sB.length - 1);
    const d = dist(nodes[sA[idxA]].position, nodes[sB[idxB]].position);
    if (d < 200 && rng.next() > 0.3) {
      addEdge(nodes, edges, sA[idxA], sB[idxB], 2, rng, ca);
    }
  }

  return { nodes, edges };
}

const GENERATORS = {
  Organic: generateOrganic,
  Grid: generateGrid,
  Mountain: generateMountain,
  Rural: generateRural,
};

// ============================================================
// BuildingPlacer
// ============================================================

const TIER_ROAD_WIDTH = [[12, 8], [8, 5], [5, 3]];
const TIER_BUILDING_WIDTH = [[16, 12], [11, 8], [7, 5]];

function placeBuildings(nodes, edges, rng, preset, terrain = null) {
  const buildings = [];
  const hash = new SpatialHash(40);
  let bldId = 0;

  for (const seg of edges) {
    const na = nodes[seg.nodeA];
    const nb = nodes[seg.nodeB];
    const d = dist(na.position, nb.position);
    if (d < 15) continue;

    const direction = dir(na.position, nb.position);
    const pd = perp(direction);
    const ti = clamp(seg.tier, 0, 2);
    const hw = TIER_ROAD_WIDTH[ti];
    const bw = TIER_BUILDING_WIDTH[ti];

    for (let side = -1; side <= 1; side += 2) {
      const baseOff = (hw[0] + 3) * side;
      const spacing = 7 + rng.next() * 8;
      const count = Math.floor(d / spacing);
      let ci = Math.floor(rng.next() * 2);

      while (ci < count) {
        if (rng.next() > preset.buildingDensity) { ci++; continue; }

        const t = (ci + 0.5) / count;
        const bp = bezierPoint(na.position, seg.controlPoint, nb.position, t);
        const bx = bp.x + pd.x * baseOff + (rng.next() - 0.5) * 2.5;
        const by = bp.y + pd.y * baseOff + (rng.next() - 0.5) * 2.5;
        const buildW = bw[0] + rng.next() * (bw[1] - bw[0]);
        const buildH = bw[0] * 0.65 + rng.next() * bw[1] * 0.5;
        const angle = Math.atan2(direction.y, direction.x) + (rng.next() - 0.5) * 0.15;
        const isLm = rng.next() > 0.95 && seg.tier === 0;

        let floors;
        if (isLm) floors = 5 + Math.floor(rng.next() * 6);
        else if (ti === 0) floors = 2 + Math.floor(rng.next() * 5);
        else if (ti === 1) floors = 1 + Math.floor(rng.next() * 4);
        else floors = 1 + Math.floor(rng.next() * 2);

        let shapeType;
        if (isLm) shapeType = 3;
        else if (ti === 0) shapeType = Math.floor(rng.next() * 3);
        else shapeType = rng.next() > 0.85 ? Math.floor(rng.next() * 2) : 0;

        const b = makeBuilding(
          vec2(bx, by),
          isLm ? buildW * 1.9 : buildW,
          isLm ? buildH * 1.9 : buildH,
          angle, seg.tier, isLm, floors, shapeType,
          `B${bldId++}`
        );

        if (!hash.overlaps(b) && !isInsideCoast(vec2(bx, by), terrain ? terrain.waterBodies : null)) {
          hash.insert(b);
          buildings.push(b);
        }

        ci += 1 + (rng.next() > 0.65 ? 1 : 0);
      }
    }
  }

  return buildings;
}

// ============================================================
// MapAnalyzer
// ============================================================

function analyze(nodes, edges) {
  const analysis = { deadEndIndices: [], intersectionIndices: [], plazaIndices: [], chokeEdgeIndices: [] };
  for (let i = 0; i < nodes.length; i++) {
    const deg = nodes[i].degree;
    if (deg === 1) analysis.deadEndIndices.push(i);
    if (deg >= 3) analysis.intersectionIndices.push(i);
    if (deg >= 4) analysis.plazaIndices.push(i);
  }
  for (let i = 0; i < edges.length; i++) {
    const e = edges[i];
    if (e.tier > 1) continue;
    if (nodes[e.nodeA].degree > 2 || nodes[e.nodeB].degree > 2) continue;
    if (dist(nodes[e.nodeA].position, nodes[e.nodeB].position) <= 32) continue;
    analysis.chokeEdgeIndices.push(i);
  }
  return analysis;
}

// ============================================================
// Phase B: WaterProfile defaults
// ============================================================

const DEFAULT_WATER_PROFILE = {
  river: {
    baseWidth: 12, widthGrowth: 1.8, depthBase: 2.5, depthVariation: 0.3,
    swayAmount: 55, stepSizeMin: 20, stepSizeMax: 55, meanderFrequency: 0.5,
    terrainCarveStrength: 1.0, terrainCarveRadius: 25, flowResponsiveness: 0.5,
    sandbankStrength: 0.4,
  },
  coast: {
    inlandReach: 0.35, coastlineRoughness: 0.5, stepSizeMin: 25, stepSizeMax: 55,
    bayAmplitude: 0.3, baySpacing: 120, depthBase: 1.5, depthVariation: 0.2,
    terrainCarveStrength: 0.3, terrainCarveRadius: 40,
  },
};

// ============================================================
// Phase B: TerrainGenerator — Hill clusters
// ============================================================

function generateTerrain(rng, center, preset, coastSide, nodes) {
  const terrain = { hills: [], hillClusters: [], waterBodies: [], coastSide };
  const w = preset.worldWidth, h = preset.worldHeight;
  const numClusters = Math.floor(preset.hillDensity * (3 + rng.next() * 4));
  if (numClusters === 0) return terrain;

  const clusterCenters = [];
  let clusterId = 0;

  for (let c = 0; c < numClusters; c++) {
    let ctr = pickCoastAwarePos(rng, w, h, coastSide);
    let placed = false;
    for (let a = 0; a < 3; a++) {
      let tooClose = false;
      for (const ex of clusterCenters) {
        if (dist(ctr, ex) < 60) { tooClose = true; break; }
      }
      if (!tooClose) { placed = true; break; }
      ctr = pickCoastAwarePos(rng, w, h, coastSide);
    }

    const type = placed ? pickClusterType(rng, preset.generatorType) : 'Solitary';
    const dominantProfile = pickDominantProfile(rng, preset);
    let orientation = rng.next() * Math.PI;
    if (coastSide >= 0) {
      const coastAngle = coastSide * Math.PI * 0.5;
      orientation = lerp(orientation, coastAngle + Math.PI * 0.5, 0.3);
    }

    const cluster = { id: clusterId, type, center: ctr, orientationAngle: orientation, dominantProfile };
    terrain.hillClusters.push(cluster);
    clusterCenters.push(ctr);

    switch (type) {
      case 'Ridge': genRidgeHills(terrain, rng, cluster, preset, nodes); break;
      case 'MoundGroup': genMoundGroupHills(terrain, rng, cluster, preset, nodes); break;
      case 'ValleyFramer': genValleyFramerHills(terrain, rng, cluster, preset, nodes); break;
      case 'Solitary': genSolitaryHill(terrain, rng, cluster, preset, nodes); break;
    }
    clusterId++;
  }
  return terrain;
}

function makeHill(pos, rx, ry, angle, layers, profile, clusterId) {
  return { position: pos, radiusX: rx, radiusY: ry, angle, layers, profile, clusterId };
}

function genRidgeHills(terrain, rng, cl, preset, nodes) {
  const count = 3 + Math.floor(rng.next() * 4);
  const spacing = 30 + rng.next() * 20;
  const halfLen = (count - 1) * spacing * 0.5;
  const cosA = Math.cos(cl.orientationAngle), sinA = Math.sin(cl.orientationAngle);
  for (let i = 0; i < count; i++) {
    const along = -halfLen + i * spacing;
    const perpJ = (rng.next() - 0.5) * 20;
    let px = cl.center.x + cosA * along + sinA * perpJ;
    let py = cl.center.y + sinA * along - cosA * perpJ;
    const rx = 40 + rng.next() * 40, ry = 20 + rng.next() * 25;
    const centeredness = 1 - Math.abs(2 * i / (count - 1) - 1);
    const layers = 2 + Math.floor(centeredness * 2 + rng.next());
    const profile = rng.next() < 0.7 ? cl.dominantProfile : pickVariantProfile(rng, cl.dominantProfile);
    const pos = applyNodeAvoidance(vec2(px, py), nodes, rng, preset);
    terrain.hills.push(makeHill(pos, rx, ry, cl.orientationAngle + (rng.next() - 0.5) * 0.15, layers, profile, cl.id));
  }
}

function genMoundGroupHills(terrain, rng, cl, preset, nodes) {
  const count = 3 + Math.floor(rng.next() * 3);
  const clusterRadius = 40 + rng.next() * 40;
  // Central
  const rx0 = 45 + rng.next() * 50, ry0 = 35 + rng.next() * 35;
  terrain.hills.push(makeHill(applyNodeAvoidance(cl.center, nodes, rng, preset), rx0, ry0,
    rng.next() * Math.PI, 3 + Math.floor(rng.next() * 2), cl.dominantProfile, cl.id));
  for (let i = 1; i < count; i++) {
    const angle = (i / (count - 1)) * Math.PI * 2 + rng.next() * 0.8;
    const d = clusterRadius * (0.5 + rng.next() * 0.5);
    const px = cl.center.x + Math.cos(angle) * d;
    const py = cl.center.y + Math.sin(angle) * d;
    const scale = 0.6 + rng.next() * 0.2;
    const rx = (35 + rng.next() * 40) * scale, ry = (22 + rng.next() * 30) * scale;
    const profile = rng.next() < 0.6 ? cl.dominantProfile : pickVariantProfile(rng, cl.dominantProfile);
    terrain.hills.push(makeHill(applyNodeAvoidance(vec2(px, py), nodes, rng, preset),
      rx, ry, rng.next() * Math.PI, 2 + Math.floor(rng.next() * 2), profile, cl.id));
  }
}

function genValleyFramerHills(terrain, rng, cl, preset, nodes) {
  const gapWidth = 60 + rng.next() * 40;
  const cosA = Math.cos(cl.orientationAngle), sinA = Math.sin(cl.orientationAngle);
  for (let side = -1; side <= 1; side += 2) {
    const n = 2 + Math.floor(rng.next() * 2);
    const spacing = 35 + rng.next() * 15;
    const halfLen = (n - 1) * spacing * 0.5;
    for (let i = 0; i < n; i++) {
      const along = -halfLen + i * spacing;
      const p = gapWidth * 0.5 * side + (rng.next() - 0.5) * 10;
      const px = cl.center.x + cosA * along + sinA * p;
      const py = cl.center.y + sinA * along - cosA * p;
      const rx = 35 + rng.next() * 35, ry = 20 + rng.next() * 20;
      const profile = rng.next() < 0.6 ? 'Steep' : cl.dominantProfile;
      terrain.hills.push(makeHill(applyNodeAvoidance(vec2(px, py), nodes, rng, preset),
        rx, ry, cl.orientationAngle + (rng.next() - 0.5) * 0.2, 2 + Math.floor(rng.next() * 2), profile, cl.id));
    }
  }
}

function genSolitaryHill(terrain, rng, cl, preset, nodes) {
  terrain.hills.push(makeHill(applyNodeAvoidance(cl.center, nodes, rng, preset),
    35 + rng.next() * 80, 22 + rng.next() * 50, rng.next() * Math.PI,
    2 + Math.floor(rng.next() * 3), cl.dominantProfile, cl.id));
}

function pickCoastAwarePos(rng, w, h, coastSide) {
  switch (coastSide) {
    case 0: return vec2(rng.next() * w * 0.6, rng.next() * h);
    case 1: return vec2(rng.next() * w, rng.next() * h * 0.6);
    case 2: return vec2(w * 0.4 + rng.next() * w * 0.6, rng.next() * h);
    case 3: return vec2(rng.next() * w, h * 0.4 + rng.next() * h * 0.6);
    default: return vec2(20 + rng.next() * (w - 40), 20 + rng.next() * (h - 40));
  }
}

function applyNodeAvoidance(hillPos, nodes, rng, preset) {
  if (!nodes || nodes.length === 0) return hillPos;
  const w = preset.worldWidth, h = preset.worldHeight;
  let pos = { ...hillPos };
  for (let a = 0; a < 3; a++) {
    let closestDist = Infinity, pushDir = { x: 0, y: 0 };
    for (const node of nodes) {
      const d = dist(pos, node.position);
      if (d < closestDist) {
        closestDist = d;
        if (d > 0.01) {
          const dd = dist(pos, node.position);
          pushDir = { x: (pos.x - node.position.x) / dd, y: (pos.y - node.position.y) / dd };
        }
      }
    }
    if (closestDist >= 30) break;
    const nudge = 30 - closestDist + rng.next() * 15;
    pos.x = clamp(pos.x + pushDir.x * nudge, 20, w - 20);
    pos.y = clamp(pos.y + pushDir.y * nudge, 20, h - 20);
  }
  return pos;
}

function pickClusterType(rng, genType) {
  const roll = rng.next();
  switch (genType) {
    case 'Mountain':
      if (roll < 0.40) return 'Ridge'; if (roll < 0.60) return 'MoundGroup';
      if (roll < 0.85) return 'ValleyFramer'; return 'Solitary';
    case 'Rural':
      if (roll < 0.15) return 'Ridge'; if (roll < 0.55) return 'MoundGroup';
      if (roll < 0.70) return 'ValleyFramer'; return 'Solitary';
    case 'Grid':
      if (roll < 0.20) return 'Ridge'; if (roll < 0.45) return 'MoundGroup';
      if (roll < 0.70) return 'ValleyFramer'; return 'Solitary';
    default:
      if (roll < 0.25) return 'Ridge'; if (roll < 0.55) return 'MoundGroup';
      if (roll < 0.75) return 'ValleyFramer'; return 'Solitary';
  }
}

function pickDominantProfile(rng, preset) {
  const bias = preset.steepnessBias;
  const roll = rng.next();
  switch (preset.generatorType) {
    case 'Mountain':
      if (roll < 0.30 + bias * 0.15) return 'Steep'; if (roll < 0.50 + bias * 0.10) return 'Plateau';
      if (roll < 0.65) return 'Mesa'; if (roll < 0.85) return 'Gaussian'; return 'Gentle';
    case 'Rural':
      if (roll < 0.50 - bias * 0.2) return 'Gentle'; if (roll < 0.75) return 'Gaussian';
      if (roll < 0.90) return 'Plateau'; return 'Steep';
    default:
      if (roll < 0.35) return 'Gaussian'; if (roll < 0.55) return 'Gentle';
      if (roll < 0.75 + bias * 0.1) return 'Steep'; if (roll < 0.90) return 'Plateau'; return 'Mesa';
  }
}

function pickVariantProfile(rng, dominant) {
  const roll = rng.next();
  switch (dominant) {
    case 'Steep': return roll < 0.5 ? 'Gaussian' : 'Plateau';
    case 'Gentle': return roll < 0.5 ? 'Gaussian' : 'Plateau';
    case 'Plateau': return roll < 0.5 ? 'Gaussian' : 'Mesa';
    case 'Mesa': return roll < 0.5 ? 'Plateau' : 'Steep';
    default: return roll < 0.5 ? 'Gentle' : 'Steep';
  }
}

// ============================================================
// Phase B: ElevationMap
// ============================================================

class ElevationMap {
  constructor(terrain, preset) {
    this._hills = terrain ? terrain.hills : [];
    this._scale = preset ? preset.elevationScale : 1;
    this._maxElevation = preset ? preset.maxElevation : 15;
    this._carvings = [];
  }
  addCarving(carving) { this._carvings.push(carving); }

  sample(pos) {
    let totalElev = 0;
    for (const hill of this._hills) {
      const cos = Math.cos(-hill.angle), sin = Math.sin(-hill.angle);
      const dx = pos.x - hill.position.x, dy = pos.y - hill.position.y;
      const lx = dx * cos - dy * sin, ly = dx * sin + dy * cos;
      const nx = lx / Math.max(hill.radiusX, 1), ny = ly / Math.max(hill.radiusY, 1);
      const distSq = nx * nx + ny * ny;
      if (distSq > 4) continue;
      const influence = computeFalloff(distSq, hill.profile);
      totalElev += influence * hill.layers * 2 * this._scale;
    }
    totalElev = Math.min(totalElev, this._maxElevation);

    if (this._carvings.length > 0) {
      let totalCarve = 0;
      for (const c of this._carvings) {
        const d = dist(pos, c.position);
        if (d >= c.radius) continue;
        const t = d / c.radius;
        totalCarve += c.depth * (1 - Math.pow(t, c.falloffPower));
      }
      totalElev = Math.max(0, totalElev - totalCarve);
    }
    return totalElev;
  }

  sampleSlope(pos) {
    const delta = 2;
    const ex = this.sample(vec2(pos.x + delta, pos.y));
    const wx = this.sample(vec2(pos.x - delta, pos.y));
    const ny = this.sample(vec2(pos.x, pos.y + delta));
    const sy = this.sample(vec2(pos.x, pos.y - delta));
    const dzdx = (ex - wx) / (2 * delta), dzdy = (ny - sy) / (2 * delta);
    return Math.sqrt(dzdx * dzdx + dzdy * dzdy);
  }

  applyToNodes(nodes) {
    for (let i = 0; i < nodes.length; i++) {
      if (nodes[i].elevation === 0) nodes[i].elevation = this.sample(nodes[i].position);
    }
  }
}

function computeFalloff(distSq, profile) {
  switch (profile) {
    case 'Steep': return Math.exp(-distSq * 3.0);
    case 'Gentle': return Math.exp(-distSq * 0.7);
    case 'Plateau':
      if (distSq < 0.09) return 1.0;
      { const d = (distSq - 0.09) / (4.0 - 0.09); return Math.max(0, 1.0 - d * d * 3.0); }
    case 'Mesa':
      if (distSq < 0.16) return 1.0;
      { const md = Math.sqrt(distSq) - 0.4; return Math.max(0, Math.exp(-md * md * 20)); }
    default: return Math.exp(-distSq * 1.5); // Gaussian
  }
}

// ============================================================
// Phase B: WaterGenerator
// ============================================================

function determineCoastSide(rng, preset) {
  if (!preset.hasCoast) return -1;
  return Math.floor(rng.next() * 4);
}

function generateWater(rng, center, preset, coastSide, nodes, elevMap) {
  const waterBodies = [];
  const profile = { ...DEFAULT_WATER_PROFILE };

  if (preset.hasCoast && coastSide >= 0) {
    waterBodies.push(generateCoast(rng, preset, profile.coast, coastSide));
  }

  if (preset.hasRiver) {
    let riverConfig = { ...profile.river };
    riverConfig = applyMeanderTuning(riverConfig, preset.generatorType);
    waterBodies.push(generateRiver(rng, center, preset, riverConfig, nodes, elevMap, waterBodies));
  }

  return waterBodies;
}

function generateCoast(rng, preset, config, side) {
  const coast = { bodyType: 'Coast', pathPoints: [], widths: [], depths: [], coastSide: side, boundsMin: null, boundsMax: null };
  const w = preset.worldWidth, h = preset.worldHeight;
  const reach = config.inlandReach, roughness = config.coastlineRoughness;

  const genSide = (baseVal, perpAxis, isReversed, coastLength) => {
    const amplitude = reach * (perpAxis === 'x' ? w : h) * config.bayAmplitude;
    let baySigns = null;
    if (config.bayAmplitude > 0 && config.baySpacing > 1)
      baySigns = precomputeBaySigns(rng, coastLength, config.baySpacing);
    return { amplitude, baySigns };
  };

  if (side === 0) { // Right coast
    const baseX = w * (1 - reach);
    const { amplitude, baySigns } = genSide(baseX, 'x', false, h);
    coast.pathPoints.push(vec2(baseX + rng.next() * w * 0.1, 0));
    coast.pathPoints.push(vec2(w, 0));
    coast.pathPoints.push(vec2(w, h));
    let y = h;
    while (y > 0) {
      let bayDisp = baySigns ? computeBayOffset(h - y, h, amplitude, config.baySpacing, baySigns) : 0;
      coast.pathPoints.push(vec2(baseX + w * (rng.next() - 0.5) * roughness * 0.12 + bayDisp, y));
      y -= config.stepSizeMin + rng.next() * (config.stepSizeMax - config.stepSizeMin);
    }
  } else if (side === 1) { // Bottom coast
    const baseY = h * (1 - reach);
    const { amplitude, baySigns } = genSide(baseY, 'y', false, w);
    coast.pathPoints.push(vec2(0, baseY + rng.next() * h * 0.1));
    coast.pathPoints.push(vec2(0, h));
    coast.pathPoints.push(vec2(w, h));
    let x = w;
    while (x > 0) {
      let bayDisp = baySigns ? computeBayOffset(w - x, w, amplitude, config.baySpacing, baySigns) : 0;
      coast.pathPoints.push(vec2(x, baseY + h * (rng.next() - 0.5) * roughness * 0.12 + bayDisp));
      x -= config.stepSizeMin + rng.next() * (config.stepSizeMax - config.stepSizeMin);
    }
  } else if (side === 2) { // Left coast
    const baseX = w * reach;
    const { amplitude, baySigns } = genSide(baseX, 'x', true, h);
    coast.pathPoints.push(vec2(baseX - rng.next() * w * 0.1, 0));
    coast.pathPoints.push(vec2(0, 0));
    coast.pathPoints.push(vec2(0, h));
    let y = h;
    while (y > 0) {
      let bayDisp = baySigns ? computeBayOffset(h - y, h, amplitude, config.baySpacing, baySigns) * -1 : 0;
      coast.pathPoints.push(vec2(baseX + w * (rng.next() - 0.5) * roughness * 0.12 + bayDisp, y));
      y -= config.stepSizeMin + rng.next() * (config.stepSizeMax - config.stepSizeMin);
    }
  } else { // Top coast (side === 3)
    const baseY = h * reach;
    const { amplitude, baySigns } = genSide(baseY, 'y', true, w);
    coast.pathPoints.push(vec2(0, baseY - rng.next() * h * 0.1));
    coast.pathPoints.push(vec2(0, 0));
    coast.pathPoints.push(vec2(w, 0));
    let x = w;
    while (x > 0) {
      let bayDisp = baySigns ? computeBayOffset(w - x, w, amplitude, config.baySpacing, baySigns) * -1 : 0;
      coast.pathPoints.push(vec2(x, baseY + h * (rng.next() - 0.5) * roughness * 0.12 + bayDisp));
      x -= config.stepSizeMin + rng.next() * (config.stepSizeMax - config.stepSizeMin);
    }
  }

  // Depths
  for (let i = 0; i < coast.pathPoints.length; i++) {
    const depthNoise = Math.sin(i * 0.3 + rng.next() * 6.28) * 0.5 + 0.5;
    const shoreMin = config.depthBase * 0.18;
    const shoreMax = config.depthBase * lerp(0.30, 0.55, config.depthVariation);
    coast.depths.push(lerp(shoreMin, shoreMax, depthNoise));
    coast.widths.push(0);
  }

  computeWaterBounds(coast);
  return coast;
}

function generateRiver(rng, center, preset, config, nodes, elevMap, existingWaterBodies) {
  const river = { bodyType: 'River', pathPoints: [], widths: [], depths: [], flowDirection: 0, boundsMin: null, boundsMax: null };
  const w = preset.worldWidth, h = preset.worldHeight, pad = preset.borderPadding;
  const sway = config.swayAmount;

  // Source
  let source = findRiverSource(rng, elevMap, preset, existingWaterBodies);
  let momentum = computeGradientFlow(elevMap, source);
  if (momentum.x * momentum.x + momentum.y * momentum.y < 0.0001) {
    const dx = w * 0.5 - source.x, dy = h * 0.5 - source.y;
    const dd = Math.sqrt(dx * dx + dy * dy) || 1;
    momentum = { x: dx / dd, y: dy / dd };
  }
  river.flowDirection = Math.atan2(momentum.y, momentum.x);

  let pos = { ...source };
  let meanderPhase = rng.next() * Math.PI * 2;
  let stepCount = 0;
  const maxSteps = 500;
  const maxRiverLen = Math.sqrt(w * w + h * h) * 0.8;

  while (stepCount < maxSteps) {
    const atEdge = pos.x < -5 || pos.x > w + 5 || pos.y < -5 || pos.y > h + 5;
    const enteredCoast = !atEdge && stepCount > 3 && isInsideCoast(pos, existingWaterBodies);

    river.pathPoints.push(vec2(clamp(pos.x, pad, w - pad), clamp(pos.y, pad, h - pad)));
    const distFromSource = dist(pos, source);
    const t = clamp(distFromSource / maxRiverLen, 0, 1);
    river.widths.push(config.baseWidth * lerp(1, config.widthGrowth, t));
    const depthNoise = Math.sin(stepCount * 0.2 + rng.next() * 3.14) * 0.5 + 0.5;
    river.depths.push(config.depthBase * (1 + t * 0.5) * (1 + depthNoise * config.depthVariation));

    if (atEdge || enteredCoast) break;

    let gradient = computeGradientFlow(elevMap, pos);
    let desiredDir;
    if (gradient.x * gradient.x + gradient.y * gradient.y > 0.0001) {
      const gd = Math.sqrt(gradient.x * gradient.x + gradient.y * gradient.y);
      const gn = { x: gradient.x / gd, y: gradient.y / gd };
      const gw = lerp(0.3, 0.8, config.flowResponsiveness);
      desiredDir = normalize({ x: lerp(momentum.x, gn.x, gw), y: lerp(momentum.y, gn.y, gw) });
    } else {
      desiredDir = { ...momentum };
    }

    // Loop detection
    if (river.pathPoints.length > 10) {
      const checkEnd = river.pathPoints.length - 8;
      for (let i = 0; i < checkEnd; i += 3) {
        const dx = pos.x - river.pathPoints[i].x, dy = pos.y - river.pathPoints[i].y;
        if (dx * dx + dy * dy < 400) { desiredDir = { ...momentum }; break; }
      }
    }

    // Clamp turn angle
    const angleDiff = signedAngle(momentum, desiredDir);
    if (Math.abs(angleDiff) > 45) {
      const clampedRad = Math.sign(angleDiff) * 45 * Math.PI / 180;
      desiredDir = rotateVec(momentum, clampedRad);
    }

    const stepLen = config.stepSizeMin + rng.next() * (config.stepSizeMax - config.stepSizeMin);
    meanderPhase += stepLen * config.meanderFrequency * 0.02;
    const pp = { x: -desiredDir.y, y: desiredDir.x };
    const meanderBias = Math.sin(meanderPhase) * sway * 0.6;
    const jitter = (rng.next() - 0.5) * sway * 0.4;

    const advance = { x: desiredDir.x * stepLen + pp.x * (meanderBias + jitter),
                      y: desiredDir.y * stepLen + pp.y * (meanderBias + jitter) };
    pos.x += advance.x; pos.y += advance.y;
    momentum = normalize(advance);
    stepCount++;
  }

  // Sandbanks
  if (config.sandbankStrength > 0) applySandbanks(river, config.sandbankStrength);
  computeWaterBounds(river);
  return river;
}

function findRiverSource(rng, elevMap, preset, waterBodies) {
  const w = preset.worldWidth, h = preset.worldHeight, pad = preset.borderPadding;
  const gridSize = 8;
  const candidates = [];
  for (let gy = 0; gy < gridSize; gy++) {
    for (let gx = 0; gx < gridSize; gx++) {
      const x = pad + (w - 2 * pad) * (gx + 0.5) / gridSize;
      const y = pad + (h - 2 * pad) * (gy + 0.5) / gridSize;
      const p = vec2(x, y);
      if (isInsideCoast(p, waterBodies)) continue;
      const elev = elevMap ? elevMap.sample(p) : 0;
      candidates.push({ pos: p, elev });
    }
  }
  candidates.sort((a, b) => b.elev - a.elev);
  const topN = Math.min(3, candidates.length);
  if (topN === 0 || candidates[0].elev < 0.1) {
    const along = rng.next();
    const side = Math.floor(rng.next() * 4);
    const edges = [
      vec2(w - pad, pad + along * (h - 2 * pad)),
      vec2(pad + along * (w - 2 * pad), h - pad),
      vec2(pad, pad + along * (h - 2 * pad)),
      vec2(pad + along * (w - 2 * pad), pad),
    ];
    return edges[side];
  }
  return candidates[Math.floor(rng.next() * topN)].pos;
}

function computeGradientFlow(elevMap, pos) {
  if (!elevMap) return { x: 0, y: 0 };
  const delta = 10;
  const elevE = elevMap.sample(vec2(pos.x + delta, pos.y));
  const elevW = elevMap.sample(vec2(pos.x - delta, pos.y));
  const elevN = elevMap.sample(vec2(pos.x, pos.y + delta));
  const elevS = elevMap.sample(vec2(pos.x, pos.y - delta));
  return { x: -(elevE - elevW) / (2 * delta), y: -(elevN - elevS) / (2 * delta) };
}

function normalize(v) {
  const d = Math.sqrt(v.x * v.x + v.y * v.y) || 1;
  return { x: v.x / d, y: v.y / d };
}

function signedAngle(from, to) {
  const cross = from.x * to.y - from.y * to.x;
  const dot = from.x * to.x + from.y * to.y;
  return Math.atan2(cross, dot) * 180 / Math.PI;
}

function rotateVec(v, rad) {
  const cos = Math.cos(rad), sin = Math.sin(rad);
  return { x: v.x * cos - v.y * sin, y: v.x * sin + v.y * cos };
}

function precomputeBaySigns(rng, coastLength, baySpacing) {
  const count = Math.max(1, Math.round(coastLength / baySpacing));
  const signs = [];
  for (let i = 0; i < count; i++) signs.push(rng.next() > 0.5 ? 1 : -1);
  return signs;
}

function computeBayOffset(along, coastLength, amplitude, baySpacing, baySigns) {
  const count = baySigns.length, segLen = coastLength / count;
  let offset = 0;
  for (let i = 0; i < count; i++) {
    const segCenter = (i + 0.5) * segLen;
    const localT = (along - segCenter) / (segLen * 0.5);
    if (Math.abs(localT) <= 1)
      offset += baySigns[i] * amplitude * (0.5 + 0.5 * Math.cos(localT * Math.PI));
  }
  return offset;
}

function applySandbanks(river, strength) {
  const pts = river.pathPoints;
  if (pts.length < 3) return;
  for (let i = 1; i < pts.length - 1; i++) {
    const d1 = normalize({ x: pts[i].x - pts[i - 1].x, y: pts[i].y - pts[i - 1].y });
    const d2 = normalize({ x: pts[i + 1].x - pts[i].x, y: pts[i + 1].y - pts[i].y });
    const cross = Math.abs(d1.x * d2.y - d1.y * d2.x);
    if (cross > 0.1) {
      const intensity = clamp((cross - 0.1) / 0.5, 0, 1);
      river.depths[i] *= Math.max(0.2, 1 - strength * intensity);
    }
  }
}

function applyMeanderTuning(config, genType) {
  const c = { ...config };
  switch (genType) {
    case 'Rural': c.meanderFrequency *= 0.6; c.swayAmount *= 0.65; break;
    case 'Mountain': c.meanderFrequency *= 1.6; c.swayAmount *= 0.4; break;
    case 'Grid': c.meanderFrequency *= 0.2; c.swayAmount *= 0.3; break;
  }
  return c;
}

function computeWaterBounds(wb) {
  if (wb.pathPoints.length === 0) return;
  let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
  for (let i = 0; i < wb.pathPoints.length; i++) {
    const p = wb.pathPoints[i];
    const expand = i < wb.widths.length ? wb.widths[i] * 0.5 : 0;
    if (p.x - expand < minX) minX = p.x - expand;
    if (p.y - expand < minY) minY = p.y - expand;
    if (p.x + expand > maxX) maxX = p.x + expand;
    if (p.y + expand > maxY) maxY = p.y + expand;
  }
  wb.boundsMin = vec2(minX, minY);
  wb.boundsMax = vec2(maxX, maxY);
}

function isInsideCoast(point, waterBodies) {
  if (!waterBodies) return false;
  for (const wb of waterBodies) {
    if (wb.bodyType !== 'Coast') continue;
    if (wb.boundsMin && (point.x < wb.boundsMin.x || point.x > wb.boundsMax.x ||
        point.y < wb.boundsMin.y || point.y > wb.boundsMax.y)) continue;
    if (pointInPolygon(point, wb.pathPoints)) return true;
  }
  return false;
}

function pointInPolygon(point, polygon) {
  if (!polygon || polygon.length < 3) return false;
  let inside = false;
  const n = polygon.length;
  for (let i = 0, j = n - 1; i < n; j = i++) {
    if ((polygon[i].y > point.y) !== (polygon[j].y > point.y) &&
        point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y)
          / (polygon[j].y - polygon[i].y) + polygon[i].x)
      inside = !inside;
  }
  return inside;
}

// ============================================================
// Phase B: WaterTerrainInteraction
// ============================================================

function applyWaterCarving(elevMap, waterBodies, preset) {
  if (!elevMap || !waterBodies) return;
  const profile = DEFAULT_WATER_PROFILE;
  for (const body of waterBodies) {
    if (body.bodyType === 'River' || body.bodyType === 'Stream') {
      const rc = profile.river;
      if (rc.terrainCarveStrength <= 0) continue;
      for (let i = 0; i < body.pathPoints.length; i++) {
        const t = body.pathPoints.length > 1 ? i / (body.pathPoints.length - 1) : 0;
        const width = i < body.widths.length ? body.widths[i] : rc.baseWidth;
        const carveRadius = Math.max(rc.terrainCarveRadius, width * 1.5);
        const depth = i < body.depths.length ? body.depths[i] : rc.depthBase;
        elevMap.addCarving({ position: body.pathPoints[i], radius: carveRadius,
          depth: depth * rc.terrainCarveStrength * (1 + t * 0.6), falloffPower: 2.0 });
      }
    } else if (body.bodyType === 'Coast') {
      const cc = profile.coast;
      if (cc.terrainCarveStrength <= 0) continue;
      const shoreDir = [{ x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 }, { x: 0, y: -1 }][body.coastSide] || { x: 0, y: 0 };
      let accum = 0;
      let prevPt = body.pathPoints[0] || vec2(0, 0);
      for (let i = 0; i < body.pathPoints.length; i++) {
        const pt = body.pathPoints[i];
        accum += dist(pt, prevPt);
        prevPt = pt;
        if (i > 0 && accum < 30) continue;
        accum = 0;
        const depth = i < body.depths.length ? body.depths[i] : cc.depthBase;
        elevMap.addCarving({ position: pt, radius: cc.terrainCarveRadius,
          depth: depth * cc.terrainCarveStrength * 0.5, falloffPower: 1.5 });
        elevMap.addCarving({
          position: vec2(pt.x - shoreDir.x * cc.terrainCarveRadius * 0.4,
                         pt.y - shoreDir.y * cc.terrainCarveRadius * 0.4),
          radius: cc.terrainCarveRadius * 0.6, depth: depth * cc.terrainCarveStrength * 0.2, falloffPower: 1.2 });
      }
    }
  }
}

// ============================================================
// Main Generation Pipeline
// ============================================================

function generateMap(seed, generatorType = 'Organic') {
  const preset = { ...PRESETS[generatorType] };
  const rng = new SeededRng(seed);

  // Center
  const cx = preset.worldWidth * (0.3 + rng.next() * 0.22);
  const cy = preset.worldHeight * (0.32 + rng.next() * 0.3);
  const center = vec2(cx, cy);

  // Coast side (1 rng call)
  const coastSide = determineCoastSide(rng, preset);

  // Generate nodes & edges
  const gen = GENERATORS[generatorType];
  const { nodes, edges } = gen(rng, center, preset);

  // Terrain (hills)
  const terrain = generateTerrain(rng, center, preset, coastSide, nodes);

  // ElevationMap
  const elevMap = new ElevationMap(terrain, preset);

  // Water
  terrain.waterBodies = generateWater(rng, center, preset, coastSide, nodes, elevMap);

  // Water carving
  applyWaterCarving(elevMap, terrain.waterBodies, preset);

  // Apply elevation to nodes
  elevMap.applyToNodes(nodes);

  // Place buildings (now with coast check)
  const buildings = placeBuildings(nodes, edges, rng, preset, terrain);

  // Analyze
  const analysis = analyze(nodes, edges);

  return { nodes, edges, buildings, analysis, terrain, elevMap, preset, seed, generatorType };
}

// Export for renderer
window.MapGen = {
  generateMap,
  bezierPoint,
  PRESETS,
};
