import { Logger } from "./logger.js";

export class ConfigLoader {
  constructor(environment = 'local') {
    this.env = environment;
    this.basePath = `../../config/environments/${environment}`;
    this.logger = new Logger(environment);
  }

  load() {
    try {
      this.logger.info(`🌍 Cargando configuración para: ${this.env}`);
      
      const mainConfig = this.loadJson(`${this.basePath}/config.json`);
      const credentials = this.loadJson(`${this.basePath}/test-data/credentials.json`);
      
      this.validateConfig(mainConfig, credentials);
      
      return {
        ...mainConfig,
        baseUrl: mainConfig.baseUrl.replace(/\/$/, ''),
        users: credentials.users || [],
        headers: this.buildHeaders(mainConfig),
        acceptableStatuses: mainConfig.acceptableStatusCodes || [200],
        tokenField: mainConfig.authTokenField || 'token'
      };
      
    } catch (error) {
      this.logger.error('Error cargando configuración:', error.message);
      throw error;
    }
  }

  loadScenarioConfig(scenario, testType) {
    const settingsPath = `${this.basePath}/settings/${scenario}/${testType}.json`;
    const thresholdsPath = `${this.basePath}/thresholds/${scenario}/${testType}.json`;
    
    return {
      settings: this.loadJson(settingsPath, {}),
      thresholds: this.loadJson(thresholdsPath, {})
    };
  }

  loadJson(filePath, fallback = null) {
    try {
      const content = open(filePath);
      if (!content && fallback !== null) return fallback;
      if (!content) throw new Error(`Archivo vacío: ${filePath}`);
      return JSON.parse(content);
    } catch (error) {
      if (fallback !== null) return fallback;
      throw new Error(`Error cargando ${filePath}: ${error.message}`);
    }
  }

  validateConfig(mainConfig, credentials) {
    if (!mainConfig.baseUrl) throw new Error("'baseUrl' no configurada");
    if (!credentials.users?.length) throw new Error("No hay usuarios válidos");
  }

  buildHeaders(config) {
    return {
      'Content-Type': 'application/json',
      'User-Agent': `k6-load-test/${this.env}`,
      ...(config.defaultHeaders || {})
    };
  }
}