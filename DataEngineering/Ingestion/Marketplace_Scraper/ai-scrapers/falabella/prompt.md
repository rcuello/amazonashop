Actúa como un experto extractor de datos web especializado en Falabella Colombia. Analiza el siguiente HTML y extrae la información del producto en formato JSON estructurado.

INSTRUCCIONES ESPECÍFICAS:
- Extrae SOLO la información que esté explícitamente presente en el HTML
- Si un campo no está disponible, usar null
- Mantén los precios en formato numérico sin símbolos de moneda ni puntos/comas
- Incluye todos los identificadores únicos encontrados
- Preserva URLs completas cuando estén disponibles
- Para descuentos, extrae solo el número (ej: "44" de "-44%")
- Para rating, extrae el valor numérico exacto (ej: 4.8261)
- Para review count, extrae solo el número sin paréntesis (ej: "23" de "(23)")
- No inventes ni infiera información que no esté presente

FORMATO DE SALIDA REQUERIDO:
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
    "currency": "COP"
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
    "seller": "string",
    "free_shipping": boolean
  },
  "reviews": {
    "rating": number,
    "review_count": number,
    "rating_scale": "1-5"
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

HTML A ANALIZAR:

```html
[AQUÍ VA EL HTML A ANALIZAR]
```

Responde ÚNICAMENTE con JSON válido, sin explicaciones adicionales.