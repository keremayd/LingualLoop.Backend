-- 2026-11-16 — Ünite adı dile göre: karty_unit.title → karty_unit_translation.
--
-- Ünite adlarının hepsi, Türkçe dahil, karty_unit_translation'da dil başına
-- bir satır. Türkçe adlar title kolonundaydı; buraya 'tr' satırı olarak
-- kopyalanır ve kolon kalkar (kod 16 Kas'tan beri okumuyor).
--
-- Tek işlemde çalıştır: psql --single-transaction -f bu_dosya.sql
-- Sıra: eski API durdurulur → betik → yeni API (eski kod title'ı okuyordu).
-- İdempotent: kolon yoksa kopyalama ve kaldırma atlanır.

CREATE TABLE IF NOT EXISTS karty_unit_translation (
    karty_unit_id integer NOT NULL
        REFERENCES karty_unit (karty_unit_id) ON DELETE CASCADE,
    locale text NOT NULL,
    title text NOT NULL,
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    PRIMARY KEY (karty_unit_id, locale)
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'karty_unit' AND column_name = 'title'
    ) THEN
        RAISE NOTICE 'karty_unit.title zaten yok; atlandı.';
        RETURN;
    END IF;

    INSERT INTO karty_unit_translation (karty_unit_id, locale, title, updated_at)
    SELECT karty_unit_id, 'tr', title, now()
    FROM karty_unit
    WHERE btrim(title) <> ''
    ON CONFLICT (karty_unit_id, locale) DO NOTHING;

    -- Kolon kalkmadan önce: her ünitenin Türkçe adı tabloda olmalı.
    IF EXISTS (
        SELECT 1 FROM karty_unit u
        WHERE NOT EXISTS (
            SELECT 1 FROM karty_unit_translation t
            WHERE t.karty_unit_id = u.karty_unit_id AND t.locale = 'tr'
              AND t.title = u.title)
    ) THEN
        RAISE EXCEPTION 'Türkçe adı tabloya geçmemiş ünite var; hiçbir şey değişmedi.';
    END IF;

    ALTER TABLE karty_unit DROP COLUMN title;
END $$;
