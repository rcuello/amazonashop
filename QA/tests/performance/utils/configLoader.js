import { Logger } from "./logger.js";

export class ConfigLoader {
  constructor(environment = "local") {
    this.env = environment;
    this.logger = new Logger(environment);
  }

  /**
   * Resuelve rutas absolutas para evitar warnings de open()
   * @param {string} relativePath - Ruta relativa desde el directorio config
   * @returns {string} - Ruta absoluta
   */
  resolvePath(relativePath) {
    // Usar __ENV.PWD o process.cwd() como base absoluta
    const workingDir = __ENV.PWD || __ENV.INIT_CWD || ".";
    const configBasePath = `${workingDir}/config/environments/${this.env}`;
    return `${configBasePath}/${relativePath}`;
  }

  load() {
    try {
      this.logger.info(`🌍 Cargando configuración para: ${this.env}`);

      const mainConfig = this.loadJson('config.json');
      const credentials = this.loadJson('test-data/credentials.json');

      this.validateConfig(mainConfig, credentials);

      return {
        ...mainConfig,
        baseUrl: mainConfig.baseUrl.replace(/\/$/, ""),
        users: credentials.users || [],
        headers: this.buildHeaders(mainConfig),
        acceptableStatuses: mainConfig.acceptableStatusCodes || [200],
        tokenField: mainConfig.authTokenField || "token",
      };
    } catch (error) {
      this.logger.error("Error cargando configuración:", error.message);
      throw error;
    }
  }

  loadScenarioConfig(scenario, testType) {
    const settingsPath = `settings/${scenario}/${testType}.json`;
    const thresholdsPath = `thresholds/${scenario}/${testType}.json`;

    return {
      settings: this.loadJson(settingsPath, {}),
      thresholds: this.loadJson(thresholdsPath, {}),
    };
  }

  loadJson(filePath, fallback = null) {
    try {
      const resolvedPath = this.resolvePath(filePath);

      const content = open(resolvedPath);

      if (!content && fallback !== null) return fallback;
      if (!content) throw new Error(`Archivo vacío: ${resolvedPath}`);

      return JSON.parse(content);
    } catch (error) {
      if (fallback !== null) {
        this.logger.warn(`Usando fallback para: ${filePath}`);
        return fallback;
      }
      throw new Error(`Error cargando ${filePath}: ${error.message}`);
    }
  }

  validateConfig(mainConfig, credentials) {
    if (!mainConfig.baseUrl) throw new Error("'baseUrl' no configurada");
    if (!credentials.users?.length) throw new Error("No hay usuarios válidos");
  }

  buildHeaders(config) {
    return {
      "Content-Type": "application/json",
      "User-Agent": `k6-load-test/${this.env}`,
      ...(config.defaultHeaders || {}),
    };
  }
}
