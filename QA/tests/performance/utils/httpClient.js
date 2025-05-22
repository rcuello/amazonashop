import http from 'k6/http';

export class HttpClient {
  constructor(config) {
    this.config = config;
    this.baseUrl = config.baseUrl;
    this.headers = config.headers;
    this.timeout = config.requestTimeout || '30s';
  }

  post(endpoint, payload, additionalHeaders = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    const params = {
      headers: { ...this.headers, ...additionalHeaders },
      timeout: this.timeout
    };

    return http.post(url, JSON.stringify(payload), params);
  }

  get(endpoint, additionalHeaders = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    const params = {
      headers: { ...this.headers, ...additionalHeaders },
      timeout: this.timeout
    };

    return http.get(url, params);
  }
}