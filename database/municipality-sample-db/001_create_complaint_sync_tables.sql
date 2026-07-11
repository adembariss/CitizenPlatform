-- Sample municipality database schema for local outbox synchronization tests.
-- This schema deliberately contains only the columns required by the demo writer.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS public.municipal_complaints (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    main_complaint_id uuid NOT NULL,
    municipality_id uuid NOT NULL,
    tracking_code varchar(64) NOT NULL,
    category_name varchar(200) NOT NULL,
    department_name varchar(200) NULL,
    citizen_full_name varchar(200) NULL,
    citizen_phone_number varchar(40) NULL,
    citizen_email varchar(320) NULL,
    description text NOT NULL,
    address_text text NULL,
    latitude double precision NOT NULL,
    longitude double precision NOT NULL,
    status varchar(50) NOT NULL,
    priority varchar(50) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    synced_at timestamp with time zone NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_municipal_complaints_main_complaint_id
    ON public.municipal_complaints (main_complaint_id);

CREATE INDEX IF NOT EXISTS ix_municipal_complaints_tracking_code
    ON public.municipal_complaints (tracking_code);

CREATE TABLE IF NOT EXISTS public.municipal_complaint_status_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    municipal_complaint_id uuid NOT NULL REFERENCES public.municipal_complaints (id) ON DELETE RESTRICT,
    main_complaint_id uuid NOT NULL,
    status varchar(50) NOT NULL,
    note text NULL,
    created_at timestamp with time zone NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_municipal_complaint_status_logs_main_status
    ON public.municipal_complaint_status_logs (main_complaint_id, status);
