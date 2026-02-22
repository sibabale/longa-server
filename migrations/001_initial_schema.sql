-- Longa Neon Database - Initial Schema
-- Run this against your Neon Postgres database

CREATE TABLE users (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  identifier_for_vendor TEXT NOT NULL,
  device_model        TEXT,
  device_make         TEXT,
  created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX idx_users_identifier_for_vendor ON users(identifier_for_vendor);

CREATE TABLE trips (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id          UUID NOT NULL REFERENCES users(id),
  role             TEXT NOT NULL CHECK (role IN ('driver', 'rider')),
  status           TEXT NOT NULL DEFAULT 'open' CHECK (status IN ('open', 'booked')),
  pickup_address   TEXT NOT NULL,
  pickup_lat       DECIMAL(10, 7) NOT NULL,
  pickup_lng       DECIMAL(10, 7) NOT NULL,
  destination_address TEXT NOT NULL,
  destination_lat  DECIMAL(10, 7) NOT NULL,
  destination_lng  DECIMAL(10, 7) NOT NULL,
  departure_at     TIMESTAMPTZ NOT NULL,
  price_cents      INT,
  created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT driver_price_check CHECK (
    (role = 'driver' AND price_cents IS NOT NULL) OR
    (role = 'rider')
  )
);

CREATE INDEX idx_trips_role_status ON trips(role, status);
CREATE INDEX idx_trips_departure_at ON trips(departure_at);

CREATE TABLE bookings (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  driver_trip_id  UUID NOT NULL REFERENCES trips(id),
  rider_trip_id   UUID NOT NULL REFERENCES trips(id),
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE(driver_trip_id),
  UNIQUE(rider_trip_id)
);

CREATE INDEX idx_bookings_driver_trip ON bookings(driver_trip_id);
CREATE INDEX idx_bookings_rider_trip ON bookings(rider_trip_id);

CREATE TABLE push_tokens (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id       UUID NOT NULL REFERENCES users(id),
  token         TEXT NOT NULL,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX idx_push_tokens_user ON push_tokens(user_id);

CREATE TABLE idempotency_keys (
  key             UUID PRIMARY KEY,
  user_id         UUID NOT NULL,
  trip_id         UUID NOT NULL REFERENCES trips(id),
  created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_idempotency_created ON idempotency_keys(created_at);
