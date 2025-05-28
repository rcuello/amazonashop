import { check } from 'k6';

export function validateLoginResponse(response) {
  return check(response, {
    'status is 200': (r) => r.status === 200,
    'has token': (r) => r.json('token') !== undefined,
    'token is not empty': (r) => r.json('token') !== '',
    'has user data': (r) => r.json('user') !== undefined,
    'response time < 1000ms': (r) => r.timings.duration < 1000,
  });
}

export function generateRandomUser(index) {
  return {
    email: `user${index}_${Date.now()}@example.com`,
    password: 'TestPassword123!'
  };
}

export function logTestMetrics(response, scenario) {
  if (__ENV.VERBOSE === 'true') {
    console.log(`[${scenario}] VU: ${__VU}, Status: ${response.status}, Time: ${response.timings.duration}ms`);
  }
}