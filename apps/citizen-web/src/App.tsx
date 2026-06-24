export function App() {
  return (
    <main className="citizen-shell">
      <section className="intro">
        <p>Vatandaş başvuru ekranı</p>
        <h1>Mahallendeki sorunu belediyeye ilet.</h1>
      </section>
      <form className="report-form">
        <label>
          Başlık
          <input placeholder="Örn. Kaldırım hasarı" />
        </label>
        <label>
          Açıklama
          <textarea placeholder="Sorunu kısaca anlatın" rows={5} />
        </label>
        <div className="map-placeholder" role="img" aria-label="Harita seçim alanı">
          Konum seçimi
        </div>
        <button type="button">Bildirim oluştur</button>
      </form>
    </main>
  );
}
