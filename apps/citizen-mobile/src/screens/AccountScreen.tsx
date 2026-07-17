import { Ionicons } from '@expo/vector-icons';
import { useNavigation } from '@react-navigation/native';
import { useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { GradientHeader } from '../components/Header';
import { Banner, Button, Card, EmptyState, Field, Loading, StatusBadge } from '../components/ui';
import { useAuth } from '../context/AuthContext';
import { formatDate } from '../lib/status';
import { getMyComplaints, type MyComplaint } from '../services/api';
import { colors, radius, type } from '../theme';

// Türk cep telefonu doğrulaması (SMS için zorunlu alan).
function validatePhone(raw: string): string | null {
  const digits = raw.replace(/\D/g, '');
  const local = digits.startsWith('90') ? digits.slice(2) : digits.startsWith('0') ? digits.slice(1) : digits;
  if (local.length !== 10 || !local.startsWith('5')) {
    return 'Geçerli bir cep telefonu girin. Örn: 0532 123 45 67';
  }
  return null;
}

export function AccountScreen() {
  const { ready, token, user, phoneVerified, isAuthenticated } = useAuth();

  if (!ready) {
    return (
      <View style={{ flex: 1, backgroundColor: colors.bg }}>
        <GradientHeader title="Hesabım" compact />
        <Loading label="Yükleniyor..." />
      </View>
    );
  }
  if (token && !phoneVerified) return <VerifyView />;
  if (isAuthenticated && user) return <ProfileView />;
  return <AuthView />;
}

function AuthView() {
  const { login, register } = useAuth();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [errors, setErrors] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);

  async function submit() {
    setErrors([]);
    if (!email.trim()) return setErrors(['E-posta zorunludur.']);
    if (!password) return setErrors(['Şifre zorunludur.']);
    if (mode === 'register') {
      const phoneError = validatePhone(phone);
      if (phoneError) return setErrors([phoneError]);
    }
    setBusy(true);
    const result =
      mode === 'login'
        ? await login(email.trim(), password)
        : await register({ email: email.trim(), phoneNumber: phone.trim(), password, fullName: fullName.trim() || undefined });
    setBusy(false);
    if (!result.ok) setErrors(result.errors);
    // Başarılıysa: phoneVerified false ise AccountScreen otomatik VerifyView'e geçer.
  }

  return (
    <View style={{ flex: 1, backgroundColor: colors.bg }}>
      <GradientHeader title="Hesabım" subtitle={mode === 'login' ? 'Giriş yap' : 'Üye ol'} compact />
      <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
        <View style={styles.segment}>
          <Pressable style={[styles.segBtn, mode === 'login' && styles.segActive]} onPress={() => setMode('login')}>
            <Text style={[styles.segText, mode === 'login' && styles.segTextActive]}>Giriş Yap</Text>
          </Pressable>
          <Pressable style={[styles.segBtn, mode === 'register' && styles.segActive]} onPress={() => setMode('register')}>
            <Text style={[styles.segText, mode === 'register' && styles.segTextActive]}>Üye Ol</Text>
          </Pressable>
        </View>

        <Card style={{ gap: 14 }}>
          {mode === 'register' && (
            <Field label="Ad Soyad (opsiyonel)" value={fullName} onChangeText={setFullName} placeholder="Ayşe Yılmaz" autoCapitalize="words" />
          )}
          <Field label="E-posta" value={email} onChangeText={setEmail} placeholder="ornek@eposta.com" keyboardType="email-address" autoCapitalize="none" autoCorrect={false} />
          {mode === 'register' && (
            <Field label="Cep Telefonu (zorunlu)" value={phone} onChangeText={setPhone} placeholder="0532 123 45 67" keyboardType="phone-pad" hint="Doğrulama SMS'i bu numaraya gönderilir." />
          )}
          <Field label="Şifre" value={password} onChangeText={setPassword} placeholder="••••••••" secureTextEntry hint={mode === 'register' ? 'En az 8 karakter, harf ve rakam içermeli.' : undefined} />

          {errors.length > 0 && <Banner tone="error">{errors.join('\n')}</Banner>}

          <Button title={mode === 'login' ? 'Giriş Yap' : 'Üye Ol'} icon="log-in" loading={busy} onPress={submit} />
        </Card>

        <Card style={styles.infoCard}>
          <Ionicons name="information-circle" size={20} color={colors.blue} />
          <Text style={[type.small, { flex: 1 }]}>
            Hesap oluşturmak zorunlu değil — şikayetleri anonim de bildirebilirsin. Hesapla bildirdiklerini “Şikayetlerim”de takip edersin.
          </Text>
        </Card>
        <View style={{ height: 12 }} />
      </ScrollView>
    </View>
  );
}

function VerifyView() {
  const { verifyPhone, resendCode, logout } = useAuth();
  const [code, setCode] = useState('');
  const [preview, setPreview] = useState<string | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);

  async function submit() {
    setErrors([]);
    if (code.trim().length < 4) return setErrors(['Lütfen SMS ile gelen kodu girin.']);
    setBusy(true);
    const result = await verifyPhone(code.trim());
    setBusy(false);
    if (!result.ok) setErrors(result.errors);
  }

  async function resend() {
    const p = await resendCode();
    setPreview(p);
    setErrors([]);
  }

  return (
    <View style={{ flex: 1, backgroundColor: colors.bg }}>
      <GradientHeader title="Telefon Doğrulama" subtitle="Hesabını etkinleştir" compact />
      <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
        <Card style={{ gap: 14, alignItems: 'stretch' }}>
          <View style={{ alignItems: 'center', gap: 8, paddingVertical: 8 }}>
            <View style={styles.verifyIcon}>
              <Ionicons name="chatbox-ellipses" size={30} color={colors.blue} />
            </View>
            <Text style={type.h3}>SMS kodunu gir</Text>
            <Text style={[type.small, { textAlign: 'center' }]}>Telefonuna gönderilen 6 haneli doğrulama kodunu girerek hesabını etkinleştir.</Text>
          </View>
          {preview && <Banner tone="info">Geliştirme kodu: {preview}</Banner>}
          <Field label="Doğrulama Kodu" value={code} onChangeText={setCode} placeholder="000000" keyboardType="number-pad" maxLength={6} />
          {errors.length > 0 && <Banner tone="error">{errors.join('\n')}</Banner>}
          <Button title="Doğrula" icon="checkmark-circle" loading={busy} onPress={submit} />
          <Button title="Kodu tekrar gönder" variant="ghost" onPress={resend} />
          <Button title="Vazgeç / çıkış yap" variant="ghost" onPress={logout} />
        </Card>
      </ScrollView>
    </View>
  );
}

function ProfileView() {
  const { user, token, logout } = useAuth();
  const navigation = useNavigation<{ navigate: (s: string, p?: object) => void }>();
  const [complaints, setComplaints] = useState<MyComplaint[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;
    getMyComplaints(token).then((r) => {
      if (r.success && r.data) setComplaints(r.data);
      else setError(r.message ?? 'Şikayetler yüklenemedi.');
    });
  }, [token]);

  const initials = (user?.fullName ?? user?.email ?? '?').trim().charAt(0).toUpperCase();

  return (
    <View style={{ flex: 1, backgroundColor: colors.bg }}>
      <GradientHeader title="Hesabım" subtitle="Profil ve şikayetlerin" compact />
      <ScrollView contentContainerStyle={styles.content} showsVerticalScrollIndicator={false}>
        <Card style={styles.profileCard}>
          <View style={styles.avatar}>
            <Text style={styles.avatarText}>{initials}</Text>
          </View>
          <View style={{ flex: 1 }}>
            <Text style={type.h2}>{user?.fullName || 'Vatandaş'}</Text>
            <Text style={type.small}>{user?.email}</Text>
            <View style={styles.verifiedBadge}>
              <Ionicons name="shield-checkmark" size={13} color={colors.green} />
              <Text style={styles.verifiedText}>Telefon doğrulandı</Text>
            </View>
          </View>
        </Card>

        <View style={styles.rowBetween}>
          <Text style={type.h3}>Şikayetlerim</Text>
          <Pressable onPress={() => navigation.navigate('Bildir')} style={styles.newBtn}>
            <Ionicons name="add" size={16} color={colors.blue} />
            <Text style={styles.newBtnText}>Yeni</Text>
          </Pressable>
        </View>

        {error && <Banner tone="error">{error}</Banner>}
        {complaints === null && !error && <Loading label="Şikayetlerin yükleniyor..." />}
        {complaints && complaints.length === 0 && (
          <EmptyState icon="albums" title="Henüz şikayetin yok" subtitle="İlk bildirimini oluşturmak için “Yeni”ye dokun." />
        )}
        {complaints?.map((c) => (
          <Pressable key={c.trackingCode} onPress={() => navigation.navigate('Takip', { code: c.trackingCode })}>
            <Card style={{ gap: 8 }}>
              <View style={styles.rowBetween}>
                <Text style={[type.h3, { flex: 1 }]} numberOfLines={1}>{c.title}</Text>
                <StatusBadge status={c.status} />
              </View>
              <Text style={type.small}>{c.municipalityName} • {c.categoryName}</Text>
              <View style={styles.rowBetween}>
                <Text style={[type.small, { fontFamily: undefined }]}>Kod: <Text style={{ fontWeight: '800', color: colors.blue }}>{c.trackingCode}</Text></Text>
                <Text style={type.small}>{formatDate(c.createdAt)}</Text>
              </View>
            </Card>
          </Pressable>
        ))}

        <Button title="Çıkış Yap" variant="secondary" icon="log-out" onPress={logout} style={{ marginTop: 6 }} />
        <View style={{ height: 12 }} />
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  content: { padding: 18, gap: 14 },
  segment: { flexDirection: 'row', backgroundColor: colors.surfaceAlt, borderRadius: radius.md, padding: 4, gap: 4 },
  segBtn: { flex: 1, alignItems: 'center', paddingVertical: 11, borderRadius: radius.sm },
  segActive: { backgroundColor: colors.blue },
  segText: { fontWeight: '800', color: colors.slate, fontSize: 14 },
  segTextActive: { color: '#fff' },
  infoCard: { flexDirection: 'row', gap: 10, alignItems: 'flex-start', backgroundColor: colors.cyanSoft, borderColor: '#cfe6f6' },
  verifyIcon: { width: 62, height: 62, borderRadius: 31, backgroundColor: colors.cyanSoft, alignItems: 'center', justifyContent: 'center' },
  profileCard: { flexDirection: 'row', alignItems: 'center', gap: 14 },
  avatar: { width: 58, height: 58, borderRadius: 29, backgroundColor: colors.navy, alignItems: 'center', justifyContent: 'center' },
  avatarText: { color: colors.cyan, fontSize: 24, fontWeight: '800' },
  verifiedBadge: { flexDirection: 'row', alignItems: 'center', gap: 5, backgroundColor: colors.greenSoft, alignSelf: 'flex-start', borderRadius: radius.pill, paddingHorizontal: 9, paddingVertical: 3, marginTop: 6 },
  verifiedText: { color: colors.green, fontSize: 11.5, fontWeight: '800' },
  rowBetween: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 8 },
  newBtn: { flexDirection: 'row', alignItems: 'center', gap: 4, backgroundColor: colors.cyanSoft, borderRadius: radius.pill, paddingHorizontal: 12, paddingVertical: 6 },
  newBtnText: { color: colors.blue, fontWeight: '800', fontSize: 13 }
});
