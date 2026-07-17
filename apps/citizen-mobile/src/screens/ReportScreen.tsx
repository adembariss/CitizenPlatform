import { Ionicons } from '@expo/vector-icons';
import * as ImagePicker from 'expo-image-picker';
import { useEffect, useRef, useState } from 'react';
import { Image, Platform, Pressable, ScrollView, StyleSheet, Switch, Text, View } from 'react-native';
import MapView, { Marker, PROVIDER_GOOGLE, type Region } from 'react-native-maps';
import { GradientHeader } from '../components/Header';
import { SelectField } from '../components/PickerModal';
import { Banner, Button, Card, Chip, Field, Loading, SectionTitle } from '../components/ui';
import { useAuth } from '../context/AuthContext';
import { getCurrentPosition } from '../services/location';
import {
  addComplaintAttachments,
  createComplaint,
  createComplaintAuthed,
  createComplaintWithPhotos,
  getDistricts,
  getInstitutionCategories,
  getInstitutions,
  getMunicipalityCategories,
  getProvinces,
  resolveMunicipality,
  type District,
  type Institution,
  type PhotoAsset,
  type PublicCategory
} from '../services/api';
import { colors, radius, shadow, type } from '../theme';

const MAX_PHOTOS = 5;
const TURKEY_REGION: Region = { latitude: 39.0, longitude: 35.2, latitudeDelta: 8, longitudeDelta: 8 };

function institutionEmoji(type: string): string {
  if (type === 'Electricity') return '⚡';
  if (type === 'Water') return '💧';
  if (type === 'NaturalGas') return '🔥';
  return '🏢';
}

type Nav = { navigate: (screen: string, params?: object) => void };
type Mode = 'map' | 'address';
type Muni = { id: string; name: string } | null;

export function ReportScreen({ navigation }: { navigation: Nav }) {
  const { isAuthenticated, token, user } = useAuth();
  const mapRef = useRef<MapView | null>(null);

  const [mode, setMode] = useState<Mode>('map');
  const [position, setPosition] = useState<{ latitude: number; longitude: number } | null>(null);
  const [locating, setLocating] = useState(false);
  const [muni, setMuni] = useState<Muni>(null);
  const [resolving, setResolving] = useState(false);
  const [muniError, setMuniError] = useState<string | null>(null);

  const [provinces, setProvinces] = useState<string[]>([]);
  const [province, setProvince] = useState<string | null>(null);
  const [districts, setDistricts] = useState<District[]>([]);
  const [districtId, setDistrictId] = useState<string | null>(null);

  // Kime bildireceği: null = belediye, aksi halde seçilen dağıtım kurumunun id'si.
  const [institutions, setInstitutions] = useState<Institution[]>([]);
  const [targetInstitutionId, setTargetInstitutionId] = useState<string | null>(null);

  const [categories, setCategories] = useState<PublicCategory[]>([]);
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [loadingCategories, setLoadingCategories] = useState(false);

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [fullName, setFullName] = useState('');
  const [anonymous, setAnonymous] = useState(false);
  const [photos, setPhotos] = useState<PhotoAsset[]>([]);

  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    getProvinces().then((r) => r.data && setProvinces(r.data));
  }, []);

  // Harita modunda konum değişince belediyeyi çöz.
  useEffect(() => {
    if (mode !== 'map' || !position) return;
    let cancelled = false;
    setResolving(true);
    setMuniError(null);
    resolveMunicipality(position.latitude, position.longitude).then((r) => {
      if (cancelled) return;
      setResolving(false);
      if (!r.data?.isSuccess || !r.data.municipalityId || !r.data.municipalityName) {
        setMuni(null);
        setMuniError(r.data?.failureReason ?? 'Bu konum için hizmet veren bir belediye bulunamadı.');
        return;
      }
      setMuni({ id: r.data.municipalityId, name: r.data.municipalityName });
    });
    return () => {
      cancelled = true;
    };
  }, [position, mode]);

  // Belediye belirlenince o bölgedeki kurumları getir ve hedefi belediyeye sıfırla.
  useEffect(() => {
    setInstitutions([]);
    setTargetInstitutionId(null);
    if (!muni) return;
    let cancelled = false;
    getInstitutions({ municipalityId: muni.id }).then((r) => {
      if (!cancelled && r.data) setInstitutions(r.data);
    });
    return () => {
      cancelled = true;
    };
  }, [muni]);

  // Kategoriler: hedef belediye ise belediye kategorileri, kurum ise kurum kategorileri.
  useEffect(() => {
    if (!muni) {
      setCategories([]);
      setCategoryId(null);
      return;
    }
    let cancelled = false;
    setLoadingCategories(true);
    const loader = targetInstitutionId
      ? getInstitutionCategories(targetInstitutionId)
      : getMunicipalityCategories(muni.id);
    loader.then((r) => {
      if (cancelled) return;
      setLoadingCategories(false);
      const list = r.data ?? [];
      setCategories(list);
      setCategoryId((current) => (list.some((c) => c.id === current) ? current : list[0]?.id ?? null));
    });
    return () => {
      cancelled = true;
    };
  }, [muni, targetInstitutionId]);

  function switchMode(next: Mode) {
    setMode(next);
    setPosition(null);
    setMuni(null);
    setMuniError(null);
    setProvince(null);
    setDistricts([]);
    setDistrictId(null);
  }

  async function useMyLocation() {
    setLocating(true);
    setMuniError(null);
    try {
      const current = await getCurrentPosition();
      setPosition(current);
      mapRef.current?.animateToRegion({ ...current, latitudeDelta: 0.02, longitudeDelta: 0.02 }, 600);
    } catch (e) {
      setMuniError(e instanceof Error ? e.message : 'Konum alınamadı.');
    } finally {
      setLocating(false);
    }
  }

  function onProvince(value: string) {
    setProvince(value);
    setDistrictId(null);
    setDistricts([]);
    setMuni(null);
    setPosition(null);
    getDistricts(value).then((r) => r.data && setDistricts(r.data));
  }

  function onDistrict(value: string) {
    setDistrictId(value);
    const d = districts.find((item) => item.id === value);
    if (!d) return;
    setMuni({ id: d.id, name: d.name });
    if (d.latitude != null && d.longitude != null) {
      setPosition({ latitude: d.latitude, longitude: d.longitude });
    }
  }

  async function pickPhotos() {
    if (photos.length >= MAX_PHOTOS) return;
    const perm = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (!perm.granted) {
      setFormError('Fotoğraf eklemek için galeri izni gerekiyor.');
      return;
    }
    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.Images,
      allowsMultipleSelection: true,
      selectionLimit: MAX_PHOTOS - photos.length,
      quality: 0.6
    });
    if (result.canceled) return;
    const mapped: PhotoAsset[] = result.assets.map((a, i) => ({
      uri: a.uri,
      name: a.fileName ?? `foto_${Date.now()}_${i}.jpg`,
      type: a.mimeType ?? 'image/jpeg'
    }));
    setPhotos((current) => [...current, ...mapped].slice(0, MAX_PHOTOS));
  }

  function removePhoto(index: number) {
    setPhotos((current) => current.filter((_, i) => i !== index));
  }

  async function submit() {
    setFormError(null);
    if (!position || !muni) {
      setFormError('Önce konumunu seç (harita veya İl/İlçe).');
      return;
    }
    if (!categoryId) {
      setFormError('Lütfen bir kategori seç.');
      return;
    }
    if (!description.trim()) {
      setFormError('Açıklama zorunludur.');
      return;
    }

    setSubmitting(true);
    try {
      // Kurum (elektrik/su/doğalgaz) şikayeti: public uç + institutionId; konum belediyesi kayıtta kalır.
      if (targetInstitutionId) {
        const institutionRequest = {
          categoryId,
          title: title || undefined,
          description,
          citizenFullName: anonymous ? undefined : fullName || undefined,
          latitude: position.latitude,
          longitude: position.longitude,
          isAnonymous: anonymous,
          source: 'CitizenMobile' as const,
          municipalityId: muni.id,
          institutionId: targetInstitutionId
        };
        const res =
          photos.length > 0
            ? await createComplaintWithPhotos(institutionRequest, photos)
            : await createComplaint(institutionRequest);
        if (!res.success || !res.data) {
          setFormError(res.message ?? 'Bildirim gönderilemedi.');
          return;
        }
        setSuccess(res.data.trackingCode);
        return;
      }

      if (isAuthenticated && token) {
        const res = await createComplaintAuthed(token, {
          categoryId,
          title: title || undefined,
          description,
          latitude: position.latitude,
          longitude: position.longitude,
          municipalityId: mode === 'address' ? muni.id : undefined
        });
        if (!res.success || !res.data) {
          setFormError(res.message ?? 'Bildirim gönderilemedi.');
          return;
        }
        if (photos.length > 0) await addComplaintAttachments(res.data.trackingCode, photos).catch(() => undefined);
        setSuccess(res.data.trackingCode);
        return;
      }

      const request = {
        categoryId,
        title: title || undefined,
        description,
        citizenFullName: anonymous ? undefined : fullName || undefined,
        latitude: position.latitude,
        longitude: position.longitude,
        isAnonymous: anonymous,
        source: 'CitizenMobile' as const,
        municipalityId: mode === 'address' ? muni.id : undefined
      };
      const res = photos.length > 0 ? await createComplaintWithPhotos(request, photos) : await createComplaint(request);
      if (!res.success || !res.data) {
        setFormError(res.message ?? 'Bildirim gönderilemedi.');
        return;
      }
      setSuccess(res.data.trackingCode);
    } finally {
      setSubmitting(false);
    }
  }

  function reset() {
    setSuccess(null);
    setTitle('');
    setDescription('');
    setPhotos([]);
    setPosition(null);
    setMuni(null);
    setProvince(null);
    setDistrictId(null);
    setDistricts([]);
  }

  if (success) {
    return (
      <View style={{ flex: 1, backgroundColor: colors.bg }}>
        <GradientHeader title="Bildirim Alındı" subtitle="Teşekkürler, talebin kaydedildi" compact />
        <ScrollView contentContainerStyle={styles.content}>
          <Card style={{ alignItems: 'center', gap: 12, paddingVertical: 28 }}>
            <View style={styles.successIcon}>
              <Ionicons name="checkmark" size={36} color="#fff" />
            </View>
            <Text style={type.h2}>Talebin belediyeye iletildi</Text>
            <Text style={[type.small, { textAlign: 'center' }]}>Aşağıdaki takip kodu ile durumunu her zaman sorgulayabilirsin.</Text>
            <View style={styles.codeBox}>
              <Text style={styles.codeText}>{success}</Text>
            </View>
            <Button title="Şikayetimi takip et" icon="search" onPress={() => navigation.navigate('Takip', { code: success })} style={{ alignSelf: 'stretch' }} />
            <Button title="Yeni bildirim oluştur" variant="secondary" onPress={reset} style={{ alignSelf: 'stretch' }} />
          </Card>
        </ScrollView>
      </View>
    );
  }

  return (
    <View style={{ flex: 1, backgroundColor: colors.bg }}>
      <GradientHeader title="Şikayet Bildir" subtitle="Mahallendeki sorunu ilet" compact />
      <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
        <Card style={{ gap: 14 }}>
          <SectionTitle icon="location">Konum</SectionTitle>
          <View style={styles.modeToggle}>
            <Pressable style={[styles.modeBtn, mode === 'map' && styles.modeBtnActive]} onPress={() => switchMode('map')}>
              <Ionicons name="map" size={16} color={mode === 'map' ? '#fff' : colors.slate} />
              <Text style={[styles.modeText, mode === 'map' && styles.modeTextActive]}>Haritadan</Text>
            </Pressable>
            <Pressable style={[styles.modeBtn, mode === 'address' && styles.modeBtnActive]} onPress={() => switchMode('address')}>
              <Ionicons name="list" size={16} color={mode === 'address' ? '#fff' : colors.slate} />
              <Text style={[styles.modeText, mode === 'address' && styles.modeTextActive]}>İl / İlçe</Text>
            </Pressable>
          </View>

          {mode === 'map' ? (
            <>
              <View style={styles.mapWrap}>
                <MapView
                  ref={mapRef}
                  provider={Platform.OS === 'android' ? PROVIDER_GOOGLE : undefined}
                  style={StyleSheet.absoluteFill}
                  initialRegion={TURKEY_REGION}
                  onPress={(e) => setPosition(e.nativeEvent.coordinate)}
                >
                  {position && <Marker coordinate={position} pinColor={colors.blue} />}
                </MapView>
              </View>
              <Button
                title={locating ? 'Konum alınıyor...' : 'Konumumu kullan'}
                icon="navigate"
                variant="secondary"
                loading={locating}
                onPress={useMyLocation}
              />
              <Text style={type.small}>
                {position
                  ? `Seçilen konum: ${position.latitude.toFixed(5)}, ${position.longitude.toFixed(5)}`
                  : 'Haritaya dokunarak da konum seçebilirsin.'}
              </Text>
            </>
          ) : (
            <View style={{ gap: 12 }}>
              <SelectField
                label="İl"
                placeholder="İl seçin"
                value={province}
                searchable
                options={provinces.map((p) => ({ label: p, value: p }))}
                onSelect={onProvince}
              />
              <SelectField
                label="İlçe / Belediye"
                placeholder={province ? 'İlçe seçin' : 'Önce il seçin'}
                value={districtId}
                disabled={!province || districts.length === 0}
                searchable
                options={districts.map((d) => ({ label: d.name, value: d.id }))}
                onSelect={onDistrict}
              />
            </View>
          )}

          {resolving && <Text style={[type.small, { color: colors.blue }]}>Belediye sorgulanıyor...</Text>}
          {muni && (
            <View style={styles.muniPill}>
              <Ionicons name="location" size={16} color={colors.green} />
              <Text style={styles.muniText}>Konumun: <Text style={{ fontWeight: '800' }}>{muni.name}</Text></Text>
            </View>
          )}
          {muni && institutions.length > 0 && (
            <View style={{ gap: 8 }}>
              <Text style={type.label}>Kime bildireceksin?</Text>
              <View style={styles.targetWrap}>
                <Pressable
                  style={[styles.targetChip, targetInstitutionId === null && styles.targetChipActive]}
                  onPress={() => setTargetInstitutionId(null)}
                >
                  <Text style={styles.targetEmoji}>🏛</Text>
                  <View style={{ flex: 1, minWidth: 0 }}>
                    <Text style={[styles.targetName, targetInstitutionId === null && styles.targetTextActive]} numberOfLines={1}>{muni.name}</Text>
                    <Text style={styles.targetLabel}>Belediye</Text>
                  </View>
                </Pressable>
                {institutions.map((inst) => (
                  <Pressable
                    key={inst.id}
                    style={[styles.targetChip, targetInstitutionId === inst.id && styles.targetChipActive]}
                    onPress={() => setTargetInstitutionId(inst.id)}
                  >
                    <Text style={styles.targetEmoji}>{institutionEmoji(inst.type)}</Text>
                    <View style={{ flex: 1, minWidth: 0 }}>
                      <Text style={[styles.targetName, targetInstitutionId === inst.id && styles.targetTextActive]} numberOfLines={1}>{inst.name}</Text>
                      <Text style={styles.targetLabel}>{inst.typeLabel}</Text>
                    </View>
                  </Pressable>
                ))}
              </View>
            </View>
          )}
          {muniError && <Banner tone="error">{muniError}</Banner>}
        </Card>

        <Card style={{ gap: 14 }}>
          <SectionTitle icon="create">Şikayet Detayı</SectionTitle>
          <Field label="Başlık (opsiyonel)" value={title} onChangeText={setTitle} placeholder="Örn. Kaldırım hasarı" />

          <View style={{ gap: 8 }}>
            <Text style={type.label}>Kategori</Text>
            {loadingCategories ? (
              <Loading label="Kategoriler yükleniyor..." />
            ) : categories.length === 0 ? (
              <Text style={type.small}>Kategorileri görmek için önce konum seç.</Text>
            ) : (
              <View style={styles.chipWrap}>
                {categories.map((c) => (
                  <Chip key={c.id} label={c.name} active={categoryId === c.id} onPress={() => setCategoryId(c.id)} />
                ))}
              </View>
            )}
          </View>

          <Field
            label="Açıklama"
            value={description}
            onChangeText={setDescription}
            placeholder="Sorunu kısaca anlat"
            multiline
          />

          <View style={{ gap: 8 }}>
            <Text style={type.label}>Fotoğraflar (en fazla {MAX_PHOTOS})</Text>
            <View style={styles.photoRow}>
              {photos.map((p, i) => (
                <View key={p.uri} style={styles.photoThumb}>
                  <Image source={{ uri: p.uri }} style={styles.photoImg} />
                  <Pressable style={styles.photoRemove} onPress={() => removePhoto(i)} hitSlop={6}>
                    <Ionicons name="close" size={14} color="#fff" />
                  </Pressable>
                </View>
              ))}
              {photos.length < MAX_PHOTOS && (
                <Pressable style={styles.photoAdd} onPress={pickPhotos}>
                  <Ionicons name="camera" size={22} color={colors.blue} />
                  <Text style={styles.photoAddText}>Ekle</Text>
                </Pressable>
              )}
            </View>
          </View>
        </Card>

        <Card style={{ gap: 12 }}>
          {isAuthenticated ? (
            <View style={styles.accountHint}>
              <Ionicons name="person-circle" size={20} color={colors.blue} />
              <Text style={[type.small, { flex: 1 }]}>
                <Text style={{ fontWeight: '800', color: colors.ink }}>{user?.fullName ?? 'Hesabınla'}</Text> gönderiyorsun — bu bildirim
                “Şikayetlerim” sayfanda görünecek.
              </Text>
            </View>
          ) : (
            <>
              <View style={styles.switchRow}>
                <View style={{ flex: 1 }}>
                  <Text style={type.h3}>Anonim gönder</Text>
                  <Text style={type.small}>Ad soyad paylaşmadan bildir.</Text>
                </View>
                <Switch
                  value={anonymous}
                  onValueChange={setAnonymous}
                  trackColor={{ true: colors.blue, false: colors.lineStrong }}
                  thumbColor="#fff"
                />
              </View>
              {!anonymous && (
                <Field label="Ad Soyad (opsiyonel)" value={fullName} onChangeText={setFullName} placeholder="Ayşe Yılmaz" />
              )}
            </>
          )}
        </Card>

        {formError && <Banner tone="error">{formError}</Banner>}

        <Button
          title={submitting ? 'Gönderiliyor...' : 'Bildirim Oluştur'}
          icon="send"
          loading={submitting}
          disabled={!muni}
          onPress={submit}
        />
        <View style={{ height: 12 }} />
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  content: { padding: 18, gap: 16 },
  modeToggle: { flexDirection: 'row', backgroundColor: colors.surfaceAlt, borderRadius: radius.md, padding: 4, gap: 4 },
  modeBtn: { flex: 1, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 6, paddingVertical: 10, borderRadius: radius.sm },
  modeBtnActive: { backgroundColor: colors.blue },
  modeText: { fontWeight: '700', color: colors.slate, fontSize: 13 },
  modeTextActive: { color: '#fff' },
  mapWrap: { height: 240, borderRadius: radius.md, overflow: 'hidden', borderWidth: 1, borderColor: colors.lineStrong },
  muniPill: { flexDirection: 'row', alignItems: 'center', gap: 8, backgroundColor: colors.greenSoft, borderRadius: radius.md, paddingHorizontal: 12, paddingVertical: 10 },
  targetWrap: { gap: 8 },
  targetChip: { flexDirection: 'row', alignItems: 'center', gap: 10, padding: 11, borderWidth: 1, borderColor: colors.line, borderRadius: radius.md, backgroundColor: colors.surface },
  targetChipActive: { borderColor: colors.blue, backgroundColor: colors.cyanSoft },
  targetEmoji: { fontSize: 20 },
  targetName: { fontSize: 13.5, fontWeight: '800', color: colors.ink },
  targetTextActive: { color: colors.blue },
  targetLabel: { fontSize: 11.5, fontWeight: '700', color: colors.muted },
  muniText: { flex: 1, color: colors.ink, fontSize: 13, fontWeight: '600' },
  chipWrap: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  photoRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  photoThumb: { width: 74, height: 74, borderRadius: radius.md, overflow: 'hidden' },
  photoImg: { width: '100%', height: '100%' },
  photoRemove: { position: 'absolute', top: 3, right: 3, backgroundColor: 'rgba(13,26,68,0.7)', borderRadius: 10, width: 20, height: 20, alignItems: 'center', justifyContent: 'center' },
  photoAdd: { width: 74, height: 74, borderRadius: radius.md, borderWidth: 1.5, borderStyle: 'dashed', borderColor: colors.blue, alignItems: 'center', justifyContent: 'center', gap: 2, backgroundColor: colors.cyanSoft },
  photoAddText: { color: colors.blue, fontSize: 12, fontWeight: '700' },
  accountHint: { flexDirection: 'row', gap: 10, alignItems: 'flex-start' },
  switchRow: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  successIcon: { width: 72, height: 72, borderRadius: 36, backgroundColor: colors.green, alignItems: 'center', justifyContent: 'center', ...shadow(10) },
  codeBox: { backgroundColor: colors.navy, borderRadius: radius.md, paddingVertical: 16, paddingHorizontal: 28, marginVertical: 4 },
  codeText: { color: colors.cyan, fontSize: 26, fontWeight: '800', letterSpacing: 3 }
});
