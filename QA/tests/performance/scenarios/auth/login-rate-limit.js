import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// Configuración del ambiente - con fallback a 'local'
const ENVIRONMENT = __ENV.ENVIRONMENT || 'local';
// Ruta relativa desde la raíz del proyecto (donde se ejecuta k6)
const CONFIG_BASE_PATH = `../../config/environments/${ENVIRONMENT}`;

console.log(`🌍 Ambiente detectado: ${ENVIRONMENT}`);
console.log(`📁 Ruta de configuración: ${CONFIG_BASE_PATH}`);

// Variables para configuraciones
let config, rateLimitThreshold, credentials, settingsOptions;

// Función helper para cargar archivos JSON con mejor manejo de errores
function loadJsonConfig(filePath, description) {
  try {
    const content = open(filePath);
    if (!content) {
      throw new Error(`Archivo vacío o no encontrado: ${filePath}`);
    }
    return JSON.parse(content);
  } catch (error) {
    console.error(`❌ Error cargando ${description} desde ${filePath}:`);
    console.error(`   Detalle: ${error.message}`);
    throw new Error(`No se pudo cargar ${description}. Verifique que el archivo existe y tiene formato JSON válido.`);
  }
}

// Cargar todas las configuraciones del ambiente
try {
  console.log('📦 Cargando configuraciones...');
  
  // Configuración principal del ambiente
  config = loadJsonConfig(`${CONFIG_BASE_PATH}/config.json`, 'configuración principal');
  console.log('✅ config.json cargado');
  
  // Configuración específica de rate limit
  settingsOptions = loadJsonConfig(`${CONFIG_BASE_PATH}/settings/login/rate_limit.json`, 'configuración de rate limit');
  console.log('✅ rate_limit.json (settings) cargado');
  
  // Thresholds específicos para rate limit
  rateLimitThreshold = loadJsonConfig(`${CONFIG_BASE_PATH}/thresholds/login/rate_limit.json`, 'thresholds de rate limit');
  console.log('✅ rate_limit.json (thresholds) cargado');
  
  // Credenciales de usuarios
  credentials = loadJsonConfig(`${CONFIG_BASE_PATH}/test-data/credentials.json`, 'credenciales de usuarios');
  console.log('✅ credentials.json cargado');
  
  console.log('🎉 Todas las configuraciones cargadas exitosamente');

} catch (error) {
  console.error(`💥 Error fatal cargando configuración para ambiente '${ENVIRONMENT}':`);
  console.error(`   ${error.message}`);
  console.error(`\n📋 Archivos requeridos:`);
  console.error(`   • ${CONFIG_BASE_PATH}/config.json`);
  console.error(`   • ${CONFIG_BASE_PATH}/settings/login/rate_limit.json`);
  console.error(`   • ${CONFIG_BASE_PATH}/thresholds/login/rate_limit.json`);
  console.error(`   • ${CONFIG_BASE_PATH}/test-data/credentials.json`);
  console.error(`\n💡 Comando de ejemplo:`);
  console.error(`   k6 run scenarios/auth/login-rate-limit.js -e ENVIRONMENT=dev`);
  throw error;
}

// Validar que las configuraciones cargadas tengan la estructura esperada
if (!config.baseUrl) {
  throw new Error(`❌ 'baseUrl' no está configurada en ${CONFIG_BASE_PATH}/settings.json`);
}

if (!credentials.users || !Array.isArray(credentials.users) || credentials.users.length === 0) {
  throw new Error(`❌ No se encontraron usuarios válidos en 'users' array en ${CONFIG_BASE_PATH}/test-data/credentials.json`);
}

// Métricas personalizadas
export const rateLimitErrors = new Counter('rate_limit_errors');
export const successfulLogins = new Counter('successful_logins');
export const loginDuration = new Trend('login_duration');
export const authTokenValidation = new Rate('auth_token_validation_rate');
export const blockedRequests = new Counter('blocked_requests');
export const unauthorizedRequests = new Counter('unauthorized_requests');

// Configuración de opciones de k6
export const options = {
  // Stages desde la configuración del ambiente con fallback
  stages: settingsOptions?.stages || [
    { duration: '30s', target: 5 },
    { duration: '1m', target: 15 },
    { duration: '2m', target: 25 },
    { duration: '1m', target: 50 },
    { duration: '30s', target: 0 },
  ],
  
  // Thresholds desde la configuración del ambiente
  thresholds: {
    http_req_duration: rateLimitThreshold?.http_req_duration || ['p(95)<500', 'p(99)<1000'],
    http_req_failed: rateLimitThreshold?.http_req_failed || ['rate<0.1'],
    rate_limit_errors: rateLimitThreshold?.rate_limit_errors || ['count<10'],
    successful_logins: rateLimitThreshold?.successful_logins || ['count>100'],
    auth_token_validation_rate: rateLimitThreshold?.auth_token_validation_rate || ['rate>0.95'],
    blocked_requests: rateLimitThreshold?.blocked_requests || ['count<5'],
    unauthorized_requests: rateLimitThreshold?.unauthorized_requests || ['count<3'],
  },
  
  // Configuraciones adicionales específicas del ambiente
  ...(settingsOptions?.options || {}),
  
  // Tags para identificar el ambiente en los reportes
  tags: {
    environment: ENVIRONMENT,
    test_type: 'rate_limit',
    scenario: 'login',
  },
};

// URLs y endpoints
const BASE_URL = config.baseUrl.replace(/\/$/, ''); // Eliminar slash final si existe
const LOGIN_ENDPOINT = `${BASE_URL}${config.endpoints?.login || '/api/v1/Usuario/login'}`;

// Usuarios de prueba del ambiente
const usersCredentials = credentials.users;

console.log(`🔗 Endpoint de login: ${LOGIN_ENDPOINT}`);
console.log(`👥 Usuarios disponibles: ${usersCredentials.length}`);

// Función principal de prueba
export default function () {
  // Seleccionar usuario aleatorio
  const randomUser = usersCredentials[Math.floor(Math.random() * usersCredentials.length)];
  
  // Validar que el usuario tenga email y password
  if (!randomUser.email || !randomUser.password) {
    console.error(`❌ Usuario inválido: ${JSON.stringify(randomUser)}`);
    return;
  }
  
  const payload = JSON.stringify({
    email: randomUser.email,
    password: randomUser.password
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
      'User-Agent': `k6-load-test/${ENVIRONMENT}`,
      // Headers adicionales del ambiente
      ...(config.defaultHeaders || {}),
    },
    timeout: config.requestTimeout || '30s',
    // Configuraciones adicionales de request
    ...(config.requestConfig || {}),
  };

  const startTime = Date.now();
  const response = http.post(LOGIN_ENDPOINT, payload, params);
  const endTime = Date.now();
  
  // Agregar duración personalizada
  loginDuration.add(endTime - startTime);

  // Validaciones dinámicas basadas en configuración
  const validations = {
    'status is 200 or acceptable': (r) => {
      const acceptableStatuses = config.acceptableStatusCodes || [200];
      return acceptableStatuses.includes(r.status);
    },
    'response time is acceptable': (r) => {
      const maxTime = config.rateLimitTest?.maxResponseTime || 1000;
      return r.timings.duration < maxTime;
    },
    'response has body': (r) => r.body && r.body.length > 0,
  };

  // Validación de token solo para respuestas exitosas
  if (response.status === 200) {
    const tokenField = config.authTokenField || 'token';
    validations['has auth token'] = (r) => {
      try {
        const jsonResponse = r.json();
        return jsonResponse && jsonResponse[tokenField] !== undefined;
      } catch (error) {
        return false;
      }
    };
  }

  // Validaciones adicionales específicas del ambiente
  if (config.rateLimitTest?.customValidations) {
    Object.assign(validations, config.rateLimitTest.customValidations);
  }

  const checksResult = check(response, validations);

  // Métricas específicas por código de respuesta
  switch (response.status) {
    case 200:
      if (checksResult) {
        successfulLogins.add(1);
      }
      break;
    case 429:
      rateLimitErrors.add(1);
      blockedRequests.add(1);
      break;
    case 401:
    case 403:
      unauthorizedRequests.add(1);
      break;
  }

  // Validación del token de autenticación para métricas
  let hasValidToken = false;
  if (response.status === 200) {
    try {
      const tokenField = config.authTokenField || 'token';
      const jsonResponse = response.json();
      hasValidToken = jsonResponse && jsonResponse[tokenField] !== undefined;
    } catch (error) {
      hasValidToken = false;
    }
  }
  authTokenValidation.add(hasValidToken);

  // Logging condicional para debugging
  const debugMode = __ENV.DEBUG === 'true' || config.debug === true;
  if (debugMode || response.status >= 400) {
    const logLevel = response.status >= 400 ? 'ERROR' : 'DEBUG';
    console.log(`[${logLevel}][${ENVIRONMENT.toUpperCase()}] VU: ${__VU}, Iter: ${__ITER}, Status: ${response.status}, Duration: ${Math.round(response.timings.duration)}ms, User: ${randomUser.email}`);
    
    if (response.status >= 400) {
      //console.log(`[ERROR] Response body: ${response.body}`);
    }
  }

  // Tiempo de espera configurable por ambiente
  const sleepTime = settingsOptions?.sleepTime || 1;
  sleep(sleepTime);
}

// Función de setup (ejecutada una vez al inicio)
export function setup() {
  console.log('\n🚀 Iniciando pruebas de Rate Limit para Login');
  console.log('═'.repeat(50));
  console.log(`🌍 Ambiente: ${ENVIRONMENT.toUpperCase()}`);
  console.log(`📊 Endpoint: ${LOGIN_ENDPOINT}`);
  console.log(`👥 Usuarios de prueba: ${usersCredentials.length}`);
  console.log(`⚙️  Configuración: ${CONFIG_BASE_PATH}/`);

  console.log('═'.repeat(50));

  return {
    environment: ENVIRONMENT,
    baseUrl: BASE_URL,
    loginEndpoint: LOGIN_ENDPOINT,
    userCount: usersCredentials.length,
    startTime: new Date().toISOString(),
  };
}

// Función de teardown (ejecutada una vez al final)
export function teardown(data) {
  console.log('\n✅ Pruebas de Rate Limit completadas');
  console.log('═'.repeat(50));
  console.log(`🌍 Ambiente probado: ${data?.environment?.toUpperCase()}`);
  console.log(`📊 URL base: ${data?.baseUrl}`);
  console.log(`🔗 Endpoint: ${data?.loginEndpoint}`);
  console.log(`👥 Usuarios utilizados: ${data?.userCount}`);
  console.log(`⏱️  Iniciado: ${data.startTime}`);
  console.log(`⏱️  Finalizado: ${new Date().toISOString()}`);
  console.log(`📁 Reporte: reports/json/login-rate-limit-${data?.environment}.json`);
  console.log('═'.repeat(50));
}

/*
INSTRUCCIONES DE USO:

1. Instalar k6:
   npm install -g k6
 
2. Ejecutar con ambiente específico:
   k6 run scenarios/auth/login-rate-limit.js --out json=reports/json/login-rate-limit.json
   k6 run scenarios/auth/login-rate-limit.js -e ENVIRONMENT=dev --out json=reports/json/login-rate-limit-dev.json
   k6 run scenarios/auth/login-rate-limit.js -e ENVIRONMENT=staging --out json=reports/json/login-rate-limit-staging.json
   k6 run scenarios/auth/login-rate-limit.js -e ENVIRONMENT=prod --out json=reports/json/login-rate-limit-prod.json

3. Ejecutar con debugging:
   k6 run scenarios/auth/login-rate-limit.js -e ENVIRONMENT=local -e DEBUG=true

4. Ejecutar con múltiples outputs:
   k6 run scenarios/auth/login-rate-limit.js -e ENVIRONMENT=dev \
     --out json=reports/json/login-rate-limit-dev.json \
     --out csv=reports/csv/login-rate-limit-dev.csv

ARCHIVOS REQUERIDOS POR AMBIENTE:
- config/environments/{ENVIRONMENT}/settings.json
- config/environments/{ENVIRONMENT}/settings/login/rate_limit.json  
- config/environments/{ENVIRONMENT}/thresholds/login/rate_limit.json
- config/environments/{ENVIRONMENT}/test-data/credentials.json
*/