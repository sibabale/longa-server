-- Change price from cents (int) to display string (text)
-- Run after 001_initial_schema.sql

ALTER TABLE trips ADD COLUMN price_display TEXT;

-- Migrate existing driver trips: format as "R {amount}" (legacy default)
UPDATE trips
SET price_display = 'R ' || (price_cents / 100)
WHERE role = 'driver' AND price_cents IS NOT NULL;

ALTER TABLE trips DROP CONSTRAINT driver_price_check;
ALTER TABLE trips DROP COLUMN price_cents;

ALTER TABLE trips ADD CONSTRAINT driver_price_display_check CHECK (
  (role = 'driver' AND price_display IS NOT NULL AND price_display != '') OR
  (role = 'rider')
);
