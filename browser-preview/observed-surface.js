// SP-041: Observed Surface Synthesis — surface classification approach
//
// Every cell on the map is classified into a surface type.
// Each type defines its color palette, density pattern, and micro-variation.
// The result is a continuous surface image — not scattered glyphs on relief.
//
// Surface types:
//   OPEN       — bare ground, grass, open field (default)
//   FOREST     — canopy mass, ridge/valley color shift, density variation
//   RIPARIAN   — wet belt along rivers, transitional vegetation
//   RESIDENTIAL— roof-dense zone, parcel texture, service clutter tone
//   WATER      — handled by hard-edge layer, excluded here
//   ROAD       — handled by hard-edge layer, excluded here

'use strict';

// ============================================================
// Seeded PRNG (independent from map-gen.js)
// ============================================================

class SurfaceRng {
  constructor(seed) { this._state = seed & 0x7fffffff || 1; }
  _next() { this._state = (this._state * 48271) % 0x7fffffff; return this._state; }
  float() { return this._next() / 0x7fffffff; }
  range(lo, hi) { return lo + this.float() * (hi - lo); }
}

// ============================================================
// Surface type enum
// ============================================================

const SURFACE = { OPEN: 0, FOREST: 1, RIPARIAN: 2, RESIDENTIAL: 3 };

// ============================================================
// Theme palettes per surface type
// ============================================================

const SURFACE_PALETTES = {
  Parchment: {
    [SURFACE.OPEN]: {
      base: [195, 190, 160], mid: [205, 198, 168], range: 12,
    },
    [SURFACE.FOREST]: {
      base: [95, 120, 75], mid: [80, 108, 65], dark: [65, 90, 52],
      ridge: [110, 128, 85], valley: [72, 95, 58], range: 18,
    },
    [SURFACE.RIPARIAN]: {
      base: [120, 148, 125], mid: [105, 135, 112], wet: [95, 128, 108], range: 14,
    },
    [SURFACE.RESIDENTIAL]: {
      base: [175, 165, 145], roof: [148, 135, 118], gap: [188, 180, 160],
      clutter: [158, 148, 128], range: 10,
    },
  },
  Dark: {
    [SURFACE.OPEN]: {
      base: [68, 78, 58], mid: [75, 84, 64], range: 10,
    },
    [SURFACE.FOREST]: {
      base: [40, 58, 35], mid: [35, 52, 30], dark: [28, 42, 24],
      ridge: [52, 65, 42], valley: [30, 45, 26], range: 14,
    },
    [SURFACE.RIPARIAN]: {
      base: [45, 62, 52], mid: [38, 55, 46], wet: [32, 50, 42], range: 10,
    },
    [SURFACE.RESIDENTIAL]: {
      base: [72, 68, 60], roof: [62, 58, 52], gap: [80, 75, 68],
      clutter: [66, 62, 55], range: 8,
    },
  },
};

// ============================================================
// Surface classification
// ============================================================

function classifySurface(mapData) {
  const { preset, elevMap, terrain, buildings, nodes, edges } = mapData;
  const w = preset.worldWidth, h = preset.worldHeight;
  const maxElev = Math.max(preset.maxElevation, 0.01);

  const FW = 256, FH = Math.round(256 * h / w);
  const cellW = w / FW, cellH = h / FH;
  const count = FW * FH;

  const typeField = new Uint8Array(count);     // SURFACE enum
  const densityField = new Float32Array(count); // 0-1, type-specific intensity
  const elevField = new Float32Array(count);
  const slopeField = new Float32Array(count);

  // Pass 1: elevation and slope
  for (let iy = 0; iy < FH; iy++) {
    for (let ix = 0; ix < FW; ix++) {
      const wx = (ix + 0.5) * cellW, wy = (iy + 0.5) * cellH;
      const idx = iy * FW + ix;
      const e = elevMap.sample({ x: wx, y: wy });
      elevField[idx] = e / maxElev;
      slopeField[idx] = elevMap.sampleSlope({ x: wx, y: wy });
    }
  }

  // Pass 2: Classify — start with OPEN, then override

  // 2a: Forest — hills with moderate slope, away from flat center
  for (let i = 0; i < count; i++) {
    const e = elevField[i], s = slopeField[i];
    const forestScore = smoothstep(0.08, 0.30, e) * (1 - smoothstep(0.90, 1.0, e))
                      * (1 - smoothstep(0.9, 1.6, s));
    if (forestScore > 0.2) {
      typeField[i] = SURFACE.FOREST;
      densityField[i] = forestScore;
    }
  }

  // 2b: Riparian — near rivers
  if (terrain && terrain.waterBodies) {
    for (const wb of terrain.waterBodies) {
      if (wb.bodyType !== 'River' || !wb.pathPoints) continue;
      for (let i = 0; i < wb.pathPoints.length - 1; i++) {
        const p0 = wb.pathPoints[i], p1 = wb.pathPoints[i + 1];
        const rw = (wb.widths[Math.min(i, wb.widths.length - 1)] || 12);
        const beltWidth = rw * 2.5; // riparian belt extends beyond water edge

        // Rasterize belt into field
        const segLen = Math.sqrt((p1.x - p0.x) ** 2 + (p1.y - p0.y) ** 2);
        const steps = Math.ceil(segLen / (cellW * 0.5));
        for (let s = 0; s <= steps; s++) {
          const t = s / steps;
          const cx = p0.x + (p1.x - p0.x) * t;
          const cy = p0.y + (p1.y - p0.y) * t;

          const rad = Math.ceil(beltWidth / cellW);
          const cix = Math.floor(cx / cellW), ciy = Math.floor(cy / cellH);

          for (let dy = -rad; dy <= rad; dy++) {
            for (let dx = -rad; dx <= rad; dx++) {
              const fx = cix + dx, fy = ciy + dy;
              if (fx < 0 || fx >= FW || fy < 0 || fy >= FH) continue;
              const dist = Math.sqrt(dx * dx + dy * dy) * cellW;
              if (dist < beltWidth) {
                const idx = fy * FW + fx;
                const strength = 1 - dist / beltWidth;
                if (strength > densityField[idx] || typeField[idx] === SURFACE.OPEN) {
                  typeField[idx] = SURFACE.RIPARIAN;
                  densityField[idx] = Math.max(densityField[idx], strength);
                }
              }
            }
          }
        }
      }
    }
  }

  // 2c: Residential — near buildings (non-landmark, low-rise)
  const residentials = buildings.filter(b => !b.isLandmark && b.floors <= 4);
  for (const b of residentials) {
    const bix = Math.floor(b.position.x / cellW);
    const biy = Math.floor(b.position.y / cellH);
    const parcelRad = Math.ceil(Math.max(b.width, b.height) * 1.2 / cellW);

    for (let dy = -parcelRad; dy <= parcelRad; dy++) {
      for (let dx = -parcelRad; dx <= parcelRad; dx++) {
        const fx = bix + dx, fy = biy + dy;
        if (fx < 0 || fx >= FW || fy < 0 || fy >= FH) continue;
        const dist = Math.sqrt(dx * dx + dy * dy) / parcelRad;
        if (dist < 1) {
          const idx = fy * FW + fx;
          const strength = 1 - dist;
          // Residential overrides open and weak forest, not riparian
          if (typeField[idx] === SURFACE.OPEN ||
              (typeField[idx] === SURFACE.FOREST && densityField[idx] < 0.4)) {
            typeField[idx] = SURFACE.RESIDENTIAL;
            densityField[idx] = Math.max(densityField[idx], strength * 0.8);
          }
        }
      }
    }
  }

  // 2d: Suppress forest directly on roads
  for (const edge of edges) {
    const na = nodes[edge.nodeA], nb = nodes[edge.nodeB];
    const roadW = [8, 5, 3][Math.min(edge.tier, 2)];
    const segLen = Math.sqrt((nb.position.x - na.position.x) ** 2 + (nb.position.y - na.position.y) ** 2);
    const steps = Math.ceil(segLen / (cellW * 0.5));
    for (let s = 0; s <= steps; s++) {
      const t = s / steps;
      const cx = na.position.x + (nb.position.x - na.position.x) * t;
      const cy = na.position.y + (nb.position.y - na.position.y) * t;
      const rad = Math.ceil(roadW / cellW);
      const cix = Math.floor(cx / cellW), ciy = Math.floor(cy / cellH);
      for (let dy = -rad; dy <= rad; dy++) {
        for (let dx = -rad; dx <= rad; dx++) {
          const fx = cix + dx, fy = ciy + dy;
          if (fx < 0 || fx >= FW || fy < 0 || fy >= FH) continue;
          const idx = fy * FW + fx;
          if (typeField[idx] === SURFACE.FOREST) {
            const dist = Math.sqrt(dx * dx + dy * dy) / rad;
            if (dist < 1) {
              typeField[idx] = SURFACE.OPEN;
              densityField[idx] *= dist;
            }
          }
        }
      }
    }
  }

  return { FW, FH, cellW, cellH, typeField, densityField, elevField, slopeField, maxElev };
}

// ============================================================
// Render classified surface as ImageData
// ============================================================

function renderClassifiedSurface(ctx, mapData, theme, options, tx, ty, ts) {
  const fields = classifySurface(mapData);
  const { FW, FH, typeField, densityField, elevField, slopeField } = fields;
  const palette = SURFACE_PALETTES[theme.name] || SURFACE_PALETTES.Parchment;
  const rng = new SurfaceRng(mapData.seed * 17 + 73);

  const offscreen = new OffscreenCanvas(FW, FH);
  const octx = offscreen.getContext('2d');
  const img = octx.createImageData(FW, FH);
  const px = img.data;

  // Hillshade for modulation
  const maxElev = fields.maxElev;
  const lx = -0.6, ly = 0.5;
  const lLen = Math.sqrt(lx * lx + ly * ly);
  const nlx = lx / lLen, nly = ly / lLen;

  for (let i = 0; i < FW * FH; i++) {
    const type = typeField[i];
    const density = densityField[i];
    const elev = elevField[i];
    const slope = slopeField[i];
    const p = palette[type] || palette[SURFACE.OPEN];

    // Micro-noise per cell
    const noise = rng.range(-p.range, p.range);

    let r, g, b;

    switch (type) {
      case SURFACE.FOREST: {
        // Ridge/valley color shift based on elevation
        const ridgeFactor = smoothstep(0.4, 0.7, elev);
        const valleyFactor = smoothstep(0.25, 0.05, elev);
        const base = ridgeFactor > 0.1 ? lerpArr(p.base, p.ridge, ridgeFactor)
                   : valleyFactor > 0.1 ? lerpArr(p.base, p.valley, valleyFactor)
                   : p.base;
        // Density modulates between base and dark
        const mixed = lerpArr(p.mid, base, density);
        r = mixed[0] + noise; g = mixed[1] + noise * 0.8; b = mixed[2] + noise * 0.5;

        // Canopy variation — occasional lighter patches (not holes, color shifts)
        const variation = rng.float();
        if (variation > 0.85 && density > 0.3) {
          r += 12; g += 15; b += 5; // sunlit patch
        } else if (variation < 0.08 && density > 0.4) {
          r -= 8; g -= 6; b -= 3; // deep shadow
        }
        break;
      }

      case SURFACE.RIPARIAN: {
        const wetness = density;
        const base = lerpArr(p.base, p.wet, wetness);
        r = base[0] + noise; g = base[1] + noise * 0.7; b = base[2] + noise * 0.6;
        break;
      }

      case SURFACE.RESIDENTIAL: {
        // Alternate between roof-tone and gap-tone for parcel texture
        const parcelNoise = rng.float();
        if (parcelNoise > 0.7 && density > 0.3) {
          // Roof-like
          r = p.roof[0] + noise; g = p.roof[1] + noise * 0.8; b = p.roof[2] + noise * 0.6;
        } else if (parcelNoise < 0.15) {
          // Gap / garden
          r = p.gap[0] + noise; g = p.gap[1] + noise * 0.9; b = p.gap[2] + noise * 0.7;
        } else {
          // Service / mixed
          const t = density;
          const mixed = lerpArr(p.base, p.clutter, t);
          r = mixed[0] + noise; g = mixed[1] + noise * 0.8; b = mixed[2] + noise * 0.6;
        }
        break;
      }

      default: { // OPEN
        const t = smoothstep(0, 0.3, elev);
        const mixed = lerpArr(p.base, p.mid, t);
        r = mixed[0] + noise; g = mixed[1] + noise * 0.85; b = mixed[2] + noise * 0.7;
        break;
      }
    }

    // Hillshade modulation (subtle, applies to all types)
    const iy = (i / FW) | 0, ix = i % FW;
    let hillshade = 1.0;
    if (ix > 0 && ix < FW - 1 && iy > 0 && iy < FH - 1) {
      const eE = elevField[iy * FW + ix + 1];
      const eW = elevField[iy * FW + ix - 1];
      const eN = elevField[(iy - 1) * FW + ix];
      const eS = elevField[(iy + 1) * FW + ix];
      const dEdx = (eE - eW) * 0.5;
      const dEdy = (eS - eN) * 0.5;
      hillshade = clamp01((-dEdx * nlx + -dEdy * nly) * 2.5 + 0.5);
    }
    const shade = 0.82 + hillshade * 0.36; // range 0.82-1.18
    r *= shade; g *= shade; b *= shade;

    const idx = i * 4;
    px[idx]     = clamp8(r);
    px[idx + 1] = clamp8(g);
    px[idx + 2] = clamp8(b);
    px[idx + 3] = 255;
  }

  octx.putImageData(img, 0, 0);

  // Draw as full map-area image
  const mapRect = {
    x: tx(0), y: ty(0),
    w: ts(mapData.preset.worldWidth),
    h: ts(mapData.preset.worldHeight)
  };
  ctx.imageSmoothingEnabled = true;
  ctx.drawImage(offscreen, mapRect.x, mapRect.y, mapRect.w, mapRect.h);
}

// ============================================================
// Main entry
// ============================================================

function synthesizeObservedSurface(ctx, mapData, theme, options, tx, ty, ts) {
  renderClassifiedSurface(ctx, mapData, theme, options, tx, ty, ts);
}

// ============================================================
// Utilities
// ============================================================

function lerpArr(a, b, t) {
  return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
}

function smoothstep(edge0, edge1, x) {
  const t = Math.max(0, Math.min(1, (x - edge0) / (edge1 - edge0)));
  return t * t * (3 - 2 * t);
}

function clamp01(v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
function clamp8(v) { return Math.max(0, Math.min(255, Math.round(v))); }

window.ObservedSurface = { synthesizeObservedSurface };
