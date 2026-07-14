import { FormEvent, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { AdminComplaintListResponse, Category, getAdminComplaints, getCategories, getProvinces, getStoredUser } from '../lib/api';
import { COMPLAINT_STATUSES, formatDateTime, statusClass, statusLabel } from '../lib/labels';

const PAGE_SIZE = 20;

export function ComplaintsPage() {
  const isSystemAdmin = getStoredUser()?.userType === 'SystemAdmin';
  const [statusFilter, setStatusFilter] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [provinceFilter, setProvinceFilter] = useState('');
  const [provinces, setProvinces] = useState<string[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [categories, setCategories] = useState<Category[]>([]);
  const [data, setData] = useState<AdminComplaintListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    getCategories()
      .then((response) => {
        if (!cancelled && response.success && response.data) {
          setCategories(response.data);
        }
      })
      .catch(() => undefined);

    if (isSystemAdmin) {
      getProvinces()
        .then((list) => {
          if (!cancelled) setProvinces(list);
        })
        .catch(() => undefined);
    }

    return () => {
      cancelled = true;
    };
  }, [isSystemAdmin]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setLoadError(null);

    getAdminComplaints({
      status: statusFilter || undefined,
      categoryId: categoryFilter || undefined,
      search: search || undefined,
      province: provinceFilter || undefined,
      page,
      pageSize: PAGE_SIZE
    })
      .then((response) => {
        if (!cancelled) {
          setData(response);
        }
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          setLoadError(error instanceof Error ? error.message : 'Şikayetler yüklenemedi.');
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [statusFilter, categoryFilter, search, provinceFilter, page]);

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;

  function handleSearchSubmit(event: FormEvent) {
    event.preventDefault();
    setPage(1);
    setSearch(searchInput);
  }

  return (
    <>
      <header>
        <p>Belediye yönetim paneli</p>
        <h1>Şikayetler</h1>
      </header>

      <form className="filter-bar" onSubmit={handleSearchSubmit}>
        <label>
          Durum
          <select
            value={statusFilter}
            onChange={(event) => {
              setPage(1);
              setStatusFilter(event.target.value);
            }}
          >
            <option value="">Tümü</option>
            {COMPLAINT_STATUSES.map((status) => (
              <option key={status} value={status}>
                {statusLabel(status)}
              </option>
            ))}
          </select>
        </label>
        <label>
          Kategori
          <select
            value={categoryFilter}
            onChange={(event) => {
              setPage(1);
              setCategoryFilter(event.target.value);
            }}
          >
            <option value="">Tümü</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </label>
        {isSystemAdmin && (
          <label>
            İl
            <select
              value={provinceFilter}
              onChange={(event) => {
                setPage(1);
                setProvinceFilter(event.target.value);
              }}
            >
              <option value="">Tümü</option>
              {provinces.map((province) => (
                <option key={province} value={province}>
                  {province}
                </option>
              ))}
            </select>
          </label>
        )}
        <label>
          Arama
          <input
            type="search"
            placeholder="Takip kodu, konu, adres..."
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
          />
        </label>
        <button type="submit" className="button-secondary">
          Ara
        </button>
      </form>

      {loadError && <p className="form-error">{loadError}</p>}

      <section className="table-panel" aria-label="Şikayet listesi">
        <div className="table-row table-row-complaints table-head">
          <span>Takip Kodu</span>
          <span>Konu</span>
          <span>Belediye</span>
          <span>Kategori</span>
          <span>Birim</span>
          <span>Durum</span>
          <span>Tarih</span>
        </div>
        {loading && <div className="table-row table-row-complaints">Yükleniyor...</div>}
        {!loading && (data?.items.length ?? 0) === 0 && (
          <div className="table-row table-row-complaints">Kayıt bulunamadı.</div>
        )}
        {!loading &&
          data?.items.map((complaint) => (
            <Link
              className="table-row table-row-complaints table-row-link"
              key={complaint.id}
              to={`/complaints/${complaint.id}`}
            >
              <span>{complaint.trackingCode}</span>
              <span>{complaint.title}</span>
              <span>{complaint.municipalityName}</span>
              <span>{complaint.categoryName}</span>
              <span>{complaint.departmentName ?? '-'}</span>
              <span>
                <span className={statusClass(complaint.status)}>{statusLabel(complaint.status)}</span>
              </span>
              <span>{formatDateTime(complaint.createdAt)}</span>
            </Link>
          ))}
      </section>

      <div className="pagination">
        <button type="button" className="button-secondary" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>
          Önceki
        </button>
        <span>
          Sayfa {data?.page ?? page} / {totalPages} — {data?.totalCount ?? 0} kayıt
        </span>
        <button
          type="button"
          className="button-secondary"
          disabled={page >= totalPages || loading}
          onClick={() => setPage(page + 1)}
        >
          Sonraki
        </button>
      </div>
    </>
  );
}
