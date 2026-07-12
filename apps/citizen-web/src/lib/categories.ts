// Temporary hardcoded list matching database/main-db/003_seed_demo_municipality.sql.
// There is no public "list categories for my municipality" endpoint yet - once one
// exists, this should be replaced with a real fetch after resolveMunicipality().
export const DEMO_CATEGORIES = [
  { id: '22222222-2222-2222-2222-222222222201', name: 'Yol ve Kaldırım' },
  { id: '22222222-2222-2222-2222-222222222202', name: 'Çöp ve Temizlik' },
  { id: '22222222-2222-2222-2222-222222222203', name: 'Aydınlatma' },
  { id: '22222222-2222-2222-2222-222222222204', name: 'Park ve Bahçe' },
  { id: '22222222-2222-2222-2222-222222222205', name: 'Trafik' },
  { id: '22222222-2222-2222-2222-222222222206', name: 'Sokak Hayvanları' },
  { id: '22222222-2222-2222-2222-222222222207', name: 'Diğer' }
];
