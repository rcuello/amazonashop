
## 🧪 Instalación de K6 en Windows

K6 es una herramienta moderna de código abierto para realizar pruebas de carga y rendimiento en tus APIs. A continuación, se presentan varias formas de instalar K6 en Windows.

---

### 📦 Paso previo (opcional): Instalar Chocolatey (`choco`)

Si decides usar Chocolatey y **aún no lo tienes instalado**, sigue estos pasos:

1. Abre **PowerShell como Administrador** (haz clic derecho sobre PowerShell y elige "Ejecutar como administrador").
2. Ejecuta el siguiente comando para permitir scripts:

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force
```

3. Luego, instala Chocolatey con este comando:

```powershell
iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
```

4. Cierra y vuelve a abrir la terminal PowerShell como Administrador.

5. Verifica que Chocolatey se instaló correctamente:

```bash
choco --version
```

---

### ✅ Opción 1: Instalar K6 con Chocolatey

Una vez que tengas `choco` instalado:

```bash
choco install k6
```

Verifica que se haya instalado correctamente:

```bash
k6 version
```

---

### ✅ Opción 2: Instalar K6 con `winget` (Windows Package Manager)

Si usas `winget`, puedes instalar K6 con el siguiente comando:

```bash
winget install k6 --source winget
```

Y luego verificar:

```bash
k6 version
```

---

### ✅ Opción 3: Instalar K6 manualmente

1. Dirígete a la página de releases de K6:
   👉 [https://github.com/grafana/k6/releases](https://github.com/grafana/k6/releases)

2. Descarga el archivo `.msi` correspondiente a tu sistema (ejemplo: `k6-v0.47.0-windows-amd64.msi`).

3. Ejecuta el instalador y sigue las instrucciones.

4. Verifica la instalación:

```bash
k6 version
```

---

### ✅ Resultado esperado

Después de instalar correctamente, deberías ver una salida como:

```bash
k6 v0.47.0
```
