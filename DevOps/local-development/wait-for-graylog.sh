echo "[Graylog] 🕓 Esperando que Graylog esté listo..."
while ! docker logs graylog | grep -q "Graylog server up and running."; do
    echo "⌛ [Graylog] Esperando... (verificando cada 10 segundos)"
    sleep 10
done
echo "✅ [Graylog] El servidor Graylog está listo para usar !"