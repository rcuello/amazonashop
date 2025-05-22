#!/bin/bash

echo "🔐 Ejecutando todas las pruebas de autenticación..."

# Configurar variables de entorno
export BASE_URL="https://localhost:5001"
export K6_WEB_DASHBOARD=true
export K6_WEB_DASHBOARD_EXPORT="reports/html/auth-dashboard.html"

# Crear directorio de reportes si no existe
mkdir -p reports/html reports/json reports/csv

echo "📊 1. Ejecutando pruebas de Rate Limit..."
k6 run --out json=reports/json/login-rate-limit.json scenarios/auth/login-rate-limit.js

echo "💪 2. Ejecutando pruebas de Stress..."
k6 run --out json=reports/json/login-stress.json scenarios/auth/login-stress.js

echo "⚡ 3. Ejecutando pruebas de Spike..."
k6 run --out json=reports/json/login-spike.json scenarios/auth/login-spike.js

echo "⏱️ 4. Ejecutando pruebas de Endurance..."
k6 run --out json=reports/json/login-endurance.json scenarios/auth/login-endurance.js

echo "✅ Todas las pruebas de autenticación completadas!"
echo "📈 Reportes guardados en: reports/"