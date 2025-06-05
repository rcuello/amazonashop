# Guía de Prompts para Web Scraping con LLM

## 1. Prompt Estructurado para Scraping

### Prompt Template Recomendado:

```
Actúa como un experto extractor de datos web. Analiza el siguiente HTML y extrae la información del producto en formato JSON estructurado.

**INSTRUCCIONES ESPECÍFICAS:**
- Extrae SOLO la información que esté explícitamente presente en el HTML
- Si un campo no está disponible, usar null
- Mantén los precios en formato numérico sin símbolos de moneda
- Incluye todos los identificadores únicos encontrados
- Preserva la estructura jerárquica cuando sea relevante

**FORMATO DE SALIDA REQUERIDO:**
```json
{
  "product_id": "string",
  "name": "string", 
  "brand": "string",
  "description": "string",
  "pricing": {
    "current_price": number,
    "original_price": number,
    "discount_percentage": number,
    "currency": "string"
  },
  "specifications": {
    "size": "string",
    "resolution": "string",
    "technology": "string",
    "model": "string"
  },
  "availability": {
    "in_stock": boolean,
    "shipping_info": "string",
    "seller": "string"
  },
  "media": {
    "images": ["array of image URLs"],
    "image_count": number
  },
  "categories": {
    "primary_category": "string",
    "category_codes": ["array"]
  },
  "metadata": {
    "sponsored": boolean,
    "badges": ["array"],
    "url": "string",
    "extraction_timestamp": "ISO date"
  }
}
```

**HTML A ANALIZAR:**
[INSERTAR HTML AQUÍ]
```

## 2. Salida JSON Estructurada Ideal

### Ejemplo de Output Optimizado:

```json
{
  "product_id": "73203518",
  "name": "Televisor 85 pulgadas 4K UHD QLED 85C655",
  "brand": "TCL",
  "description": "Televisor | | 85 pulgadas | 4K UHD | QLED | 85C655",
  "pricing": {
    "current_price": 5299900,
    "original_price": 9499900,
    "discount_percentage": 44,
    "currency": "COP",
    "savings": 4200000
  },
  "specifications": {
    "screen_size": "85 pulgadas",
    "resolution": "4K UHD",
    "display_technology": "QLED",
    "model_number": "85C655"
  },
  "availability": {
    "in_stock": true,
    "shipping_info": "Llega mañana",
    "seller": "Falabella",
    "seller_type": "official"
  },
  "media": {
    "images": [
      "https://www.falabella.com.co/cdn-cgi/imagedelivery/4fYuQyy-r8_rpBpcY7lH_A/falabellaCO/73203518_1/public",
      "https://www.falabella.com.co/cdn-cgi/imagedelivery/4fYuQyy-r8_rpBpcY7lH_A/falabellaCO/73203518_2/public"
    ],
    "image_count": 5
  },
  "categories": {
    "primary_category": "Televisores",
    "category_codes": ["J11010307", "G19080602"]
  },
  "metadata": {
    "sponsored": true,
    "badges": ["Llega mañana"],
    "url": "https://www.falabella.com.co/falabella-co/product/73203518/",
    "extraction_timestamp": "2025-06-05T10:30:00Z",
    "source_platform": "falabella_co"
  }
}
```

## 3. Prompts Especializados por Tipo de Dato

### Para E-commerce:
```
Extrae información de producto e-commerce del HTML proporcionado. Enfócate en:
- Precios y descuentos (formato numérico)
- Especificaciones técnicas
- Disponibilidad y envío
- Imágenes del producto
- Reseñas y calificaciones

Salida en JSON con estructura estandarizada para integración con sistemas de inventario.
```

### Para Información de Contacto:
```
Extrae datos de contacto empresarial del HTML. Incluye:
- Nombres y cargos
- Números de teléfono (formato internacional)
- Direcciones de email
- Direcciones físicas
- Enlaces de redes sociales

Validar formato de emails y números de teléfono.
```

### Para Contenido Editorial:
```
Extrae contenido editorial estructurado:
- Título principal y subtítulos
- Autor y fecha de publicación
- Contenido del artículo (párrafos separados)
- Etiquetas y categorías
- Enlaces internos y externos

Preservar formato de texto y estructura jerárquica.
```

## 4. Mejores Prácticas para Prompts

### Elementos Clave:

1. **Rol Específico**: "Actúa como experto en..."
2. **Instrucciones Claras**: Qué extraer y cómo formatear
3. **Formato de Salida**: JSON estructurado predefinido
4. **Manejo de Errores**: Qué hacer con datos faltantes
5. **Validaciones**: Formatos específicos para datos críticos

### Validaciones Importantes:

```json
{
  "validation_rules": {
    "prices": "Solo números, sin símbolos de moneda",
    "urls": "URLs completas y válidas",
    "emails": "Formato de email válido",
    "phones": "Formato internacional +XX XXX XXX XXXX",
    "dates": "Formato ISO 8601",
    "required_fields": ["product_id", "name", "current_price"]
  }
}
```

## 5. Optimización para Sistemas de Procesamiento

### Estructura para Bases de Datos:

```json
{
  "flat_structure": {
    "id": "73203518",
    "name": "TCL Televisor 85 QLED",
    "brand": "TCL",
    "price_current": 5299900,
    "price_original": 9499900,
    "discount_pct": 44,
    "category_id": "J11010307",
    "in_stock": true,
    "sponsored": true,
    "created_at": "2025-06-05T10:30:00Z"
  }
}
```

### Para APIs REST:

```json
{
  "api_response": {
    "status": "success",
    "data": {
      "product": { /* datos del producto */ },
      "extracted_fields": 12,
      "confidence_score": 0.95,
      "processing_time_ms": 245
    },
    "metadata": {
      "extraction_method": "llm_parsing",
      "model_version": "claude-4",
      "timestamp": "2025-06-05T10:30:00Z"
    }
  }
}
```

## 6. Prompt Final Recomendado

```
Eres un experto extractor de datos web. Analiza el HTML proporcionado y extrae información de producto en formato JSON válido.

REGLAS ESTRICTAS:
1. Solo extrae datos explícitamente presentes
2. Usa null para campos no disponibles
3. Formatea precios como números enteros
4. Incluye timestamp de extracción
5. Valida URLs antes de incluirlas

SALIDA REQUERIDA: JSON válido con estructura estandarizada

HTML:
[INSERTAR HTML]

Responde ÚNICAMENTE con JSON válido, sin explicaciones adicionales.
```

Este enfoque garantiza consistencia, facilita el procesamiento automatizado y mejora la confiabilidad de la extracción de datos.