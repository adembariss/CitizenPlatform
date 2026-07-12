import { useState } from 'react';
import { ReportForm } from './views/ReportForm';
import { TrackComplaint } from './views/TrackComplaint';

type View = { name: 'report' } | { name: 'track'; initialCode?: string };

export function App() {
  const [view, setView] = useState<View>({ name: 'report' });

  return (
    <div className="app-shell">
      <nav className="view-tabs" aria-label="Ekranlar">
        <button
          type="button"
          className={view.name === 'report' ? 'tab active' : 'tab'}
          onClick={() => setView({ name: 'report' })}
        >
          Bildirim Oluştur
        </button>
        <button
          type="button"
          className={view.name === 'track' ? 'tab active' : 'tab'}
          onClick={() => setView({ name: 'track' })}
        >
          Şikayet Sorgula
        </button>
      </nav>
      <main className="citizen-shell">
        {view.name === 'report' ? (
          <ReportForm onTrack={(trackingCode) => setView({ name: 'track', initialCode: trackingCode })} />
        ) : (
          <TrackComplaint initialCode={view.initialCode} />
        )}
      </main>
    </div>
  );
}
