// MiniMapGame Browser Preview — Canvas 2D Renderer
// Phase A: Road network + Buildings
// Phase B: Terrain elevation heatmap + Water (rivers/coasts)
// Phase C: Decorations (street furniture, vegetation, terrain scatter)

const ROAD_COLORS = ['#2c2c2c', '#4a4a4a', '#6e6e6e']; // tier 0, 1, 2
const ROAD_WIDTHS = [6, 3.5, 1.8]; // tier 0, 1, 2

const BUILDING_COLORS = {
  0: '#7a6a5a', // tier 0: darker, larger buildings
  1: '#8b7d6b', // tier 1: medium
  2: '#9e9080', // tier 2: lighter, smaller
};
const LANDMARK_COLOR = '#c44';
const NODE_COLOR = '#333';
const LABEL_COLOR = '#1a1a1a';
const BG_COLOR = '#d4c9a8';
const GRID_COLOR = '#c8bea0';

// Terrain elevation color ramp (low → high)
const ELEV_COLORS = [
  [212, 201, 168], // 0: base ground
  [195, 185, 150], // 1: slight rise
  [178, 168, 130], // 2: low hill
  [160, 148, 110], // 3: medium hill
  [140, 126, 90],  // 4: high hill
  [120, 105, 72],  // 5: ridge
  [100, 88, 58],   // 6: peak
];
const WATER_COLOR = 'rgba(65, 120, 170, 0.65)';
const WATER_DEEP_COLOR = 'rgba(35, 80, 135, 0.75)';
const RIVER_COLOR = 'rgba(55, 110, 165, 0.7)';

function renderMap(canvas, mapData, options = {}) {
  const ctx = canvas.getContext('2d');
  const { nodes, edges, buildings, analysis, preset, terrain, elevMap } = mapData;
  const scale = options.scale || Math.min(
    canvas.width / preset.worldWidth,
    canvas.height / preset.worldHeight
  );
  const ox = (canvas.width - preset.worldWidth * scale) / 2;
  const oy = (canvas.height - preset.worldHeight * scale) / 2;

  const tx = (x) => ox + x * scale;
  const ty = (y) => oy + y * scale;
  const ts = (s) => s * scale;

  // Clear
  ctx.fillStyle = BG_COLOR;
  ctx.fillRect(0, 0, canvas.width, canvas.height);

  // === Phase B: Terrain elevation heatmap ===
  if (elevMap && options.showTerrain !== false) {
    renderTerrainHeatmap(ctx, elevMap, preset, tx, ty, ts, scale);
  }

  // Grid lines (subtle)
  if (options.showGrid) {
    ctx.strokeStyle = GRID_COLOR;
    ctx.lineWidth = 0.5;
    const step = 50;
    for (let x = 0; x <= preset.worldWidth; x += step) {
      ctx.beginPath();
      ctx.moveTo(tx(x), ty(0));
      ctx.lineTo(tx(x), ty(preset.worldHeight));
      ctx.stroke();
    }
    for (let y = 0; y <= preset.worldHeight; y += step) {
      ctx.beginPath();
      ctx.moveTo(tx(0), ty(y));
      ctx.lineTo(tx(preset.worldWidth), ty(y));
      ctx.stroke();
    }
  }

  // === Phase B: Water bodies (coast polygon + river polyline) ===
  if (terrain && terrain.waterBodies) {
    renderWater(ctx, terrain.waterBodies, tx, ty, ts);
  }

  // Border
  ctx.strokeStyle = '#aaa';
  ctx.lineWidth = 1;
  ctx.strokeRect(tx(preset.borderPadding), ty(preset.borderPadding * 0.8),
    ts(preset.worldWidth - preset.borderPadding * 2),
    ts(preset.worldHeight - preset.borderPadding * 1.6));

  // Roads — draw lower tiers first, then higher (so arterials are on top)
  const edgesByTier = [[], [], []];
  for (const e of edges) {
    const ti = Math.min(e.tier, 2);
    edgesByTier[ti].push(e);
  }

  for (let tier = 2; tier >= 0; tier--) {
    ctx.strokeStyle = ROAD_COLORS[tier];
    ctx.lineWidth = ts(ROAD_WIDTHS[tier]);
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';

    for (const e of edgesByTier[tier]) {
      const na = nodes[e.nodeA];
      const nb = nodes[e.nodeB];
      ctx.beginPath();
      ctx.moveTo(tx(na.position.x), ty(na.position.y));
      ctx.quadraticCurveTo(
        tx(e.controlPoint.x), ty(e.controlPoint.y),
        tx(nb.position.x), ty(nb.position.y)
      );
      ctx.stroke();
    }

    // Road outline (lighter, for road edge effect)
    if (tier < 2) {
      ctx.strokeStyle = '#fff4';
      ctx.lineWidth = ts(ROAD_WIDTHS[tier]) + 1;
      ctx.globalCompositeOperation = 'destination-over';
      for (const e of edgesByTier[tier]) {
        const na = nodes[e.nodeA];
        const nb = nodes[e.nodeB];
        ctx.beginPath();
        ctx.moveTo(tx(na.position.x), ty(na.position.y));
        ctx.quadraticCurveTo(
          tx(e.controlPoint.x), ty(e.controlPoint.y),
          tx(nb.position.x), ty(nb.position.y)
        );
        ctx.stroke();
      }
      ctx.globalCompositeOperation = 'source-over';
    }
  }

  // Buildings
  for (const b of buildings) {
    ctx.save();
    ctx.translate(tx(b.position.x), ty(b.position.y));
    ctx.rotate(b.angle);

    const w = ts(b.width);
    const h = ts(b.height);

    if (b.isLandmark) {
      ctx.fillStyle = LANDMARK_COLOR;
      ctx.strokeStyle = '#922';
    } else {
      const floorDarken = Math.min(b.floors * 0.06, 0.4);
      ctx.fillStyle = darkenColor(BUILDING_COLORS[Math.min(b.tier, 2)], floorDarken);
      ctx.strokeStyle = darkenColor(BUILDING_COLORS[Math.min(b.tier, 2)], floorDarken + 0.15);
    }

    ctx.lineWidth = 0.5;

    switch (b.shapeType) {
      case 1: // L-shape
        ctx.beginPath();
        ctx.moveTo(-w / 2, -h / 2);
        ctx.lineTo(w / 2, -h / 2);
        ctx.lineTo(w / 2, 0);
        ctx.lineTo(0, 0);
        ctx.lineTo(0, h / 2);
        ctx.lineTo(-w / 2, h / 2);
        ctx.closePath();
        break;
      case 2: // Cylinder
        ctx.beginPath();
        ctx.ellipse(0, 0, w / 2, h / 2, 0, 0, Math.PI * 2);
        break;
      case 3: // Stepped (landmark)
        ctx.beginPath();
        ctx.rect(-w / 2, -h / 2, w, h);
        ctx.fill();
        ctx.stroke();
        ctx.fillStyle = darkenColor(LANDMARK_COLOR, 0.15);
        ctx.beginPath();
        ctx.rect(-w * 0.35, -h * 0.35, w * 0.7, h * 0.7);
        break;
      default: // Box
        ctx.beginPath();
        ctx.rect(-w / 2, -h / 2, w, h);
        break;
    }
    ctx.fill();
    ctx.stroke();
    ctx.restore();
  }

  // === Phase C: Decorations ===
  if (mapData.decorations && options.showDecorations !== false) {
    renderDecorations(ctx, mapData.decorations, tx, ty, ts);
  }

  // Nodes
  if (options.showNodes) {
    for (let i = 0; i < nodes.length; i++) {
      const n = nodes[i];
      const r = n.degree >= 4 ? 3 : n.degree >= 3 ? 2.5 : 1.5;

      ctx.fillStyle = analysis.plazaIndices.includes(i) ? '#c80' :
        analysis.intersectionIndices.includes(i) ? '#666' :
        analysis.deadEndIndices.includes(i) ? '#a44' : NODE_COLOR;
      ctx.beginPath();
      ctx.arc(tx(n.position.x), ty(n.position.y), ts(r), 0, Math.PI * 2);
      ctx.fill();
    }
  }

  // Labels
  if (options.showLabels) {
    ctx.fillStyle = LABEL_COLOR;
    ctx.font = `bold ${Math.max(9, Math.round(10 * scale))}px sans-serif`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'bottom';

    for (const n of nodes) {
      if (!n.label) continue;
      ctx.fillText(n.label, tx(n.position.x), ty(n.position.y) - ts(4));
    }
  }

  // Stats overlay
  if (options.showStats) {
    const waterCount = terrain && terrain.waterBodies ? terrain.waterBodies.length : 0;
    const hillCount = terrain ? terrain.hills.length : 0;
    ctx.fillStyle = 'rgba(0,0,0,0.7)';
    ctx.fillRect(4, 4, 220, 145);
    ctx.fillStyle = '#fff';
    ctx.font = '12px monospace';
    ctx.textAlign = 'left';
    ctx.textBaseline = 'top';
    const lines = [
      `Seed: ${mapData.seed}`,
      `Type: ${mapData.generatorType}`,
      `Nodes: ${nodes.length}`,
      `Edges: ${edges.length}`,
      `Buildings: ${buildings.length}`,
      `Hills: ${hillCount}`,
      `Water bodies: ${waterCount}`,
      `Dead-ends: ${analysis.deadEndIndices.length}`,
      `Decorations: ${mapData.decorations ? mapData.decorations.length : 0}`,
    ];
    lines.forEach((l, i) => ctx.fillText(l, 10, 10 + i * 15));
  }
}

// === Phase B: Terrain heatmap rendering ===

function renderTerrainHeatmap(ctx, elevMap, preset, tx, ty, ts, scale) {
  // Sample elevation on a grid and draw colored cells
  const cellSize = Math.max(4, Math.floor(8 / scale)); // map-space cell size
  const w = preset.worldWidth, h = preset.worldHeight;
  const maxElev = preset.maxElevation;

  for (let my = 0; my < h; my += cellSize) {
    for (let mx = 0; mx < w; mx += cellSize) {
      const elev = elevMap.sample({ x: mx + cellSize * 0.5, y: my + cellSize * 0.5 });
      if (elev < 0.3) continue; // Skip flat areas (base color already drawn)

      const t = Math.min(elev / maxElev, 1);
      const idx = t * (ELEV_COLORS.length - 1);
      const lo = Math.floor(idx), hi = Math.min(lo + 1, ELEV_COLORS.length - 1);
      const frac = idx - lo;
      const r = ELEV_COLORS[lo][0] + (ELEV_COLORS[hi][0] - ELEV_COLORS[lo][0]) * frac;
      const g = ELEV_COLORS[lo][1] + (ELEV_COLORS[hi][1] - ELEV_COLORS[lo][1]) * frac;
      const b = ELEV_COLORS[lo][2] + (ELEV_COLORS[hi][2] - ELEV_COLORS[lo][2]) * frac;

      ctx.fillStyle = `rgb(${Math.round(r)},${Math.round(g)},${Math.round(b)})`;
      ctx.fillRect(tx(mx), ty(my), ts(cellSize) + 1, ts(cellSize) + 1);
    }
  }
}

// === Phase B: Water rendering ===

function renderWater(ctx, waterBodies, tx, ty, ts) {
  for (const wb of waterBodies) {
    if (wb.bodyType === 'Coast') {
      // Draw coast as filled polygon
      ctx.fillStyle = WATER_COLOR;
      ctx.beginPath();
      for (let i = 0; i < wb.pathPoints.length; i++) {
        const p = wb.pathPoints[i];
        if (i === 0) ctx.moveTo(tx(p.x), ty(p.y));
        else ctx.lineTo(tx(p.x), ty(p.y));
      }
      ctx.closePath();
      ctx.fill();

      // Shoreline edge
      ctx.strokeStyle = 'rgba(40, 90, 140, 0.5)';
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      // Draw only the irregular coastline edge (skip the straight map-edge parts)
      const startIdx = 3; // First 3 points are map corners
      if (wb.pathPoints.length > startIdx) {
        ctx.moveTo(tx(wb.pathPoints[startIdx].x), ty(wb.pathPoints[startIdx].y));
        for (let i = startIdx + 1; i < wb.pathPoints.length; i++) {
          ctx.lineTo(tx(wb.pathPoints[i].x), ty(wb.pathPoints[i].y));
        }
      }
      ctx.stroke();
    } else if (wb.bodyType === 'River') {
      // Draw river as variable-width polyline
      ctx.lineCap = 'round';
      ctx.lineJoin = 'round';

      for (let i = 0; i < wb.pathPoints.length - 1; i++) {
        const p0 = wb.pathPoints[i];
        const p1 = wb.pathPoints[i + 1];
        const w0 = i < wb.widths.length ? wb.widths[i] : 12;
        const w1 = i + 1 < wb.widths.length ? wb.widths[i + 1] : 12;
        const avgW = (w0 + w1) / 2;

        // Depth-based color
        const d = i < wb.depths.length ? wb.depths[i] : 2;
        const depthT = Math.min(d / 5, 1);
        const alpha = 0.45 + depthT * 0.35;

        ctx.strokeStyle = `rgba(55, 110, 165, ${alpha})`;
        ctx.lineWidth = ts(avgW);
        ctx.beginPath();
        ctx.moveTo(tx(p0.x), ty(p0.y));
        ctx.lineTo(tx(p1.x), ty(p1.y));
        ctx.stroke();
      }

      // River center highlight (lighter, narrower)
      ctx.strokeStyle = 'rgba(100, 160, 210, 0.3)';
      ctx.lineWidth = ts(4);
      ctx.beginPath();
      if (wb.pathPoints.length > 0) {
        ctx.moveTo(tx(wb.pathPoints[0].x), ty(wb.pathPoints[0].y));
        for (let i = 1; i < wb.pathPoints.length; i++) {
          ctx.lineTo(tx(wb.pathPoints[i].x), ty(wb.pathPoints[i].y));
        }
      }
      ctx.stroke();
    }
  }
}

// === Phase C: Decoration rendering ===

const DECORATION_STYLES = {
  StreetLight: { color: '#888', shape: 'circle', baseSize: 1.5 },
  Tree:        { color: '#4a7a3a', shape: 'circle', baseSize: 3.0 },
  Stump:       { color: '#7a6a4a', shape: 'circle', baseSize: 1.2 },
  Fence:       { color: '#8a7a5a', shape: 'line', baseSize: 4.0 },
  Bollard:     { color: '#666', shape: 'square', baseSize: 1.0 },
  SignPost:    { color: '#9a7030', shape: 'diamond', baseSize: 1.8 },
  Bench:       { color: '#7a5a3a', shape: 'rect', baseSize: 2.0 },
  Boulder:     { color: '#6a6a6a', shape: 'circle', baseSize: 3.5 },
  Rock:        { color: '#7a7a7a', shape: 'circle', baseSize: 1.8 },
  Shrub:       { color: '#5a8a3a', shape: 'circle', baseSize: 2.5 },
  Wildflower:  { color: '#9a6aaa', shape: 'dot', baseSize: 1.5 },
  GrassClump:  { color: '#6a9a5a', shape: 'dot', baseSize: 1.2 },
};

function renderDecorations(ctx, decorations, tx, ty, ts) {
  for (const dec of decorations) {
    const style = DECORATION_STYLES[dec.type];
    if (!style) continue;

    const x = tx(dec.position.x);
    const y = ty(dec.position.y);
    const s = ts(style.baseSize * dec.scale * 0.5);

    ctx.fillStyle = style.color;
    ctx.strokeStyle = darkenColor(style.color.padEnd(7, '0'), 0.2);
    ctx.lineWidth = 0.5;

    switch (style.shape) {
      case 'circle':
        ctx.beginPath();
        ctx.arc(x, y, Math.max(s, 1), 0, Math.PI * 2);
        ctx.fill();
        if (s > 2) ctx.stroke();
        break;

      case 'square':
        ctx.fillRect(x - s, y - s, s * 2, s * 2);
        break;

      case 'rect':
        ctx.save();
        ctx.translate(x, y);
        ctx.rotate(dec.angle);
        ctx.fillRect(-s * 1.5, -s * 0.5, s * 3, s);
        ctx.restore();
        break;

      case 'diamond':
        ctx.beginPath();
        ctx.moveTo(x, y - s);
        ctx.lineTo(x + s * 0.7, y);
        ctx.lineTo(x, y + s);
        ctx.lineTo(x - s * 0.7, y);
        ctx.closePath();
        ctx.fill();
        break;

      case 'line':
        ctx.save();
        ctx.translate(x, y);
        ctx.rotate(dec.angle);
        ctx.strokeStyle = style.color;
        ctx.lineWidth = Math.max(ts(0.8), 0.5);
        ctx.beginPath();
        ctx.moveTo(-s * 1.5, 0);
        ctx.lineTo(s * 1.5, 0);
        ctx.stroke();
        ctx.restore();
        break;

      case 'dot':
        ctx.globalAlpha = 0.7;
        ctx.beginPath();
        ctx.arc(x, y, Math.max(s * 0.6, 0.8), 0, Math.PI * 2);
        ctx.fill();
        ctx.globalAlpha = 1.0;
        break;
    }
  }
}

// Utility: darken a hex color
function darkenColor(hex, amount) {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  const f = 1 - amount;
  return `rgb(${Math.round(r * f)},${Math.round(g * f)},${Math.round(b * f)})`;
}

window.Renderer = { renderMap };
