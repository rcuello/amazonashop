from typing import Optional, List
from models.product import Product

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
            price_text = (await price_element.inner_text()).replace("\n", "").strip()
            link = await link_element.get_attribute('href')
            
            # Extraer datos opcionales
            brand_text = await self._extract_brand(element)
            image_url = await self._extract_image(element)
            
            # Crear objeto Product
            product = Product(
                title=title,
                price=price_text,
                marketplace=self.marketplace_name,
                currency="COP" if self.country == "co" else "USD",
                brand=brand_text,
                parent_category=category_info.get('parent_category', ''),
                category=category_info.get('category', ''),
                category2=category_info.get('category2', ''),
                url=link,
                image_url=image_url,
            )
            
            return product
            
        except Exception as e:
            print(f"⚠️ Error extrayendo información de producto: {e}")
            return None
    
    async def _extract_brand(self, element) -> str:
        """Extrae la marca del producto"""
        try:
            brand_element = await element.query_selector('span.poly-component__brand')
            if brand_element:
                return await brand_element.inner_text()
            return ""
        except:
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