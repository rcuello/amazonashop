from typing import Optional, List
from models.product import Product
from models.price_info import PriceInfo
from utils.helpers import extract_number,clean_price


class ProductExtractor:
    """Extrae información de productos"""
    
    def __init__(self, page, marketplace_name: str, country: str):
        self.page = page
        self.marketplace_name = marketplace_name
        self.country = country
    
    async def get_product_elements(self):
        """Obtiene los elementos de productos de Falabella"""
        try:
            page = self.page
            
            # Selectores múltiples para mayor robustez
            product_selectors = [
                '[data-testid="product-item"]',
                '.product-item',
                '.grid-pod',
                '.product-card',
                '.pod'
            ]
            
            elements = []
            
            found_elements = await page.query_selector_all(".grid-pod")
            
            if found_elements and len(found_elements) > 0:
                elements = found_elements
                #print(f"🔍 Se encontraron {len(elements)} productos con selector: {selector}")
                #break
            
            
            
            return elements
            
        except Exception as e:
            print(f"❌ Error obteniendo elementos de productos: {e}")
            return []
    
    async def extract_product_info(self, element, category_info: dict) -> Optional[Product]:
        """Extrae información del producto desde el elemento HTML"""
        try:
            title = await self._extract_title(element)
            
            # Extraer información de precios
            price_info = await self._extract_price(element)
            
            # Extraer enlace
            link = await self._extract_link(element)
            
            # Extraer imagen
            image_url = await self._extract_image(element)
            
            # Extraer marca
            brand_text = await self._extract_brand(element)
            
            # Extraer vendedor
            seller_text = await self._extract_seller(element)
            
            free_shipping= await self._check_free_shipping(element)
            
            # Verificar datos mínimos requeridos
            if not title:
                return None
            
            if price_info.current_price is None:
                print(f"⚠️ Precio no encontrado: {title}")
                #return None
            
            # Crear objeto Product
            product = Product(
                title           = title,
                price           = price_info.current_price,
                original_price  = price_info.original_price,
                marketplace     = self.marketplace_name,
                currency        = "COP",  # Falabella Colombia usa pesos colombianos
                brand           = brand_text,
                seller          = seller_text,
                parent_category = "",
                category        = "",
                category2       = "",
                free_shipping   = free_shipping,
                url             = link,
                image_url       = image_url,
            )
            
            return product
            
        except Exception as e:
            print(f"⚠️ Error extrayendo información de producto: {e}")
            return None
    async def _extract_title(self, element) -> Optional[str]:
        """Extrae el titulo del producto"""
        try:
            title_element = await element.query_selector('b.pod-subTitle')
            if title_element:
                return await title_element.inner_text()
            return None
        except:
            return None
    
    async def _check_free_shipping(self, element) -> bool:
        """Extrae si el producto tiene envío gratis"""
        try:
            #'span[id*="free_shipping"]' 
            # span:has-text("gratis") 
            #shipping_element = await element.query_selector('span#testId-Pod-badges-free_shipping')
            #shipping2_element = await element.query_selector('span#testId-Pod-badges-Envío Gratis')
            shipping_element = await element.query_selector('span:has-text("gratis")')
            shipping2_element = await element.query_selector('span:has-text("Gratis")')
            
            if shipping_element or shipping2_element:
                return True
                        
            return False
        except:
            return False
    
    async def _extract_link(self, element) -> Optional[str]:
        """Extrae el link del producto"""
        try:
            link_element = await element.query_selector('a.pod-link')
            if link_element:
                return await link_element.get_attribute('href')
            return None
        except:
            return None
        
    async def _extract_brand(self, element) -> str:
        """Extrae la marca del producto"""
        try:
            brand_element = await element.query_selector('b.title-rebrand')
            if brand_element:
                return await brand_element.inner_text()
            return ""
        except:
            return ""
    
    async def _extract_seller(self, element) -> str:
        """Extrae vendedor del producto"""
        try:
            seller_element = await element.query_selector('b.pod-sellerText')
            if seller_element:
                return await seller_element.inner_text()
            return ""
        except:
            return ""
        
    async def _extract_image(self, element) -> str:
        """Extrae la URL de la imagen del producto"""
        try:
            image_element = await element.query_selector('div.pod-head img')
            if image_element:
                return await image_element.get_attribute('src')
            return ""
        except:
            return ""
        
    async def extract_breadcrumb(self) -> List[dict]:
        """
        Extrae el breadcrumb de Falabella para navegación por categorías
        
        Returns:
            List[dict]: Lista de diccionarios con información del breadcrumb
        """
        breadcrumb_items = []
        
        try:
            page = self.browser_manager.page
            print("🍞 Extrayendo breadcrumb...")
            
            # Selectores posibles para breadcrumb en Falabella
            breadcrumb_selectors = [
                '.breadcrumb',
                '[data-testid="breadcrumb"]',
                '.breadcrumb-container',
                'nav[aria-label="breadcrumb"]',
                '.navigation-breadcrumb'
            ]
            
            breadcrumb_container = None
            
            for selector in breadcrumb_selectors:
                try:
                    breadcrumb_container = await page.query_selector(selector)
                    if breadcrumb_container:
                        print(f"✅ Breadcrumb encontrado con selector: {selector}")
                        break
                except:
                    continue
            
            if not breadcrumb_container:
                print("⚠️ No se encontró el breadcrumb")
                return breadcrumb_items
            
            # Extraer elementos del breadcrumb
            breadcrumb_elements = await breadcrumb_container.query_selector_all('a, span')
            
            position = 1
            for element in breadcrumb_elements:
                try:
                    name = await element.inner_text()
                    name = name.strip()
                    
                    if not name or name in ['>', '/', '|']:  # Separadores
                        continue
                    
                    url = ""
                    if element.tag_name.lower() == 'a':
                        url = await element.get_attribute('href')
                        if url and url.startswith('/'):
                            url = f"https://www.falabella.com.{self.country}{url}"
                    
                    breadcrumb_item = {
                        'name': name,
                        'url': url,
                        'position': position
                    }
                    
                    breadcrumb_items.append(breadcrumb_item)
                    position += 1
                    
                except Exception as e:
                    print(f"⚠️ Error procesando elemento del breadcrumb: {e}")
                    continue
            
            print(f"✅ Breadcrumb extraído: {len(breadcrumb_items)} elementos")
            for item in breadcrumb_items:
                print(f"   {item['position']}. {item['name']} -> {item['url']}")
            
            return breadcrumb_items
            
        except Exception as e:
            print(f"❌ Error extrayendo breadcrumb: {e}")
            return breadcrumb_items
    
    async def _extract_price(self,element) -> PriceInfo:
        """Extrae información de precios del producto."""
        try: 
            original_price = await self._get_original_price(element)
            
            current_price = await self._get_current_price(element)
            
            discount = await self._get_discount_text(element)
                
            return PriceInfo(original_price, current_price, discount)
        
        except Exception as e:
            print(f"⚠️ Error extrayendo precios: {e}")
            return PriceInfo.empty()
    
    async def _get_current_price(self, price_container) -> Optional[float]:
        """Extrae el precio actual del contenedor de precios"""
        try:
            current_element = await price_container.query_selector('li[data-internet-price]')
            
            if current_element:
                price_text = (await current_element.get_attribute('data-internet-price'))                             
                return clean_price(price_text)
            
            event_element = await price_container.query_selector('li[data-event-price]')
            
            if event_element:
                price_text = (await event_element.get_attribute('data-event-price'))                             
                return clean_price(price_text)
            
            #if not current_element:
            #    print("⚠️ No se encontraron elementos de precio actual")
                   
        except Exception as e:
            print(f"⚠️ Error extrayendo precio actual: {e}")
            
        return None  
    
    async def _get_original_price(self, price_container) -> Optional[float]:
        """Extrae el precio original (tachado) del contenedor de precios"""
        try:
            original_element = await price_container.query_selector('li[data-normal-price]')
            
            
            if original_element:
                price_text = (await original_element.get_attribute('data-normal-price'))
                 
                return clean_price(price_text)
                        
               
        except Exception as e:
            print(f"⚠️ Error extrayendo precio original: {e}")
            
        return None 
    
    async def _get_discount_text(self, price_container) -> str:
        """Extrae el texto de descuento del contenedor de precios"""
        try:
            discount_element = await price_container.query_selector('.discount-badge')
            
            if discount_element:
                discount_text = await discount_element.inner_text()
                return discount_text.strip()
                
        except Exception as e:
            print(f"⚠️ Error extrayendo descuento: {e}")
            
        return ""