from typing import Optional, List
from models.product import Product
from models.price_info import PriceInfo
from utils.helpers import extract_rating,extract_review_count,clean_price


class ProductExtractor:
    """Extrae información de productos"""
    
    def __init__(self, page, marketplace_name: str, country: str):
        self.page = page
        self.marketplace_name = marketplace_name
        self.country = country
    
    async def get_product_elements(self):
        """Obtiene los elementos de productos de MercadoLibre"""
        try:
            await self.page.wait_for_selector('li.ui-search-layout__item', timeout=15000)
            
            elements = await self.page.query_selector_all('li.ui-search-layout__item')
            print(f"🔍 Se encontraron {len(elements)} elementos de productos")
            
            return elements
            
        except Exception as e:
            print(f"❌ Error obteniendo elementos de productos: {e}")
            return []
    
    async def extract_product_info(self, element, category_info: dict) -> Optional[Product]:
        """Extrae información del producto desde el elemento HTML"""
        try:
            # Extraer elementos principales
            title_element = await element.query_selector('h3')
            price_element = await element.query_selector('span.andes-money-amount.andes-money-amount--cents-superscript')
            link_element = await element.query_selector('a.poly-component__title')
            
                        
            
            if not all([title_element, price_element, link_element]):
                return None
            
            # Extraer datos básicos
            title = (await title_element.inner_text()).strip()
            #price_text = (await price_element.inner_text()).replace("\n", "").strip()
            link = await link_element.get_attribute('href')
            
            # Extraer datos opcionales
            seller_text = await self._extract_seller(element)
            image_url = await self._extract_image(element)
            rating = await self._extract_rating(element)
            reviews_count = await self._extract_reviews_count(element)
            
            # Extraer información de precios
            price_info = await self._extract_price(element)
            
            # Crear objeto Product
            product = Product(
                title = title,
                price = price_info.current_price,
                original_price = price_info.original_price,
                marketplace = self.marketplace_name,
                currency = "COP" if self.country == "co" else "USD",
                seller = seller_text,
                parent_category = category_info.get('parent_category', ''),
                category = category_info.get('category', ''),
                category2 = category_info.get('category2', ''),
                rating = rating,
                reviews_count = reviews_count,
                url = link,
                image_url = image_url,
            )
            
            return product
            
        except Exception as e:
            print(f"⚠️ Error extrayendo información de producto: {e}")
            return None
    
    async def _extract_price(self, element)  -> PriceInfo:
        """Extrae información de precios del producto."""
        try:            
            price_container = await element.query_selector('div.poly-component__price')
            
            if not price_container:
                print(f"⚠️ No price container found")
                return PriceInfo.empty()
            
            # Extraer precios usando métodos auxiliares
            original_price = await self._get_original_price(price_container)
            
            current_price = await self._get_current_price(price_container)
            
            if not current_price:
                current_price = original_price
            
            discount = await self._get_discount_text(price_container)
            
            return PriceInfo(original_price, current_price, discount)
            
        except Exception as e:
            print(f"⚠️ Error extrayendo precios: {e}")
            return PriceInfo.empty()
        
    async def _get_original_price(self, price_container) -> Optional[float]:
        """Extrae el precio original (tachado) del contenedor de precios"""
        try:
            original_element = await price_container.query_selector(
                'div.poly-price__current span.andes-money-amount.andes-money-amount--cents-superscript'
            )
            
            s_element = await price_container.query_selector(
                's.andes-money-amount.andes-money-amount--previous.andes-money-amount--cents-comma'
            )
            
            #test = await price_container.inner_html()
            
            #print(f"price_container: {test}")
            #print(f"==============================")
            
            # Precio subrayado
            if s_element:                
                price_text = (await s_element.inner_text()).replace("\n", "").strip()
                #print(f"✅ Precio subrayado encontrado: {price_text}")
                return clean_price(price_text)
            
            if original_element:
                price_text = (await original_element.inner_text()).replace("\n", "").strip() 
                #print(f"✅ Precio original encontrado: {price_text}")               
                return clean_price(price_text)
               
        except Exception as e:
            print(f"⚠️ Error extrayendo precio original: {e}")
            
        return None    
    
    async def _get_current_price(self, price_container) -> Optional[float]:
        """Extrae el precio actual del contenedor de precios"""
        try:
            current_element = await price_container.query_selector(
                '.poly-price__current .andes-money-amount__fraction'
            )
            
            if current_element:
                price_text = await current_element.inner_text()
                #print(f"✅ Precio actual encontrado: {price_text}")
                return clean_price(price_text)
            
            if not current_element:
                print("⚠️ No se encontraron elementos de precio actual")
                    
        except Exception as e:
            print(f"⚠️ Error extrayendo precio actual: {e}")
            
        return None
    async def _extract_seller(self, element) -> str:
        """Extrae la marca del producto"""
        try:
            brand_element = await element.query_selector('span.poly-component__brand')
            if brand_element:
                return await brand_element.inner_text()
            return ""
        except:
            return ""
        
    async def _get_discount_text(self, price_container) -> str:
        """Extrae el texto de descuento del contenedor de precios"""
        try:
            discount_element = await price_container.query_selector(
                '.poly-price__current .andes-money-amount__discount'
            )
            
            if discount_element:
                discount_text = await discount_element.inner_text()
                return discount_text.strip()
                
        except Exception as e:
            print(f"⚠️ Error extrayendo descuento: {e}")
            
        return ""
    
    async def _extract_image(self, element) -> str:
        """Extrae la URL de la imagen del producto"""
        try:
            image_element = await element.query_selector('img.poly-component__picture')
            if image_element:
                return await image_element.get_attribute('src')
            return ""
        except:
            return ""
    
    async def _extract_rating(self, element) -> Optional[float]:
        """Extrae la calificación del producto"""
        try:
            span_element = await element.query_selector('span.poly-reviews__rating')
            if span_element:
                span_text = await span_element.inner_text()
                return extract_rating(span_text)
            
            return None
        except:
            return None
    
    async def _extract_reviews_count(self, element) -> Optional[float]:
        """Extrae total de calificaciones del producto"""
        try:
            span_element = await element.query_selector('span.poly-reviews__total')
            if span_element:
                span_text = await span_element.inner_text()
                return extract_review_count(span_text)
            
            return None
        except:
            return None