import { Ionicons } from '@expo/vector-icons';
import { useRoute, type RouteProp } from '@react-navigation/native';
import { useEffect, useState } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import { GradientHeader } from '../components/Header';
import { Banner, Button, Card, EmptyState, Field, Loading, StatusBadge } from '../components/ui';
import { formatDateTime, statusColor, statusLabel } from '../lib/status';
import { trackComplaint, type TrackedComplaint } from '../services/api';
import { colors, radius, type } from '../theme';

export function TrackScreen() {
  const route = useRoute<RouteProp<Record<string, { code?: string } | undefined>, string>>();
  const [code, setCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [data, setData] = useState<TrackedComplaint | null>(null);

  useEffect(() => {
    const incoming = route.params?.code;
    if (incoming) {
      setCode(incoming);
      search(incoming);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [route.params?.code]);

  async function search(value = code) {
    const trimmed = value.trim();
    if (!trimmed) {
      setError('Lütfen takip kodunu girin.');
      return;
    }
    setLoading(true);
    setError(null);
    setData(null);
    const r = await trackComplaint(trimmed);
    setLoading(false);
    if (!r.success || !r.data) {
      setError(r.message ?? 'Bu takip koduna ait bir kayıt bulunamadı.');
      return;
    }
    setData(r.data);
  }

  return (
    <View style={{ flex: 1, backgroundColor: colors.bg }}>
      <GradientHeader title="Şikayet Takibi" subtitle="Takip kodunla durum sorgula" compact />
      <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
        <Card style={{ gap: 12 }}>
          <Field
            label="Takip Kodu"
            value={code}
            onChangeText={setCode}
            placeholder="Örn. ABC12345"
            autoCapitalize="characters"
            autoCorrect={false}
          />
          <Button title="Sorgula" icon="search" loading={loading} onPress={() => search()} />
        </Card>

        {loading && <Loading label="Sorgulanıyor..." />}
        {error && !loading && <Banner tone="error">{error}</Banner>}

        {data && (
          <>
            <Card style={{ gap: 12 }}>
              <View style={styles.detailHead}>
                <View style={{ flex: 1 }}>
                  <Text style={type.small}>{data.municipalityName} • {data.categoryName}</Text>
                  <Text style={[type.h2, { marginTop: 2 }]}>{data.title}</Text>
                </View>
                <StatusBadge status={data.status} />
              </View>
              <Text style={type.body}>{data.description}</Text>
              <View style={styles.metaRow}>
                <Meta icon="calendar" label="Oluşturma" value={formatDateTime(data.createdAt)} />
                {data.departmentName && <Meta icon="business" label="Birim" value={data.departmentName} />}
                {data.addressText && <Meta icon="location" label="Adres" value={data.addressText} />}
                {data.attachmentCount > 0 && <Meta icon="image" label="Fotoğraf" value={`${data.attachmentCount} adet`} />}
              </View>
            </Card>

            {data.responses.length > 0 && (
              <Card style={{ gap: 12 }}>
                <Text style={type.h3}>{data.isInstitution ? 'Kurum Yanıtları' : 'Belediye Yanıtları'}</Text>
                {data.responses.map((r, i) => (
                  <View key={i} style={styles.response}>
                    <Ionicons name="chatbubble-ellipses" size={18} color={colors.blue} />
                    <View style={{ flex: 1 }}>
                      <Text style={type.body}>{r.body}</Text>
                      <Text style={[type.small, { marginTop: 4 }]}>{formatDateTime(r.createdAt)}</Text>
                    </View>
                  </View>
                ))}
              </Card>
            )}

            <Card style={{ gap: 4 }}>
              <Text style={[type.h3, { marginBottom: 8 }]}>Durum Geçmişi</Text>
              {data.statusHistory.length === 0 ? (
                <Text style={type.small}>Henüz durum güncellemesi yok.</Text>
              ) : (
                data.statusHistory.map((entry, i) => {
                  const last = i === data.statusHistory.length - 1;
                  const color = statusColor(entry.newStatus);
                  return (
                    <View key={i} style={styles.timelineRow}>
                      <View style={styles.timelineLeft}>
                        <View style={[styles.timelineDot, { backgroundColor: color }]} />
                        {!last && <View style={styles.timelineLine} />}
                      </View>
                      <View style={{ flex: 1, paddingBottom: last ? 0 : 16 }}>
                        <Text style={[type.h3, { color }]}>{statusLabel(entry.newStatus)}</Text>
                        {entry.note && <Text style={[type.small, { marginTop: 2 }]}>{entry.note}</Text>}
                        <Text style={[type.small, { marginTop: 2 }]}>{formatDateTime(entry.createdAt)}</Text>
                      </View>
                    </View>
                  );
                })
              )}
            </Card>
          </>
        )}

        {!data && !loading && !error && (
          <EmptyState icon="search" title="Takip koduyla sorgula" subtitle="Bildirim sonunda verilen kodu girerek durumunu takip edebilirsin." />
        )}
        <View style={{ height: 12 }} />
      </ScrollView>
    </View>
  );
}

function Meta({ icon, label, value }: { icon: keyof typeof Ionicons.glyphMap; label: string; value: string }) {
  return (
    <View style={styles.meta}>
      <Ionicons name={icon} size={15} color={colors.muted} />
      <Text style={styles.metaLabel}>{label}:</Text>
      <Text style={styles.metaValue}>{value}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  content: { padding: 18, gap: 14 },
  detailHead: { flexDirection: 'row', alignItems: 'flex-start', gap: 10 },
  metaRow: { gap: 8, borderTopWidth: 1, borderTopColor: colors.line, paddingTop: 12 },
  meta: { flexDirection: 'row', alignItems: 'center', gap: 6, flexWrap: 'wrap' },
  metaLabel: { fontSize: 13, fontWeight: '700', color: colors.slate },
  metaValue: { fontSize: 13, fontWeight: '600', color: colors.ink, flex: 1 },
  response: { flexDirection: 'row', gap: 10, alignItems: 'flex-start', backgroundColor: colors.cyanSoft, borderRadius: radius.md, padding: 12 },
  timelineRow: { flexDirection: 'row', gap: 12 },
  timelineLeft: { alignItems: 'center', width: 14 },
  timelineDot: { width: 12, height: 12, borderRadius: 6, marginTop: 3 },
  timelineLine: { flex: 1, width: 2, backgroundColor: colors.line, marginTop: 2 },
});
