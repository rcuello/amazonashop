export const StagePresets = {
  RATE_LIMIT: [
    { duration: '30s', target: 5 },
    { duration: '1m', target: 15 },
    { duration: '2m', target: 25 },
    { duration: '1m', target: 50 },
    { duration: '30s', target: 0 }
  ],

  SMOKE: [
    { duration: '1m', target: 1 }
  ],

  LOAD: [
    { duration: '2m', target: 10 },
    { duration: '5m', target: 10 },
    { duration: '2m', target: 0 }
  ],

  STRESS: [
    { duration: '2m', target: 10 },
    { duration: '5m', target: 20 },
    { duration: '2m', target: 40 },
    { duration: '5m', target: 40 },
    { duration: '10m', target: 0 }
  ],

  SPIKE: [
    { duration: '10s', target: 100 },
    { duration: '1m', target: 100 },
    { duration: '10s', target: 1400 },
    { duration: '3m', target: 1400 },
    { duration: '10s', target: 100 },
    { duration: '3m', target: 100 },
    { duration: '10s', target: 0 }
  ]
};