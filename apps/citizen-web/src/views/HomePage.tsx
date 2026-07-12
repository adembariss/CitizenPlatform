import { ReactNode, useEffect, useState } from 'react';
import {
  ComplaintMapPoint,
  PublicStats,
  getComplaintMapPoints,
  getPublicStats
} from '../lib/api';
import { InsightsMap } from '../components/InsightsMap';

type HomePageProps = {
  onReport: () => void;
  onTrack: () => void;
};

const numberFormat = new Intl.NumberFormat('tr-TR');

export function HomePage({ onReport, onTrack }: HomePageProps) {
  const [stats, setStats] = useState<PublicStats | null>(null);
  const [mapPoints, setMapPoints] = useState<ComplaintMapPoint[]>([]);

  useEffect(() => {
    let cancelled = false;

    getPublicStats()
      .then((result) => {
        if (!cancelled && result.success && result.data) {
          setStats(result.data);
        }
      })
      .catch(() => undefined);

    getComplaintMapPoints(300)
      .then((result) => {
        if (!cancelled && result.success && result.data) {
          setMapPoints(result.data);
        }
      })
      .catch(() => undefined);

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="home">
      {/* Hero */}
      <section className="hero">
        <div className="hero-copy">
          <span className="hero-eyebrow">BELEDİYEM · VATANDAŞ PORTALI</span>
          <h1>Şehrindeki sorunu birkaç adımda belediyene ilet.</h1>
          <p>
            Bozuk kaldırım, sokak lambası, çöp, park ve daha fazlası… Konumunu seç, fotoğrafını çek,
            gönder. Başvurunu takip kodunla anlık olarak izle.
          </p>
          <div className="hero-actions">
            <button type="button" className="btn btn-primary btn-lg" onClick={onReport}>
              Şikayet Oluştur
            </button>
            <button type="button" className="btn btn-ghost btn-lg" onClick={onTrack}>
              Şikayet Sorgula
            </button>
          </div>
          <ul className="hero-trust">
            <li>
              <CheckIcon /> Konuma göre doğru belediye
            </li>
            <li>
              <CheckIcon /> Fotoğraflı bildirim
            </li>
            <li>
              <CheckIcon /> Anonim gönderim
            </li>
          </ul>
        </div>
        <div className="hero-visual" aria-hidden="true">
          <HeroArt />
        </div>
      </section>

      {/* Canlı istatistikler */}
      <section className="stats-band">
        <StatTile value={stats ? numberFormat.format(stats.totalComplaints) : '—'} label="Toplam Bildirim" />
        <StatTile value={stats ? numberFormat.format(stats.resolvedComplaints) : '—'} label="Çözülen Bildirim" />
        <StatTile value={stats ? numberFormat.format(stats.activeMunicipalities) : '—'} label="Aktif Belediye" />
        <StatTile value={stats ? numberFormat.format(stats.categories) : '—'} label="Hizmet Kategorisi" />
      </section>

      {/* Nasıl çalışır */}
      <section className="section">
        <div className="section-head">
          <span className="section-kicker">NASIL ÇALIŞIR</span>
          <h2>Üç adımda başvuru</h2>
        </div>
        <div className="steps">
          <Step index="1" title="Konumunu seç" icon={<PinIcon />}>
            Haritadan sorunun yerini işaretle ya da tek dokunuşla mevcut konumunu kullan. Sistem
            otomatik olarak yetkili belediyeyi bulur.
          </Step>
          <Step index="2" title="Sorunu anlat" icon={<PhotoIcon />}>
            Kategori seç, kısaca açıkla ve dilersen fotoğraf ekle. İstersen adını paylaşmadan anonim
            olarak da gönderebilirsin.
          </Step>
          <Step index="3" title="Takip et" icon={<TrackIcon />}>
            Sana özel takip kodunu al. Başvurunun hangi birimde ve hangi aşamada olduğunu istediğin
            zaman gör.
          </Step>
        </div>
      </section>

      {/* Hizmet alanları */}
      <section className="section section-tint">
        <div className="section-head">
          <span className="section-kicker">HİZMET ALANLARI</span>
          <h2>Neleri bildirebilirsin?</h2>
          <p className="section-sub">
            Bildirim oluştururken belediyenin güncel kategorileri otomatik olarak listelenir. İşte
            en sık kullanılanlardan bazıları:
          </p>
        </div>
        <div className="category-grid">
          {CATEGORIES.map((category) => (
            <div className="category-card" key={category.label}>
              <span className="category-icon">{category.icon}</span>
              <span className="category-label">{category.label}</span>
            </div>
          ))}
        </div>
      </section>

      {/* Özellikler */}
      <section className="section">
        <div className="section-head">
          <span className="section-kicker">NEDEN BELEDİYEM?</span>
          <h2>Vatandaş için tasarlandı</h2>
        </div>
        <div className="feature-grid">
          <Feature title="Harita ile hassas konum" icon={<PinIcon />}>
            Leaflet tabanlı interaktif harita ile sorunun yerini metre metre işaretle.
          </Feature>
          <Feature title="Fotoğraflı kanıt" icon={<PhotoIcon />}>
            Bildirimine 5'e kadar fotoğraf ekle, birimlerin sorunu daha hızlı anlamasını sağla.
          </Feature>
          <Feature title="Anonim gönderim" icon={<ShieldIcon />}>
            İstemiyorsan kimliğini paylaşma. Başvurun yine de takip kodunla izlenebilir.
          </Feature>
          <Feature title="Canlı durum takibi" icon={<TrackIcon />}>
            "Yeni"den "Çözüldü"ye kadar tüm aşamaları ve belediye yanıtlarını gör.
          </Feature>
          <Feature title="Mobil uyumlu" icon={<MobileIcon />}>
            Telefon, tablet ve bilgisayardan aynı deneyim. Ayrıca yerel mobil uygulama da mevcut.
          </Feature>
          <Feature title="Belediye entegrasyonu" icon={<BuildingIcon />}>
            Başvurular belediyenin yönetim paneline ve kurumsal sistemine güvenle aktarılır.
          </Feature>
        </div>
      </section>

      {/* Şehirdeki bildirimler haritası */}
      <section className="section section-tint">
        <div className="section-head">
          <span className="section-kicker">ŞEFFAFLIK</span>
          <h2>Şehirdeki bildirimler</h2>
          <p className="section-sub">
            Vatandaşların ilettiği bildirimler harita üzerinde durumlarına göre renklendirilmiştir. Kişisel
            bilgi paylaşılmaz; yalnızca konum, kategori ve işlem durumu gösterilir.
          </p>
        </div>
        <div className="insights-panel">
          <InsightsMap points={mapPoints} />
          <div className="map-legend">
            <LegendDot color="#1c7ed6" label="Yeni / İnceleniyor" />
            <LegendDot color="#f59f00" label="İşlemde" />
            <LegendDot color="#2f9e44" label="Çözüldü" />
            <LegendDot color="#e03131" label="Reddedildi" />
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="cta-band">
        <div className="cta-inner">
          <div>
            <h2>Bir sorun mu fark ettin?</h2>
            <p>Bildirimini birkaç dakikada oluştur, gerisini belediyeye bırak.</p>
          </div>
          <button type="button" className="btn btn-white btn-lg" onClick={onReport}>
            Hemen Bildirim Oluştur
          </button>
        </div>
      </section>
    </div>
  );
}

function StatTile({ value, label }: { value: string; label: string }) {
  return (
    <div className="stat-tile">
      <span className="stat-value">{value}</span>
      <span className="stat-label">{label}</span>
    </div>
  );
}

function LegendDot({ color, label }: { color: string; label: string }) {
  return (
    <span className="legend-item">
      <span className="legend-dot" style={{ background: color }} />
      {label}
    </span>
  );
}

function Step({ index, title, icon, children }: { index: string; title: string; icon: ReactNode; children: ReactNode }) {
  return (
    <div className="step-card">
      <div className="step-top">
        <span className="step-badge">{index}</span>
        <span className="step-icon">{icon}</span>
      </div>
      <h3>{title}</h3>
      <p>{children}</p>
    </div>
  );
}

function Feature({ title, icon, children }: { title: string; icon: ReactNode; children: ReactNode }) {
  return (
    <div className="feature-card">
      <span className="feature-icon">{icon}</span>
      <h3>{title}</h3>
      <p>{children}</p>
    </div>
  );
}

const CATEGORIES = [
  { label: 'Yol ve Kaldırım', icon: <RoadIcon /> },
  { label: 'Çöp ve Temizlik', icon: <TrashIcon /> },
  { label: 'Aydınlatma', icon: <BulbIcon /> },
  { label: 'Park ve Bahçe', icon: <TreeIcon /> },
  { label: 'Trafik', icon: <TrafficIcon /> },
  { label: 'Sokak Hayvanları', icon: <PawIcon /> },
  { label: 'Su ve Kanalizasyon', icon: <DropIcon /> },
  { label: 'Diğer', icon: <DotsIcon /> }
];

/* --- icons (inline, currentColor) --- */
function CheckIcon() {
  return (
    <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round">
      <path d="m5 13 4 4L19 7" />
    </svg>
  );
}
function PinIcon() {
  return (
    <svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 21s7-6.3 7-11a7 7 0 1 0-14 0c0 4.7 7 11 7 11Z" />
      <circle cx="12" cy="10" r="2.4" />
    </svg>
  );
}
function PhotoIcon() {
  return (
    <svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="5" width="18" height="14" rx="2" />
      <circle cx="8.5" cy="10" r="1.5" />
      <path d="m21 16-5-5L5 19" />
    </svg>
  );
}
function TrackIcon() {
  return (
    <svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="11" cy="11" r="7" />
      <path d="m20 20-3.2-3.2" />
    </svg>
  );
}
function ShieldIcon() {
  return (
    <svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 3 5 6v6c0 4 3 7 7 9 4-2 7-5 7-9V6l-7-3Z" />
      <path d="m9 12 2 2 4-4" />
    </svg>
  );
}
function MobileIcon() {
  return (
    <svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <rect x="7" y="3" width="10" height="18" rx="2.4" />
      <path d="M11 18h2" />
    </svg>
  );
}
function BuildingIcon() {
  return (
    <svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M4 21V6l7-3 7 3v15" />
      <path d="M9 9h.01M9 13h.01M9 17h.01M14 9h.01M14 13h.01M14 17h.01" />
    </svg>
  );
}
function RoadIcon() {
  return (
    <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M7 21 9 3M17 21 15 3M12 6v2M12 11v2M12 16v2" />
    </svg>
  );
}
function TrashIcon() {
  return (
    <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13" />
    </svg>
  );
}
function BulbIcon() {
  return (
    <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9 18h6M10 21h4M12 3a6 6 0 0 0-3.5 10.9c.5.4.5 1 .5 1.6V16h6v-.5c0-.6 0-1.2.5-1.6A6 6 0 0 0 12 3Z" />
    </svg>
  );
}
function TreeIcon() {
  return (
    <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 21v-5M12 16a5 5 0 0 0 3-9 4 4 0 0 0-6 0 5 5 0 0 0 3 9Z" />
    </svg>
  );
}
function TrafficIcon() {
  return (
    <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <rect x="8" y="3" width="8" height="18" rx="3" />
      <path d="M12 7v.01M12 12v.01M12 17v.01" />
    </svg>
  );
}
function PawIcon() {
  return (
    <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="7" cy="9" r="1.6" />
      <circle cx="12" cy="7" r="1.6" />
      <circle cx="17" cy="9" r="1.6" />
      <path d="M12 12c-2.4 0-4 1.7-4 3.6 0 1.6 1.4 2.4 4 2.4s4-.8 4-2.4c0-1.9-1.6-3.6-4-3.6Z" />
    </svg>
  );
}
function DropIcon() {
  return (
    <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 3s6 6.5 6 10.5A6 6 0 0 1 6 13.5C6 9.5 12 3 12 3Z" />
    </svg>
  );
}
function DotsIcon() {
  return (
    <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round">
      <path d="M6 12h.01M12 12h.01M18 12h.01" />
    </svg>
  );
}

function HeroArt() {
  return (
    <svg viewBox="0 0 420 360" width="100%" role="img" aria-label="Harita üzerinde bildirim noktaları">
      <defs>
        <linearGradient id="mapGrad" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#12235c" />
          <stop offset="1" stopColor="#1b3a86" />
        </linearGradient>
        <linearGradient id="pinGrad" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#22b8e8" />
          <stop offset="1" stopColor="#1c65c9" />
        </linearGradient>
      </defs>
      <rect x="14" y="14" width="392" height="332" rx="22" fill="url(#mapGrad)" />
      {/* roads */}
      <g stroke="#2f4f9e" strokeWidth="10" strokeLinecap="round" opacity="0.9">
        <path d="M40 120 H380" />
        <path d="M40 235 H380" />
        <path d="M150 40 V330" />
        <path d="M280 40 V330" />
      </g>
      <g stroke="#4a6ec2" strokeWidth="3" strokeDasharray="2 10" strokeLinecap="round">
        <path d="M40 120 H380" />
        <path d="M40 235 H380" />
        <path d="M150 40 V330" />
        <path d="M280 40 V330" />
      </g>
      {/* blocks */}
      <g fill="#233f8a" opacity="0.8">
        <rect x="58" y="52" width="72" height="48" rx="7" />
        <rect x="300" y="52" width="66" height="48" rx="7" />
        <rect x="58" y="255" width="72" height="55" rx="7" />
        <rect x="300" y="255" width="66" height="55" rx="7" />
        <rect x="176" y="140" width="82" height="70" rx="8" />
      </g>
      {/* pins */}
      <Pin x={150} y={120} />
      <Pin x={280} y={235} small />
      <Pin x={95} y={230} small />
      {/* floating status card */}
      <g>
        <rect x="228" y="250" width="150" height="78" rx="12" fill="#ffffff" />
        <circle cx="248" cy="273" r="8" fill="#e6f0fd" />
        <path d="m244.5 273 2.5 2.5 4-4.5" fill="none" stroke="#1c65c9" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
        <rect x="264" y="268" width="96" height="8" rx="4" fill="#16213d" />
        <rect x="264" y="283" width="70" height="7" rx="3.5" fill="#9aa8c6" />
        <rect x="240" y="303" width="120" height="10" rx="5" fill="#e6f0fd" />
        <rect x="240" y="303" width="78" height="10" rx="5" fill="#22b8e8" />
      </g>
    </svg>
  );
}

function Pin({ x, y, small }: { x: number; y: number; small?: boolean }) {
  const s = small ? 0.72 : 1;
  return (
    <g transform={`translate(${x} ${y}) scale(${s})`}>
      <ellipse cx="0" cy="6" rx="12" ry="4" fill="#0a2a1f" opacity="0.5" />
      <path d="M0-34c-8.8 0-16 7-16 15.7C-16-8 0 6 0 6s16-14 16-24.3C16-27 8.8-34 0-34Z" fill="url(#pinGrad)" />
      <circle cx="0" cy="-18" r="6.4" fill="#ffffff" />
    </g>
  );
}
