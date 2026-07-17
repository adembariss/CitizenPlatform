import { Ionicons } from '@expo/vector-icons';
import { useMemo, useState } from 'react';
import { FlatList, Modal, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { colors, radius, type } from '../theme';

export type PickerOption = { label: string; value: string };

type SelectFieldProps = {
  label: string;
  placeholder: string;
  value: string | null;
  options: PickerOption[];
  onSelect: (value: string) => void;
  disabled?: boolean;
  searchable?: boolean;
};

export function SelectField({
  label,
  placeholder,
  value,
  options,
  onSelect,
  disabled,
  searchable
}: SelectFieldProps) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');

  const selectedLabel = useMemo(
    () => options.find((option) => option.value === value)?.label ?? null,
    [options, value]
  );

  const filtered = useMemo(() => {
    if (!query.trim()) return options;
    const q = query.toLocaleLowerCase('tr-TR');
    return options.filter((option) => option.label.toLocaleLowerCase('tr-TR').includes(q));
  }, [options, query]);

  return (
    <View style={{ gap: 6 }}>
      <Text style={type.label}>{label}</Text>
      <Pressable
        onPress={() => !disabled && setOpen(true)}
        style={[styles.trigger, disabled && styles.disabled]}
      >
        <Text style={[styles.triggerText, !selectedLabel && { color: colors.muted }]} numberOfLines={1}>
          {selectedLabel ?? placeholder}
        </Text>
        <Ionicons name="chevron-down" size={18} color={colors.slate} />
      </Pressable>

      <Modal visible={open} animationType="slide" transparent onRequestClose={() => setOpen(false)}>
        <View style={styles.backdrop}>
          <View style={styles.sheet}>
            <View style={styles.sheetHeader}>
              <Text style={type.h3}>{label}</Text>
              <Pressable onPress={() => setOpen(false)} hitSlop={10}>
                <Ionicons name="close" size={24} color={colors.slate} />
              </Pressable>
            </View>
            {searchable && (
              <View style={styles.search}>
                <Ionicons name="search" size={18} color={colors.muted} />
                <TextInput
                  value={query}
                  onChangeText={setQuery}
                  placeholder="Ara..."
                  placeholderTextColor={colors.muted}
                  style={styles.searchInput}
                  autoFocus
                />
              </View>
            )}
            <FlatList
              data={filtered}
              keyExtractor={(item) => item.value}
              keyboardShouldPersistTaps="handled"
              style={{ maxHeight: 380 }}
              renderItem={({ item }) => {
                const active = item.value === value;
                return (
                  <Pressable
                    style={styles.option}
                    onPress={() => {
                      onSelect(item.value);
                      setQuery('');
                      setOpen(false);
                    }}
                  >
                    <Text style={[styles.optionText, active && { color: colors.blue, fontWeight: '800' }]}>
                      {item.label}
                    </Text>
                    {active && <Ionicons name="checkmark" size={20} color={colors.blue} />}
                  </Pressable>
                );
              }}
              ListEmptyComponent={<Text style={[type.small, { padding: 20, textAlign: 'center' }]}>Sonuç bulunamadı.</Text>}
            />
          </View>
        </View>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  trigger: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.lineStrong,
    borderRadius: radius.md,
    paddingHorizontal: 14,
    paddingVertical: 13
  },
  disabled: { opacity: 0.5 },
  triggerText: { flex: 1, fontSize: 15, color: colors.ink, fontWeight: '600' },
  backdrop: { flex: 1, backgroundColor: 'rgba(13,26,68,0.35)', justifyContent: 'flex-end' },
  sheet: {
    backgroundColor: colors.bg,
    borderTopLeftRadius: radius.xl,
    borderTopRightRadius: radius.xl,
    paddingHorizontal: 18,
    paddingTop: 16,
    paddingBottom: 28
  },
  sheetHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 },
  search: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.lineStrong,
    borderRadius: radius.md,
    paddingHorizontal: 12,
    marginBottom: 8
  },
  searchInput: { flex: 1, paddingVertical: 11, fontSize: 15, color: colors.ink },
  option: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 14,
    paddingHorizontal: 6,
    borderBottomWidth: 1,
    borderBottomColor: colors.line
  },
  optionText: { fontSize: 15, color: colors.ink, fontWeight: '600' }
});
