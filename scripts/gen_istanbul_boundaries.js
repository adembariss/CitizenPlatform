// geoBoundaries TUR ADM2 GeoJSON'dan İstanbul'un 39 ilçesini eşleştirip
// municipality_boundaries için SQL üretir (ST_GeomFromGeoJSON).
const fs = require('fs');

const IN = process.argv[2];
const OUT = process.argv[3];

const targets = [
  ['IST_ADALAR', 'Adalar'], ['IST_ARNAVUTKOY', 'Arnavutköy'], ['IST_ATASEHIR', 'Ataşehir'],
  ['IST_AVCILAR', 'Avcılar'], ['IST_BAGCILAR', 'Bağcılar'], ['IST_BAHCELIEVLER', 'Bahçelievler'],
  ['IST_BAKIRKOY', 'Bakırköy'], ['IST_BASAKSEHIR', 'Başakşehir'], ['IST_BAYRAMPASA', 'Bayrampaşa'],
  ['IST_BESIKTAS', 'Beşiktaş'], ['IST_BEYKOZ', 'Beykoz'], ['IST_BEYLIKDUZU', 'Beylikdüzü'],
  ['IST_BEYOGLU', 'Beyoğlu'], ['IST_BUYUKCEKMECE', 'Büyükçekmece'], ['IST_CATALCA', 'Çatalca'],
  ['IST_CEKMEKOY', 'Çekmeköy'], ['IST_ESENLER', 'Esenler'], ['IST_ESENYURT', 'Esenyurt'],
  ['IST_EYUPSULTAN', 'Eyüpsultan', 'Eyüp'], ['IST_FATIH', 'Fatih'], ['IST_GAZIOSMANPASA', 'Gaziosmanpaşa'],
  ['IST_GUNGOREN', 'Güngören'], ['IST_KADIKOY', 'Kadıköy'], ['IST_KAGITHANE', 'Kağıthane'],
  ['IST_KARTAL', 'Kartal'], ['IST_KUCUKCEKMECE', 'Küçükçekmece'], ['IST_MALTEPE', 'Maltepe'],
  ['IST_PENDIK', 'Pendik'], ['IST_SANCAKTEPE', 'Sancaktepe'], ['IST_SARIYER', 'Sarıyer'],
  ['IST_SILIVRI', 'Silivri'], ['IST_SULTANBEYLI', 'Sultanbeyli'], ['IST_SULTANGAZI', 'Sultangazi'],
  ['IST_SILE', 'Şile'], ['IST_SISLI', 'Şişli'], ['IST_TUZLA', 'Tuzla'],
  ['IST_UMRANIYE', 'Ümraniye'], ['IST_USKUDAR', 'Üsküdar'], ['IST_ZEYTINBURNU', 'Zeytinburnu']
];

// İstanbul kaba bbox (centroid ile diğer illerdeki aynı adlı ilçelerden ayırmak için)
const BBOX = { minLng: 27.7, minLat: 40.6, maxLng: 30.2, maxLat: 41.85 };

function norm(s) {
  return s
    .replace(/ı/g, 'i').replace(/İ/g, 'i')
    .replace(/ş/gi, 's').replace(/ğ/gi, 'g').replace(/ü/gi, 'u').replace(/ö/gi, 'o').replace(/ç/gi, 'c')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');
}

function centroid(geom) {
  let sx = 0, sy = 0, n = 0;
  const walk = (a) => {
    if (typeof a[0] === 'number') { sx += a[0]; sy += a[1]; n++; return; }
    for (const c of a) walk(c);
  };
  walk(geom.coordinates);
  return n ? [sx / n, sy / n] : [0, 0];
}

const gj = JSON.parse(fs.readFileSync(IN, 'utf8'));
const feats = gj.features;

// normalize name -> list of features
const byName = new Map();
for (const f of feats) {
  const key = norm(f.properties.shapeName || '');
  if (!byName.has(key)) byName.set(key, []);
  byName.get(key).push(f);
}

const lines = [];
lines.push('-- İstanbul ilçe sınırları (geoBoundaries gbOpen TUR ADM2, ODbL). Otomatik üretildi.');
lines.push('BEGIN;');
let matched = 0;
const missing = [];

for (const [code, ...names] of targets) {
  let feat = null;
  for (const nm of names) {
    const cands = byName.get(norm(nm)) || [];
    // İstanbul bbox içindeki centroid'i olanı tercih et
    const inBox = cands.filter((f) => {
      const [x, y] = centroid(f.geometry);
      return x >= BBOX.minLng && x <= BBOX.maxLng && y >= BBOX.minLat && y <= BBOX.maxLat;
    });
    feat = inBox[0] || (cands.length === 1 ? cands[0] : null);
    if (feat) break;
  }
  if (!feat) { missing.push(code); continue; }

  const geomJson = JSON.stringify(feat.geometry).replace(/'/g, "''");
  const label = names[0] + ' - geoBoundaries ADM2';
  lines.push(`DELETE FROM public.municipality_boundaries WHERE municipality_id = (SELECT id FROM public.municipalities WHERE code = '${code}');`);
  lines.push(
    `INSERT INTO public.municipality_boundaries (id, municipality_id, name, boundary_geometry, is_active, created_at, updated_at, deleted_at, is_deleted) ` +
    `SELECT gen_random_uuid(), m.id, '${label}', ST_Multi(ST_SetSRID(ST_GeomFromGeoJSON('${geomJson}'), 4326)), true, now(), NULL, NULL, false ` +
    `FROM public.municipalities m WHERE m.code = '${code}';`
  );
  matched++;
}

// Demo sınırı artık İstanbul'u yakalamasın (gerçek ilçe poligonları devrede)
lines.push(`UPDATE public.municipality_boundaries SET is_active = false WHERE municipality_id = (SELECT id FROM public.municipalities WHERE code = 'DEMO');`);
lines.push('COMMIT;');

fs.writeFileSync(OUT, lines.join('\n'), 'utf8');
console.error(`Eşleşen: ${matched}/39. Eksik: ${missing.join(', ') || 'yok'}`);
