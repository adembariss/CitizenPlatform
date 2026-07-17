import { Ionicons } from '@expo/vector-icons';
import { useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { GradientHeader } from '../components/Header';
import { Card } from '../components/ui';
import { useAuth } from '../context/AuthContext';
import { getPublicStats, type PublicStats } from '../services/api';
import { colors, gradients, radius, shadow, type } from '../theme';

type Nav = { navigate: (screen: string) => void };

const QUICK = [
  { key: 'Bildir', label: 'Şikayet Bildir', icon: 'megaphone', tint: colors.blue, desc: 'Sorunu belediyene ilet' },
  { key: 'Eczaneler', label: 'Nöbetçi Eczane', icon: 'medkit', tint: colors.green, desc: 'En yakın açık eczane' },
  { key: 'Takip', label: 'Takip Et', icon: 'search', tint: colors.cyan, desc: 'Kodla durum sorgula' },
  { key: 'Hesap', label: 'Şikayetlerim', icon: 'albums', tint: colors.amber, desc: 'Geçmiş başvuruların' }
] as const;

export function HomeScreen({ navigation }: { navigation: Nav }) {
  const { isAuthenticated, user } = useAuth();
  const [stats, setStats] = useState<PublicStats | null>(null);

  useEffect(() => {
    getPublicStats().then((r) => r.data && setStats(r.data));
  }, []);

  const statTiles = [
    { label: 'Toplam Başvuru', value: stats?.totalComplaints, icon: 'documents' as const },
    { label: 'Çözülen', value: stats?.resolvedComplaints, icon: 'checkmark-done' as const },
    { label: 'Belediye', value: stats?.activeMunicipalities, icon: 'business' as const },
    { label: 'Kategori', value: stats?.categories, icon: 'pricetags' as const }
  ];

  return (
    <View style={{ flex: 1, backgroundColor: colors.bg }}>
      <GradientHeader
        title="Belediyem"
        subtitle={isAuthenticated && user ? `Merhaba, ${user.fullName.split(' ')[0]} 👋` : 'Şehrini birlikte iyileştirelim'}
      />
      <ScrollView contentContainerStyle={styles.content} showsVerticalScrollIndicator={false}>
        <Pressable onPress={() => navigation.navigate('Bildir')}>
          <LinearGradient colors={gradients.brand} start={{ x: 0, y: 0 }} end={{ x: 1, y: 1 }} style={[styles.hero, shadow(14)]}>
            <View style={styles.heroBubble} />
            <View style={styles.heroBubble2} />
            <Text style={styles.heroEyebrow}>VATANDAŞ HİZMETİ</Text>
            <Text style={styles.heroTitle}>Mahallendeki sorunu{'\n'}30 saniyede bildir.</Text>
            <View style={styles.heroCta}>
              <Text style={styles.heroCtaText}>Hemen bildir</Text>
              <Ionicons name="arrow-forward" size={18} color={colors.navy} />
            </View>
          </LinearGradient>
        </Pressable>

        <View style={styles.statGrid}>
          {statTiles.map((tile) => (
            <View key={tile.label} style={[styles.statTile, shadow(6)]}>
              <Ionicons name={tile.icon} size={18} color={colors.blue} />
              <Text style={styles.statValue}>{tile.value ?? '—'}</Text>
              <Text style={styles.statLabel}>{tile.label}</Text>
            </View>
          ))}
        </View>

        <Text style={[type.h3, { marginTop: 4 }]}>Neye ihtiyacın var?</Text>
        <View style={styles.quickGrid}>
          {QUICK.map((item) => (
            <Pressable key={item.key} style={{ width: '48%' }} onPress={() => navigation.navigate(item.key)}>
              <Card style={styles.quickCard}>
                <View style={[styles.quickIcon, { backgroundColor: `${item.tint}1a` }]}>
                  <Ionicons name={item.icon as keyof typeof Ionicons.glyphMap} size={22} color={item.tint} />
                </View>
                <Text style={styles.quickLabel}>{item.label}</Text>
                <Text style={type.small}>{item.desc}</Text>
              </Card>
            </Pressable>
          ))}
        </View>

        <Card style={styles.infoCard}>
          <Ionicons name="shield-checkmark" size={22} color={colors.green} />
          <View style={{ flex: 1 }}>
            <Text style={type.h3}>Konumundan doğru belediyeye</Text>
            <Text style={[type.small, { marginTop: 2 }]}>
              Bildirimin, seçtiğin konuma göre yetkili belediyeye otomatik iletilir. İstersen anonim de gönderebilirsin.
            </Text>
          </View>
        </Card>
        <View style={{ height: 12 }} />
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  content: { padding: 18, gap: 16 },
  hero: { borderRadius: radius.xl, padding: 22, overflow: 'hidden' },
  heroBubble: { position: 'absolute', right: -30, top: -30, width: 130, height: 130, borderRadius: 65, backgroundColor: 'rgba(34,184,232,0.22)' },
  heroBubble2: { position: 'absolute', right: 40, bottom: -40, width: 90, height: 90, borderRadius: 45, backgroundColor: 'rgba(255,255,255,0.08)' },
  heroEyebrow: { color: '#8fc4f0', fontSize: 12, fontWeight: '800', letterSpacing: 1.2 },
  heroTitle: { color: '#fff', fontSize: 23, fontWeight: '800', marginTop: 8, lineHeight: 30 },
  heroCta: { flexDirection: 'row', alignItems: 'center', gap: 8, backgroundColor: colors.cyan, alignSelf: 'flex-start', paddingHorizontal: 16, paddingVertical: 11, borderRadius: radius.pill, marginTop: 18 },
  heroCtaText: { color: colors.navy, fontWeight: '800', fontSize: 14 },
  statGrid: { flexDirection: 'row', gap: 10 },
  statTile: { flex: 1, backgroundColor: colors.surface, borderRadius: radius.md, paddingVertical: 14, paddingHorizontal: 8, alignItems: 'center', gap: 3, borderWidth: 1, borderColor: colors.line },
  statValue: { fontSize: 19, fontWeight: '800', color: colors.ink },
  statLabel: { fontSize: 10.5, fontWeight: '600', color: colors.muted, textAlign: 'center' },
  quickGrid: { flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'space-between', gap: 12 },
  quickCard: { padding: 16, gap: 6, minHeight: 118 },
  quickIcon: { width: 44, height: 44, borderRadius: 14, alignItems: 'center', justifyContent: 'center', marginBottom: 4 },
  quickLabel: { fontSize: 15, fontWeight: '800', color: colors.ink },
  infoCard: { flexDirection: 'row', gap: 12, alignItems: 'flex-start', backgroundColor: '#f0f9f3', borderColor: '#d5efdd' }
});
