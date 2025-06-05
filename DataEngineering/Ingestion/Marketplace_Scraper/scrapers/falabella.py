from playwright.sync_api import sync_playwright, Page
import asyncio
from typing import List, Optional
from urllib.parse import quote_plus
from models.product import Product
from scrapers.base_scraper import BaseScraper
from utils.helpers import clean_price, clean_text, extract_number, make_absolute_url
from models.price_info import PriceInfo

class FalabellaScraper(BaseScraper):
    """Scraper para Falabella Colombia"""
    
    def __init__(self, country: str = "co"):
        super().__init__()
        self.marketplace_name = "Falabella"
        self.country = country
        self.base_url = f"https://www.falabella.com.{country}/falabella-{country}"
        self.search_url = f"https://www.falabella.com.{country}/falabella-{country}/search"
        self.products_per_page = 40  # Falabella típicamente muestra 40 productos por página
        
        # Información de categorías
        self.category_info = {
            'parent_category': '',
            'parent_category_url': '',
            'category': '',
            'category_url': '',
            'category2': '',
            'category2_url': '',
            'breadcrumb': []
        }
    
    async def build_search_url(self, query: str, **kwargs) -> str:
        """Construye URL de búsqueda para Falabella"""
        encoded_query = quote_plus(query)
        page = kwargs.get('page', 1)
        
        # Estructura típica de Falabella: /search?Ntt=query&page=1
        if page == 1:
            return f"{self.search_url}?Ntt={encoded_query}"
        else:
            return f"{self.search_url}?Ntt={encoded_query}&page={page}"
    
    async def post_navigate_validation(self) -> bool:
        """
        Maneja validaciones específicas de Falabella después de navegar
        """
        try:
            print("🔍 Ejecutando validaciones post-navegación...")
            
            # 1. Manejar posibles popups o modales
            await self._handle_popups()
            
            # 2. Esperar a que cargue el contenido dinámico
            await self._wait_for_content_load()
            
            # 3. Extraer información de categorías si está disponible
            await self._extract_category_info()
            
            # 4. Verificar que se cargaron productos
            if not await self._verify_products_loaded():
                print("❌ No se pudieron cargar los productos")
                return False
            
            print("✅ Validaciones post-navegación completadas")
            return True
            
        except Exception as e:
            print(f"❌ Error en validaciones post-navegación: {e}")
            return False
    
    async def _handle_popups(self):
        """Maneja popups típicos de Falabella"""
        try:
            page = self.browser_manager.page
            print("🔍 Verificando popups...")
            
            # Posibles selectores de popups comunes en Falabella
            popup_selectors = [
                'button[aria-label="Cerrar"]',
                '.modal-close',
                '.popup-close',
                '[data-testid="modal-close"]',
                'button:has-text("Cerrar")',
                'button:has-text("×")'
            ]
            
            for selector in popup_selectors:
                try:
                    await page.wait_for_selector(selector, timeout=3000)
                    await page.click(selector)
                    print(f"✅ Popup cerrado con selector: {selector}")
                    await asyncio.sleep(1)
                    break
                except:
                    continue
                    
        except Exception as e:
            print(f"📝 No se detectaron popups: {e}")
    
    async def _wait_for_content_load(self):
        """Espera a que el contenido dinámico se cargue completamente"""
        try:
            page = self.browser_manager.page
            print("⏳ Esperando carga de contenido...")
            
            # Esperar por elementos típicos de Falabella
            content_selectors = [
                '[data-testid="product-item"]',
                '.product-item',
                '.grid-pod',
                '.product-card',
                '.product-list-item'
            ]
            
            for selector in content_selectors:
                try:
                    await page.wait_for_selector(selector, timeout=10000)
                    print(f"✅ Contenido cargado: {selector}")
                    await asyncio.sleep(2)  # Pausa adicional para JS dinámico
                    return
                except:
                    continue
                    
            print("⚠️ No se detectó contenido específico, continuando...")
            
        except Exception as e:
            print(f"⚠️ Error esperando contenido: {e}")
    
    async def _extract_category_info(self):
        """Extrae información de categorías desde el breadcrumb"""
        try:
            print("📂 Extrayendo información de categorías...")
            
            breadcrumb = await self.extract_breadcrumb()
            self.category_info['breadcrumb'] = breadcrumb
            
            if not breadcrumb:
                print("⚠️ No se pudo extraer breadcrumb")
                return
            
            # Procesar breadcrumb para extraer categorías
            for item in breadcrumb:
                position = item.get('position', 0)
                
                if position == 1:
                    self.category_info['parent_category'] = item['name']
                    self.category_info['parent_category_url'] = item['url']
                elif position == 2:
                    self.category_info['category'] = item['name']
                    self.category_info['category_url'] = item['url']
                elif position == 3:
                    self.category_info['category2'] = item['name']
                    self.category_info['category2_url'] = item['url']
            
            print(f"✅ Categorías extraídas correctamente")
            
        except Exception as e:
            print(f"❌ Error extrayendo categorías: {e}")
    
    async def _verify_products_loaded(self) -> bool:
        """Verifica que los productos se hayan cargado"""
        try:
            page = self.browser_manager.page
            print("🔍 Verificando carga de productos...")
            
            # Selectores posibles para productos de Falabella
            product_selectors = [
                '[data-testid="product-item"]',
                '.product-item',
                '.grid-pod',
                '.product-card',
                '.product-list-item',
                '.pod'
            ]
            
            for selector in product_selectors:
                try:
                    elements = await page.query_selector_all(selector)
                    if elements and len(elements) > 0:
                        print(f"✅ Productos encontrados: {len(elements)} elementos con selector {selector}")
                        return True
                except:
                    continue
            
            print("❌ No se encontraron productos")
            return False
            
        except Exception as e:
            print(f"❌ Error verificando productos: {e}")
            return False
    
    async def get_product_elements(self):
        """Obtiene los elementos de productos de Falabella"""
        try:
            page = self.browser_manager.page
            
            # Selectores múltiples para mayor robustez
            product_selectors = [
                '[data-testid="product-item"]',
                '.product-item',
                '.grid-pod',
                '.product-card',
                '.pod'
            ]
            
            elements = []
            
            for selector in product_selectors:
                try:
                    found_elements = await page.query_selector_all(selector)
                    if found_elements and len(found_elements) > 0:
                        elements = found_elements
                        print(f"🔍 Se encontraron {len(elements)} productos con selector: {selector}")
                        break
                except:
                    continue
            
            if not elements:
                print("❌ No se encontraron elementos de productos")
            
            return elements
            
        except Exception as e:
            print(f"❌ Error obteniendo elementos de productos: {e}")
            return []
    
    async def extract_product_info(self, element) -> Optional[Product]:
        """Extrae información del producto desde el elemento HTML"""
        try:
            
            #algo = (await element.evaluate('node => node.parentElement.innerHTML')).strip()
            #print(algo)
            #print("===============")

            # Extraer título
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
                parent_category = self.category_info.get('parent_category', ''),
                category        = self.category_info.get('category', ''),
                category2       = self.category_info.get('category2', ''),
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
    
    async def extract_applied_filters(self) -> List[str]:
        """
        Extrae los filtros actualmente aplicados
        
        Returns:
            List[str]: Lista con los filtros activos
        """
        filters = []
        
        try:
            page = self.browser_manager.page
            print("🔍 Extrayendo filtros aplicados...")
            
            # Selectores posibles para filtros aplicados
            filter_selectors = [
                '.applied-filters .filter-tag',
                '[data-testid="applied-filter"]',
                '.active-filters .filter-item',
                '.selected-filters .tag'
            ]
            
            for selector in filter_selectors:
                try:
                    filter_elements = await page.query_selector_all(selector)
                    if filter_elements:
                        for element in filter_elements:
                            filter_text = await element.inner_text()
                            filter_text = filter_text.strip()
                            if filter_text and filter_text not in filters:
                                filters.append(filter_text)
                        break
                except:
                    continue
            
            print(f"✅ Se extrajeron {len(filters)} filtros aplicados")
            return filters
            
        except Exception as e:
            print(f"❌ Error extrayendo filtros aplicados: {e}")
            return filters
    
    async def get_total_results(self) -> int:
        """Obtiene el número total de resultados"""
        try:
            page = self.browser_manager.page
            
            # Selectores posibles para el contador de resultados
            result_selectors = [
                '[data-testid="results-count"]',
                '.results-count',
                '.total-results',
                '.search-results-count'
            ]
            
            for selector in result_selectors:
                try:
                    result_element = await page.query_selector(selector)
                    if result_element:
                        result_text = await result_element.inner_text()
                        # Extraer número del texto
                        number = extract_number(result_text)
                        if number:
                            return int(number)
                except:
                    continue
            
            return 0
            
        except Exception as e:
            print(f"❌ Error obteniendo total de resultados: {e}")
            return 0
    
    async def has_next_page(self) -> bool:
        """Verifica si hay una página siguiente disponible"""
        try:
            page = self.browser_manager.page
            
            # Selectores para botón de página siguiente
            next_page_selectors = [
                '[data-testid="next-page"]:not([disabled])',
                '.pagination-next:not(.disabled)',
                'a[aria-label="Siguiente"]',
                '.next-page:not(.disabled)'
            ]
            
            for selector in next_page_selectors:
                try:
                    next_button = await page.query_selector(selector)
                    if next_button:
                        return True
                except:
                    continue
            
            return False
            
        except Exception as e:
            print(f"❌ Error verificando página siguiente: {e}")
            return False