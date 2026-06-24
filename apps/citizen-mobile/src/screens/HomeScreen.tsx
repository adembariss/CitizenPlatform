import { StyleSheet, Text, TextInput, TouchableOpacity, View } from 'react-native';

export function HomeScreen() {
  return (
    <View style={styles.screen}>
      <View style={styles.header}>
        <Text style={styles.eyebrow}>Vatandaş mobil</Text>
        <Text style={styles.title}>Bildirim oluştur</Text>
      </View>
      <View style={styles.form}>
        <TextInput placeholder="Başlık" style={styles.input} />
        <TextInput placeholder="Açıklama" multiline numberOfLines={5} style={[styles.input, styles.textArea]} />
        <View style={styles.locationBox}>
          <Text style={styles.locationText}>Konum seçimi</Text>
        </View>
        <TouchableOpacity style={styles.button}>
          <Text style={styles.buttonText}>Gönder</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    padding: 20,
    gap: 20
  },
  header: {
    gap: 8
  },
  eyebrow: {
    color: '#52635e',
    fontSize: 14
  },
  title: {
    color: '#1f2933',
    fontSize: 30,
    fontWeight: '700'
  },
  form: {
    gap: 14
  },
  input: {
    minHeight: 48,
    borderWidth: 1,
    borderColor: '#c8d5d0',
    borderRadius: 8,
    backgroundColor: '#ffffff',
    paddingHorizontal: 14,
    paddingVertical: 10
  },
  textArea: {
    minHeight: 120,
    textAlignVertical: 'top'
  },
  locationBox: {
    minHeight: 160,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: '#7d918a',
    borderStyle: 'dashed',
    borderRadius: 8
  },
  locationText: {
    color: '#40524c'
  },
  button: {
    alignItems: 'center',
    borderRadius: 8,
    backgroundColor: '#1f6f55',
    paddingVertical: 14
  },
  buttonText: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '700'
  }
});
