import { ReactNode } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { colors, gradients, radius } from '../theme';
import { BrandMark } from './BrandMark';

// Ekranların üstünde lacivert gradyanlı marka başlığı.
export function GradientHeader({
  title,
  subtitle,
  right,
  compact
}: {
  title: string;
  subtitle?: string;
  right?: ReactNode;
  compact?: boolean;
}) {
  const insets = useSafeAreaInsets();
  return (
    <LinearGradient
      colors={gradients.hero}
      start={{ x: 0, y: 0 }}
      end={{ x: 1, y: 1 }}
      style={[styles.wrap, { paddingTop: insets.top + (compact ? 10 : 16) }]}
    >
      <View style={styles.row}>
        <View style={styles.brandRow}>
          <View style={styles.mark}>
            <BrandMark size={30} />
          </View>
          <View style={{ flex: 1 }}>
            <Text style={styles.title}>{title}</Text>
            {subtitle && <Text style={styles.subtitle}>{subtitle}</Text>}
          </View>
        </View>
        {right}
      </View>
    </LinearGradient>
  );
}

const styles = StyleSheet.create({
  wrap: {
    paddingHorizontal: 18,
    paddingBottom: 18,
    borderBottomLeftRadius: radius.xl,
    borderBottomRightRadius: radius.xl
  },
  row: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 12 },
  brandRow: { flexDirection: 'row', alignItems: 'center', gap: 12, flex: 1 },
  mark: { backgroundColor: 'rgba(255,255,255,0.14)', borderRadius: radius.md, padding: 6 },
  title: { color: colors.white, fontSize: 20, fontWeight: '800', letterSpacing: -0.2 },
  subtitle: { color: '#bcd4f5', fontSize: 13, fontWeight: '500', marginTop: 2 }
});
