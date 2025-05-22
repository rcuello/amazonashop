import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate } from 'k6/metrics';

export const loginErrors = new Counter('login_errors');
export const loginSuccessRate = new Rate('login_success_rate');

export const options = {
  stages: [
    { duration: '1m', target: 10 },   // Warm up
    { duration: '5m', target: 50 },   // Stress normal
    { duration: '5m', target: 100 },  // Stress alto
    { duration: '5m', target: 200 },  // Stress extremo
    { duration: '2m', target: 0 },    // Recovery
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000', 'p(99)<2000'],
    http_req_failed: ['rate<0.05'],
    login_success_rate: ['rate>0.95'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'https://localhost:5001';
const LOGIN_ENDPOINT = `${BASE_URL}/api/v1/Usuario/login`;

export default function () {
  const payload = JSON.stringify({
    email: `stress${__VU}@example.com`,
    password: 'StressTest123!'
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  const response = http.post(LOGIN_ENDPOINT, payload, params);

  const success = check(response, {
    'status is 200 or 401': (r) => [200, 401].includes(r.status),
    'response time acceptable': (r) => r.timings.duration < 2000,
  });

  loginSuccessRate.add(success);
  
  if (!success) {
    loginErrors.add(1);
  }

  sleep(Math.random() * 2 + 1); // Entre 1-3 segundos
}