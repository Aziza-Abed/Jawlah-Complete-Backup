// ──────────────────────────────────────────────────────────────
// Shared helpers for integration tests
// ──────────────────────────────────────────────────────────────
import axios from 'axios';
import FormData from 'form-data';
import { CONFIG } from './config.js';

// ── Axios clients (one per role) ─────────────────────────────

function createClient(label) {
  const client = axios.create({
    baseURL: CONFIG.API_BASE_URL,
    timeout: 15000,
    headers: { 'Content-Type': 'application/json' },
    validateStatus: () => true, // never throw — we assert manually
  });
  client._label = label;
  return client;
}

export const clients = {
  anonymous: createClient('anonymous'),
  admin: createClient('admin'),
  supervisor: createClient('supervisor'),
  worker: createClient('worker'),
  // Simulates mobile client (adds X-Device-Id header)
  mobileWorker: createClient('mobileWorker'),
};

// ── Token store ──────────────────────────────────────────────

// Use globalThis to share tokens across test files (vitest isolates modules per file)
if (!globalThis.__followup_tokens) {
  globalThis.__followup_tokens = { admin: null, supervisor: null, worker: null, mobileWorker: null };
}
export const tokens = globalThis.__followup_tokens;

// ── Login helper ─────────────────────────────────────────────

/**
 * Authenticate a role and attach the Bearer token to its client.
 * Supports both web-style (/auth/login) and mobile-style (/auth/login-gps).
 * If OTP is required, the test will skip (cannot automate SMS).
 */
export async function loginAs(role, { mobile = false } = {}) {
  // Skip if already logged in (avoids hitting rate limits across test files)
  if (tokens[role]) {
    clients[role].defaults.headers.common['Authorization'] = `Bearer ${tokens[role]}`;
    return { token: tokens[role] };
  }

  const creds = CONFIG.users[role === 'mobileWorker' ? 'worker' : role];
  if (!creds) throw new Error(`Unknown role: ${role}`);

  const endpoint = mobile ? '/auth/login-gps' : '/auth/login';
  const body = { username: creds.username, password: creds.password };

  if (mobile) {
    body.deviceId = CONFIG.devices.mobileDeviceId;
  }

  const res = await clients.anonymous.post(endpoint, body);

  if (res.status === 429) {
    throw new Error(`Login rate-limited for ${role}: wait 60s and retry`);
  }

  if (res.status !== 200 || !res.data?.success) {
    const msg = res.data?.message || res.statusText;
    throw new Error(`Login failed for ${role}: ${res.status} — ${msg}`);
  }

  let data = res.data.data;

  // OTP required — complete verification using the demo OTP code (MockSms mode)
  if (data.requiresOtp) {
    const otpCode = data.demoOtpCode;
    if (!otpCode) {
      return { requiresOtp: true, sessionToken: data.sessionToken };
    }
    const otpRes = await clients.anonymous.post('/auth/verify-otp', {
      sessionToken: data.sessionToken,
      otpCode: String(otpCode),
    });
    if (otpRes.status !== 200 || !otpRes.data?.success) {
      throw new Error(`OTP verification failed for ${role}: ${otpRes.status} — ${otpRes.data?.message || ''}`);
    }
    data = otpRes.data.data;
  }

  const token = data.accessToken || data.token;
  if (!token) throw new Error(`No token returned for ${role}`);

  tokens[role] = token;
  clients[role].defaults.headers.common['Authorization'] = `Bearer ${token}`;

  if (mobile) {
    clients[role].defaults.headers.common['X-Device-Id'] = CONFIG.devices.mobileDeviceId;
  }

  return { token, user: data.user, refreshToken: data.refreshToken };
}

/**
 * Login all three standard roles (admin, supervisor, worker).
 * Returns an object with results keyed by role.
 */
export async function loginAllRoles() {
  const results = {};
  for (const role of ['admin', 'supervisor', 'worker']) {
    results[role] = await loginAs(role);
  }
  return results;
}

// ── Assertion helpers ────────────────────────────────────────

export function assertSuccess(res, context = '') {
  const prefix = context ? `[${context}] ` : '';
  if (res.status < 200 || res.status >= 300) {
    throw new Error(
      `${prefix}Expected 2xx, got ${res.status}: ${JSON.stringify(res.data)}`
    );
  }
  if (res.data && res.data.success === false) {
    throw new Error(
      `${prefix}API returned success=false: ${res.data.message || JSON.stringify(res.data.errors)}`
    );
  }
  return res.data;
}

export function assertStatus(res, expected, context = '') {
  const prefix = context ? `[${context}] ` : '';
  if (res.status !== expected) {
    throw new Error(
      `${prefix}Expected ${expected}, got ${res.status}: ${JSON.stringify(res.data)}`
    );
  }
  return res.data;
}

// ── Data factories ───────────────────────────────────────────

let counter = Date.now();
const uid = () => ++counter;

export function makeTask(workerId, overrides = {}) {
  return {
    title: `Integration Test Task ${uid()}`,
    description: 'Created by integration test suite',
    assignedToUserId: workerId,
    priority: 1, // Normal
    dueDate: new Date(Date.now() + 86400000 * 30).toISOString(), // 30 days out
    latitude: CONFIG.gps.validLocation.latitude,
    longitude: CONFIG.gps.validLocation.longitude,
    ...overrides,
  };
}

export function makeZone(overrides = {}) {
  const id = uid();
  return {
    zoneName: `منطقة اختبار ${id}`,
    zoneCode: `TZ${id}`.slice(-20),
    description: 'Created by integration test suite',
    areaSquareMeters: 50000,
    boundaryGeoJson: JSON.stringify({
      type: 'Polygon',
      coordinates: [[
        [35.2000, 31.9000],
        [35.2100, 31.9000],
        [35.2100, 31.9100],
        [35.2000, 31.9100],
        [35.2000, 31.9000],
      ]],
    }),
    district: 'البيرة',
    zoneType: 'Quarters',
    ...overrides,
  };
}

// ── Cleanup registry ─────────────────────────────────────────

const cleanups = [];

export function registerCleanup(fn) {
  cleanups.push(fn);
}

export async function runCleanups() {
  for (const fn of cleanups.reverse()) {
    try { await fn(); } catch { /* best effort */ }
  }
  cleanups.length = 0;
}

// ── Multipart / Photo helpers ────────────────────────────────

/**
 * Returns a Buffer containing a valid 1x1 white PNG image (67 bytes).
 * Used for photo upload tests without needing real image files.
 */
export function makeMinimalPng() {
  return Buffer.from(
    'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==',
    'base64'
  );
}

/**
 * Build a multipart/form-data request config for axios.
 * @param {Object} fields  - key/value pairs for text fields
 * @param {Object} files   - key: { buffer, filename, contentType }
 * @returns {{ data: FormData, headers: Object }}
 */
export function buildMultipart(fields = {}, files = {}) {
  const form = new FormData();
  for (const [k, v] of Object.entries(fields)) {
    if (v != null) form.append(k, String(v));
  }
  for (const [k, { buffer, filename, contentType }] of Object.entries(files)) {
    form.append(k, buffer, { filename, contentType });
  }
  return { data: form, headers: form.getHeaders() };
}

// ── Misc ─────────────────────────────────────────────────────

export function sleep(ms) {
  return new Promise(r => setTimeout(r, ms));
}
