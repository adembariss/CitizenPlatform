import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  AdminComplaintListItem,
  DashboardSummary,
  MunicipalityMapContext,
  getAdminComplaints,
  getDashboardSummary,
  getMunicipalityMapContext,
  getStoredUser
} from '../lib/api';
import { formatDateTime, statusClass, statusLabel } from '../lib/labels';
import { complaintAddress, reporterLabel } from '../lib/complaintPresentation';
import { MunicipalityComplaintMap } from '../components/MunicipalityComplaintMap';

export function DashboardPage() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [complaints, setComplaints] = useState<AdminComplaintListItem[]>([]);
  const [mapComplaints, setMapComplaints] = useState<AdminComplaintListItem[]>([]);
  const [mapContext, setMapContext] = useState<MunicipalityMapContext | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const currentUser = getStoredUser();
  const municipalityName = currentUser?.municipalityName ?? 'Tüm belediyeler';
  const employeeFirstName = currentUser?.fullName.split(' ').filter(Boolean)[0] ?? 'çalışma arkadaşımız';

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        const [summaryResponse, complaintsResponse, mapContextResponse] = await Promise.all([
          getDashboardSummary(),
          getAdminComplaints({ pageSize: 100 }),
          getMunicipalityMapContext()
        ]);

        if (cancelled) {
          return;
        }

        if (summaryResponse.success && summaryResponse.data) {
          setSummary(summaryResponse.data);
        }

        if (mapContextResponse.success && mapContextResponse.data) {
          setMapContext(mapContextResponse.data);
        }

        const items = complaintsResponse.items ?? [];
        setComplaints(items.slice(0, 10));
        setMapComplaints(items.filter((item) => isOpenStatus(item.status)));
      } catch {
        if (!cancelled) {
          setLoadError('Veriler yüklenemedi. API çalışıyor mu kontrol edin.');
        }
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  const metrics = summary
    ? [
        { label: 'Toplam bildirim', value: summary.totalComplaints, caption: 'Tüm zamanlar', tone: 'blue', icon: 'inbox' },
        { label: 'Açık bildirim', value: summary.openComplaints, caption: 'İşlem bekleyen', tone: 'amber', icon: 'clock' },
        { label: 'Bugün gelen', value: summary.todayComplaints, caption: 'Yeni başvurular', tone: 'violet', icon: 'today' },
        { label: 'Çözülen', value: summary.resolvedComplaints, caption: 'Tamamlanan işler', tone: 'green', icon: 'check' }
      ]
    : [];

  return (
    <>
      <header className="page-header dashboard-header">
        <div>
          <p>{municipalityName} · ÇALIŞAN PANELİ</p>
          <h1>Günaydın, {employeeFirstName}.</h1>
          <span>{municipalityName} sınırlarındaki talepleri takip edin, önceliklendirin ve sonuçlandırın.</span>
        </div>
        <div className="dashboard-header-actions">
          <span className="municipality-context-badge"><span aria-hidden="true">B</span>{municipalityName}</span>
          <Link className="header-primary-action" to="/complaints">
            Tüm bildirimleri aç <span aria-hidden="true">→</span>
          </Link>
        </div>
      </header>

      {loadError && <p className="form-error">{loadError}</p>}

      <div className="metric-grid">
        {metrics.map((metric) => (
          <article key={metric.label} className={`metric-card metric-${metric.tone}`}>
            <span className="metric-icon"><MetricIcon name={metric.icon} /></span>
            <div className="metric-copy">
              <span>{metric.label}</span>
              <strong>{metric.value}</strong>
              <small>{metric.caption}</small>
            </div>
          </article>
        ))}
      </div>

      <section className="municipality-map-card" aria-labelledby="municipality-map-title">
        <div className="section-heading-row municipality-map-heading">
          <div>
            <span className="section-eyebrow">KONUMSAL OPERASYON</span>
            <h2 id="municipality-map-title">{municipalityName} açık talep haritası</h2>
            <p>
              Devam eden {mapComplaints.length} talebin konumsal dağılımını inceleyin.
              {mapContext?.boundaryGeoJson ? ' Harita belediye hizmet sınırına kilitlidir.' : ''}
            </p>
          </div>
          <div className="municipality-map-heading-badges">
            {mapContext?.boundaryGeoJson && <span className="map-boundary-badge">Hizmet sınırı</span>}
            <span className="map-open-count"><strong>{mapComplaints.length}</strong> açık talep</span>
          </div>
        </div>
        <div className="municipality-map-body">
          <MunicipalityComplaintMap complaints={mapComplaints} context={mapContext} />
          <aside className="municipality-map-rail" aria-label="Açık talepler listesi">
            <div className="map-rail-head">
              <h3>Açık talepler</h3>
              <span className="map-rail-count">{mapComplaints.length}</span>
            </div>
            {mapComplaints.length === 0 ? (
              <div className="map-rail-empty">
                <span aria-hidden="true">✓</span>
                <p>Şu an bekleyen konumsal talep yok. Yeni başvurular burada listelenecek.</p>
              </div>
            ) : (
              <ul className="map-rail-list">
                {mapComplaints.slice(0, 8).map((complaint) => (
                  <li key={complaint.id}>
                    <Link className="map-rail-item" to={`/complaints/${complaint.id}`}>
                      <span className="map-rail-dot" style={{ background: dotColor(complaint.status) }} aria-hidden="true" />
                      <span className="map-rail-item-copy">
                        <strong>{complaint.title}</strong>
                        <small>{complaint.categoryName} · {complaintAddress(complaint)}</small>
                      </span>
                      <span className="map-rail-status">{statusLabel(complaint.status)}</span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
            {mapComplaints.length > 8 && (
              <Link className="map-rail-more" to="/complaints">
                +{mapComplaints.length - 8} talep daha <span aria-hidden="true">→</span>
              </Link>
            )}
          </aside>
        </div>
      </section>

      <section className="dashboard-section" aria-labelledby="recent-complaints-title">
        <div className="section-heading-row">
          <div>
            <h2 id="recent-complaints-title">Son bildirimler</h2>
            <p>En son oluşturulan {complaints.length} vatandaş kaydı</p>
          </div>
          <Link className="section-link" to="/complaints">Tümünü görüntüle <span aria-hidden="true">→</span></Link>
        </div>
        <div className="table-panel dashboard-table" aria-label="Son bildirimler">
          <div className="table-row table-row-dashboard table-head">
            <span>Takip Kodu</span>
            <span>Konu</span>
            <span>Başvuran</span>
            <span>Adres</span>
            <span>Kategori</span>
            <span>Durum</span>
          </div>
          {complaints.length === 0 && <div className="table-row">Henüz bildirim yok.</div>}
          {complaints.map((complaint) => (
            <Link className="table-row table-row-dashboard table-row-link" key={complaint.id} to={`/complaints/${complaint.id}`}>
              <TableCell label="Takip kodu" value={complaint.trackingCode} />
              <TableCell label="Konu" value={complaint.title} />
              <TableCell label="Başvuran" value={reporterLabel(complaint.citizenFullName)} />
              <TableCell label="Adres" value={complaintAddress(complaint)} title={complaintAddress(complaint)} />
              <TableCell label="Kategori" value={complaint.categoryName} />
              <span>
                <span className="mobile-cell-label">Durum</span>
                <span className={statusClass(complaint.status)}>{statusLabel(complaint.status)}</span>
              </span>
            </Link>
          ))}
        </div>
      </section>

      {summary && summary.byStatus.length > 0 && (
        <section className="dashboard-insights" aria-label="Operasyon özeti">
          <article className="dashboard-status-panel">
            <div className="section-heading-row compact">
              <div>
                <h2>Durum dağılımı</h2>
                <p>Aktif iş yükünün güncel görünümü</p>
              </div>
            </div>
            <div className="status-breakdown">
              {summary.byStatus.map((entry) => {
                const percentage = summary.totalComplaints > 0 ? Math.round((entry.count / summary.totalComplaints) * 100) : 0;
                return (
                  <div className="status-breakdown-row" key={entry.status}>
                    <div><span>{statusLabel(entry.status)}</span><strong>{entry.count}</strong></div>
                    <span className="status-progress"><span style={{ width: `${Math.max(percentage, 4)}%` }} /></span>
                    <small>%{percentage}</small>
                  </div>
                );
              })}
            </div>
          </article>
          <article className="resolution-summary-card">
            <span className="resolution-icon"><MetricIcon name="speed" /></span>
            <span>Ortalama çözüm süresi</span>
            <strong>{summary.averageResolutionHours.toFixed(1)} <small>saat</small></strong>
            <p>Vatandaş taleplerini sonuçlandırma ortalamanız.</p>
            <Link to="/complaints">İş yükünü incele <span aria-hidden="true">→</span></Link>
          </article>
        </section>
      )}

      {complaints.length > 0 && (
        <p className="panel-footnote">
          Son bildirim: {formatDateTime(complaints[0].createdAt)} — tüm kayıtlar için{' '}
          <Link className="inline-link" to="/complaints">
            Şikayetler
          </Link>{' '}
          sayfasına gidin.
        </p>
      )}
    </>
  );
}

function isOpenStatus(status: string): boolean {
  return !['resolved', 'closed', 'rejected', 'duplicate', 'outofscope'].includes(status.toLowerCase());
}

// Yan listedeki durum noktası rengi — harita ile aynı renk kodları.
function dotColor(status: string): string {
  const normalized = status.toLowerCase();
  if (normalized === 'inprogress' || normalized === 'assigned') return '#6750c5';
  if (normalized === 'underreview' || normalized === 'waitingforcitizen') return '#e28a12';
  return '#2775d7';
}

function MetricIcon({ name }: { name: string }) {
  if (name === 'clock') return <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></svg>;
  if (name === 'today') return <svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="5" width="18" height="16" rx="3" /><path d="M8 3v4M16 3v4M3 10h18M8 14h3M8 17h6" /></svg>;
  if (name === 'check') return <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9" /><path d="m8 12 2.5 2.5L16 9" /></svg>;
  if (name === 'speed') return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 16a8 8 0 1 1 16 0M12 16l4-5" /><path d="M7 19h10" /></svg>;
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 7h16v12H4zM7 4h10v3M8 11h8M8 15h5" /></svg>;
}

function TableCell({ label, value, title }: { label: string; value: string; title?: string }) {
  return (
    <span title={title}>
      <span className="mobile-cell-label">{label}</span>
      <span>{value}</span>
    </span>
  );
}
