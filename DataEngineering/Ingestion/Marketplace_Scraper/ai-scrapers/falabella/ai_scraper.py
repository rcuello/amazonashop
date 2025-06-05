import json
import asyncio
from typing import List, Optional, Dict, Any
from dataclasses import dataclass
from datetime import datetime
import openai
from models.product import Product
from models.price_info import PriceInfo
from utils.helpers import clean_price, clean_text

# TODO: Next project
@dataclass
class AIExtractionResult:
    """Resultado de la extracción con IA"""
    success: bool
    product: Optional[Product] = None
    confidence_score: float = 0.0
    extracted_fields: int = 0
    processing_time_ms: int = 0
    error_message: str = ""
    raw_json: Dict[str, Any] = None


class FalabellaAIExtractor:
    """
    Extractor de productos de Falabella usando IA (OpenAI) para parsing de HTML
    
    Esta clase utiliza modelos de lenguaje para extraer información estructurada
    de productos desde HTML crudo de Falabella Colombia.
    """
    
    def __init__(self, api_key: str, model: str = "gpt-4o-mini"):
        """
        Inicializa el extractor con IA
        
        Args:
            api_key: Clave de API de OpenAI
            model: Modelo a utilizar (gpt-4o-mini, gpt-4, etc.)
        """
        self.client = openai.OpenAI(api_key=api_key)
        self.model = model
        self.marketplace_name = "Falabella"
        self.country = "co"
        
        # Configuración del prompt
        self.extraction_prompt = self._build_extraction_prompt()
        
        # Métricas
        self.total_extractions = 0
        self.successful_extractions = 0
        self.failed_extractions = 0
    
    def _build_extraction_prompt(self) -> str:
        """Construye el prompt optimizado para extracción de productos de Falabella"""
        return """
Actúa como un experto extractor de datos web especializado en Falabella Colombia. Analiza el siguiente HTML y extrae la información del producto en formato JSON estructurado.

INSTRUCCIONES ESPECÍFICAS:
- Extrae SOLO la información que esté explícitamente presente en el HTML
- Si un campo no está disponible, usar null
- Mantén los precios en formato numérico sin símbolos de moneda ni puntos/comas
- Incluye todos los identificadores únicos encontrados
- Preserva URLs completas cuando estén disponibles
- Para descuentos, extrae solo el número (ej: "44" de "-44%")
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
{html_content}

Responde ÚNICAMENTE con JSON válido, sin explicaciones adicionales.
"""
    
    async def extract_product_from_html(self, html_content: str, timeout: int = 30) -> AIExtractionResult:
        """
        Extrae información de producto desde HTML usando IA
        
        Args:
            html_content: HTML del producto a analizar
            timeout: Tiempo límite en segundos
            
        Returns:
            AIExtractionResult: Resultado de la extracción
        """
        start_time = datetime.now()
        
        try:
            self.total_extractions += 1
            
            # Preparar el prompt con el HTML
            prompt = self.extraction_prompt.format(html_content=html_content)
            
            # Llamada a OpenAI
            response = await self._call_openai_api(prompt, timeout)
            
            if not response:
                return AIExtractionResult(
                    success=False,
                    error_message="No se recibió respuesta de OpenAI",
                    processing_time_ms=self._calculate_processing_time(start_time)
                )
            
            # Parsear respuesta JSON
            parsed_data = await self._parse_ai_response(response)
            
            if not parsed_data:
                return AIExtractionResult(
                    success=False,
                    error_message="Error parseando respuesta JSON de IA",
                    processing_time_ms=self._calculate_processing_time(start_time)
                )
            
            # Convertir a objeto Product
            product = await self._convert_to_product(parsed_data)
            
            if not product:
                return AIExtractionResult(
                    success=False,
                    error_message="Error convirtiendo datos a objeto Product",
                    raw_json=parsed_data,
                    processing_time_ms=self._calculate_processing_time(start_time)
                )
            
            # Métricas de calidad
            confidence_score = self._calculate_confidence_score(parsed_data)
            extracted_fields = self._count_extracted_fields(parsed_data)
            
            self.successful_extractions += 1
            
            return AIExtractionResult(
                success=True,
                product=product,
                confidence_score=confidence_score,
                extracted_fields=extracted_fields,
                processing_time_ms=self._calculate_processing_time(start_time),
                raw_json=parsed_data
            )
            
        except Exception as e:
            self.failed_extractions += 1
            return AIExtractionResult(
                success=False,
                error_message=f"Error en extracción: {str(e)}",
                processing_time_ms=self._calculate_processing_time(start_time)
            )
    
    async def _call_openai_api(self, prompt: str, timeout: int) -> Optional[str]:
        """Realiza la llamada a la API de OpenAI"""
        try:
            response = await asyncio.wait_for(
                asyncio.to_thread(
                    self.client.chat.completions.create,
                    model=self.model,
                    messages=[
                        {
                            "role": "system",
                            "content": "Eres un experto extractor de datos web. Responde solo con JSON válido."
                        },
                        {
                            "role": "user",
                            "content": prompt
                        }
                    ],
                    temperature=0.1,  # Baja temperatura para mayor consistencia
                    max_tokens=2000,
                    response_format={"type": "json_object"}  # Forzar respuesta JSON
                ),
                timeout=timeout
            )
            
            return response.choices[0].message.content
            
        except asyncio.TimeoutError:
            print(f"⏱️ Timeout en llamada a OpenAI ({timeout}s)")
            return None
        except Exception as e:
            print(f"❌ Error en llamada a OpenAI: {e}")
            return None
    
    async def _parse_ai_response(self, response: str) -> Optional[Dict[str, Any]]:
        """Parsea la respuesta JSON de la IA"""
        try:
            # Limpiar respuesta si tiene markdown
            response = response.strip()
            if response.startswith('```json'):
                response = response[7:]
            if response.endswith('```'):
                response = response[:-3]
            
            return json.loads(response)
            
        except json.JSONDecodeError as e:
            print(f"❌ Error parseando JSON: {e}")
            print(f"📝 Respuesta recibida: {response[:200]}...")
            return None
        except Exception as e:
            print(f"❌ Error procesando respuesta: {e}")
            return None
    
    async def _convert_to_product(self, data: Dict[str, Any]) -> Optional[Product]:
        """Convierte los datos extraídos a objeto Product"""
        try:
            # Extraer información de precios
            pricing = data.get('pricing', {})
            current_price = pricing.get('current_price')
            original_price = pricing.get('original_price')
            
            # Extraer disponibilidad
            availability = data.get('availability', {})
            
            # Extraer media
            media = data.get('media', {})
            images = media.get('images', [])
            image_url = images[0] if images else ""
            
            # Extraer categorías
            categories = data.get('categories', {})
            
            # Extraer metadata
            metadata = data.get('metadata', {})
            
            # Crear objeto Product
            product = Product(
                title=data.get('name', ''),
                price=current_price,
                original_price=original_price,
                marketplace=self.marketplace_name,
                currency=pricing.get('currency', 'COP'),
                brand=data.get('brand', ''),
                seller=availability.get('seller', ''),
                parent_category=categories.get('primary_category', ''),
                category='',  # Falabella no siempre tiene subcategorías claras
                category2='',
                free_shipping=availability.get('free_shipping', False),
                url=metadata.get('url', ''),
                image_url=image_url
                # Campos adicionales que podrían agregarse al modelo Product
                #product_id=data.get('product_id', ''),
                #sponsored=metadata.get('sponsored', False),
                #badges=metadata.get('badges', []),
                #specifications=data.get('specifications', {}),
                #shipping_info=availability.get('shipping_info', ''),
                #in_stock=availability.get('in_stock', True)
            )
            
            return product
            
        except Exception as e:
            print(f"❌ Error convirtiendo a Product: {e}")
            return None
    
    def _calculate_confidence_score(self, data: Dict[str, Any]) -> float:
        """Calcula un score de confianza basado en campos extraídos"""
        try:
            total_fields = 0
            filled_fields = 0
            
            # Campos críticos con mayor peso
            critical_fields = {
                'product_id': 3,
                'name': 3,
                'brand': 2,
                'pricing.current_price': 3,
                'availability.seller': 2,
                'metadata.url': 2
            }
            
            # Campos opcionales con menor peso
            optional_fields = {
                'description': 1,
                'pricing.original_price': 1,
                'pricing.discount_percentage': 1,
                'specifications': 1,
                'media.images': 1,
                'categories.primary_category': 1,
                'metadata.badges': 1
            }
            
            all_fields = {**critical_fields, **optional_fields}
            
            for field_path, weight in all_fields.items():
                total_fields += weight
                
                # Navegar por el path del campo
                value = data
                for key in field_path.split('.'):
                    if isinstance(value, dict) and key in value:
                        value = value[key]
                    else:
                        value = None
                        break
                
                # Verificar si el campo tiene valor
                if value is not None and value != "" and value != []:
                    filled_fields += weight
            
            return round(filled_fields / total_fields, 2) if total_fields > 0 else 0.0
            
        except Exception as e:
            print(f"⚠️ Error calculando score de confianza: {e}")
            return 0.0
    
    def _count_extracted_fields(self, data: Dict[str, Any]) -> int:
        """Cuenta el número de campos extraídos exitosamente"""
        try:
            count = 0
            
            def count_fields(obj, parent_key=""):
                nonlocal count
                if isinstance(obj, dict):
                    for key, value in obj.items():
                        if value is not None and value != "" and value != []:
                            if isinstance(value, (dict, list)):
                                count_fields(value, f"{parent_key}.{key}" if parent_key else key)
                            else:
                                count += 1
                elif isinstance(obj, list) and obj:
                    count += 1
            
            count_fields(data)
            return count
            
        except Exception as e:
            print(f"⚠️ Error contando campos: {e}")
            return 0
    
    def _calculate_processing_time(self, start_time: datetime) -> int:
        """Calcula el tiempo de procesamiento en milisegundos"""
        return int((datetime.now() - start_time).total_seconds() * 1000)
    
    async def extract_multiple_products(self, html_elements: List[str], max_concurrent: int = 5) -> List[AIExtractionResult]:
        """
        Extrae múltiples productos de forma concurrente
        
        Args:
            html_elements: Lista de HTMLs de productos
            max_concurrent: Máximo número de extracciones concurrentes
            
        Returns:
            List[AIExtractionResult]: Lista de resultados
        """
        semaphore = asyncio.Semaphore(max_concurrent)
        
        async def extract_with_semaphore(html_content: str) -> AIExtractionResult:
            async with semaphore:
                return await self.extract_product_from_html(html_content)
        
        tasks = [extract_with_semaphore(html) for html in html_elements]
        results = await asyncio.gather(*tasks, return_exceptions=True)
        
        # Filtrar excepciones
        valid_results = []
        for result in results:
            if isinstance(result, AIExtractionResult):
                valid_results.append(result)
            else:
                valid_results.append(AIExtractionResult(
                    success=False,
                    error_message=f"Excepción durante extracción: {str(result)}"
                ))
        
        return valid_results
    
    def get_extraction_stats(self) -> Dict[str, Any]:
        """Obtiene estadísticas de extracción"""
        success_rate = (
            self.successful_extractions / self.total_extractions * 100
            if self.total_extractions > 0 else 0
        )
        
        return {
            "total_extractions": self.total_extractions,
            "successful_extractions": self.successful_extractions,
            "failed_extractions": self.failed_extractions,
            "success_rate_percentage": round(success_rate, 2),
            "model_used": self.model,
            "marketplace": self.marketplace_name
        }
    
    def reset_stats(self):
        """Reinicia las estadísticas"""
        self.total_extractions = 0
        self.successful_extractions = 0
        self.failed_extractions = 0


# Ejemplo de uso integrado con FalabellaScraper
class FalabellaAIIntegration:
    """
    Clase que integra el scraping tradicional con extracción por IA
    Útil para comparar resultados o como fallback
    """
    
    def __init__(self, traditional_scraper, ai_extractor: FalabellaAIExtractor):
        self.traditional_scraper = traditional_scraper
        self.ai_extractor = ai_extractor
    
    async def extract_with_fallback(self, element, html_content: str) -> Optional[Product]:
        """
        Intenta extracción tradicional primero, luego IA como fallback
        """
        try:
            # Intento 1: Extracción tradicional
            traditional_product = await self.traditional_scraper.extract_product_info(element)
            
            if traditional_product and traditional_product.price:
                print("✅ Extracción tradicional exitosa")
                return traditional_product
            
            # Intento 2: Extracción con IA
            print("🤖 Fallback a extracción con IA...")
            ai_result = await self.ai_extractor.extract_product_from_html(html_content)
            
            if ai_result.success and ai_result.product:
                print(f"✅ Extracción con IA exitosa (confianza: {ai_result.confidence_score})")
                return ai_result.product
            
            print("❌ Ambos métodos fallaron")
            return None
            
        except Exception as e:
            print(f"❌ Error en extracción híbrida: {e}")
            return None
    
    async def compare_extraction_methods(self, element, html_content: str) -> Dict[str, Any]:
        """
        Compara ambos métodos de extracción para análisis
        """
        results = {
            "traditional": {"success": False, "product": None, "error": None},
            "ai": {"success": False, "product": None, "error": None, "confidence": 0.0}
        }
        
        try:
            # Extracción tradicional
            traditional_product = await self.traditional_scraper.extract_product_info(element)
            if traditional_product:
                results["traditional"]["success"] = True
                results["traditional"]["product"] = traditional_product
            
        except Exception as e:
            results["traditional"]["error"] = str(e)
        
        try:
            # Extracción con IA
            ai_result = await self.ai_extractor.extract_product_from_html(html_content)
            if ai_result.success:
                results["ai"]["success"] = True
                results["ai"]["product"] = ai_result.product
                results["ai"]["confidence"] = ai_result.confidence_score
            else:
                results["ai"]["error"] = ai_result.error_message
                
        except Exception as e:
            results["ai"]["error"] = str(e)
        
        return results