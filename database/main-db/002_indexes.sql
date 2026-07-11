-- Supplemental indexes for CitizenPlatform main database.
-- EF Core migrations also create the core indexes. These IF NOT EXISTS statements
-- are intentionally idempotent for manually provisioned databases.

CREATE INDEX IF NOT EXISTS ix_municipality_boundaries_boundary_geometry_gist
    ON public.municipality_boundaries
    USING gist (boundary_geometry);

CREATE INDEX IF NOT EXISTS ix_complaints_location_geometry_gist
    ON public.complaints
    USING gist (location_geometry);

CREATE UNIQUE INDEX IF NOT EXISTS ux_complaints_tracking_code
    ON public.complaints (tracking_code);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email_active
    ON public.users (email)
    WHERE is_deleted = false;

CREATE UNIQUE INDEX IF NOT EXISTS ux_citizens_email_active
    ON public.citizens (email)
    WHERE email IS NOT NULL AND is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_integration_outbox_pending
    ON public.integration_outbox (status, next_retry_at)
    WHERE status = 'Pending' AND is_deleted = false;
