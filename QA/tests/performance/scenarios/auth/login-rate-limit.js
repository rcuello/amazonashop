import { check, sleep } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";
import { Logger } from "../../utils/logger.js";
import { ConfigLoader } from "../../utils/configLoader.js";
import { UserManager } from "../../utils/userManager.js";
import { HttpClient } from "../../utils/httpClient.js";

// === CONFIGURACIÓN ===
const ENVIRONMENT = __ENV.ENVIRONMENT || "local";
const logger = new Logger(ENVIRONMENT);


// Cargar configuración
const configLoader = new ConfigLoader(ENVIRONMENT);
const config = configLoader.load();
const scenarioConfig = configLoader.loadScenarioConfig("login", "rate_limit");

// Configurar componentes
const userManager = new UserManager(config.users);
const httpClient = new HttpClient(config);

// Logging condicional para debugging
const debugMode = __ENV.DEBUG === "true" || config.debug === true;

// Ruta relativa desde la raíz del proyecto (donde se ejecuta k6)
const CONFIG_BASE_PATH = `../../config/environments/${ENVIRONMENT}`;

logger.info(`🌍 Ambiente detectado: ${ENVIRONMENT}`);
logger.info(`📁 Ruta de configuración: ${CONFIG_BASE_PATH}`);


// Métricas personalizadas
export const rateLimitErrors = new Counter("rate_limit_errors");
export const successfulLogins = new Counter("successful_logins");
export const loginDuration = new Trend("login_duration");
export const authTokenValidation = new Rate("auth_token_validation_rate");
export const blockedRequests = new Counter("blocked_requests");
export const unauthorizedRequests = new Counter("unauthorized_requests");

// === OPCIONES DE K6 ===
export const options = {
  // Stages desde la configuración del ambiente con fallback
  stages: scenarioConfig.settings.stages,

  // Thresholds desde la configuración del ambiente
  thresholds: scenarioConfig.thresholds,

  // Configuraciones adicionales específicas del ambiente
  //...(settingsOptions?.options || {}),

  // Tags para identificar el ambiente en los reportes
  tags: {
    environment: ENVIRONMENT,
    test_type: "rate_limit",
    scenario: "login",
  },
};


// Función principal de prueba
export default function () {
  // Seleccionar usuario aleatorio
  const user = userManager.getRandomUser();
  const loginEndpoint = config.endpoints?.login || "/api/v1/Usuario/login";

  const startTime = Date.now();
  const response = httpClient.post(loginEndpoint, {
    email: user.email,
    password: user.password,
  });
  const endTime = Date.now();

  // Agregar duración personalizada
  loginDuration.add(endTime - startTime);

  // Validaciones dinámicas basadas en configuración
  const validations = {
    "status is 200 or acceptable": (r) => {
      const acceptableStatuses = config.acceptableStatusCodes || [200];
      return acceptableStatuses.includes(r.status);
    },
    "response time is acceptable": (r) => {
      const maxTime = config.rateLimitTest?.maxResponseTime || 1000;
      return r.timings.duration < maxTime;
    },
    "response has body": (r) => r.body && r.body.length > 0,
  };

  // Validación de token solo para respuestas exitosas
  if (response.status === 200) {
    const tokenField = config.authTokenField || "token";
    validations["has auth token"] = (r) => {
      try {
        const jsonResponse = r.json();
        return jsonResponse && jsonResponse[tokenField] !== undefined;
      } catch (error) {
        return false;
      }
    };
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
      const tokenField = config.authTokenField || "token";
      const jsonResponse = response.json();
      hasValidToken = jsonResponse && jsonResponse[tokenField] !== undefined;
    } catch (error) {
      hasValidToken = false;
    }
  }
  authTokenValidation.add(hasValidToken);

  if (debugMode || response.status >= 400) {
    const logLevel = response.status >= 400 ? "ERROR" : "DEBUG";

    logger.info(
      `[${logLevel}][${ENVIRONMENT.toUpperCase()}] VU: ${__VU}, Iter: ${__ITER}, Status: ${
        response.status
      }, Duration: ${Math.round(response.timings.duration)}ms, User: ${
        user.email
      }`
    );

    if (response.status >= 400) {
      //logger.debug(`[ERROR] Response body: ${response.body}`);
    }
  }

  // Tiempo de espera configurable por ambiente
  const sleepTime = config?.sleepTime || 1;
  sleep(sleepTime);
}

// Función de setup (ejecutada una vez al inicio)
export function setup() {
  logger.info("🚀 Iniciando pruebas de Rate Limit para Login");
  logger.separator();  
  logger.info(`Endpoint: ${config.baseUrl}${config.endpoints?.login}`);
  logger.info(`Usuarios: ${userManager.getUserCount()}`);
  logger.info(`Stages: ${options.stages.length}`);

  logger.separator();

  return {
    environment: ENVIRONMENT,
    userCount: userManager.getUserCount(),
    startTime: new Date().toISOString(),
  };
}

// Función de teardown (ejecutada una vez al final)
export function teardown(data) {
  logger.info('✅ Pruebas completadas');
  logger.separator();
  logger.info(`🌍 Ambiente: ${data.environment}`);    
  logger.info(`👥 Usuarios: ${data.userCount}`);    
  logger.info(`⏱️ Duración: ${data.startTime} - ${new Date().toISOString()}`);  
  logger.separator();
}

/*
INSTRUCCIONES DE USO:

1. Instalar k6:
   npm install -g k6
 
2. Ejecutar con ambiente específico:
   k6 run scenarios/auth/login-rate-limit.js --out json=reports/json/login-rate-limit.json
   k6 run scenarios/auth/login-rate-limit.js -e ENVIRONMENT=local --out json=reports/json/login-rate-limit-local.json
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
