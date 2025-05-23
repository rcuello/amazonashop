import { check, sleep } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";
import { Logger } from "../../utils/logger.js";
import { ConfigLoader } from "../../utils/configLoader.js";
import { UserManager } from "../../utils/userManager.js";
import { HttpClient } from "../../utils/httpClient.js";
import { StagePresets } from "../../utils/stagePresets.js";

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

logger.info(`🌍 Ambiente: ${ENVIRONMENT}`);

// Logging condicional para debugging
const isDebugMode = __ENV.DEBUG === "true" || config.debug === true;

// === MÉTRICAS ESPECÍFICAS ===
export const rateLimitErrors = new Counter("rate_limit_errors");
export const successfulLogins = new Counter("successful_logins");
export const loginDuration = new Trend("login_duration");
export const authTokenValidation = new Rate("auth_token_validation_rate");
export const blockedRequests = new Counter("blocked_requests");
export const unauthorizedRequests = new Counter("unauthorized_requests");

// === MÉTRICAS PARA ERRORES ADICIONALES ===
export const serverErrors = new Counter("server_errors_5xx");

// === OPCIONES DE K6 ===
export const options = {
  // Stages desde la configuración del ambiente con fallback
  stages: scenarioConfig.settings.stages || StagePresets.RATE_LIMIT,

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

// === FUNCIÓN PRINCIPAL ===
export default function () {
  const user = userManager.getRandomUser();
  const loginEndpoint = config.endpoints?.login || "/api/v1/Usuario/login";

  // Realizar login con medición de tiempo
  const startTime = Date.now();
  const response = httpClient.post(loginEndpoint, {
    email: user.email,
    password: user.password,
  });
  const endTime = Date.now();

  loginDuration.add(endTime - startTime);

  // Validaciones dinámicas basadas en configuración
  const validations = {
    "status is 200 or acceptable": (r) => {
      const acceptableStatuses = config.acceptableStatusCodes || [200];
      return acceptableStatuses.includes(r.status);
    },
    "response time is acceptable": (r) => {
      const maxTime = scenarioConfig.settings.maxResponseTime || 1000;
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

  // Actualizar métricas específicas
  updateLoginMetrics(user, response, checksResult, isDebugMode);

  // Tiempo de espera configurable por ambiente
  const sleepTime = config?.sleepTime || 1;

  sleep(sleepTime);
}

// === FUNCIONES HELPER ===
function updateLoginMetrics(user, response, checksResult, debugMode = false) {
  const statusCode = response.status;

  //const isSuccessful = statusCode >= 200 && statusCode < 300;
  //const isClientError = statusCode >= 400 && statusCode < 500;
  const isServerError = statusCode >= 500 && statusCode < 600;

  if (isServerError) {
    serverErrors.add(1);
  }

  switch (statusCode) {
    case 200:
      if (checksResult) successfulLogins.add(1);
      break;
    case 429:
      rateLimitErrors.add(1);
      blockedRequests.add(1);

      //logger.info(JSON.stringify(response.headers));
      break;
    case 401:
    case 403:
      unauthorizedRequests.add(1);
      break;
  }

  // Validación del token de autenticación para métricas
  const responseToken = getAuthToken(response);
  const hasValidToken = responseToken !== null;

  authTokenValidation.add(hasValidToken);

  const rateLimitHeaders = extractRateLimitHeaders(response.headers);

  if (debugMode || response.status >= 400) {
    const logLevel = response.status >= 400 ? "ERROR" : "DEBUG";

    let logMessage = `[${logLevel}] VU: ${__VU}, Iter: ${__ITER}, Status: ${
      response.status
    }, Duration: ${Math.round(response.timings.duration)}ms, User: ${
      user.email
    }`;

    if (rateLimitHeaders.hasRateLimitInfo) {
      logMessage += formatRateLimitMessage(rateLimitHeaders, statusCode);
    }

    logger.info(logMessage);

    /*if (response.status >= 400) {
      logger.debug(`[ERROR] Response body: ${response.body}`);
    }*/
    if (response.status >= 500) {
      logger.error(
        `[CRITICAL] Server error detected for user ${user.email}: ${response.body}`
      );
    }
  }
}

function extractRateLimitHeaders(headers) {
  const lowerHeaders = {};
  Object.keys(headers).forEach((k) => {
    lowerHeaders[k.toLowerCase()] = headers[k];
  });

  const rateLimitInfo = {
    hasRateLimitInfo: false,
    limit: null,
    remaining: null,
    reset: null,
    window: null,
    requestType: null,
    retryAfter: null,
  };

  const headerMappings = [
    { key: 'limit', headers: ['x-ratelimit-limit', 'x-rate-limit-limit'] },
    { key: 'remaining', headers: ['x-ratelimit-remaining', 'x-rate-limit-remaining'] },
    { key: 'reset', headers: ['x-ratelimit-reset', 'x-rate-limit-reset'] },
    { key: 'window', headers: ['x-ratelimit-window', 'x-rate-limit-window'] },
    { key: 'requestType', headers: ['x-ratelimit-requesttype', 'x-rate-limit-requesttype'] },
    { key: 'retryAfter', headers: ['retry-after', 'x-retry-after'] }
  ];

  headerMappings.forEach(mapping => {
    for (const headerName of mapping.headers) {
      if (lowerHeaders[headerName]) {
        rateLimitInfo[mapping.key] = lowerHeaders[headerName];
        rateLimitInfo.hasRateLimitInfo = true;
        break;
      }
    }
  });

  return rateLimitInfo;
}

function formatRateLimitMessage(rateLimitHeaders, statusCode) {
  let message = "";

  if (statusCode === 429) {
    message += " 🚫 RATE LIMITED";
  }

  const parts = [];

  if (rateLimitHeaders.limit) {
    parts.push(`Limit: ${rateLimitHeaders.limit}`);
  }

  if (rateLimitHeaders.remaining !== null) {
    const remaining = parseInt(rateLimitHeaders.remaining);
    const emoji = remaining === 0 ? "❌" : remaining < 5 ? "⚠️" : "✅";
    parts.push(`Remaining: ${remaining} ${emoji}`);
  }

  if (rateLimitHeaders.window) {
    parts.push(`Window: ${rateLimitHeaders.window}`);
  }

  if (rateLimitHeaders.reset) {
    const resetTime = new Date(parseInt(rateLimitHeaders.reset) * 1000);
    const now = new Date();
    const secondsUntilReset = Math.max(0, Math.floor((resetTime - now) / 1000));
    parts.push(`Reset in: ${secondsUntilReset}s`);
  }

  if (rateLimitHeaders.retryAfter) {
    parts.push(`Retry after: ${rateLimitHeaders.retryAfter}s`);
  }

  if (rateLimitHeaders.requestType) {
    parts.push(`Type: ${rateLimitHeaders.requestType}`);
  }

  return parts.length > 0 ? ` | ${parts.join(", ")}` : "";
}

function getAuthToken(response) {
  try {
    const tokenField = config.authTokenField || "token";
    const jsonResponse = response.json();
    return jsonResponse && jsonResponse[tokenField] !== undefined
      ? jsonResponse[tokenField]
      : null;
  } catch (error) {
    return null;
  }
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
  logger.info("✅ Pruebas completadas");
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
   k6 run scenarios/auth/login-rate-limit.js -e ENVIRONMENT=local  
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
