# 📂 scan-directory

Genera un árbol de directorios en formato visual, con soporte para exclusiones configurables mediante un archivo YAML.

---

## 🚀 Características

- Genera un esquema en árbol de cualquier directorio local.
- Permite excluir carpetas y archivos mediante un archivo `ignore.yml`.
- Compatible con entornos virtuales.
- Fácil de usar y configurar.

---

## 🧑‍💻 Requisitos

- Python 3.9 o superior

---

## ⚙️ Instalación

Clona el repositorio y configura el entorno virtual:

```bash
git clone https://github.com/tu_usuario/scan-directory.git
cd scan-directory
python -m venv .venv
3.13 la lib de playwrigth generar error greeleat
C:\Users\<Usedr>\AppData\Local\Programs\Python\Python311\python.exe -m venv .venv
````

Activa el entorno virtual:

* En Linux/macOS:

```bash
source .venv/bin/activate
```

* En Windows (PowerShell):

```powershell
.\.venv\Scripts\Activate.ps1
```

* En Windows (cmd.exe):

```cmd
.\.venv\Scripts\activate.bat
```

Luego instala las dependencias con pip:

```bash
pip install -r requirements.txt
```

---

## 🧾 Uso

```bash
python scan_directory.py <ruta_directorio> [--ignore-file ignore.yml]
```

### Ejemplo básico:

```bash
python scan_directory.py ./mi_proyecto
```

### Ejemplo con exclusiones:

```bash
python scan_directory.py ./mi_proyecto --ignore-file ignore.yml
```

---

## 🧱 Estructura del archivo `ignore.yml`

```yaml
ignore_directories:
  - __pycache__
  - .git
  - .venv

ignore_files:
  - "*.pyc"
  - "*.log"
```

---
Sistema simple y modular para hacer scraping de productos en múltiples marketplaces como MercadoLibre, Amazon, eBay y AliExpress.

## 🚀 Características

- **Múltiples marketplaces**: MercadoLibre, Amazon, eBay, AliExpress
- **Scraping asíncrono**: Utiliza Playwright para mejor rendimiento
- **Exportación flexible**: CSV y JSON con opciones de agrupación
- **Arquitectura modular**: Fácil de extender con nuevos marketplaces
- **Configuración simple**: Parámetros personalizables
- **Manejo de errores**: Robusto ante fallos de conexión

## 📦 Instalación

### Método 1: Pip (recomendado)
```bash
pip install -r requirements.txt
playwright install chromium
```

### Método 2: Virtual Environment
```bash
python -m venv venv
source venv/bin/activate  # En Windows: venv\Scripts\activate
pip install -r requirements.txt
playwright install chromium
```

### Método 3: Poetry
```bash
poetry install
poetry run playwright install chromium
```

## 📚 Estructura del proyecto

```
marketplace_scraper/
├── scrapers/
│   ├── base_scraper.py      # Clase base común
│   ├── mercadolibre.py      # Scraper MercadoLibre
│   ├── amazon.py            # Scraper Amazon
│   ├── ebay.py              # Scraper eBay
│   └── aliexpress.py        # Scraper AliExpress
├── models/
│   └── product.py           # Modelo de producto
├── utils/
│   ├── browser.py           # Wrapper Playwright
│   ├── exporters.py         # Exportadores CSV/JSON
│   └── helpers.py           # Funciones auxiliares
├── config/
│   └── settings.py          # Configuraciones
├── output/                  # Archivos exportados
├── main.py                  # Orquestador principal
├── requirements.txt         # Dependencias
└── README.md               # Este archivo
```

## 🎯 Ejemplos de uso

### 1. Comparar precios de iPhone
```bash
python main.py "iphone 15 pro" -m mercadolibre amazon ebay -p 2 -f json
```

### 2. Buscar laptops en Argentina
```bash
python main.py "laptop lenovo" -m mercadolibre --country ar -p 3
```

### 3. Scrapear AliExpress y exportar por separado
```bash
python main.py "auriculares gaming" -m aliexpress --by-marketplace
```


## 📊 Datos extraídos

Para cada producto se obtiene:

- **Título**: Nombre del producto
- **Precio**: Precio actual y original (si aplica)
- **URL**: Enlace al producto
- **Imagen**: URL de la imagen principal
- **Rating**: Calificación (si está disponible)
- **Reseñas**: Cantidad de reseñas
- **Vendedor**: Información del vendedor
- **Disponibilidad**: Estado del stock
- **Marketplace**: Origen del producto
- **Fecha**: Timestamp del scraping


### Modificar configuraciones

Edita `config/settings.py` para cambiar:
- Timeouts del navegador
- Delays entre requests
- User agents
- Configuración de exportación

## ⚠️ Consideraciones importantes

1. **Respeta los términos de servicio** de cada sitio web
2. **Usa delays apropiados** para no sobrecargar los servidores
3. **Considera usar proxies** para scraping intensivo
4. **Los selectores pueden cambiar** - mantén el código actualizado
5. **Algunos sitios usan protección anti-bot** - ajusta configuraciones si es necesario

## 🐛 Troubleshooting

### Error: Playwright no instalado
```bash
playwright install chromium
```

### Timeout en páginas lentas
Aumenta el timeout en `config/settings.py`:
```python
BROWSER_CONFIG = {
    'timeout': 60000,  # 60 segundos
}
```

## 💡 Tips y mejores prácticas

### Para scraping responsable:
- Usa delays entre requests
- No hagas demasiadas solicitudes por minuto
- Respeta el archivo robots.txt
- Considera el impacto en los servidores

### Para mejores resultados:
- Usa términos de búsqueda específicos
- Limita el número de páginas inicialmente
- Verifica los datos exportados
- Mantén actualizados los selectores

### Para desarrollo:
- Usa modo no-headless para debugging
- Implementa logging detallado
- Maneja excepciones apropiadamente
- Testea con diferentes queries