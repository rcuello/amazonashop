export class Logger {
  constructor(environment) {
    this.env = environment.toUpperCase();
  }

  info(message) {
    console.log(`[${this.env}] ${message}`);
  }

  error(message, detail = '') {
    console.error(`❌ [${this.env}] ${message} ${detail}`);
  }

  debug(message) {
    if (__ENV.DEBUG === 'true') {
      console.log(`[DEBUG][${this.env}] ${message}`);
    }
  }

  warn(message) {
    console.warn(`⚠️ [${this.env}] ${message}`);
  }

  separator(char = '═', length = 50) {
    console.log(char.repeat(length));
  }
}