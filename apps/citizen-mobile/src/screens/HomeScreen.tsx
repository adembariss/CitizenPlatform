import { useState } from 'react';
import { ActivityIndicator, StyleSheet, Text, TextInput, TouchableOpacity, View } from 'react-native';
import { createComplaint, resolveMunicipality } from '../services/api';
import { DEMO_CATEGORIES } from '../services/categories';
import { ExpoLocationProvider } from '../services/location/ExpoLocationProvider';

const locationProvider = new ExpoLocationProvider();

type LocationState =
  | { status: 'idle' }
  | { status: 'locating' }
  | { status: 'resolved'; latitude: number; longitude: number; municipalityName: string }
  | { status: 'error'; message: string };

type SubmitState = { status: 'idle' } | { status: 'submitting' } | { status: 'success'; trackingCode: string } | { status: 'error'; message: string };

export function HomeScreen() {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [location, setLocation] = useState<LocationState>({ status: 'idle' });
  const [submit, setSubmit] = useState<SubmitState>({ status: 'idle' });

  async function handleUseLocation() {
    setLocation({ status: 'locating' });
    try {
      const coordinates = await locationProvider.getCurrentPosition();
      const resolved = await resolveMunicipality(coordinates.latitude, coordinates.longitude);

      if (!resolved.data?.isSuccess || !resolved.data.municipalityName) {
        setLocation({
          status: 'error',
          message: resolved.data?.failureReason ?? 'Bu konum için hizmet veren bir belediye bulunamadı.'
        });
        return;
      }

      setLocation({
        status: 'resolved',
        latitude: coordinates.latitude,
        longitude: coordinates.longitude,
        municipalityName: resolved.data.municipalityName
      });
    } catch (error) {
      setLocation({ status: 'error', message: error instanceof Error ? error.message : 'Konum alınamadı.' });
    }
  }

  async function handleSubmit() {
    if (location.status !== 'resolved') {
      setSubmit({ status: 'error', message: 'Önce konumunuzu paylaşmalısınız.' });
      return;
    }

    if (!description.trim()) {
      setSubmit({ status: 'error', message: 'Açıklama zorunludur.' });
      return;
    }

    setSubmit({ status: 'submitting' });

    const result = await createComplaint({
      categoryId: DEMO_CATEGORIES[0].id,
      title: title || undefined,
      description,
      latitude: location.latitude,
      longitude: location.longitude,
      isAnonymous: true,
      source: 'CitizenMobile'
    });

    if (!result.success || !result.data) {
      setSubmit({ status: 'error', message: result.message ?? 'Bildirim gönderilemedi.' });
      return;
    }

    setSubmit({ status: 'success', trackingCode: result.data.trackingCode });
  }

  if (submit.status === 'success') {
    return (
      <View style={styles.screen}>
        <View style={styles.header}>
          <Text style={styles.eyebrow}>Bildirim alındı</Text>
          <Text style={styles.title}>Teşekkürler, talebiniz kaydedildi.</Text>
        </View>
        <View style={styles.trackingBox}>
          <Text style={styles.locationText}>Takip kodunuz</Text>
          <Text style={styles.trackingCode}>{submit.trackingCode}</Text>
        </View>
        <TouchableOpacity
          style={styles.button}
          onPress={() => {
            setTitle('');
            setDescription('');
            setLocation({ status: 'idle' });
            setSubmit({ status: 'idle' });
          }}
        >
          <Text style={styles.buttonText}>Yeni bildirim oluştur</Text>
        </TouchableOpacity>
      </View>
    );
  }

  return (
    <View style={styles.screen}>
      <View style={styles.header}>
        <Text style={styles.eyebrow}>Vatandaş mobil</Text>
        <Text style={styles.title}>Bildirim oluştur</Text>
      </View>
      <View style={styles.form}>
        <TextInput placeholder="Başlık" style={styles.input} value={title} onChangeText={setTitle} />
        <TextInput
          placeholder="Açıklama"
          multiline
          numberOfLines={5}
          style={[styles.input, styles.textArea]}
          value={description}
          onChangeText={setDescription}
        />
        <TouchableOpacity style={styles.locationBox} onPress={handleUseLocation} disabled={location.status === 'locating'}>
          {location.status === 'locating' && <ActivityIndicator />}
          {location.status === 'resolved' && <Text style={styles.locationText}>Konum alındı — {location.municipalityName}</Text>}
          {(location.status === 'idle' || location.status === 'error') && <Text style={styles.locationText}>Konumumu kullan</Text>}
        </TouchableOpacity>
        {location.status === 'error' && <Text style={styles.errorText}>{location.message}</Text>}
        {submit.status === 'error' && <Text style={styles.errorText}>{submit.message}</Text>}
        <TouchableOpacity style={styles.button} onPress={handleSubmit} disabled={submit.status === 'submitting'}>
          {submit.status === 'submitting' ? <ActivityIndicator color="#ffffff" /> : <Text style={styles.buttonText}>Gönder</Text>}
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
  errorText: {
    color: '#b3261e',
    fontWeight: '600'
  },
  trackingBox: {
    alignItems: 'center',
    gap: 8,
    padding: 20,
    borderRadius: 8,
    backgroundColor: '#eef5f2'
  },
  trackingCode: {
    fontSize: 24,
    fontWeight: '700',
    letterSpacing: 1,
    color: '#1f2933'
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
