// geoBoundaries TUR ADM1 (il) + ADM2 (ilçe) GeoJSON'larından tüm Türkiye ilçelerini
// gerçek sınır poligonlarıyla municipalities + municipality_boundaries için SQL üretir.
// Her ilçeye, centroid'inin içinde bulunduğu il (ADM1) atanır (nokta-poligon).
// İstanbul atlanır (39 ilçe zaten IST_ kodlarıyla mevcut). Gelibolu/Çeşme/Bodrum/Alanya
// için mevcut kayda gerçek sınır bağlanır (yeni kayıt açılmaz).
//
// Kullanım: node scripts/gen_turkey_boundaries.js adm1.geojson adm2.geojson out.sql
const fs = require('fs');
const [, , ADM1, ADM2, OUT] = process.argv;

function norm(s) {
  return (s || '')
    .replace(/ı/g, 'i').replace(/İ/g, 'i')
    .replace(/ş/gi, 's').replace(/ğ/gi, 'g').replace(/ü/gi, 'u').replace(/ö/gi, 'o').replace(/ç/gi, 'c')
    .toLowerCase().replace(/[^a-z0-9]/g, '');
}
function slug(s) {
  return norm(s).toUpperCase();
}
function esc(s) { return s.replace(/'/g, "''"); }

function eachCoord(geom, cb) {
  const walk = (a) => { if (typeof a[0] === 'number') cb(a); else for (const c of a) walk(c); };
  walk(geom.coordinates);
}
function centroid(geom) { let sx = 0, sy = 0, n = 0; eachCoord(geom, ([x, y]) => { sx += x; sy += y; n++; }); return [sx / n, sy / n]; }
function bbox(geom) { let a = Infinity, b = Infinity, c = -Infinity, d = -Infinity; eachCoord(geom, ([x, y]) => { if (x < a) a = x; if (y < b) b = y; if (x > c) c = x; if (y > d) d = y; }); return [a, b, c, d]; }

function pointInRing(pt, ring) {
  let inside = false; const [x, y] = pt;
  for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
    const [xi, yi] = ring[i], [xj, yj] = ring[j];
    if (((yi > y) !== (yj > y)) && (x < ((xj - xi) * (y - yi)) / (yj - yi) + xi)) inside = !inside;
  }
  return inside;
}
function pointInGeom(pt, geom) {
  const polys = geom.type === 'Polygon' ? [geom.coordinates] : geom.coordinates;
  for (const poly of polys) {
    if (pointInRing(pt, poly[0])) {
      let hole = false;
      for (let k = 1; k < poly.length; k++) if (pointInRing(pt, poly[k])) { hole = true; break; }
      if (!hole) return true;
    }
  }
  return false;
}

const provinces = JSON.parse(fs.readFileSync(ADM1, 'utf8')).features.map((f) => ({
  name: f.properties.shapeName, geom: f.geometry, bb: bbox(f.geometry)
}));
function provinceOf(pt) {
  for (const p of provinces) {
    const [a, b, c, d] = p.bb;
    if (pt[0] < a || pt[0] > c || pt[1] < b || pt[1] > d) continue;
    if (pointInGeom(pt, p.geom)) return p.name;
  }
  return null;
}

// Mevcut örnek ilçeler: gerçek sınırı bunlara bağla (yeni kayıt açma)
const existing = { gelibolu: 'GELIBOLU', cesme: 'CESME', bodrum: 'BODRUM', alanya: 'ALANYA' };

const adm2 = JSON.parse(fs.readFileSync(ADM2, 'utf8')).features;
const usedCodes = new Set();
const out = [];
out.push('-- Tüm Türkiye ilçe belediyeleri: gerçek sınırlar (geoBoundaries gbOpen TUR ADM2, ODbL).');
out.push('-- İl ataması ADM1 nokta-poligon ile; İstanbul atlanır (IST_ mevcut).');
out.push('BEGIN;');

let created = 0, attached = 0, skipped = 0;
for (const f of adm2) {
  const name = f.properties.shapeName;
  const c = centroid(f.geometry);
  const prov = provinceOf(c);
  if (!prov || norm(prov) === norm('İstanbul')) { skipped++; continue; }

  const geomJson = esc(JSON.stringify(f.geometry));
  const nkey = norm(name);

  if (existing[nkey]) {
    // mevcut örnek belediyeye gerçek sınırı bağla
    const code = existing[nkey];
    out.push(`DELETE FROM public.municipality_boundaries WHERE municipality_id = (SELECT id FROM public.municipalities WHERE code = '${code}');`);
    out.push(`INSERT INTO public.municipality_boundaries (id, municipality_id, name, boundary_geometry, is_active, created_at, updated_at, deleted_at, is_deleted) SELECT gen_random_uuid(), m.id, '${esc(name)} - geoBoundaries ADM2', ST_Multi(ST_SetSRID(ST_GeomFromGeoJSON('${geomJson}'),4326)), true, now(), NULL, NULL, false FROM public.municipalities m WHERE m.code = '${code}';`);
    out.push(`UPDATE public.municipalities SET center_latitude=${c[1].toFixed(5)}, center_longitude=${c[0].toFixed(5)} WHERE code='${code}';`);
    attached++;
    continue;
  }

  let code = 'TR_' + slug(prov) + '_' + slug(name);
  code = code.slice(0, 48);
  let base = code, n = 1;
  while (usedCodes.has(code)) code = (base.slice(0, 45) + n++);
  usedCodes.add(code);

  const mid = 'gen_random_uuid()';
  out.push(`WITH m AS (INSERT INTO public.municipalities (id, name, code, is_active, province, center_latitude, center_longitude, created_at, updated_at, deleted_at, is_deleted) VALUES (${mid}, '${esc(name)} Belediyesi', '${code}', true, '${esc(prov)}', ${c[1].toFixed(5)}, ${c[0].toFixed(5)}, now(), NULL, NULL, false) RETURNING id) INSERT INTO public.municipality_boundaries (id, municipality_id, name, boundary_geometry, is_active, created_at, updated_at, deleted_at, is_deleted) SELECT gen_random_uuid(), m.id, '${esc(name)} - geoBoundaries ADM2', ST_Multi(ST_SetSRID(ST_GeomFromGeoJSON('${geomJson}'),4326)), true, now(), NULL, NULL, false FROM m;`);
  created++;
}

// Yaklaşık kutu sınırları devre dışı (yalnızca gerçek geoBoundaries poligonları aktif kalsın)
out.push(`UPDATE public.municipality_boundaries SET is_active = false WHERE position('geoBoundaries' in name) = 0;`);
out.push('COMMIT;');

fs.writeFileSync(OUT, out.join('\n'), 'utf8');
console.error(`Oluşturulan: ${created}, mevcut örneğe bağlanan: ${attached}, atlanan(İstanbul/il yok): ${skipped}`);
