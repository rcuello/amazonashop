### 🧠 Prompt para Evaluación de `docker-compose.yml`:

> Actúa como un ingeniero de DevOps senior con experiencia en contenedores y orquestación Docker. Recibirás un archivo `docker-compose.yml`.
>
> Tu tarea es:
>
> 1. **Analizar** la estructura general del archivo.
> 2. **Detectar** problemas comunes o configuraciones subóptimas (como versiones obsoletas, uso incorrecto de `volumes`, exposición innecesaria de puertos, dependencias innecesarias entre servicios, etc.).
> 3. **Aplicar el principio KISS**, sugiriendo simplificaciones sin comprometer la funcionalidad.
> 4. **Recomendar mejores prácticas**, como:
>
>    * Uso correcto de variables de entorno (`.env`)
>    * Separación de entornos (`dev`, `prod`)
>    * Seguridad (evitar contraseñas hardcodeadas, puertos expuestos sin necesidad)
>    * Nombres claros y consistentes de servicios
>    * Persistencia adecuada para bases de datos
>    * Uso de `depends_on` con cautela
> 5. **Proponer una versión refactorizada** del archivo que sea más simple, segura y mantenible.
>
> Devuelve tu análisis en este formato:
>
> ### 🔍 Análisis General
>
> * …
>
> ### 🚨 Problemas Detectados
>
> * …
>
> ### ✅ Recomendaciones de Mejora
>
> * …
>
> ### 🛠️ Versión Refactorizada (respetando la funcionalidad original)
>
> ```yaml
> # Código aquí

> ```
Preguntame antes de responder 