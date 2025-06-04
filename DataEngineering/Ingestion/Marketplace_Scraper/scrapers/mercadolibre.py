from playwright.sync_api import sync_playwright, Page
import asyncio
from typing import List, Optional
from urllib.parse import quote_plus
from models.product import Product
from scrapers.base_scraper import BaseScraper
from utils.helpers import clean_price, clean_text, extract_rating, extract_number, make_absolute_url

class MercadoLibreScraper(BaseScraper):
    """Scraper para MercadoLibre"""
    
    def __init__(self, country: str = "co"):
        super().__init__()
        self.marketplace_name = "MercadoLibre"
        self.country = country
        self.base_url = f"https://listado.mercadolibre.com.{country}"
        self.products_per_page = 50  # MercadoLibre muestra 50 productos por página
        self.parent_category = ""
        self.parent_category_url=""
        self.category = ""
        self.category_url=""
        # Cache para la URL base de paginación (se construye una vez)
        self._pagination_base_url = None
        
        self.category_info = {
            'parent_category': '',
            'parent_category_url': '',
            'category': '',
            'category_url': '',
            'category2': '',
            'category2_url': '',
            'breadcrumb': []
        }
        
        # Cache para URLs de paginación
        self._pagination_cache = {
            'base_url': None,
            'search_params': {},
            'filters': []
        }
    
    async def build_search_url(self, query: str, **kwargs) -> str:
        """Construye URL de búsqueda para MercadoLibre"""
        encoded_query = quote_plus(query)
        page = kwargs.get('page', 1)
                
        
        # Página 1: URL simple sin paginación
        if page == 1:
            return f"{self.base_url}/{encoded_query}"
        
        # Para páginas siguientes, construir URL con paginación
        return await self._build_pagination_url(encoded_query, page)
  
    async def _extract_category_info(self) -> None:
        """Extrae y almacena información completa de categorías"""
        try:
            print("📂 Extrayendo información de categorías...")
            
            # Extraer breadcrumb
            breadcrumb = await self.extract_breadcrumb()
            self.category_info['breadcrumb'] = breadcrumb
            
            if not breadcrumb:
                print("⚠️ No se pudo extraer breadcrumb")
                return
            
            # Encontrar categoría padre (posición 1)
            parent_category = next(
                (item for item in breadcrumb if item['position'] == 1), 
                None
            )
            
            if parent_category:
                self.category_info['parent_category'] = parent_category['name']
                self.category_info['parent_category_url'] = parent_category['url']
                print(f"📁 Categoría padre: {parent_category['name']}")
            
            # Encontrar subcategoría (posición 2)
            subcategory = next(
                (item for item in breadcrumb if item['position'] == 2), 
                None
            )
            
            # Encontrar subcategoría (posición 2)
            subcategory2 = next(
                (item for item in breadcrumb if item['position'] == 3), 
                None
            )
            
            if subcategory:
                self.category_info['category'] = subcategory['name']
                self.category_info['category_url'] = subcategory['url']
                print(f"📂 Subcategoría: {subcategory['name']}")
                
            if subcategory2:
                self.category_info['category2'] = subcategory2['name']
                self.category_info['category2_url'] = subcategory2['url']
                print(f"📂 Subcategoría 2: {subcategory2['name']}")
            
            # Si no hay subcategoría, usar la categoría padre como categoría principal
            if not subcategory and parent_category:
                self.category_info['category'] = parent_category['name']
                self.category_info['category_url'] = parent_category['url']
                print("📝 Usando categoría padre como categoría principal")
                
        except Exception as e:
            print(f"❌ Error extrayendo información de categorías: {e}")
                         
    async def _build_pagination_url(self, encoded_query: str, page: int) -> str:
        """Construye URL de paginación basada en la estructura actual de la página"""
        try:
            # Si no tenemos info de categorías, extraerla
            if not self.category_info['category_url']:
                await self._extract_category_info()
            
            # Calcular offset
            offset = ((page - 1) * self.products_per_page) + 1
            
            # Obtener filtros aplicados
            filters = await self._get_active_filters()
            
            # Construir URL base para paginación
            if self.category_info['category_url']:
                base_url = self.category_info['category_url'].rstrip('/')
            else:
                base_url = self.base_url
            
            # Agregar filtros si existen
            filter_path = f"/{filters[0]}" if filters else ""
            
            # URL final de paginación
            pagination_url = f"{base_url}{filter_path}/{encoded_query}_Desde_{offset}_NoIndex_True"
            
            print(f"📄 Página {page} | Offset: {offset}")
            print(f"🔗 URL: {pagination_url}")
            
            return pagination_url
            
        except Exception as e:
            print(f"❌ Error construyendo URL de paginación: {e}")
            # Fallback a URL simple
            offset = ((page - 1) * self.products_per_page) + 1
            return f"{self.base_url}/{encoded_query}_Desde_{offset}_NoIndex_True"
     
     
    async def _get_active_filters(self) -> List[str]:
        """Obtiene filtros activos de manera más robusta"""
        try:
            filters = []
            page = self.browser_manager.page
            
            # Múltiples selectores para filtros aplicados
            filter_selectors = [
                'section.ui-search-applied-filters .andes-tag__label',
                '.ui-search-applied-filters .ui-search-applied-filter-name',
                '[data-testid="applied-filters"] .andes-tag__label'
            ]
            
            for selector in filter_selectors:
                elements = await page.query_selector_all(selector)
                if elements:
                    for element in elements:
                        try:
                            filter_text = await element.inner_text()
                            filter_text = filter_text.strip()
                            if filter_text and filter_text not in filters:
                                filters.append(filter_text)
                        except:
                            continue
                    break  # Si encontramos filtros con un selector, usar esos
            
            return filters
            
        except Exception as e:
            print(f"⚠️ Error obteniendo filtros activos: {e}")
            return []
            
    async def _build_pagination_base_url(self, encoded_query: str) -> None:
        """
        Construye la URL base para paginación extrayendo breadcrumb y filtros
        Solo se ejecuta una vez y se cachea el resultado
        """
        try:
            print("🔗 Construyendo URL base de paginación...")
            
            # Extraer breadcrumb para obtener la categoría
            #breadcrumb = await self.extract_breadcrumb()
            #if not breadcrumb:
            #    print("⚠️ No se pudo extraer breadcrumb, usando URL base simple")
            #    self._pagination_base_url = self.base_url
            #    return
            
            # Buscar la categoría de nivel 2 (la más específica para productos)
            #parent_category_item = next((item for item in breadcrumb if item['position'] == 1), None)
            #category_item = next((item for item in breadcrumb if item['position'] == 2), None)
            
                
            #if not category_item:
            #    print("⚠️ No se encontró categoría de nivel 2, usando URL base simple")
            #    self._pagination_base_url = self.base_url
            #    return
            
            #if parent_category_item:
            #    self.parent_category = parent_category_item['name']
                
            #if category_item:
            #    self.category = category_item['name']
                    
            # Extraer filtros aplicados
            applied_filters = await self.extract_applied_filters()
            filter_suffix = f"/{applied_filters[0]}" if applied_filters else ""
            
            # Construir URL base de paginación
            self._pagination_base_url = f"{self.category_url.rstrip('/')}{filter_suffix}"
            
            print(f"✅ URL base de paginación: {self._pagination_base_url}")
            
        except Exception as e:
            print(f"❌ Error construyendo URL de paginación: {e}")
            self._pagination_base_url = self.base_url
              
    async def post_navigate_validation(self) -> bool:
        """
        Maneja validaciones específicas de MercadoLibre después de navegar        
        """
        try:
            print("🔍 Ejecutando validaciones post-navegación...")
            
            # 1. Manejar popup de ubicación
            await self._handle_location_popup()
            
            # 2. Aplicar filtro de envío destacado
            await self._apply_shipping_filter()
            
             # 3. Extraer información de categorías
            await self._extract_category_info()
            
            # 4. Verificar carga de productos
            if not await self._verify_page_loaded():
                #print("❌ La página no cargó correctamente")
                return False
            
            print("✅ Validaciones post-navegación completadas")
            return True
            
        except Exception as e:
            print(f"❌ Error en validaciones post-navegación: {e}")
            return False
        
    async def _handle_location_popup(self):
        """
        Maneja el popup de ubicación si aparece       
        """
        try:
            page = self.browser_manager.page
            print("🔍 Verificando popup de ubicación...")
            
            # Esperar por el popup de ubicación (máximo 5 segundos)
            await page.wait_for_selector('text="Agregar ubicación"', timeout=5000)
            print("📍 Popup de ubicación detectado, haciendo clic en 'Más tarde'...")
            await page.click('text="Más tarde"')
            await asyncio.sleep(1)  # Pequeña pausa para que se procese
                
        except Exception:
            # Si no aparece el popup, continúa normalmente
            print("📍 No se detectó popup de ubicación")
            pass
        
    async def _apply_shipping_filter(self):
        """
        Aplica filtro de envío destacado        
        """
        try:
            page = self.browser_manager.page
            print("🚛 Aplicando filtro de envío destacado...")
            
            await page.wait_for_selector('#shipping_highlighted_fulfillment', timeout=10000)
            await page.click('#shipping_highlighted_fulfillment')
            print("✅ Filtro de envío aplicado")
            await asyncio.sleep(2)  # Esperar a que se aplique el filtro
            
        except Exception as e:
            print(f"⚠️ No se pudo aplicar el filtro de envío: {e}")
            
            
    async def _verify_page_loaded(self) -> bool:
        """Verifica que la página de resultados haya cargado correctamente"""
        try:
            page = self.browser_manager.page
            print("⏳ Verificando que la página haya cargado...")
            
            # Esperar por los elementos de productos (del script original)
            await page.wait_for_selector('li.ui-search-layout__item', timeout=15000)
            print("✅ Elementos de productos detectados")
            return True
            
        except Exception as e:
            print(f"❌ Error verificando carga de página: {e}")
            return False
    
    async def get_product_elements(self):
        """
        Obtiene los elementos de productos de MercadoLibre        
        """
        try:
            page = self.browser_manager.page
            
            # Esperar a que los elementos estén presentes
            await page.wait_for_selector('li.ui-search-layout__item', timeout=15000)
            
            # Obtener todos los elementos de productos
            elements = await page.query_selector_all('li.ui-search-layout__item')
            print(f"🔍 Se encontraron {len(elements)} elementos de productos")
            
            return elements
            
        except Exception as e:
            print(f"❌ Error obteniendo elementos de productos: {e}")
            return []
        
    async def extract_product_info(self, element) -> Optional[Product]:
        """
        Extrae información del producto desde el elemento HTML
        """
        try:
            # Buscar elementos específicos (del script original)
            title_element = await element.query_selector('h3')
            price_element = await element.query_selector('span.andes-money-amount.andes-money-amount--cents-superscript')
            link_element = await element.query_selector('a.poly-component__title')
            
            
            # Verificar que todos los elementos necesarios existan
            if not all([title_element, price_element, link_element]):
                return None
            
            # Extraer textos
            title = await title_element.inner_text()
            title = title.strip()
            
            price_text = await price_element.inner_text()
            price_text = price_text.replace("\n", "").strip()
            
            link = await link_element.get_attribute('href')
            
            brand_text = ""
            brand_element = await element.query_selector('span.poly-component__brand')
            if brand_element:
                brand_text = await brand_element.inner_text()
            
            image_url =""    
            image_element = await element.query_selector('img.poly-component__picture')
            if image_element:
                image_url = await image_element.get_attribute('src')
            
            parent_category = self.category_info.get('parent_category', '')
            category = self.category_info.get('category', '')
            category2 = self.category_info.get('category2', '')
            
            # Crear objeto Product
            product = Product(
                title=title,
                price=price_text,
                
                marketplace=self.marketplace_name,
                currency="COP" if self.country == "co" else "USD",
                brand=brand_text,
                
                parent_category=parent_category,
                category=category,
                category2=category2,
                
                url=link,
                image_url = image_url,
            )
            
            return product
            
        except Exception as e:
            print(f"⚠️ Error extrayendo información de producto: {e}")
            return None
    
    async def pagination_url(self)->str:
        """
        Devuelve la URL de la página de paginación
        """
        
        breadcrumb = await self.extract_breadcrumb()
        # https://listado.mercadolibre.com.co/electronica-audio-video/televisores/
        
        
        filter = await self.extract_applied_filters()        
        # https://listado.mercadolibre.com.co/electronica-audio-video/televisores/apple/iphone_Desde_1951_NoIndex_True
        
        first_filter = next((x for x in filter), "")
        second_breadcrumb = next((x for x in breadcrumb if x['position'] == 2), {})
                
        #print(f"{second_breadcrumb['url']}")
        #print(f"{first_filter}")
        url = f"{second_breadcrumb['url']}{first_filter}"
                
        
        return url
    
    async def extract_breadcrumb(self) -> List[dict]:
        """
        Extrae el breadcrumb de MercadoLibre para navegación por categorías
        
        Returns:
            List[dict]: Lista de diccionarios con la estructura:
            [
                # Categoria padre
                {
                    'name': 'Electrónica, Audio y Video',
                    'url': 'https://www.mercadolibre.com.co/c/electronica-audio-y-video',
                    'position': 1
                },
                # Subcategoría
                {
                    'name': 'Televisores', 
                    'url': 'https://listado.mercadolibre.com.co/electronica-audio-video/televisores/',
                    'position': 2
                }
            ]
        """
        breadcrumb_items = []
        
        try:
            page = self.browser_manager.page
            print("🍞 Extrayendo breadcrumb...")
            
            # Buscar el contenedor del breadcrumb
            breadcrumb_container = await page.query_selector('ol.andes-breadcrumb')
            
            if not breadcrumb_container:
                print("⚠️ No se encontró el breadcrumb")
                return breadcrumb_items
                        
            # Obtener todos los elementos del breadcrumb
            breadcrumb_elements = await breadcrumb_container.query_selector_all('li.andes-breadcrumb__item')
            
            for element in breadcrumb_elements:
                try:
                    # Extraer el enlace
                    link_element = await element.query_selector('a.andes-breadcrumb__link')
                    if not link_element:
                        continue
                    
                    # Extraer nombre
                    name_element = await link_element.query_selector('span[itemprop="name"]')
                    if not name_element:
                        continue
                    
                    name = await name_element.inner_text()
                    name = name.strip()
                    
                    # Extraer URL
                    url = await link_element.get_attribute('href')
                    
                    # Extraer posición
                    position_element = await element.query_selector('meta[itemprop="position"]')
                    position = 0
                    if position_element:
                        position_content = await position_element.get_attribute('content')
                        try:
                            position = int(position_content) if position_content else 0
                        except ValueError:
                            position = 0
                    
                    # Crear item del breadcrumb
                    breadcrumb_item = {
                        'name': name,
                        'url': url,
                        'position': position
                    }
                    
                    breadcrumb_items.append(breadcrumb_item)
                    
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
    
    async def extract_applied_filters(self) -> list[str]:
        """
        Extrae los filtros actualmente aplicados desde el HTML
        
        Returns:
            List[str]: Lista con los filtros activos
        """
        filters = []
        
        try:
            page = self.browser_manager.page
            print("🔍 Extrayendo filtros aplicados...")
            
            # Buscar la sección de filtros aplicados
            filters_section = await page.query_selector('section.ui-search-applied-filters')
            
            if not filters_section:
                print("📝 No se encontraron filtros aplicados")
                return filters
            
            # Obtener todos los tags de filtros
            filter_tags = await filters_section.query_selector_all('.andes-tag__label')
            
            print(f"🏷️ Se encontraron {len(filter_tags)} etiquetas de filtros")
            
            for tag in filter_tags:
                try:
                    # Extraer el label del filtro
                    filter_value = await tag.inner_text()
                    filter_value = filter_value.strip()
                    
                    if filter_value:  
                        print(f"   📌 Filtro encontrado: {filter_value}")
                        filters.append(filter_value)
                        
                except Exception as e:
                    print(f"⚠️ Error procesando filtro: {e}")
                    continue
                    
            print(f"✅ Se extrajeron {len(filters)} filtros válidos")
            return filters
            
        except Exception as e:
            print(f"❌ Error extrayendo filtros aplicados: {e}")
            return filters    