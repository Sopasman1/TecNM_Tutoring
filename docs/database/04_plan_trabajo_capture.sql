-- Ejecutar en tecnm_tutoring para actualizar un volumen existente.
ALTER TABLE tutoring_deliverables
    ADD COLUMN IF NOT EXISTS form_payload jsonb NOT NULL DEFAULT '{}';
