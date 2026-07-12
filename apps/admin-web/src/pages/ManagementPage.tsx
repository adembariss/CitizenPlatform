import { FormEvent, useCallback, useEffect, useState } from 'react';
import { ApiResponse } from '../lib/api';

export type ManagementItem = {
  id: string;
  municipalityId: string | null;
  name: string;
  code: string;
  isActive: boolean;
};

type ManagementPageProps = {
  title: string;
  subtitle: string;
  itemLabel: string;
  showScope?: boolean;
  load: () => Promise<ApiResponse<ManagementItem[]>>;
  create: (body: { name: string; code: string }) => Promise<ApiResponse<ManagementItem>>;
  update: (id: string, body: { name?: string | null; isActive?: boolean | null }) => Promise<ApiResponse<ManagementItem>>;
};

export function ManagementPage({ title, subtitle, itemLabel, showScope, load, create, update }: ManagementPageProps) {
  const [items, setItems] = useState<ManagementItem[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  const [newName, setNewName] = useState('');
  const [newCode, setNewCode] = useState('');
  const [creating, setCreating] = useState(false);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingName, setEditingName] = useState('');
  const [savingId, setSavingId] = useState<string | null>(null);

  const reload = useCallback(async () => {
    const response = await load();
    if (response.success && response.data) {
      setItems(response.data);
      setLoadError(null);
    } else {
      setLoadError(response.message ?? 'Liste yüklenemedi.');
    }
  }, [load]);

  useEffect(() => {
    void reload();
  }, [reload]);

  function showResult(response: ApiResponse<ManagementItem>, successText: string): boolean {
    if (response.success) {
      setActionError(null);
      setActionSuccess(successText);
      return true;
    }

    const details = response.errors.length > 0 ? ` ${response.errors.join(' ')}` : '';
    setActionSuccess(null);
    setActionError(`${response.message ?? 'İşlem başarısız oldu.'}${details}`);
    return false;
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    setCreating(true);

    const response = await create({ name: newName.trim(), code: newCode.trim() });
    setCreating(false);

    if (showResult(response, `${itemLabel} oluşturuldu.`)) {
      setNewName('');
      setNewCode('');
      await reload();
    }
  }

  async function handleRename(item: ManagementItem) {
    const name = editingName.trim();
    if (!name) {
      setActionSuccess(null);
      setActionError('Ad boş olamaz.');
      return;
    }

    setSavingId(item.id);
    const response = await update(item.id, { name });
    setSavingId(null);

    if (showResult(response, `${itemLabel} yeniden adlandırıldı.`)) {
      setEditingId(null);
      await reload();
    }
  }

  async function handleToggle(item: ManagementItem) {
    setSavingId(item.id);
    const response = await update(item.id, { isActive: !item.isActive });
    setSavingId(null);

    if (showResult(response, item.isActive ? `${itemLabel} pasifleştirildi.` : `${itemLabel} aktifleştirildi.`)) {
      await reload();
    }
  }

  return (
    <>
      <header>
        <p>{subtitle}</p>
        <h1>{title}</h1>
      </header>

      <form className="filter-bar" onSubmit={handleCreate}>
        <label>
          Ad
          <input type="text" required value={newName} onChange={(event) => setNewName(event.target.value)} />
        </label>
        <label>
          Kod
          <input
            type="text"
            required
            placeholder="örn. FEN-ISLERI"
            value={newCode}
            onChange={(event) => setNewCode(event.target.value)}
          />
        </label>
        <button type="submit" disabled={creating}>
          {creating ? 'Oluşturuluyor...' : `Yeni ${itemLabel.toLowerCase()} ekle`}
        </button>
      </form>

      {loadError && <p className="form-error">{loadError}</p>}
      {actionError && <p className="form-error">{actionError}</p>}
      {actionSuccess && <p className="form-success">{actionSuccess}</p>}

      <section className="table-panel" aria-label={title}>
        <div className={`table-row table-head ${showScope ? 'table-row-manage-scope' : 'table-row-manage'}`}>
          <span>Ad</span>
          <span>Kod</span>
          {showScope && <span>Kapsam</span>}
          <span>Durum</span>
          <span>İşlemler</span>
        </div>
        {items.length === 0 && !loadError && (
          <div className={`table-row ${showScope ? 'table-row-manage-scope' : 'table-row-manage'}`}>Kayıt bulunamadı.</div>
        )}
        {items.map((item) => (
          <div className={`table-row ${showScope ? 'table-row-manage-scope' : 'table-row-manage'}`} key={item.id}>
            <span>
              {editingId === item.id ? (
                <input
                  type="text"
                  className="inline-input"
                  value={editingName}
                  onChange={(event) => setEditingName(event.target.value)}
                />
              ) : (
                item.name
              )}
            </span>
            <span>{item.code}</span>
            {showScope && <span>{item.municipalityId ? 'Belediye' : 'Genel'}</span>}
            <span>
              <span className={item.isActive ? 'status-badge status-active' : 'status-badge status-passive'}>
                {item.isActive ? 'Aktif' : 'Pasif'}
              </span>
            </span>
            <span className="row-actions">
              {editingId === item.id ? (
                <>
                  <button
                    type="button"
                    className="button-secondary"
                    disabled={savingId === item.id}
                    onClick={() => void handleRename(item)}
                  >
                    Kaydet
                  </button>
                  <button type="button" className="button-secondary" onClick={() => setEditingId(null)}>
                    Vazgeç
                  </button>
                </>
              ) : (
                <>
                  <button
                    type="button"
                    className="button-secondary"
                    onClick={() => {
                      setEditingId(item.id);
                      setEditingName(item.name);
                    }}
                  >
                    Yeniden adlandır
                  </button>
                  <button
                    type="button"
                    className="button-secondary"
                    disabled={savingId === item.id}
                    onClick={() => void handleToggle(item)}
                  >
                    {item.isActive ? 'Pasifleştir' : 'Aktifleştir'}
                  </button>
                </>
              )}
            </span>
          </div>
        ))}
      </section>
    </>
  );
}
