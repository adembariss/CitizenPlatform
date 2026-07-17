import { Ionicons } from '@expo/vector-icons';
import { useEffect, useRef, useState } from 'react';
import { Linking, Platform, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import MapView, { Marker, PROVIDER_GOOGLE, type Region } from 'react-native-maps';
import { GradientHeader } from '../components/Header';
import { SelectField } from '../components/PickerModal';
import { Banner, Button, Card, EmptyState, Loading } from '../components/ui';
import { getCurrentPosition } from '../services/location';
import { getDistricts, getNearbyPharmacies, getPharmacies, getProvinces, type District, type Pharmacy } from '../services/api';
import { colors, radius, shadow, type } from '../theme';

const TURKEY_REGION: Region = { latitude: 39.0, longitude: 35.2, latitudeDelta: 8, longitudeDelta: 8 };
type Mode = 'nearby' | 'filter';

export function PharmaciesScreen() {
  const mapRef = useRef<MapView | null>(null);
  const [mode, setMode] = useState<Mode>('nearby');
  const [onDutyOnly, setOnDutyOnly] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pharmacies, setPharmacies] = useState<Pharmacy[]>([]);
  const [focusId, setFocusId] = useState<string | null>(null);

  const [provinces, setProvinces] = useState<string[]>([]);
  const [province, setProvince] = useState<string | null>(null);
  const [districts, setDistricts] = useState<District[]>([]);
  const [district, setDistrict] = useState<string | null>(null);

  useEffect(() => {
    getProvinces().then((r) => r.data && setProvinces(r.data));
    loadNearby();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadNearby() {
    setLoading(true);
    setError(null);
    try {
      const pos = await getCurrentPosition();
      const r = await getNearbyPharmacies(pos.latitude, pos.longitude, onDutyOnly);
      const list = r.data ?? [];
      setPharmacies(list);
      if (list[0]) focusPharmacy(list[0], false);
      if (list.length === 0) setError('Yakında kayıtlı eczane bulunamadı.');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Konum alınamadı.');
    } finally {
      setLoading(false);
    }
  }

  async function loadFilter(nextProvince = province, nextDistrict = district, duty = onDutyOnly) {
    if (!nextProvince) return;
    setLoading(true);
    setError(null);
    const districtName = districts.find((d) => d.id === nextDistrict)?.name;
    const r = await getPharmacies({ province: nextProvince, district: districtName, onDutyOnly: duty });
    const list = r.data ?? [];
    setPharmacies(list);
    if (list[0]) focusPharmacy(list[0], false);
    else setError('Bu filtreye uygun eczane bulunamadı.');
    setLoading(false);
  }

  function focusPharmacy(p: Pharmacy, openCallout = true) {
    setFocusId(p.id);
    mapRef.current?.animateToRegion({ latitude: p.latitude, longitude: p.longitude, latitudeDelta: 0.02, longitudeDelta: 0.02 }, 500);
  }

  function switchMode(next: Mode) {
    setMode(next);
    setError(null);
    setPharmacies([]);
    if (next === 'nearby') loadNearby();
  }

  function toggleDuty() {
    const next = !onDutyOnly;
    setOnDutyOnly(next);
    if (mode === 'nearby') loadNearby();
    else loadFilter(province, district, next);
  }

  function onProvince(value: string) {
    setProvince(value);
    setDistrict(null);
    getDistricts(value).then((r) => {
      const list = r.data ?? [];
      setDistricts(list);
      loadFilter(value, null);
    });
  }

  function onDistrict(value: string) {
    setDistrict(value);
    loadFilter(province, value);
  }

  return (
    <View style={{ flex: 1, backgroundColor: colors.bg }}>
      <GradientHeader title="Nöbetçi Eczaneler" subtitle="En yakın açık eczaneyi bul" compact />
      <ScrollView contentContainerStyle={styles.content} showsVerticalScrollIndicator={false}>
        <View style={styles.modeToggle}>
          <Pressable style={[styles.modeBtn, mode === 'nearby' && styles.modeBtnActive]} onPress={() => switchMode('nearby')}>
            <Ionicons name="navigate" size={16} color={mode === 'nearby' ? '#fff' : colors.slate} />
            <Text style={[styles.modeText, mode === 'nearby' && styles.modeTextActive]}>En Yakın</Text>
          </Pressable>
          <Pressable style={[styles.modeBtn, mode === 'filter' && styles.modeBtnActive]} onPress={() => switchMode('filter')}>
            <Ionicons name="filter" size={16} color={mode === 'filter' ? '#fff' : colors.slate} />
            <Text style={[styles.modeText, mode === 'filter' && styles.modeTextActive]}>İl / İlçe</Text>
          </Pressable>
        </View>

        {mode === 'filter' && (
          <Card style={{ gap: 12 }}>
            <SelectField label="İl" placeholder="İl seçin" value={province} searchable options={provinces.map((p) => ({ label: p, value: p }))} onSelect={onProvince} />
            <SelectField
              label="İlçe (opsiyonel)"
              placeholder={province ? 'Tüm ilçeler' : 'Önce il seçin'}
              value={district}
              disabled={!province || districts.length === 0}
              searchable
              options={districts.map((d) => ({ label: d.name, value: d.id }))}
              onSelect={onDistrict}
            />
          </Card>
        )}

        <Pressable onPress={toggleDuty} style={[styles.dutyToggle, onDutyOnly && styles.dutyToggleActive]}>
          <Ionicons name={onDutyOnly ? 'checkmark-circle' : 'ellipse-outline'} size={20} color={onDutyOnly ? colors.green : colors.muted} />
          <Text style={[styles.dutyText, onDutyOnly && { color: colors.green }]}>Sadece nöbetçi (açık) eczaneler</Text>
        </Pressable>

        <View style={[styles.mapWrap, shadow(8)]}>
          <MapView
            ref={mapRef}
            provider={Platform.OS === 'android' ? PROVIDER_GOOGLE : undefined}
            style={StyleSheet.absoluteFill}
            initialRegion={TURKEY_REGION}
          >
            {pharmacies.map((p) => (
              <Marker
                key={p.id}
                coordinate={{ latitude: p.latitude, longitude: p.longitude }}
                title={p.name}
                description={`${p.district}${p.isOnDuty ? ' • Nöbetçi' : ''}`}
                pinColor={p.isOnDuty ? colors.green : colors.muted}
                onPress={() => setFocusId(p.id)}
              />
            ))}
          </MapView>
        </View>

        {loading && <Loading label="Eczaneler yükleniyor..." />}
        {error && !loading && <Banner tone="info">{error}</Banner>}

        {!loading && pharmacies.length > 0 && (
          <View style={{ gap: 10 }}>
            <Text style={type.h3}>{pharmacies.length} eczane</Text>
            {pharmacies.map((p) => (
              <Pressable key={p.id} onPress={() => focusPharmacy(p)}>
                <Card style={[styles.item, focusId === p.id && styles.itemActive]}>
                  <View style={[styles.itemIcon, { backgroundColor: p.isOnDuty ? colors.greenSoft : colors.surfaceAlt }]}>
                    <Ionicons name="medkit" size={20} color={p.isOnDuty ? colors.green : colors.muted} />
                  </View>
                  <View style={{ flex: 1 }}>
                    <View style={styles.itemHead}>
                      <Text style={styles.itemName} numberOfLines={1}>{p.name}</Text>
                      {p.isOnDuty && (
                        <View style={styles.dutyBadge}>
                          <Text style={styles.dutyBadgeText}>NÖBETÇİ</Text>
                        </View>
                      )}
                    </View>
                    <Text style={type.small} numberOfLines={1}>
                      {p.district}, {p.province}
                      {p.distanceKm != null ? `  •  ${p.distanceKm.toFixed(2)} km` : ''}
                    </Text>
                    {p.addressText && <Text style={[type.small, { marginTop: 2 }]} numberOfLines={1}>{p.addressText}</Text>}
                  </View>
                  {p.phoneNumber && (
                    <Pressable hitSlop={8} onPress={() => Linking.openURL(`tel:${p.phoneNumber}`)} style={styles.callBtn}>
                      <Ionicons name="call" size={18} color="#fff" />
                    </Pressable>
                  )}
                </Card>
              </Pressable>
            ))}
          </View>
        )}

        {!loading && pharmacies.length === 0 && !error && (
          <EmptyState icon="medkit" title="Eczane listelenmedi" subtitle="Konumunu paylaş veya İl/İlçe seçerek eczaneleri gör." />
        )}
        <View style={{ height: 12 }} />
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  content: { padding: 18, gap: 14 },
  modeToggle: { flexDirection: 'row', backgroundColor: colors.surfaceAlt, borderRadius: radius.md, padding: 4, gap: 4 },
  modeBtn: { flex: 1, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 6, paddingVertical: 11, borderRadius: radius.sm },
  modeBtnActive: { backgroundColor: colors.blue },
  modeText: { fontWeight: '700', color: colors.slate, fontSize: 13 },
  modeTextActive: { color: '#fff' },
  dutyToggle: { flexDirection: 'row', alignItems: 'center', gap: 10, backgroundColor: colors.surface, borderRadius: radius.md, paddingHorizontal: 14, paddingVertical: 12, borderWidth: 1, borderColor: colors.line },
  dutyToggleActive: { borderColor: '#cdeed6', backgroundColor: '#f2fbf5' },
  dutyText: { fontWeight: '700', color: colors.slate, fontSize: 14 },
  mapWrap: { height: 260, borderRadius: radius.lg, overflow: 'hidden', borderWidth: 1, borderColor: colors.lineStrong },
  item: { flexDirection: 'row', alignItems: 'center', gap: 12, padding: 14 },
  itemActive: { borderColor: colors.blue, borderWidth: 1.5 },
  itemIcon: { width: 42, height: 42, borderRadius: 14, alignItems: 'center', justifyContent: 'center' },
  itemHead: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  itemName: { flex: 1, fontSize: 15, fontWeight: '800', color: colors.ink },
  dutyBadge: { backgroundColor: colors.green, borderRadius: radius.pill, paddingHorizontal: 8, paddingVertical: 2 },
  dutyBadgeText: { color: '#fff', fontSize: 9.5, fontWeight: '800', letterSpacing: 0.5 },
  callBtn: { backgroundColor: colors.green, width: 38, height: 38, borderRadius: 19, alignItems: 'center', justifyContent: 'center' }
});
