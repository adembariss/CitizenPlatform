import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  AdminComplaintListItem,
  DashboardSummary,
  getAdminComplaints,
  getDashboardSummary
} from '../lib/api';
import { formatDateTime, statusClass, statusLabel } from '../lib/labels';

export function DashboardPage() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [complaints, setComplaints] = useState<AdminComplaintListItem[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        const [summaryResponse, complaintsResponse] = await Promise.all([
          getDashboardSummary(),
          getAdminComplaints({ pageSize: 10 })
        ]);

        if (cancelled) {
          return;
        }

        if (summaryResponse.success && summaryResponse.data) {
          setSummary(summaryResponse.data);
        }

        setComplaints(complaintsResponse.items ?? []);
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
        { label: 'Toplam bildirim', value: summary.totalComplaints },
        { label: 'Açık bildirim', value: summary.openComplaints },
        { label: 'Bugün gelen', value: summary.todayComplaints },
        { label: 'Çözülen', value: summary.resolvedComplaints }
      ]
    : [];

  return (
    <>
      <header>
        <p>Belediye yönetim paneli</p>
        <h1>Vatandaş bildirimleri</h1>
      </header>

      {loadError && <p className="form-error">{loadError}</p>}

      <div className="metric-grid">
        {metrics.map((metric) => (
          <article key={metric.label} className="metric-card">
            <span>{metric.label}</span>
            <strong>{metric.value}</strong>
          </article>
        ))}
      </div>

      <section className="table-panel" aria-label="Son bildirimler">
        <div className="table-row table-head">
          <span>Takip Kodu</span>
          <span>Konu</span>
          <span>Kategori</span>
          <span>Durum</span>
        </div>
        {complaints.length === 0 && <div className="table-row">Henüz bildirim yok.</div>}
        {complaints.map((complaint) => (
          <Link className="table-row table-row-link" key={complaint.id} to={`/complaints/${complaint.id}`}>
            <span>{complaint.trackingCode}</span>
            <span>{complaint.title}</span>
            <span>{complaint.categoryName}</span>
            <span>
              <span className={statusClass(complaint.status)}>{statusLabel(complaint.status)}</span>
            </span>
          </Link>
        ))}
      </section>

      {summary && summary.byStatus.length > 0 && (
        <section className="table-panel" aria-label="Duruma göre dağılım">
          <div className="table-row table-head table-row-2col">
            <span>Durum</span>
            <span>Adet</span>
          </div>
          {summary.byStatus.map((entry) => (
            <div className="table-row table-row-2col" key={entry.status}>
              <span>{statusLabel(entry.status)}</span>
              <span>{entry.count}</span>
            </div>
          ))}
          <div className="table-row table-row-2col table-footnote">
            <span>Ortalama çözüm süresi</span>
            <span>{summary.averageResolutionHours.toFixed(1)} saat</span>
          </div>
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
