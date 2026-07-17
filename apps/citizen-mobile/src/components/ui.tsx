import { Ionicons } from '@expo/vector-icons';
import { ReactNode } from 'react';
import {
  ActivityIndicator,
  Pressable,
  StyleProp,
  StyleSheet,
  Text,
  TextInput,
  TextInputProps,
  View,
  ViewStyle
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { colors, gradients, radius, shadow, type } from '../theme';
import { statusColor, statusLabel } from '../lib/status';

export function Card({ children, style }: { children: ReactNode; style?: StyleProp<ViewStyle> }) {
  return <View style={[styles.card, shadow(10), style]}>{children}</View>;
}

export function SectionTitle({ children, icon }: { children: ReactNode; icon?: keyof typeof Ionicons.glyphMap }) {
  return (
    <View style={styles.sectionTitle}>
      {icon && <Ionicons name={icon} size={18} color={colors.blue} />}
      <Text style={type.h3}>{children}</Text>
    </View>
  );
}

type ButtonProps = {
  title: string;
  onPress?: () => void;
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger';
  loading?: boolean;
  disabled?: boolean;
  icon?: keyof typeof Ionicons.glyphMap;
  style?: StyleProp<ViewStyle>;
};

export function Button({ title, onPress, variant = 'primary', loading, disabled, icon, style }: ButtonProps) {
  const isDisabled = disabled || loading;
  const content = (
    <View style={styles.btnInner}>
      {loading ? (
        <ActivityIndicator color={variant === 'secondary' || variant === 'ghost' ? colors.blue : '#fff'} />
      ) : (
        <>
          {icon && (
            <Ionicons
              name={icon}
              size={18}
              color={variant === 'secondary' || variant === 'ghost' ? colors.blue : '#fff'}
            />
          )}
          <Text style={[styles.btnText, (variant === 'secondary' || variant === 'ghost') && { color: colors.blue }]}>
            {title}
          </Text>
        </>
      )}
    </View>
  );

  if (variant === 'primary') {
    return (
      <Pressable onPress={onPress} disabled={isDisabled} style={[styles.btnBase, isDisabled && styles.btnDisabled, style]}>
        <LinearGradient colors={gradients.cyan} start={{ x: 0, y: 0 }} end={{ x: 1, y: 1 }} style={styles.btnGradient}>
          {content}
        </LinearGradient>
      </Pressable>
    );
  }
  if (variant === 'danger') {
    return (
      <Pressable
        onPress={onPress}
        disabled={isDisabled}
        style={[styles.btnBase, styles.btnSolid, { backgroundColor: colors.red }, isDisabled && styles.btnDisabled, style]}
      >
        {content}
      </Pressable>
    );
  }
  return (
    <Pressable
      onPress={onPress}
      disabled={isDisabled}
      style={[
        styles.btnBase,
        variant === 'secondary' ? styles.btnSecondary : styles.btnGhost,
        isDisabled && styles.btnDisabled,
        style
      ]}
    >
      {content}
    </Pressable>
  );
}

export function Field({
  label,
  hint,
  ...inputProps
}: { label?: string; hint?: string } & TextInputProps) {
  return (
    <View style={{ gap: 6 }}>
      {label && <Text style={type.label}>{label}</Text>}
      <TextInput
        placeholderTextColor={colors.muted}
        style={[styles.input, inputProps.multiline && styles.inputMultiline]}
        {...inputProps}
      />
      {hint && <Text style={type.small}>{hint}</Text>}
    </View>
  );
}

export function Chip({ label, active, onPress }: { label: string; active?: boolean; onPress?: () => void }) {
  return (
    <Pressable onPress={onPress} style={[styles.chip, active && styles.chipActive]}>
      <Text style={[styles.chipText, active && styles.chipTextActive]}>{label}</Text>
    </Pressable>
  );
}

export function StatusBadge({ status }: { status: string }) {
  const color = statusColor(status);
  return (
    <View style={[styles.badge, { backgroundColor: `${color}1a` }]}>
      <View style={[styles.dot, { backgroundColor: color }]} />
      <Text style={[styles.badgeText, { color }]}>{statusLabel(status)}</Text>
    </View>
  );
}

export function Banner({ tone, children }: { tone: 'error' | 'success' | 'info'; children: ReactNode }) {
  const map = {
    error: { bg: colors.redSoft, fg: colors.red, icon: 'alert-circle' as const },
    success: { bg: colors.greenSoft, fg: colors.green, icon: 'checkmark-circle' as const },
    info: { bg: colors.cyanSoft, fg: colors.blue, icon: 'information-circle' as const }
  }[tone];
  return (
    <View style={[styles.banner, { backgroundColor: map.bg }]}>
      <Ionicons name={map.icon} size={18} color={map.fg} />
      <Text style={[styles.bannerText, { color: map.fg }]}>{children}</Text>
    </View>
  );
}

export function EmptyState({
  icon,
  title,
  subtitle
}: {
  icon: keyof typeof Ionicons.glyphMap;
  title: string;
  subtitle?: string;
}) {
  return (
    <View style={styles.empty}>
      <View style={styles.emptyIcon}>
        <Ionicons name={icon} size={30} color={colors.blue} />
      </View>
      <Text style={type.h3}>{title}</Text>
      {subtitle && <Text style={[type.small, { textAlign: 'center' }]}>{subtitle}</Text>}
    </View>
  );
}

export function Loading({ label }: { label?: string }) {
  return (
    <View style={styles.loading}>
      <ActivityIndicator color={colors.blue} />
      {label && <Text style={type.small}>{label}</Text>}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderRadius: radius.lg,
    padding: 18,
    borderWidth: 1,
    borderColor: colors.line
  },
  sectionTitle: { flexDirection: 'row', alignItems: 'center', gap: 8, marginBottom: 4 },
  btnBase: { borderRadius: radius.md, overflow: 'hidden' },
  btnGradient: { paddingVertical: 15, paddingHorizontal: 20 },
  btnSolid: { paddingVertical: 15, paddingHorizontal: 20 },
  btnSecondary: { backgroundColor: colors.cyanSoft, paddingVertical: 14, paddingHorizontal: 20, borderWidth: 1, borderColor: '#cfe6f6' },
  btnGhost: { backgroundColor: 'transparent', paddingVertical: 12, paddingHorizontal: 16 },
  btnDisabled: { opacity: 0.5 },
  btnInner: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8 },
  btnText: { color: '#fff', fontSize: 15, fontWeight: '800', letterSpacing: 0.2 },
  input: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.lineStrong,
    borderRadius: radius.md,
    paddingHorizontal: 14,
    paddingVertical: 13,
    fontSize: 15,
    color: colors.ink
  },
  inputMultiline: { minHeight: 110, textAlignVertical: 'top' },
  chip: {
    borderWidth: 1,
    borderColor: colors.lineStrong,
    backgroundColor: colors.surface,
    borderRadius: radius.pill,
    paddingHorizontal: 15,
    paddingVertical: 9
  },
  chipActive: { backgroundColor: colors.navy, borderColor: colors.navy },
  chipText: { color: colors.slate, fontWeight: '700', fontSize: 13 },
  chipTextActive: { color: '#fff' },
  badge: { flexDirection: 'row', alignItems: 'center', gap: 6, borderRadius: radius.pill, paddingHorizontal: 10, paddingVertical: 5, alignSelf: 'flex-start' },
  dot: { width: 7, height: 7, borderRadius: 4 },
  badgeText: { fontSize: 12, fontWeight: '800' },
  banner: { flexDirection: 'row', gap: 8, alignItems: 'flex-start', borderRadius: radius.md, padding: 12 },
  bannerText: { flex: 1, fontSize: 13, fontWeight: '600', lineHeight: 18 },
  empty: { alignItems: 'center', gap: 8, paddingVertical: 36 },
  emptyIcon: { width: 62, height: 62, borderRadius: 31, backgroundColor: colors.cyanSoft, alignItems: 'center', justifyContent: 'center', marginBottom: 4 },
  loading: { alignItems: 'center', gap: 10, paddingVertical: 30 }
});
