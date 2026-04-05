// MiniMapGame Browser Preview - Canvas 2D Renderer
// SP-040 corrective pass:
// - browser-preview is a fast visual probe, not a literal shader port
// - priority is map readability at screenshot / reduced scale

const THEMES = {
  Parchment: {
    name: 'Parchment',
    pageBg: '#121210',
    frameShadow: 'rgba(0, 0, 0, 0.28)',
    frameLine: '#7b715a',
    paper: [229, 220, 194],
    paperTint: [210, 199, 170],
    baseColor: [176, 179, 146],
    midColor: [196, 196, 162],
    highColor: [220, 212, 180],
    ridgeTint: [240, 231, 202],
    valleyTint: [118, 118, 93],
    slopeColor: [150, 145, 118],
    moistureTint: [132, 155, 143],
    roadDust: [197, 182, 151],
    buildingDust: [171, 159, 135],
    contourMinor: [123, 116, 92],
    contourMajor: [87, 80, 61],
    gridColor: [118, 112, 91],
    waterShallow: [128, 161, 185],
    waterDeep: [77, 116, 150],
    waterLine: 'rgba(230, 238, 246, 0.65)',
    waterBank: 'rgba(52, 85, 118, 0.32)',
    roadCasing: ['#51493d', '#655c4c', '#7e755e'],
    roadFill: ['#bcae90', '#a89b80', '#94886f'],
    roadCenter: ['rgba(255, 247, 220, 0.42)', 'rgba(249, 240, 210, 0.28)', null],
    roadEdge: 'rgba(48, 41, 33, 0.16)',
    buildingBase: '#7f7263',
    buildingRoof: '#a49784',
    buildingLine: '#584f43',
    buildingShadow: 'rgba(46, 37, 27, 0.18)',
    landmarkBase: '#bb5b4a',
    landmarkRoof: '#db8770',
    label: '#201d18',
    statBg: 'rgba(28, 23, 19, 0.78)',
    statText: '#f6efe3',
  },
  Dark: {
    name: 'Dark',
    pageBg: '#0d1313',
    frameShadow: 'rgba(0, 0, 0, 0.34)',
    frameLine: '#3e483b',
    paper: [72, 84, 62],
    paperTint: [58, 67, 50],
    baseColor: [76, 90, 66],
    midColor: [101, 99, 78],
    highColor: [132, 122, 99],
    ridgeTint: [173, 162, 137],
    valleyTint: [44, 55, 42],
    slopeColor: [93, 82, 69],
    moistureTint: [48, 74, 83],
    roadDust: [102, 97, 88],
    buildingDust: [88, 82, 74],
    contourMinor: [44, 53, 40],
    contourMajor: [27, 33, 25],
    gridColor: [58, 68, 51],
    waterShallow: [52, 92, 136],
    waterDeep: [28, 60, 108],
    waterLine: 'rgba(189, 213, 232, 0.45)',
    waterBank: 'rgba(16, 33, 58, 0.4)',
    roadCasing: ['#171919', '#262828', '#393b3b'],
    roadFill: ['#787267', '#676258', '#57534b'],
    roadCenter: ['rgba(230, 224, 208, 0.24)', 'rgba(213, 208, 190, 0.16)', null],
    roadEdge: 'rgba(0, 0, 0, 0.2)',
    buildingBase: '#5e5550',
    buildingRoof: '#82786d',
    buildingLine: '#332e2b',
    buildingShadow: 'rgba(0, 0, 0, 0.26)',
    landmarkBase: '#9f5749',
    landmarkRoof: '#c47d6c',
    label: '#d7d4c8',
    statBg: 'rgba(10, 12, 12, 0.78)',
    statText: '#ecede8',
  },
};

const PRESET_DEFAULTS = {
  Mountain: { hillshade: 0.92, contour: 0.62, moisture: 0.28, road: 0.16, building: 0.12 },
  Rural: { hillshade: 0.75, contour: 0.4, moisture: 0.48, road: 0.18, building: 0.16 },
  Grid: { hillshade: 0.34, contour: 0.18, moisture: 0.18, road: 0.42, building: 0.34 },
  Organic: { hillshade: 0.82, contour: 0.46, moisture: 0.36, road: 0.24, building: 0.22 },
};

const ROAD_WIDTHS = [6, 3.5, 1.8];
const MINOR_CONTOUR_INTERVAL = 2.2;
const MAJOR_CONTOUR_INTERVAL = 5.4;
const SUN_DIR = normalize3({ x: -0.74, y: 0.48, z: 0.64 });

function renderMap(canvas, mapData, options = {}) {
  const ctx = canvas.getContext('2d');
  const { nodes, edges, buildings, analysis, preset, terrain, elevMap } = mapData;
  const theme = THEMES[options.theme] || THEMES.Parchment;
  const strengths = PRESET_DEFAULTS[preset.generatorType || mapData.generatorType] || PRESET_DEFAULTS.Organic;

  const scale = options.scale || Math.min(
    canvas.width / preset.worldWidth,
    canvas.height / preset.worldHeight
  );
  const ox = (canvas.width - preset.worldWidth * scale) / 2;
  const oy = (canvas.height - preset.worldHeight * scale) / 2;

  const tx = (x) => ox + x * scale;
  const ty = (y) => oy + y * scale;
  const ts = (s) => s * scale;
  const mapRect = { x: tx(0), y: ty(0), w: ts(preset.worldWidth), h: ts(preset.worldHeight) };

  const roadGeometry = buildRoadGeometry(nodes, edges);
  const waterFeatures = buildWaterFeatures(terrain && terrain.waterBodies ? terrain.waterBodies : []);

  ctx.fillStyle = theme.pageBg;
  ctx.fillRect(0, 0, canvas.width, canvas.height);

  renderBackdrop(ctx, canvas, mapRect, theme);
  renderPaperBase(ctx, mapRect, mapData.seed, theme);

  ctx.save();
  ctx.beginPath();
  ctx.rect(mapRect.x, mapRect.y, mapRect.w, mapRect.h);
  ctx.clip();

  if (options.showTerrain !== false) {
    if (typeof ObservedSurface !== 'undefined') {
      // SP-041: Surface classification renders the entire ground
      ObservedSurface.synthesizeObservedSurface(ctx, mapData, theme, options, tx, ty, ts);
    } else if (elevMap) {
      // Fallback: relief-only rendering
      renderGroundComposite(ctx, mapData, theme, strengths, roadGeometry, waterFeatures, scale, tx, ty, ts);
    }
  }

  if (terrain && terrain.waterBodies) {
    renderWater(ctx, waterFeatures, theme, tx, ty, ts);
  }

  renderRoads(ctx, roadGeometry, theme, tx, ty, ts);
  renderBuildings(ctx, buildings, theme, tx, ty, ts);

  if (options.showGrid) {
    renderGridOverlay(ctx, preset, theme, tx, ty);
  }

  ctx.restore();

  renderFrame(ctx, mapRect, theme);

  if (options.showNodes) {
    renderNodes(ctx, nodes, analysis, tx, ty, ts);
  }

  if (options.showLabels) {
    renderLabels(ctx, nodes, theme, tx, ty, ts, scale);
  }

  if (options.showStats) {
    renderStats(ctx, mapData, theme);
  }
}

function renderBackdrop(ctx, canvas, mapRect, theme) {
  const gradient = ctx.createRadialGradient(
    mapRect.x + mapRect.w * 0.52,
    mapRect.y + mapRect.h * 0.44,
    mapRect.w * 0.12,
    mapRect.x + mapRect.w * 0.52,
    mapRect.y + mapRect.h * 0.44,
    Math.max(canvas.width, canvas.height) * 0.7
  );
  gradient.addColorStop(0, 'rgba(255,255,255,0.035)');
  gradient.addColorStop(1, 'rgba(0,0,0,0)');
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, canvas.width, canvas.height);
}

function renderPaperBase(ctx, mapRect, seed, theme) {
  const gradient = ctx.createLinearGradient(mapRect.x, mapRect.y, mapRect.x + mapRect.w, mapRect.y + mapRect.h);
  gradient.addColorStop(0, rgbStr(theme.paper));
  gradient.addColorStop(1, rgbStr(theme.paperTint));
  ctx.fillStyle = gradient;
  ctx.fillRect(mapRect.x, mapRect.y, mapRect.w, mapRect.h);

  const patternCanvas = createTempCanvas(96, 96);
  const pctx = patternCanvas.getContext('2d');
  const img = pctx.createImageData(96, 96);
  const data = img.data;
  for (let y = 0; y < 96; y++) {
    for (let x = 0; x < 96; x++) {
      const idx = (y * 96 + x) * 4;
      const noise = hashNoise(x * 0.91, y * 0.77, seed * 0.13);
      const warm = noise * 26 - 13;
      data[idx] = clampByte(theme.paper[0] + warm);
      data[idx + 1] = clampByte(theme.paper[1] + warm * 0.75);
      data[idx + 2] = clampByte(theme.paper[2] + warm * 0.45);
      data[idx + 3] = 18;
    }
  }
  pctx.putImageData(img, 0, 0);

  const pattern = ctx.createPattern(patternCanvas, 'repeat');
  ctx.fillStyle = pattern;
  ctx.fillRect(mapRect.x, mapRect.y, mapRect.w, mapRect.h);
}

function renderFrame(ctx, mapRect, theme) {
  ctx.save();
  ctx.shadowColor = theme.frameShadow;
  ctx.shadowBlur = 22;
  ctx.shadowOffsetY = 12;
  ctx.fillStyle = 'rgba(0,0,0,0.06)';
  ctx.fillRect(mapRect.x, mapRect.y, mapRect.w, mapRect.h);
  ctx.restore();

  ctx.strokeStyle = theme.frameLine;
  ctx.lineWidth = 1.2;
  ctx.strokeRect(mapRect.x, mapRect.y, mapRect.w, mapRect.h);
}

// ============================================================
// Ground composite — layer-separated (NOT pixel-by-pixel shader)
// L2: elevation gradient, L3: hillshade overlay, L4: contour lines
// ============================================================

const FIELD_W = 128;
const FIELD_H = 87;
let _cachedFields = null;
let _cachedSeed = null;

function getOrBakeFields(elevMap, preset, seed) {
  if (_cachedSeed === seed && _cachedFields) return _cachedFields;
  _cachedFields = prebakeFields(elevMap, preset);
  _cachedSeed = seed;
  return _cachedFields;
}

function prebakeFields(elevMap, preset) {
  const cellW = preset.worldWidth / FIELD_W;
  const cellH = preset.worldHeight / FIELD_H;
  const maxElev = Math.max(preset.maxElevation, 0.01);

  const elevField = new Float32Array(FIELD_W * FIELD_H);
  const hillshadeField = new Float32Array(FIELD_W * FIELD_H);

  for (let iy = 0; iy < FIELD_H; iy++) {
    for (let ix = 0; ix < FIELD_W; ix++) {
      const wx = (ix + 0.5) * cellW;
      const wy = (iy + 0.5) * cellH;
      const idx = iy * FIELD_W + ix;

      const elev = elevMap.sample({ x: wx, y: wy });
      elevField[idx] = clamp(elev / maxElev, 0, 1);
      hillshadeField[idx] = computeHillshade(elevMap, wx, wy, cellW, maxElev);
    }
  }

  return { elevField, hillshadeField, cellW, cellH };
}

function renderGroundComposite(ctx, mapData, theme, strengths, roadGeometry, waterFeatures, scale, tx, ty, ts) {
  const { preset, elevMap, seed } = mapData;
  const fields = getOrBakeFields(elevMap, preset, seed);
  const mapRect = { x: tx(0), y: ty(0), w: ts(preset.worldWidth), h: ts(preset.worldHeight) };

  // L2: Elevation gradient
  renderElevationGradient(ctx, fields, theme, seed, mapRect);

  // L3: Hillshade overlay (alpha blend, NOT multiplicative)
  renderHillshadeOverlay(ctx, fields, theme, strengths, mapRect);

  // L4: Contour lines (marching squares)
  renderContourLines(ctx, fields, preset, theme, strengths, tx, ty);
}

function renderElevationGradient(ctx, fields, theme, seed, mapRect) {
  const offscreen = createTempCanvas(FIELD_W, FIELD_H);
  const octx = offscreen.getContext('2d');
  const img = octx.createImageData(FIELD_W, FIELD_H);
  const px = img.data;

  for (let i = 0; i < fields.elevField.length; i++) {
    const t = clamp(Math.pow(fields.elevField[i], 0.92), 0, 1);
    const c = elevGradient(theme.baseColor, theme.midColor, theme.highColor, t);

    // Subtle macro noise for texture
    const iy = (i / FIELD_W) | 0;
    const ix = i % FIELD_W;
    const macro = fbm(ix * 0.15, iy * 0.15, seed * 0.17 + 13);
    const macroShift = (macro - 0.5) * 14;

    const idx = i * 4;
    px[idx]     = clampByte(c[0] + macroShift);
    px[idx + 1] = clampByte(c[1] + macroShift * 0.82);
    px[idx + 2] = clampByte(c[2] + macroShift * 0.6);
    px[idx + 3] = 255;
  }

  octx.putImageData(img, 0, 0);
  ctx.imageSmoothingEnabled = true;
  ctx.drawImage(offscreen, mapRect.x, mapRect.y, mapRect.w, mapRect.h);
}

function renderHillshadeOverlay(ctx, fields, theme, strengths, mapRect) {
  const offscreen = createTempCanvas(FIELD_W, FIELD_H);
  const octx = offscreen.getContext('2d');
  const img = octx.createImageData(FIELD_W, FIELD_H);
  const px = img.data;
  const str = strengths.hillshade;

  for (let i = 0; i < fields.hillshadeField.length; i++) {
    const hs = fields.hillshadeField[i];
    const idx = i * 4;

    if (hs > 0.5) {
      // Lit side — warm highlight overlay
      const t = (hs - 0.5) * 2;
      px[idx]     = theme.ridgeTint[0];
      px[idx + 1] = theme.ridgeTint[1];
      px[idx + 2] = theme.ridgeTint[2];
      px[idx + 3] = Math.round(t * 50 * str);
    } else {
      // Shadow side — dark overlay
      const t = (0.5 - hs) * 2;
      px[idx]     = theme.valleyTint[0];
      px[idx + 1] = theme.valleyTint[1];
      px[idx + 2] = theme.valleyTint[2];
      px[idx + 3] = Math.round(t * 65 * str);
    }
  }

  octx.putImageData(img, 0, 0);
  ctx.imageSmoothingEnabled = true;
  ctx.drawImage(offscreen, mapRect.x, mapRect.y, mapRect.w, mapRect.h);
}

function renderContourLines(ctx, fields, preset, theme, strengths, tx, ty) {
  const maxElev = Math.max(preset.maxElevation, 0.01);
  if (strengths.contour < 0.05) return;

  // Minor contours
  ctx.strokeStyle = rgbaStr(theme.contourMinor, 0.22 * strengths.contour);
  ctx.lineWidth = 0.8;
  for (let level = MINOR_CONTOUR_INTERVAL; level < maxElev; level += MINOR_CONTOUR_INTERVAL) {
    traceContour(ctx, fields, level / maxElev, preset, tx, ty);
  }

  // Major contours (overdraw, thicker)
  ctx.strokeStyle = rgbaStr(theme.contourMajor, 0.45 * strengths.contour);
  ctx.lineWidth = 1.4;
  for (let level = MAJOR_CONTOUR_INTERVAL; level < maxElev; level += MAJOR_CONTOUR_INTERVAL) {
    traceContour(ctx, fields, level / maxElev, preset, tx, ty);
  }
}

function traceContour(ctx, fields, threshold, preset, tx, ty) {
  const { elevField, cellW, cellH } = fields;
  ctx.beginPath();

  for (let iy = 0; iy < FIELD_H - 1; iy++) {
    for (let ix = 0; ix < FIELD_W - 1; ix++) {
      const tl = elevField[iy * FIELD_W + ix];
      const tr = elevField[iy * FIELD_W + ix + 1];
      const bl = elevField[(iy + 1) * FIELD_W + ix];
      const br = elevField[(iy + 1) * FIELD_W + ix + 1];

      const code = (tl >= threshold ? 8 : 0)
                 | (tr >= threshold ? 4 : 0)
                 | (br >= threshold ? 2 : 0)
                 | (bl >= threshold ? 1 : 0);
      if (code === 0 || code === 15) continue;

      const x0 = ix * cellW, y0 = iy * cellH;
      const x1 = (ix + 1) * cellW, y1 = (iy + 1) * cellH;

      const top    = interpEdge(x0, x1, tl, tr, threshold);
      const bottom = interpEdge(x0, x1, bl, br, threshold);
      const left   = interpEdge(y0, y1, tl, bl, threshold);
      const right  = interpEdge(y0, y1, tr, br, threshold);

      const segments = MARCHING_TABLE[code];
      if (!segments) continue;
      for (let s = 0; s < segments.length; s += 2) {
        const [ax, ay] = contourEdgePoint(segments[s], top, bottom, left, right, x0, y0, x1, y1);
        const [bx, by] = contourEdgePoint(segments[s + 1], top, bottom, left, right, x0, y0, x1, y1);
        ctx.moveTo(tx(ax), ty(ay));
        ctx.lineTo(tx(bx), ty(by));
      }
    }
  }
  ctx.stroke();
}

function interpEdge(a, b, va, vb, threshold) {
  const t = (threshold - va) / (vb - va || 0.001);
  return a + clamp(t, 0, 1) * (b - a);
}

function contourEdgePoint(edge, top, bottom, left, right, x0, y0, x1, y1) {
  switch (edge) {
    case 0: return [top, y0];
    case 1: return [x1, right];
    case 2: return [bottom, y1];
    case 3: return [x0, left];
  }
}

const MARCHING_TABLE = {
  1: [3, 2],        2: [2, 1],        3: [3, 1],
  4: [0, 1],        5: [0, 3, 2, 1],  6: [0, 2],
  7: [0, 3],        8: [0, 3],        9: [0, 2],
  10: [0, 1, 2, 3], 11: [0, 1],       12: [3, 1],
  13: [2, 1],       14: [3, 2],
};

function renderWater(ctx, waterFeatures, theme, tx, ty, ts) {
  for (const coast of waterFeatures.coasts) {
    renderCoast(ctx, coast, theme, tx, ty, ts);
  }
  for (const river of waterFeatures.rivers) {
    renderRiver(ctx, river, theme, tx, ty, ts);
  }
}

function renderCoast(ctx, coast, theme, tx, ty, ts) {
  const gradient = createCoastGradient(ctx, coast, theme, tx, ty);
  ctx.fillStyle = gradient;
  ctx.beginPath();
  tracePointPath(ctx, coast.polygon, tx, ty, true);
  ctx.fill();

  ctx.save();
  ctx.beginPath();
  tracePointPath(ctx, coast.polygon, tx, ty, true);
  ctx.clip();
  const shallowBands = [30, 18, 9];
  const shallowAlpha = [0.16, 0.12, 0.08];
  for (let i = 0; i < shallowBands.length; i++) {
    ctx.strokeStyle = rgbaStr(theme.waterShallow, shallowAlpha[i]);
    ctx.lineWidth = ts(shallowBands[i]);
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.beginPath();
    tracePointPath(ctx, coast.shoreline, tx, ty, false);
    ctx.stroke();
  }
  ctx.restore();

  ctx.strokeStyle = theme.waterLine;
  ctx.lineWidth = Math.max(1, ts(1.8));
  ctx.beginPath();
  tracePointPath(ctx, coast.shoreline, tx, ty, false);
  ctx.stroke();
}

function renderRiver(ctx, river, theme, tx, ty, ts) {
  const pts = river.points;
  if (pts.length < 2) return;

  // Build ribbon polygon: left bank + right bank as continuous outline
  // This eliminates round-cap segment joints
  const leftBank = [];
  const rightBank = [];
  const leftOuter = [];
  const rightOuter = [];

  for (let i = 0; i < pts.length; i++) {
    const w = river.widths[Math.min(i, river.widths.length - 1)] || 12;

    // Compute perpendicular direction at this point
    let nx, ny;
    if (i === 0) {
      const dx = pts[1].x - pts[0].x, dy = pts[1].y - pts[0].y;
      const len = Math.sqrt(dx * dx + dy * dy) || 1;
      nx = -dy / len; ny = dx / len;
    } else if (i === pts.length - 1) {
      const dx = pts[i].x - pts[i - 1].x, dy = pts[i].y - pts[i - 1].y;
      const len = Math.sqrt(dx * dx + dy * dy) || 1;
      nx = -dy / len; ny = dx / len;
    } else {
      // Average of prev and next segment normals for smooth bends
      const dx1 = pts[i].x - pts[i - 1].x, dy1 = pts[i].y - pts[i - 1].y;
      const dx2 = pts[i + 1].x - pts[i].x, dy2 = pts[i + 1].y - pts[i].y;
      const len1 = Math.sqrt(dx1 * dx1 + dy1 * dy1) || 1;
      const len2 = Math.sqrt(dx2 * dx2 + dy2 * dy2) || 1;
      const nx1 = -dy1 / len1, ny1 = dx1 / len1;
      const nx2 = -dy2 / len2, ny2 = dx2 / len2;
      nx = (nx1 + nx2) * 0.5;
      ny = (ny1 + ny2) * 0.5;
      const nlen = Math.sqrt(nx * nx + ny * ny) || 1;
      nx /= nlen; ny /= nlen;
    }

    const halfW = w * 0.5;
    const outerHalfW = halfW + 2; // bank edge

    leftBank.push({ x: pts[i].x + nx * halfW, y: pts[i].y + ny * halfW });
    rightBank.push({ x: pts[i].x - nx * halfW, y: pts[i].y - ny * halfW });
    leftOuter.push({ x: pts[i].x + nx * outerHalfW, y: pts[i].y + ny * outerHalfW });
    rightOuter.push({ x: pts[i].x - nx * outerHalfW, y: pts[i].y - ny * outerHalfW });
  }

  // Draw bank shadow as outer polygon
  ctx.fillStyle = theme.waterBank;
  ctx.beginPath();
  for (let i = 0; i < leftOuter.length; i++) {
    const p = leftOuter[i];
    i === 0 ? ctx.moveTo(tx(p.x), ty(p.y)) : ctx.lineTo(tx(p.x), ty(p.y));
  }
  for (let i = rightOuter.length - 1; i >= 0; i--) {
    ctx.lineTo(tx(rightOuter[i].x), ty(rightOuter[i].y));
  }
  ctx.closePath();
  ctx.fill();

  // Draw water core as inner polygon with depth gradient
  ctx.beginPath();
  for (let i = 0; i < leftBank.length; i++) {
    const p = leftBank[i];
    i === 0 ? ctx.moveTo(tx(p.x), ty(p.y)) : ctx.lineTo(tx(p.x), ty(p.y));
  }
  for (let i = rightBank.length - 1; i >= 0; i--) {
    ctx.lineTo(tx(rightBank[i].x), ty(rightBank[i].y));
  }
  ctx.closePath();

  // Use average depth for fill color
  const avgDepth = river.depths.reduce((a, b) => a + b, 0) / (river.depths.length || 1);
  const depthT = clamp(avgDepth / 5.5, 0, 1);
  const fill = lerpColor(theme.waterShallow, theme.waterDeep, depthT);
  ctx.fillStyle = rgbaStr(fill, 0.80);
  ctx.fill();

  // Water surface highlight — thin line along centerline
  ctx.strokeStyle = theme.waterLine;
  ctx.lineWidth = Math.max(0.8, ts(1.5));
  ctx.beginPath();
  tracePointPath(ctx, pts, tx, ty, false);
  ctx.stroke();
}

function renderRoads(ctx, roadGeometry, theme, tx, ty, ts) {
  const byTier = [[], [], []];
  for (const road of roadGeometry) {
    byTier[road.tier].push(road);
  }

  for (let tier = 2; tier >= 0; tier--) {
    for (const road of byTier[tier]) {
      drawRoadStroke(ctx, road.points, theme.roadCasing[tier], ROAD_WIDTHS[tier] + 2.4, tx, ty, ts);
    }
    for (const road of byTier[tier]) {
      drawRoadStroke(ctx, road.points, theme.roadFill[tier], ROAD_WIDTHS[tier], tx, ty, ts);
    }
    for (const road of byTier[tier]) {
      drawRoadStroke(ctx, road.points, theme.roadEdge, ROAD_WIDTHS[tier] + 0.2, tx, ty, ts);
    }
    if (theme.roadCenter[tier]) {
      for (const road of byTier[tier]) {
        drawRoadStroke(ctx, road.points, theme.roadCenter[tier], Math.max(0.7, ROAD_WIDTHS[tier] * 0.22), tx, ty, ts);
      }
    }
  }
}

function drawRoadStroke(ctx, points, color, width, tx, ty, ts) {
  if (!color || points.length < 2) return;
  ctx.strokeStyle = color;
  ctx.lineWidth = ts(width);
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  ctx.beginPath();
  tracePointPath(ctx, points, tx, ty, false);
  ctx.stroke();
}

function renderBuildings(ctx, buildings, theme, tx, ty, ts) {
  const shadowOffset = { x: ts(3.2), y: ts(4.8) };

  for (const building of buildings) {
    ctx.save();
    ctx.translate(tx(building.position.x), ty(building.position.y));
    ctx.rotate(building.angle);

    const width = ts(building.width);
    const height = ts(building.height);
    const baseFill = building.isLandmark ? theme.landmarkBase : darkenHex(theme.buildingBase, Math.min(building.floors * 0.045, 0.26));
    const roofFill = building.isLandmark ? theme.landmarkRoof : theme.buildingRoof;

    ctx.save();
    ctx.translate(shadowOffset.x, shadowOffset.y);
    ctx.fillStyle = theme.buildingShadow;
    traceBuildingShape(ctx, building.shapeType, width, height);
    ctx.fill();
    ctx.restore();

    ctx.fillStyle = baseFill;
    ctx.strokeStyle = theme.buildingLine;
    ctx.lineWidth = 0.75;
    traceBuildingShape(ctx, building.shapeType, width, height);
    ctx.fill();
    ctx.stroke();

    ctx.fillStyle = roofFill;
    ctx.save();
    ctx.scale(0.72, 0.72);
    traceBuildingShape(ctx, building.shapeType, width, height);
    ctx.fill();
    ctx.restore();

    ctx.strokeStyle = 'rgba(255,255,255,0.18)';
    ctx.lineWidth = 0.5;
    ctx.beginPath();
    ctx.moveTo(-width * 0.3, -height * 0.28);
    ctx.lineTo(width * 0.24, -height * 0.28);
    ctx.stroke();

    ctx.restore();
  }
}

function renderGridOverlay(ctx, preset, theme, tx, ty) {
  const steps = [
    { size: 100, alpha: 0.08 },
    { size: 50, alpha: 0.05 },
  ];
  ctx.lineWidth = 0.8;
  for (const step of steps) {
    ctx.strokeStyle = rgbaStr(theme.gridColor, step.alpha);
    for (let x = 0; x <= preset.worldWidth; x += step.size) {
      ctx.beginPath();
      ctx.moveTo(tx(x), ty(0));
      ctx.lineTo(tx(x), ty(preset.worldHeight));
      ctx.stroke();
    }
    for (let y = 0; y <= preset.worldHeight; y += step.size) {
      ctx.beginPath();
      ctx.moveTo(tx(0), ty(y));
      ctx.lineTo(tx(preset.worldWidth), ty(y));
      ctx.stroke();
    }
  }
}

function renderNodes(ctx, nodes, analysis, tx, ty, ts) {
  for (let i = 0; i < nodes.length; i++) {
    const node = nodes[i];
    const radius = node.degree >= 4 ? 3.2 : node.degree >= 3 ? 2.6 : 1.8;
    ctx.fillStyle = analysis.plazaIndices.includes(i) ? '#d39a2f'
      : analysis.intersectionIndices.includes(i) ? '#5b5b57'
      : analysis.deadEndIndices.includes(i) ? '#a44843'
      : '#2c2c29';
    ctx.beginPath();
    ctx.arc(tx(node.position.x), ty(node.position.y), ts(radius), 0, Math.PI * 2);
    ctx.fill();
  }
}

function renderLabels(ctx, nodes, theme, tx, ty, ts, scale) {
  ctx.fillStyle = theme.label;
  ctx.font = `700 ${Math.max(10, Math.round(10 * scale))}px Georgia, "Times New Roman", serif`;
  ctx.textAlign = 'center';
  ctx.textBaseline = 'bottom';
  for (const node of nodes) {
    if (!node.label) continue;
    ctx.fillText(node.label, tx(node.position.x), ty(node.position.y) - ts(5));
  }
}

function renderStats(ctx, mapData, theme) {
  const { nodes, edges, buildings, analysis, terrain } = mapData;
  const waterCount = terrain && terrain.waterBodies ? terrain.waterBodies.length : 0;
  const hillCount = terrain ? terrain.hills.length : 0;
  ctx.fillStyle = theme.statBg;
  ctx.fillRect(12, 12, 250, 156);
  ctx.fillStyle = theme.statText;
  ctx.font = '12px Consolas, "Courier New", monospace';
  ctx.textAlign = 'left';
  ctx.textBaseline = 'top';
  const lines = [
    `Seed: ${mapData.seed}`,
    `Type: ${mapData.generatorType}`,
    `Theme: ${theme.name}`,
    `Nodes: ${nodes.length}`,
    `Edges: ${edges.length}`,
    `Buildings: ${buildings.length}`,
    `Hills: ${hillCount}`,
    `Water bodies: ${waterCount}`,
    `Dead-ends: ${analysis.deadEndIndices.length}`,
  ];
  lines.forEach((line, index) => ctx.fillText(line, 20, 20 + index * 15));
}

function buildRoadGeometry(nodes, edges) {
  return edges.map((edge) => {
    const start = nodes[edge.nodeA].position;
    const end = nodes[edge.nodeB].position;
    const chord = Math.hypot(end.x - start.x, end.y - start.y);
    const segments = clamp(Math.ceil(chord / 24), 5, 14);
    const points = sampleQuadraticBezier(start, edge.controlPoint, end, segments);
    const bounds = getPointBounds(points);
    return {
      tier: Math.min(edge.tier, 2),
      points,
      bounds,
      maxInfluence: ROAD_WIDTHS[Math.min(edge.tier, 2)] * 4.2,
    };
  });
}

function buildWaterFeatures(waterBodies) {
  const features = { coasts: [], rivers: [] };
  for (const body of waterBodies) {
    if (body.bodyType === 'Coast') {
      const shoreline = getCoastShoreline(body);
      features.coasts.push({
        polygon: body.pathPoints.slice(),
        shoreline,
        coastSide: body.coastSide,
        bounds: getPointBounds(body.pathPoints),
        shoreBounds: getPointBounds(shoreline),
      });
    } else if (body.bodyType === 'River') {
      const points = body.pathPoints.slice();
      features.rivers.push({
        points,
        widths: body.widths.slice(),
        depths: body.depths.slice(),
        bounds: getPointBounds(points),
      });
    }
  }
  return features;
}

function getCoastShoreline(body) {
  if (!body.pathPoints || body.pathPoints.length <= 3) {
    return body.pathPoints ? body.pathPoints.slice() : [];
  }
  const shoreline = body.pathPoints.slice(3);
  shoreline.push(body.pathPoints[0]);
  return shoreline;
}

// computeMoistureInfluence, computeRoadInfluence, computeBuildingInfluence
// — removed: influence tints replaced by feature layers (water/road/building render on top)

function computeHillshade(elevMap, x, y, delta, maxElev) {
  const e = elevMap.sample({ x: x + delta, y });
  const w = elevMap.sample({ x: x - delta, y });
  const n = elevMap.sample({ x, y: y + delta });
  const s = elevMap.sample({ x, y: y - delta });
  const dx = (e - w) / (2 * delta);
  const dy = (n - s) / (2 * delta);
  const normal = normalize3({ x: -dx / maxElev * 9, y: -dy / maxElev * 9, z: 1 });
  return clamp(normal.x * SUN_DIR.x + normal.y * SUN_DIR.y + normal.z * SUN_DIR.z, 0, 1);
}

// computeCurvatureSigned, computeContourMask — removed: replaced by marching squares contours

function createCoastGradient(ctx, coast, theme, tx, ty) {
  const xs = coast.polygon.map((point) => point.x);
  const ys = coast.polygon.map((point) => point.y);
  const minX = Math.min(...xs);
  const maxX = Math.max(...xs);
  const minY = Math.min(...ys);
  const maxY = Math.max(...ys);

  let gradient;
  if (coast.coastSide === 0 || coast.coastSide === 2) {
    const startX = coast.coastSide === 0 ? tx(minX) : tx(maxX);
    const endX = coast.coastSide === 0 ? tx(maxX) : tx(minX);
    gradient = ctx.createLinearGradient(startX, ty(minY), endX, ty(minY));
  } else {
    const startY = coast.coastSide === 1 ? ty(minY) : ty(maxY);
    const endY = coast.coastSide === 1 ? ty(maxY) : ty(minY);
    gradient = ctx.createLinearGradient(tx(minX), startY, tx(minX), endY);
  }

  gradient.addColorStop(0, rgbStr(theme.waterShallow));
  gradient.addColorStop(1, rgbStr(theme.waterDeep));
  return gradient;
}

function sampleQuadraticBezier(a, control, b, segments) {
  const points = [];
  for (let i = 0; i <= segments; i++) {
    const t = i / segments;
    const u = 1 - t;
    points.push({
      x: u * u * a.x + 2 * u * t * control.x + t * t * b.x,
      y: u * u * a.y + 2 * u * t * control.y + t * t * b.y,
    });
  }
  return points;
}

function tracePointPath(ctx, points, tx, ty, closePath) {
  if (!points || points.length === 0) return;
  ctx.moveTo(tx(points[0].x), ty(points[0].y));
  for (let i = 1; i < points.length; i++) {
    ctx.lineTo(tx(points[i].x), ty(points[i].y));
  }
  if (closePath) ctx.closePath();
}

function traceBuildingShape(ctx, shapeType, width, height) {
  ctx.beginPath();
  switch (shapeType) {
    case 1:
      ctx.moveTo(-width / 2, -height / 2);
      ctx.lineTo(width / 2, -height / 2);
      ctx.lineTo(width / 2, 0);
      ctx.lineTo(0, 0);
      ctx.lineTo(0, height / 2);
      ctx.lineTo(-width / 2, height / 2);
      ctx.closePath();
      break;
    case 2:
      ctx.ellipse(0, 0, width / 2, height / 2, 0, 0, Math.PI * 2);
      break;
    case 3:
      ctx.rect(-width / 2, -height / 2, width, height);
      break;
    default:
      ctx.rect(-width / 2, -height / 2, width, height);
      break;
  }
}

function distanceToPolyline(x, y, points) {
  if (!points || points.length < 2) return Infinity;
  let best = Infinity;
  for (let i = 0; i < points.length - 1; i++) {
    best = Math.min(best, pointToSegmentDistance(
      x,
      y,
      points[i].x,
      points[i].y,
      points[i + 1].x,
      points[i + 1].y
    ));
  }
  return best;
}

function getPointBounds(points) {
  let minX = Infinity;
  let minY = Infinity;
  let maxX = -Infinity;
  let maxY = -Infinity;
  for (const point of points) {
    if (point.x < minX) minX = point.x;
    if (point.y < minY) minY = point.y;
    if (point.x > maxX) maxX = point.x;
    if (point.y > maxY) maxY = point.y;
  }
  return { minX, minY, maxX, maxY };
}

function isWithinBounds(x, y, bounds, padding) {
  return x >= bounds.minX - padding
    && x <= bounds.maxX + padding
    && y >= bounds.minY - padding
    && y <= bounds.maxY + padding;
}

function pointToSegmentDistance(px, py, ax, ay, bx, by) {
  const dx = bx - ax;
  const dy = by - ay;
  const lengthSq = dx * dx + dy * dy;
  if (lengthSq === 0) {
    return Math.hypot(px - ax, py - ay);
  }
  const t = clamp(((px - ax) * dx + (py - ay) * dy) / lengthSq, 0, 1);
  const cx = ax + dx * t;
  const cy = ay + dy * t;
  return Math.hypot(px - cx, py - cy);
}

function pointInPolygon(point, polygon) {
  let inside = false;
  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
    if ((polygon[i].y > point.y) !== (polygon[j].y > point.y)
      && point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x) {
      inside = !inside;
    }
  }
  return inside;
}

function elevGradient(base, mid, high, t) {
  if (t < 0.5) {
    return lerpColor(base, mid, t * 2);
  }
  return lerpColor(mid, high, (t - 0.5) * 2);
}

function lerpColor(a, b, t) {
  return [
    lerp(a[0], b[0], t),
    lerp(a[1], b[1], t),
    lerp(a[2], b[2], t),
  ];
}

function addColor(color, r, g, b) {
  return [
    clampByte(color[0] + r),
    clampByte(color[1] + g),
    clampByte(color[2] + b),
  ];
}

// mulColor — removed: hillshade uses alpha overlay instead of multiplication

function rgbStr(color) {
  return `rgb(${Math.round(color[0])}, ${Math.round(color[1])}, ${Math.round(color[2])})`;
}

function rgbaStr(color, alpha) {
  return `rgba(${Math.round(color[0])}, ${Math.round(color[1])}, ${Math.round(color[2])}, ${alpha})`;
}

function createTempCanvas(width, height) {
  if (typeof OffscreenCanvas !== 'undefined') {
    return new OffscreenCanvas(width, height);
  }
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  return canvas;
}

function darkenHex(hex, amount) {
  const factor = 1 - amount;
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  return `rgb(${Math.round(r * factor)}, ${Math.round(g * factor)}, ${Math.round(b * factor)})`;
}

function lerp(a, b, t) {
  return a + (b - a) * t;
}

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

function clampByte(value) {
  return Math.max(0, Math.min(255, Math.round(value)));
}

function frac(value) {
  return value - Math.floor(value);
}

function smoothstep(edge0, edge1, value) {
  const t = clamp((value - edge0) / (edge1 - edge0), 0, 1);
  return t * t * (3 - 2 * t);
}

function hashNoise(x, y, seed) {
  return frac(Math.sin(x * 127.1 + y * 311.7 + seed * 74.7) * 43758.5453123);
}

function fbm(x, y, seed) {
  let sum = 0;
  let amplitude = 0.56;
  let frequency = 1;
  for (let i = 0; i < 3; i++) {
    sum += hashNoise(x * frequency, y * frequency, seed + i * 13.17) * amplitude;
    frequency *= 2.03;
    amplitude *= 0.48;
  }
  return sum;
}

function normalize3(vector) {
  const length = Math.hypot(vector.x, vector.y, vector.z) || 1;
  return {
    x: vector.x / length,
    y: vector.y / length,
    z: vector.z / length,
  };
}

window.Renderer = { renderMap, THEMES };
