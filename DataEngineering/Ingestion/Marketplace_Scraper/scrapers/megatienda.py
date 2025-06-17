from playwright.sync_api import sync_playwright, Page
import asyncio
from typing import List, Optional
from urllib.parse import quote_plus
from models.product import Product
from scrapers.base_scraper import BaseScraper
from utils.helpers import clean_price, clean_text, extract_number, make_absolute_url
from models.price_info import PriceInfo

class MegaTiendaScraper(BaseScraper):
    """Scraper para Megatiendas Colombia"""
    
    def __init__(self, country: str = "co", mobile: bool = False, device: Optional[str] = None):
        super().__init__(mobile=mobile, device=device)
        
        # Configuración de Megatiendas
        self.marketplace_name = "Megatiendas"
        self.country = country
        self.base_url = "https://www.megatiendas.co"
        self.search_url = f"{self.base_url}/buscar"
        self.products_per_page = 24  # Megatiendas típicamente muestra 24 productos por página
        
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
        """Construye URL de búsqueda para Megatiendas"""
        encoded_query = quote_plus(query)
        page = kwargs.get('page', 1)
        
        # Estructura típica de Megatiendas: /buscar?q=query o /buscar?q=query&page=1
        if page == 1:
            return f"{self.search_url}?q={encoded_query}"
        else:
            return f"{self.search_url}?q={encoded_query}&page={page}"
    
    async def post_navigate_validation(self) -> bool:
        """
        Maneja validaciones específicas de Megatiendas después de navegar
        """
        try:
            print("🔍 Ejecutando validaciones post-navegación para Megatiendas...")
            
            # 1. Manejar posibles popups o modales
            await self._handle_popups()
            
            # 2. Esperar a que cargue el contenido dinámico
            await self._wait_for_content_load()
            
            # 3. Extraer información de categorías si está disponible
            #await self._extract_category_info()
            
            # 4. Verificar que se cargaron productos
            if not await self._verify_products_loaded():
                print("❌ No se pudieron cargar los productos de Megatiendas")
                return False
            
            print("✅ Validaciones post-navegación completadas para Megatiendas")
            return True
            
        except Exception as e:
            print(f"❌ Error en validaciones post-navegación de Megatiendas: {e}")
            return False
    
    async def _handle_popups(self):
        """Maneja popups típicos de Megatiendas"""
        try:
            page = self.browser_manager.page
            print("🔍 Verificando popup bloqueante de Megatiendas [Como quieres recibir tu pedido]...")
            
            print("   ⏳ Esperando que la página cargue completamente...")
            await page.wait_for_function("document.readyState === 'complete'")
            print("   ✅ Página cargada.")
                        
            
            # Pasos para remover el popup de Megatiendas (No se puede cerrar sin diligenciarlo):            
            # 1. Seleccionar (Html Button) "Recoge en tienda"
            # 2. Seleccionar (Html Select) "Departamento" => Bolívar
            # 3. Seleccionar (Html Select) "Ciudad" => Cartagena
            # 4. Seleccionar (Html Radio) "Tienda" => Megatiendas Prado
            # 5. Click en Boton "Guardar"
                        
            button_element = await page.wait_for_selector('text="Recoge en tienda"', timeout=5000)
            await button_element.click()
            await asyncio.sleep(1)

            region_input_selector = 'div.megatiendas-delivery-modal-1-x-ModalFormAddress__card_content_store--active input[type="text"]'
            region_element_inputs = await page.query_selector_all(region_input_selector)
            
            #print(f"region_element_inputs: {len(region_element_inputs)} elementos")
            
            departamento_element = region_element_inputs[0]
            ciudad_element = region_element_inputs[1]
            
            await departamento_element.click()
            await departamento_element.fill('Bolívar') 
            await asyncio.sleep(1)           
            await page.keyboard.press("ArrowDown")
            await page.keyboard.press("Enter")
            await asyncio.sleep(1)
            await ciudad_element.click()
            await ciudad_element.fill('Cartagena')
            await asyncio.sleep(1)
            await page.keyboard.press("ArrowDown")
            await page.keyboard.press("Enter")
            await asyncio.sleep(1)
            await page.get_by_role("button", name="Guardar").click()
            
            # npx playwright codegen https://www.megatiendas.co/galleta?_q=galleta
            
            print("✅ Popups de Megatiendas manejados")
            
                    
        except Exception as e:
            print(f"📝 No se detectaron popups en Megatiendas: {e}")
    
    async def _wait_for_content_load(self):
        """Espera a que el contenido dinámico se cargue completamente"""
        try:
            page = self.browser_manager.page
            print("⏳ Esperando carga de contenido de Megatiendas...")
            
            # Esperar por elementos típicos de Megatiendas
            content_selectors = [
                '.gallery-layout-container',
                '.vtex-store-components-3-x-container'
            ]
            
            for selector in content_selectors:
                try:
                    await page.wait_for_selector(selector, timeout=15000)
                    print(f"✅ Contenido de Megatiendas cargado: {selector}")
                    await asyncio.sleep(3)  # Pausa adicional para JS dinámico
                    return
                except:
                    continue
                    
            print("⚠️ No se detectó contenido específico de Megatiendas, continuando...")
            
        except Exception as e:
            print(f"⚠️ Error esperando contenido de Megatiendas: {e}")    
    
    async def _verify_products_loaded(self) -> bool:
        """Verifica que los productos se hayan cargado"""
        try:
            page = self.browser_manager.page
            print("🔍 Verificando carga de productos de Megatiendas...")
            
            # Selectores posibles para productos de Megatiendas
            product_selectors = [                
                '[data-testid="product-item"]',
                '.vtex-product-summary-2-x-productBrand'
                '.vtex-product-summary-2-x-container',
            ]
            
            for selector in product_selectors:
                try:
                    elements = await page.query_selector_all(selector)
                    if elements and len(elements) > 0:
                        print(f"✅ Productos de Megatiendas encontrados: {len(elements)} elementos con selector {selector}")
                        return True
                except:
                    continue
            
            print("❌ No se encontraron productos de Megatiendas")
            return False
            
        except Exception as e:
            print(f"❌ Error verificando productos de Megatiendas: {e}")
            return False
    
    async def get_product_elements(self):
        """Obtiene los elementos de productos de Megatiendas"""
        try:
            page = self.browser_manager.page
            
            # Selectores múltiples para mayor robustez
            product_selectors = [
                '.product-item',
                '.product-card',
                '.product-container',
                '.shelf-item',
                '.product-tile',
                '[data-testid="product-item"]',
                '.vtex-product-summary',
                '.product-summary'
            ]
            
            elements = []
            
            for selector in product_selectors:
                try:
                    found_elements = await page.query_selector_all(selector)
                    if found_elements and len(found_elements) > 0:
                        elements = found_elements
                        print(f"🔍 Se encontraron {len(elements)} productos de Megatiendas con selector: {selector}")
                        break
                except:
                    continue
            
            if not elements:
                print("❌ No se encontraron elementos de productos de Megatiendas")
            
            return elements
            
        except Exception as e:
            print(f"❌ Error obteniendo elementos de productos de Megatiendas: {e}")
            return []
    
    async def extract_product_info(self, element) -> Optional[Product]:
        """Extrae información del producto desde el elemento HTML"""
        try:
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
            
            # Extraer vendedor (en Megatiendas es siempre Megatiendas)
            seller_text = "Megatiendas"
            
            # Verificar envío gratis
            free_shipping = await self._check_free_shipping(element)
            
            # Verificar datos mínimos requeridos
            if not title:
                print("⚠️ Título no encontrado en producto de Megatiendas")
                return None
            
            if price_info.current_price is None:
                print(f"⚠️ Precio no encontrado en Megatiendas: {title}")
                # No retornamos None para permitir productos sin precio visible
            
            # Crear objeto Product
            product = Product(
                title           = title,
                price           = price_info.current_price,
                original_price  = price_info.original_price,
                marketplace     = self.marketplace_name,
                currency        = "COP",  # Megatiendas Colombia usa pesos colombianos
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
            print(f"⚠️ Error extrayendo información de producto de Megatiendas: {e}")
            return None
    
    async def _extract_title(self, element) -> Optional[str]:
        """Extrae el titulo del producto"""
        try:
            # Selectores posibles para el título en Megatiendas
            title_selectors = [
                '.product-name',
                '.product-title',
                'h3.product-name',
                '.vtex-product-summary__product-name',
                '.product-summary-name',
                '[data-testid="product-name"]',
                'a.product-name',
                '.shelf-item__name'
            ]
            
            for selector in title_selectors:
                try:
                    title_element = await element.query_selector(selector)
                    if title_element:
                        title = await title_element.inner_text()
                        return clean_text(title)
                except:
                    continue
                    
            return None
        except:
            return None
    
    async def _check_free_shipping(self, element) -> bool:
        """Extrae si el producto tiene envío gratis"""
        try:
            # Selectores para envío gratis en Megatiendas
            shipping_selectors = [
                '.free-shipping',
                '.envio-gratis',
                '.shipping-free',
                'span:has-text("Envío gratis")',
                'span:has-text("gratis")',
                'span:has-text("Gratis")',
                '.delivery-free',
                '[data-testid="free-shipping"]'
            ]
            
            for selector in shipping_selectors:
                try:
                    shipping_element = await element.query_selector(selector)
                    if shipping_element:
                        return True
                except:
                    continue
                        
            return False
        except:
            return False
    
    async def _extract_link(self, element) -> Optional[str]:
        """Extrae el link del producto"""
        try:
            # Selectores para enlaces en Megatiendas
            link_selectors = [
                'a.product-link',
                'a[href*="/p"]',
                '.product-name a',
                '.product-title a',
                'a.shelf-item__link',
                '.vtex-product-summary a'
            ]
            
            for selector in link_selectors:
                try:
                    link_element = await element.query_selector(selector)
                    if link_element:
                        href = await link_element.get_attribute('href')
                        if href:
                            return make_absolute_url(href, self.base_url)
                except:
                    continue
                    
            return None
        except:
            return None
        
    async def _extract_brand(self, element) -> str:
        """Extrae la marca del producto"""
        try:
            # Selectores para marca en Megatiendas
            brand_selectors = [
                '.product-brand',
                '.brand-name',
                '.vtex-product-summary__brand',
                '[data-testid="product-brand"]',
                '.shelf-item__brand'
            ]
            
            for selector in brand_selectors:
                try:
                    brand_element = await element.query_selector(selector)
                    if brand_element:
                        brand = await brand_element.inner_text()
                        return clean_text(brand)
                except:
                    continue
                    
            return ""
        except:
            return ""
        
    async def _extract_image(self, element) -> str:
        """Extrae la URL de la imagen del producto"""
        try:
            # Selectores para imagen en Megatiendas
            image_selectors = [
                '.product-image img',
                '.product-img img',
                '.vtex-product-summary__image img',
                '.shelf-item__image img',
                'img.product-image',
                '[data-testid="product-image"] img'
            ]
            
            for selector in image_selectors:
                try:
                    image_element = await element.query_selector(selector)
                    if image_element:
                        src = await image_element.get_attribute('src')
                        if src:
                            return make_absolute_url(src, self.base_url)
                except:
                    continue
                    
            return ""
        except:
            return ""
        
    async def extract_breadcrumb(self) -> List[dict]:
        """
        Extrae el breadcrumb de Megatiendas para navegación por categorías
        
        Returns:
            List[dict]: Lista de diccionarios con información del breadcrumb
        """
        breadcrumb_items = []
        
        try:
            page = self.browser_manager.page
            print("🍞 Extrayendo breadcrumb de Megatiendas...")
            
            # Selectores posibles para breadcrumb en Megatiendas
            breadcrumb_selectors = [
                '.breadcrumb',
                '[data-testid="breadcrumb"]',
                '.breadcrumb-container',
                'nav[aria-label="breadcrumb"]',
                '.navigation-breadcrumb',
                '.vtex-breadcrumb',
                '.breadcrumb-list'
            ]
            
            breadcrumb_container = None
            
            for selector in breadcrumb_selectors:
                try:
                    breadcrumb_container = await page.query_selector(selector)
                    if breadcrumb_container:
                        print(f"✅ Breadcrumb de Megatiendas encontrado con selector: {selector}")
                        break
                except:
                    continue
            
            if not breadcrumb_container:
                print("⚠️ No se encontró el breadcrumb de Megatiendas")
                return breadcrumb_items
            
            # Extraer elementos del breadcrumb
            breadcrumb_elements = await breadcrumb_container.query_selector_all('a, span, li')
            
            position = 1
            for element in breadcrumb_elements:
                try:
                    name = await element.inner_text()
                    name = clean_text(name)
                    
                    if not name or name in ['>', '/', '|', '»']:  # Separadores
                        continue
                    
                    url = ""
                    if element.tag_name.lower() == 'a':
                        url = await element.get_attribute('href')
                        if url:
                            url = make_absolute_url(url, self.base_url)
                    
                    breadcrumb_item = {
                        'name': name,
                        'url': url,
                        'position': position
                    }
                    
                    breadcrumb_items.append(breadcrumb_item)
                    position += 1
                    
                except Exception as e:
                    print(f"⚠️ Error procesando elemento del breadcrumb de Megatiendas: {e}")
                    continue
            
            print(f"✅ Breadcrumb de Megatiendas extraído: {len(breadcrumb_items)} elementos")
            for item in breadcrumb_items:
                print(f"   {item['position']}. {item['name']} -> {item['url']}")
            
            return breadcrumb_items
            
        except Exception as e:
            print(f"❌ Error extrayendo breadcrumb de Megatiendas: {e}")
            return breadcrumb_items
    
    async def _extract_price(self, element) -> PriceInfo:
        """Extrae información de precios del producto."""
        try: 
            original_price = await self._get_original_price(element)
            current_price = await self._get_current_price(element)
            discount = await self._get_discount_text(element)
                
            return PriceInfo(original_price, current_price, discount)
        
        except Exception as e:
            print(f"⚠️ Error extrayendo precios de Megatiendas: {e}")
            return PriceInfo.empty()
    
    async def _get_current_price(self, element) -> Optional[float]:
        """Extrae el precio actual del producto"""
        try:
            # Selectores para precio actual en Megatiendas
            price_selectors = [
                '.price-current',
                '.current-price',
                '.price-selling',
                '.vtex-product-price__selling-price',
                '.product-price',
                '[data-testid="current-price"]',
                '.shelf-item__price',
                '.price-value',
                '.selling-price-value'
            ]
            
            for selector in price_selectors:
                try:
                    price_element = await element.query_selector(selector)
                    if price_element:
                        price_text = await price_element.inner_text()
                        price = clean_price(price_text)
                        if price:
                            return price
                except:
                    continue
                    
            return None
            
        except Exception as e:
            print(f"⚠️ Error extrayendo precio actual de Megatiendas: {e}")
            return None
    
    async def _get_original_price(self, element) -> Optional[float]:
        """Extrae el precio original (tachado) del producto"""
        try:
            # Selectores para precio original en Megatiendas
            original_price_selectors = [
                '.price-original',
                '.original-price',
                '.price-list',
                '.vtex-product-price__list-price',
                '.list-price',
                '[data-testid="list-price"]',
                '.old-price',
                '.price-was',
                '.list-price-value'
            ]
            
            for selector in original_price_selectors:
                try:
                    price_element = await element.query_selector(selector)
                    if price_element:
                        price_text = await price_element.inner_text()
                        price = clean_price(price_text)
                        if price:
                            return price
                except:
                    continue
                    
            return None
            
        except Exception as e:
            print(f"⚠️ Error extrayendo precio original de Megatiendas: {e}")
            return None
    
    async def _get_discount_text(self, element) -> str:
        """Extrae el texto de descuento del producto"""
        try:
            # Selectores para descuento en Megatiendas
            discount_selectors = [
                '.discount-badge',
                '.discount-percent',
                '.vtex-product-price__savings',
                '.price-discount',
                '[data-testid="discount"]',
                '.discount-label',
                '.percentage-discount'
            ]
            
            for selector in discount_selectors:
                try:
                    discount_element = await element.query_selector(selector)
                    if discount_element:
                        discount_text = await discount_element.inner_text()
                        return clean_text(discount_text)
                except:
                    continue
                    
            return ""
            
        except Exception as e:
            print(f"⚠️ Error extrayendo descuento de Megatiendas: {e}")
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
            print("🔍 Extrayendo filtros aplicados de Megatiendas...")
            
            # Selectores posibles para filtros aplicados
            filter_selectors = [
                '.applied-filters .filter-tag',
                '[data-testid="applied-filter"]',
                '.active-filters .filter-item',
                '.selected-filters .tag',
                '.vtex-search-result__selected-filters',
                '.filter-applied'
            ]
            
            for selector in filter_selectors:
                try:
                    filter_elements = await page.query_selector_all(selector)
                    if filter_elements:
                        for element in filter_elements:
                            filter_text = await element.inner_text()
                            filter_text = clean_text(filter_text)
                            if filter_text and filter_text not in filters:
                                filters.append(filter_text)
                        break
                except:
                    continue
            
            print(f"✅ Se extrajeron {len(filters)} filtros aplicados de Megatiendas")
            return filters
            
        except Exception as e:
            print(f"❌ Error extrayendo filtros aplicados de Megatiendas: {e}")
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
                '.search-results-count',
                '.vtex-search-result__total-products',
                '.products-found'
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
            print(f"❌ Error obteniendo total de resultados de Megatiendas: {e}")
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
                '.next-page:not(.disabled)',
                '.vtex-pagination__next:not(.disabled)',
                '.pagination .next:not(.disabled)'
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
            print(f"❌ Error verificando página siguiente de Megatiendas: {e}")
            return False