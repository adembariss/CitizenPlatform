const metrics = [
  { label: 'Açık bildirim', value: '128' },
  { label: 'Bugün gelen', value: '34' },
  { label: 'SLA riski', value: '7' }
];

export function App() {
  return (
    <main className="app-shell">
      <aside className="sidebar">
        <strong>CitizenPlatform</strong>
        <nav>
          <a href="/">Panel</a>
          <a href="/">Bildirimler</a>
          <a href="/">Harita</a>
          <a href="/">Birimler</a>
        </nav>
      </aside>
      <section className="content">
        <header>
          <p>Belediye yönetim paneli</p>
          <h1>Vatandaş bildirimleri</h1>
        </header>
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
            <span>Konu</span>
            <span>Mahalle</span>
            <span>Durum</span>
          </div>
          <div className="table-row">
            <span>Yol bakım talebi</span>
            <span>Atatürk</span>
            <span>İncelemede</span>
          </div>
          <div className="table-row">
            <span>Park aydınlatması</span>
            <span>Cumhuriyet</span>
            <span>Atandı</span>
          </div>
        </section>
      </section>
    </main>
  );
}
