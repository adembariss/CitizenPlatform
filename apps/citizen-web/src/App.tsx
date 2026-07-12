import { FormEvent, useState } from 'react';
import { createComplaint, getCurrentPosition, resolveMunicipality } from './lib/api';
import { DEMO_CATEGORIES } from './lib/categories';

type LocationState =
  | { status: 'idle' }
  | { status: 'locating' }
  | { status: 'resolved'; latitude: number; longitude: number; municipalityName: string }
  | { status: 'error'; message: string };

type SubmitState =
  | { status: 'idle' }
  | { status: 'submitting' }
  | { status: 'success'; trackingCode: string }
  | { status: 'error'; message: string };

export function App() {
  const [location, setLocation] = useState<LocationState>({ status: 'idle' });
  const [submit, setSubmit] = useState<SubmitState>({ status: 'idle' });
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [categoryId, setCategoryId] = useState(DEMO_CATEGORIES[0].id);
  const [citizenFullName, setCitizenFullName] = useState('');
  const [isAnonymous, setIsAnonymous] = useState(false);

  async function handleUseMyLocation() {
    setLocation({ status: 'locating' });
    try {
      const position = await getCurrentPosition();
      const { latitude, longitude } = position.coords;
      const resolved = await resolveMunicipality(latitude, longitude);

      if (!resolved.data?.isSuccess || !resolved.data.municipalityName) {
        setLocation({
          status: 'error',
          message: resolved.data?.failureReason ?? 'Bu konum için hizmet veren bir belediye bulunamadı.'
        });
        return;
      }

      setLocation({ status: 'resolved', latitude, longitude, municipalityName: resolved.data.municipalityName });
    } catch (error) {
      setLocation({ status: 'error', message: error instanceof Error ? error.message : 'Konum alınamadı.' });
    }
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();

    if (location.status !== 'resolved') {
      setSubmit({ status: 'error', message: 'Önce konumunuzu paylaşmalısınız.' });
      return;
    }

    setSubmit({ status: 'submitting' });

    const result = await createComplaint({
      categoryId,
      title: title || undefined,
      description,
      citizenFullName: isAnonymous ? undefined : citizenFullName || undefined,
      latitude: location.latitude,
      longitude: location.longitude,
      isAnonymous,
      source: 'CitizenWeb'
    });

    if (!result.success || !result.data) {
      setSubmit({ status: 'error', message: result.message ?? 'Bildirim gönderilemedi.' });
      return;
    }

    setSubmit({ status: 'success', trackingCode: result.data.trackingCode });
  }

  if (submit.status === 'success') {
    return (
      <main className="citizen-shell">
        <section className="intro">
          <p>Bildirim alındı</p>
          <h1>Teşekkürler, talebiniz kaydedildi.</h1>
        </section>
        <div className="report-form">
          <p>Takip kodunuz:</p>
          <p className="tracking-code">{submit.trackingCode}</p>
          <button type="button" onClick={() => window.location.reload()}>
            Yeni bildirim oluştur
          </button>
        </div>
      </main>
    );
  }

  return (
    <main className="citizen-shell">
      <section className="intro">
        <p>Vatandaş başvuru ekranı</p>
        <h1>Mahallendeki sorunu belediyeye ilet.</h1>
      </section>
      <form className="report-form" onSubmit={handleSubmit}>
        <label>
          Başlık
          <input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Örn. Kaldırım hasarı" />
        </label>
        <label>
          Kategori
          <select value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
            {DEMO_CATEGORIES.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          Açıklama
          <textarea
            required
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            placeholder="Sorunu kısaca anlatın"
            rows={5}
          />
        </label>
        <label className="checkbox-label">
          <input type="checkbox" checked={isAnonymous} onChange={(event) => setIsAnonymous(event.target.checked)} />
          Anonim gönder
        </label>
        {!isAnonymous && (
          <label>
            Ad Soyad (opsiyonel)
            <input value={citizenFullName} onChange={(event) => setCitizenFullName(event.target.value)} placeholder="Ada Lovelace" />
          </label>
        )}

        {location.status === 'resolved' ? (
          <div className="map-placeholder resolved">
            Konum alındı — <strong>{location.municipalityName}</strong>
          </div>
        ) : (
          <button type="button" onClick={handleUseMyLocation} disabled={location.status === 'locating'}>
            {location.status === 'locating' ? 'Konum alınıyor...' : 'Konumumu kullan'}
          </button>
        )}
        {location.status === 'error' && <p className="form-error">{location.message}</p>}

        {submit.status === 'error' && <p className="form-error">{submit.message}</p>}

        <button type="submit" disabled={submit.status === 'submitting' || location.status !== 'resolved'}>
          {submit.status === 'submitting' ? 'Gönderiliyor...' : 'Bildirim oluştur'}
        </button>
      </form>
    </main>
  );
}
