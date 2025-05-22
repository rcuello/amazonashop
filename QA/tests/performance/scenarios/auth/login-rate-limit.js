import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// Métricas personalizadas
export const rateLimitErrors = new Counter('rate_limit_errors');
export const successfulLogins = new Counter('successful_logins');
export const loginDuration = new Trend('login_duration');

// Configuración del test
export const options = {
  stages: [
    { duration: '30s', target: 5 },   // Warm up
    { duration: '1m', target: 15 },   // Incremento gradual
    { duration: '2m', target: 25 },   // Rate limit testing
    { duration: '1m', target: 50 },   // Spike para probar límites
    { duration: '30s', target: 0 },   // Cool down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    http_req_failed: ['rate<0.1'],
    rate_limit_errors: ['count<10'],
    successful_logins: ['count>100'],
  },
};

// Configuración del entorno
const BASE_URL = __ENV.BASE_URL || 'https://localhost:5001';
const LOGIN_ENDPOINT = `${BASE_URL}/api/v1/Usuario/login`;

// Datos de prueba
const testUsers = [
  { email: 'jhon.wick@gmail.com', password: 'Pa$$word.Admin.1234.$' },
  { email: 'userbot01@gmail.com', password: 'Password1234567$' },
  { email: 'userbot02@gmail.com', password: 'Password123456$' },
];

export default function () {
  // Seleccionar usuario aleatorio
  const user = testUsers[Math.floor(Math.random() * testUsers.length)];
  
  const payload = JSON.stringify({
    email: user.email,
    password: user.password
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  const startTime = Date.now();
  const response = http.post(LOGIN_ENDPOINT, payload, params);
  const endTime = Date.now();
  
  loginDuration.add(endTime - startTime);

  // Validaciones
  const isSuccess = check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
    'has auth token': (r) => r.json('token') !== undefined,
  });

  // Métricas personalizadas
  if (response.status === 429) {
    rateLimitErrors.add(1);
  } else if (isSuccess) {
    successfulLogins.add(1);
  }

  // Log para debugging (solo en desarrollo)
  if (__ENV.DEBUG === 'true') {
    console.log(`VU: ${__VU}, Status: ${response.status}, Duration: ${response.timings.duration}ms`);
  }

  sleep(1); // Pausa entre requests
}

// Función de setup (ejecutada una vez al inicio)
export function setup() {
  console.log('🚀 Iniciando pruebas de Rate Limit para Login');
  console.log(`📊 Endpoint: ${LOGIN_ENDPOINT}`);
  console.log(`👥 Usuarios de prueba: ${testUsers.length}`);
}

// Función de teardown (ejecutada una vez al final)
export function teardown(data) {
  console.log('✅ Pruebas de Rate Limit completadas');
}

// 1. Instalar k6 Primero
// 2. Luego ejecutar el script con:
//  k6 run scenarios/auth/login-rate-limit.js
//  k6 run scenarios/auth/login-rate-limit.js --out json=reports/json/login-rate-limit.json