import { useEffect, useState } from 'react';
import { AdminComplaintAttachment, getAdminComplaintAttachment } from '../lib/api';
import { formatDateTime } from '../lib/labels';

type AttachmentGalleryProps = {
  complaintId: string;
  attachments: AdminComplaintAttachment[];
};

type PreviewState = {
  attachment: AdminComplaintAttachment;
  objectUrl: string;
};

export function AttachmentGallery({ complaintId, attachments }: AttachmentGalleryProps) {
  const [preview, setPreview] = useState<PreviewState | null>(null);
  const [loadingId, setLoadingId] = useState<string | null>(null);
  const [thumbnailUrls, setThumbnailUrls] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    const objectUrls: string[] = [];

    async function loadThumbnails() {
      const entries = await Promise.all(
        attachments
          .filter((attachment) => attachment.contentType.startsWith('image/'))
          .map(async (attachment) => {
            try {
              const blob = await getAdminComplaintAttachment(complaintId, attachment.id);
              const objectUrl = URL.createObjectURL(blob);
              objectUrls.push(objectUrl);
              return [attachment.id, objectUrl] as const;
            } catch {
              return null;
            }
          })
      );

      if (!cancelled) {
        setThumbnailUrls(Object.fromEntries(entries.filter((entry) => entry !== null)));
      }
    }

    void loadThumbnails();

    return () => {
      cancelled = true;
      objectUrls.forEach((objectUrl) => URL.revokeObjectURL(objectUrl));
    };
  }, [attachments, complaintId]);

  useEffect(() => {
    return () => {
      if (preview) {
        URL.revokeObjectURL(preview.objectUrl);
      }
    };
  }, [preview]);

  useEffect(() => {
    if (!preview) return;

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') closePreview();
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    window.addEventListener('keydown', handleKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [preview]);

  async function loadAttachment(attachment: AdminComplaintAttachment): Promise<string | null> {
    setLoadingId(attachment.id);
    setError(null);

    try {
      const blob = await getAdminComplaintAttachment(complaintId, attachment.id);
      return URL.createObjectURL(blob);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Ek dosya alınamadı.');
      return null;
    } finally {
      setLoadingId(null);
    }
  }

  async function showPreview(attachment: AdminComplaintAttachment) {
    const objectUrl = await loadAttachment(attachment);
    if (!objectUrl) return;

    setPreview((current) => {
      if (current) URL.revokeObjectURL(current.objectUrl);
      return { attachment, objectUrl };
    });
  }

  async function downloadAttachment(attachment: AdminComplaintAttachment) {
    const objectUrl = await loadAttachment(attachment);
    if (!objectUrl) return;

    const anchor = document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = attachment.originalFileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(objectUrl);
  }

  function closePreview() {
    setPreview((current) => {
      if (current) URL.revokeObjectURL(current.objectUrl);
      return null;
    });
  }

  return (
    <section className="detail-panel" aria-label="Ekler">
      <div className="attachment-section-head">
        <div>
          <h2>Ekler ({attachments.length})</h2>
          <p>Bildirimle birlikte gönderilen fotoğraf ve belgeler</p>
        </div>
      </div>

      {error && (
        <p className="form-error" role="alert">
          {error} Lütfen tekrar deneyin.
        </p>
      )}

      <ul className="attachment-grid">
        {attachments.map((attachment) => {
          const isImage = attachment.contentType.startsWith('image/');
          const isLoading = loadingId === attachment.id;
          const thumbnailUrl = thumbnailUrls[attachment.id];
          return (
            <li className="attachment-card" key={attachment.id}>
              <button
                type="button"
                className="attachment-preview-button"
                onClick={() => void showPreview(attachment)}
                disabled={isLoading}
                aria-label={`${attachment.originalFileName} dosyasını önizle`}
              >
                <span className={`attachment-type-badge${isImage ? ' is-image' : ''}`}>
                  {fileTypeLabel(attachment.contentType)}
                </span>
                {thumbnailUrl ? (
                  <img className="attachment-thumbnail" src={thumbnailUrl} alt="" />
                ) : (
                  <span className={`attachment-placeholder${isImage ? ' is-loading' : ''}`} aria-hidden="true">
                    <FileIcon image={isImage} />
                  </span>
                )}
                <span className="attachment-preview-overlay">
                  <EyeIcon />
                  {isLoading ? 'Yükleniyor…' : 'Önizle'}
                </span>
              </button>
              <div className="attachment-info">
                <strong title={attachment.originalFileName}>{attachment.originalFileName}</strong>
                <span>{formatFileSize(attachment.sizeInBytes)} · {formatDateTime(attachment.createdAt)}</span>
                <span>{attachment.contentType}</span>
              </div>
              <div className="attachment-actions">
                <button type="button" onClick={() => void showPreview(attachment)} disabled={isLoading}>
                  <EyeIcon /> Önizle
                </button>
                <button type="button" onClick={() => void downloadAttachment(attachment)} disabled={isLoading}>
                  <DownloadIcon /> İndir
                </button>
              </div>
            </li>
          );
        })}
      </ul>

      {preview && (
        <div
          className="attachment-modal"
          role="dialog"
          aria-modal="true"
          aria-labelledby="attachment-modal-title"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) closePreview();
          }}
        >
          <div className="attachment-modal-card">
            <div className="attachment-modal-head">
              <div>
                <strong id="attachment-modal-title">{preview.attachment.originalFileName}</strong>
                <span>{preview.attachment.contentType} · {formatFileSize(preview.attachment.sizeInBytes)}</span>
              </div>
              <button type="button" className="modal-close" onClick={closePreview} aria-label="Önizlemeyi kapat">
                ×
              </button>
            </div>
            <div className="attachment-modal-body">
              {preview.attachment.contentType.startsWith('image/') ? (
                <img src={preview.objectUrl} alt={preview.attachment.originalFileName} />
              ) : (
                <iframe src={preview.objectUrl} title={preview.attachment.originalFileName} />
              )}
            </div>
            <div className="attachment-modal-actions">
              <a href={preview.objectUrl} target="_blank" rel="noreferrer"><ExternalIcon /> Yeni sekmede aç</a>
              <a href={preview.objectUrl} download={preview.attachment.originalFileName}><DownloadIcon /> İndir</a>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}

function fileTypeLabel(contentType: string): string {
  if (contentType === 'image/jpeg') return 'JPG';
  if (contentType === 'image/png') return 'PNG';
  if (contentType === 'image/webp') return 'WEBP';
  if (contentType === 'application/pdf') return 'PDF';
  const parts = contentType.split('/');
  return parts[parts.length - 1]?.toUpperCase() || 'DOSYA';
}

function FileIcon({ image }: { image: boolean }) {
  return image ? (
    <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 5.5A1.5 1.5 0 0 1 5.5 4h13A1.5 1.5 0 0 1 20 5.5v13a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 4 18.5v-13Z"/><path d="m5 17 4.2-4.2 2.6 2.6 2.2-2.2 5 5M15.8 9.2h.01"/></svg>
  ) : (
    <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 3.5h6l4 4v13H7v-17Z"/><path d="M13 3.5v4h4M9.5 12h5M9.5 15h5"/></svg>
  );
}

function EyeIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M2.5 12s3.5-5 9.5-5 9.5 5 9.5 5-3.5 5-9.5 5-9.5-5-9.5-5Z"/><circle cx="12" cy="12" r="2.5"/></svg>;
}

function DownloadIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3v12m0 0 4-4m-4 4-4-4M5 20h14"/></svg>;
}

function ExternalIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M14 5h5v5M19 5l-8 8"/><path d="M18 13v6H5V6h6"/></svg>;
}

function formatFileSize(sizeInBytes: number): string {
  if (sizeInBytes < 1024) return `${sizeInBytes} B`;
  if (sizeInBytes < 1024 * 1024) return `${(sizeInBytes / 1024).toFixed(1)} KB`;
  return `${(sizeInBytes / (1024 * 1024)).toFixed(1)} MB`;
}
