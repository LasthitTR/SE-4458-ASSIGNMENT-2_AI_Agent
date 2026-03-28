import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL =
  __ENV.BASE_URL ||
  'https://se4448-midterm-c2dnamecffbhehfd.swedencentral-01.azurewebsites.net';

const DUMMY_JWT_TOKEN =
  __ENV.JWT_TOKEN ||
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.dummy.payload.signature';

const BOOKING_PAYLOAD = JSON.stringify({
  listingId: '11111111-1111-1111-1111-111111111111',
  fromDate: '2026-04-10',
  toDate: '2026-04-12',
  noOfPeople: 2
});

const bookingHeaders = {
  headers: {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${DUMMY_JWT_TOKEN}`
  }
};

export const options = {
  scenarios: {
    normal_20_vus: {
      executor: 'constant-vus',
      vus: 20,
      duration: '30s',
      startTime: '0s',
      exec: 'runScenario'
    },
    peak_50_vus: {
      executor: 'constant-vus',
      vus: 50,
      duration: '30s',
      startTime: '30s',
      exec: 'runScenario'
    },
    stress_100_vus: {
      executor: 'constant-vus',
      vus: 100,
      duration: '30s',
      startTime: '60s',
      exec: 'runScenario'
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<1500']
  }
};

export function runScenario() {
  const listRes = http.get(`${BASE_URL}/api/Listings`);
  check(listRes, {
    'GET /api/Listings status is 200': (r) => r.status === 200
  });

  const bookingRes = http.post(
    `${BASE_URL}/api/Bookings`,
    BOOKING_PAYLOAD,
    bookingHeaders
  );

  check(bookingRes, {
    'POST /api/Bookings status is 200 or 201 or 400 or 401': (r) =>
      [200, 201, 400, 401].includes(r.status)
  });

  sleep(1);
}
