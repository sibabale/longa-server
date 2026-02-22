#!/usr/bin/env node
/**
 * Seeds the Neon database with driver trips for testing.
 * Run: node scripts/seed-trips.js
 *
 * Requires DATABASE_URL or ConnectionStrings__Default in env, or pass --url.
 * Example: DATABASE_URL="postgresql://user:pass@host/db?sslmode=require" node scripts/seed-trips.js
 */

const { Client } = require("pg");

const SEED_USER_IDENTIFIER = "seed-script-driver";
const SEED_TRIPS = [
  {
    pickup_address: "OR Tambo International Airport, Johannesburg",
    pickup_lat: -26.1367,
    pickup_lng: 28.2411,
    destination_address: "Sandton City, Johannesburg",
    destination_lat: -26.1076,
    destination_lng: 28.0567,
    departure_at: () => addHours(new Date(), 2),
    price_display: "R 245",
  },
  {
    pickup_address: "Cape Town International Airport",
    pickup_lat: -33.9715,
    pickup_lng: 18.6021,
    destination_address: "V&A Waterfront, Cape Town",
    destination_lat: -33.9024,
    destination_lng: 18.4222,
    departure_at: () => addHours(new Date(), 3),
    price_display: "R 220",
  },
  {
    pickup_address: "OR Tambo International Airport, Johannesburg",
    pickup_lat: -26.1367,
    pickup_lng: 28.2411,
    destination_address: "Pretoria Central",
    destination_lat: -25.7479,
    destination_lng: 28.2293,
    departure_at: () => addHours(new Date(), 5),
    price_display: "R 260",
  },
  {
    pickup_address: "Durban Central",
    pickup_lat: -29.8587,
    pickup_lng: 31.0218,
    destination_address: "King Shaka International Airport",
    destination_lat: -29.6486,
    destination_lng: 31.1055,
    departure_at: () => addHours(new Date(), 6),
    price_display: "R 235",
  },
  {
    pickup_address: "Sandton City, Johannesburg",
    pickup_lat: -26.1076,
    pickup_lng: 28.0567,
    destination_address: "OR Tambo International Airport, Johannesburg",
    destination_lat: -26.1367,
    destination_lng: 28.2411,
    departure_at: () => addHours(new Date(), 8),
    price_display: "R 250",
  },
];

function addHours(d, h) {
  const out = new Date(d);
  out.setTime(out.getTime() + h * 60 * 60 * 1000);
  return out.toISOString();
}

function getConnectionString() {
  const url = process.env.DATABASE_URL || process.env.ConnectionStrings__Default;
  if (url) return url;
  const idx = process.argv.indexOf("--url");
  if (idx !== -1 && process.argv[idx + 1]) return process.argv[idx + 1];
  console.error(
    "Missing connection string. Set DATABASE_URL or ConnectionStrings__Default, or use --url <connection-string>",
  );
  process.exit(1);
}

async function main() {
  const connectionString = getConnectionString();
  const client = new Client({ connectionString });

  try {
    await client.connect();

    const userRes = await client.query(
      `INSERT INTO users (identifier_for_vendor, device_model, device_make)
       VALUES ($1, 'Seed Script', 'CLI')
       ON CONFLICT (identifier_for_vendor) DO UPDATE SET updated_at = NOW()
       RETURNING id`,
      [SEED_USER_IDENTIFIER],
    );
    const userId = userRes.rows[0].id;
    console.log("Using user:", userId);

    for (const t of SEED_TRIPS) {
      const departureAt =
        typeof t.departure_at === "function" ? t.departure_at() : t.departure_at;
      await client.query(
        `INSERT INTO trips (user_id, role, status, pickup_address, pickup_lat, pickup_lng,
         destination_address, destination_lat, destination_lng, departure_at, price_display)
         VALUES ($1, 'driver', 'open', $2, $3, $4, $5, $6, $7, $8, $9)`,
        [
          userId,
          t.pickup_address,
          t.pickup_lat,
          t.pickup_lng,
          t.destination_address,
          t.destination_lat,
          t.destination_lng,
          departureAt,
          t.price_display,
        ],
      );
    }

    console.log(`Seeded ${SEED_TRIPS.length} driver trips.`);
  } catch (err) {
    console.error("Seed failed:", err.message);
    process.exit(1);
  } finally {
    await client.end();
  }
}

main();
