import { ChangeEvent, FormEvent, useEffect, useRef, useState } from 'react';
import {
  createComplaint,
  createComplaintWithPhotos,
  getCurrentPosition,
  getMunicipalityCategories,
  resolveMunicipality,
  PublicCategory
} from '../lib/api';
import { LatLng, LocationMap } from '../components/LocationMap';

const MAX_PHOTOS = 5;

type MunicipalityState =
  | { status: 'idle' }
  | { status: 'resolving' }
  | { status: 'resolved'; municipalityId: string; municipalityName: string }
  | { status: 'error'; message: string };

type CategoriesState =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'loaded'; categories: PublicCategory[] }
  | { status: 'error'; message: string };

type SubmitState =
  | { status: 'idle' }
  | { status: 'submitting' }
  | { status: 'success'; trackingCode: string }
  | { status: 'error'; message: string };

type PhotoItem = {
  file: File;
  previewUrl: string;
};

type ReportFormProps = {
  onTrack: (trackingCode: string) => void;
};

export function ReportForm({ onTrack }: ReportFormProps) {
  const [position, setPosition] = useState<LatLng | null>(null);
  const [locating, setLocating] = useState(false);
  const [municipality, setMunicipality] = useState<MunicipalityState>({ status: 'idle' });
  const [categoriesState, setCategoriesState] = useState<CategoriesState>({ status: 'idle' });
  const [submit, setSubmit] = useState<SubmitState>({ status: 'idle' });
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [citizenFullName, setCitizenFullName] = useState('');
  const [isAnonymous, setIsAnonymous] = useState(false);
  const [photos, setPhotos] = useState<PhotoItem[]>([]);
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const photosRef = useRef<PhotoItem[]>([]);
  photosRef.current = photos;

  useEffect(() => {
    return () => {
      for (const photo of photosRef.current) {
        URL.revokeObjectURL(photo.previewUrl);
      }
    };
  }, []);

  useEffect(() => {
    if (!position) {
      return;
    }

    let cancelled = false;
    setMunicipality({ status: 'resolving' });

    resolveMunicipality(position.latitude, position.longitude)
      .then((resolved) => {
        if (cancelled) return;

        if (!resolved.data?.isSuccess || !resolved.data.municipalityId || !resolved.data.municipalityName) {
          setMunicipality({
            status: 'error',
            message: resolved.data?.failureReason ?? resolved.message ?? 'Bu konum için hizmet veren bir belediye bulunamadı.'
          });
          setCategoriesState({ status: 'idle' });
          return;
        }

        setMunicipality({
          status: 'resolved',
          municipalityId: resolved.data.municipalityId,
          municipalityName: resolved.data.municipalityName
        });
      })
      .catch(() => {
        if (cancelled) return;
        setMunicipality({ status: 'error', message: 'Belediye sorgulanırken bir hata oluştu.' });
        setCategoriesState({ status: 'idle' });
      });

    return () => {
      cancelled = true;
    };
  }, [position]);

  const municipalityId = municipality.status === 'resolved' ? municipality.municipalityId : null;

  useEffect(() => {
    if (!municipalityId) {
      return;
    }

    let cancelled = false;
    setCategoriesState({ status: 'loading' });

    getMunicipalityCategories(municipalityId)
      .then((result) => {
        if (cancelled) return;

        if (!result.success || !result.data) {
          setCategoriesState({ status: 'error', message: result.message ?? 'Kategoriler yüklenemedi.' });
          return;
        }

        setCategoriesState({ status: 'loaded', categories: result.data });
        setCategoryId((current) => (result.data!.some((category) => category.id === current) ? current : result.data![0]?.id ?? ''));
      })
      .catch(() => {
        if (cancelled) return;
        setCategoriesState({ status: 'error', message: 'Kategoriler yüklenemedi.' });
      });

    return () => {
      cancelled = true;
    };
  }, [municipalityId]);

  async function handleUseMyLocation() {
    setLocating(true);
    try {
      const current = await getCurrentPosition();
      setPosition({ latitude: current.coords.latitude, longitude: current.coords.longitude });
    } catch (error) {
      setMunicipality({ status: 'error', message: error instanceof Error ? error.message : 'Konum alınamadı.' });
    } finally {
      setLocating(false);
    }
  }

  function handlePhotosSelected(event: ChangeEvent<HTMLInputElement>) {
    const selected = Array.from(event.target.files ?? []);
    if (selected.length === 0) {
      return;
    }

    setPhotos((current) => {
      const room = MAX_PHOTOS - current.length;
      const accepted = selected.slice(0, Math.max(room, 0)).map((file) => ({
        file,
        previewUrl: URL.createObjectURL(file)
      }));
      return [...current, ...accepted];
    });

    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  }

  function handleRemovePhoto(index: number) {
    setPhotos((current) => {
      const removed = current[index];
      if (removed) {
        URL.revokeObjectURL(removed.previewUrl);
      }
      return current.filter((_, photoIndex) => photoIndex !== index);
    });
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();

    if (!position || municipality.status !== 'resolved') {
      setSubmit({ status: 'error', message: 'Önce haritadan konum seçmeli veya konumunuzu paylaşmalısınız.' });
      return;
    }

    if (!categoryId) {
      setSubmit({ status: 'error', message: 'Lütfen bir kategori seçin.' });
      return;
    }

    setSubmit({ status: 'submitting' });

    const request = {
      categoryId,
      title: title || undefined,
      description,
      citizenFullName: isAnonymous ? undefined : citizenFullName || undefined,
      latitude: position.latitude,
      longitude: position.longitude,
      isAnonymous,
      source: 'CitizenWeb' as const
    };

    const result =
      photos.length > 0
        ? await createComplaintWithPhotos(
            request,
            photos.map((photo) => photo.file)
          )
        : await createComplaint(request);

    if (!result.success || !result.data) {
      setSubmit({ status: 'error', message: result.message ?? 'Bildirim gönderilemedi.' });
      return;
    }

    setSubmit({ status: 'success', trackingCode: result.data.trackingCode });
  }

  if (submit.status === 'success') {
    return (
      <>
        <section className="intro">
          <p>Bildirim alındı</p>
          <h1>Teşekkürler, talebiniz kaydedildi.</h1>
        </section>
        <div className="report-form">
          <p>Takip kodunuz:</p>
          <p className="tracking-code">{submit.trackingCode}</p>
          <button type="button" onClick={() => onTrack(submit.trackingCode)}>
            Şikayetimi sorgula
          </button>
          <button type="button" className="secondary-button" onClick={() => window.location.reload()}>
            Yeni bildirim oluştur
          </button>
        </div>
      </>
    );
  }

  const categories = categoriesState.status === 'loaded' ? categoriesState.categories : [];

  return (
    <>
      <section className="intro">
        <p>Vatandaş başvuru ekranı</p>
        <h1>Mahallendeki sorunu belediyeye ilet.</h1>
      </section>
      <form className="report-form" onSubmit={handleSubmit}>
        <div className="map-field">
          <span className="map-field-label">Konum</span>
          <LocationMap position={position} onPick={setPosition} />
          <div className="map-toolbar">
            <button type="button" className="secondary-button" onClick={handleUseMyLocation} disabled={locating}>
              {locating ? 'Konum alınıyor...' : 'Konumumu kullan'}
            </button>
            <span className="map-hint">
              {position
                ? `Seçilen konum: ${position.latitude.toFixed(5)}, ${position.longitude.toFixed(5)}`
                : 'Haritaya tıklayarak da konum seçebilirsiniz.'}
            </span>
          </div>
          {municipality.status === 'resolving' && <p className="map-status">Belediye sorgulanıyor...</p>}
          {municipality.status === 'resolved' && (
            <p className="map-status resolved">
              Hizmet veren belediye: <strong>{municipality.municipalityName}</strong>
            </p>
          )}
          {municipality.status === 'error' && <p className="form-error">{municipality.message}</p>}
        </div>

        <label>
          Başlık
          <input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Örn. Kaldırım hasarı" />
        </label>
        <label>
          Kategori
          <select
            required
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
            disabled={categoriesState.status !== 'loaded'}
          >
            {categoriesState.status !== 'loaded' && (
              <option value="">
                {categoriesState.status === 'loading' ? 'Kategoriler yükleniyor...' : 'Önce konum seçin'}
              </option>
            )}
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </label>
        {categoriesState.status === 'error' && <p className="form-error">{categoriesState.message}</p>}
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

        <div className="photo-field">
          <span className="map-field-label">Fotoğraflar (en fazla {MAX_PHOTOS})</span>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/*"
            multiple
            onChange={handlePhotosSelected}
            disabled={photos.length >= MAX_PHOTOS}
          />
          {photos.length > 0 && (
            <ul className="photo-previews">
              {photos.map((photo, index) => (
                <li key={photo.previewUrl}>
                  <img src={photo.previewUrl} alt={photo.file.name} />
                  <button type="button" aria-label={`${photo.file.name} fotoğrafını kaldır`} onClick={() => handleRemovePhoto(index)}>
                    Kaldır
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

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

        {submit.status === 'error' && <p className="form-error">{submit.message}</p>}

        <button type="submit" disabled={submit.status === 'submitting' || municipality.status !== 'resolved'}>
          {submit.status === 'submitting' ? 'Gönderiliyor...' : 'Bildirim oluştur'}
        </button>
      </form>
    </>
  );
}
