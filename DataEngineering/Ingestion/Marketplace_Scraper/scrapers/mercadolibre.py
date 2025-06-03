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
    
    def build_search_url(self, query: str, **kwargs) -> str:
        """Construye URL de búsqueda para MercadoLibre"""
        encoded_query = quote_plus(query)
        page = kwargs.get('page', 1)
        
        url = f"{self.base_url}/{encoded_query}"
        
        if page > 1:
            offset = (page - 1) * 48  # MercadoLibre muestra ~48 productos por página
            url += f"_Desde_{offset}"
        
        return url
    
    async def post_navigate_validation(self) -> bool:
        """
        Maneja validaciones específicas de MercadoLibre después de navegar
        Basado en el script funcional original
        """
        try:
            print("🔍 Ejecutando validaciones post-navegación para MercadoLibre...")
            
            # 1. Manejar popup de ubicación
            await self._handle_location_popup()
            
            # 2. Aplicar filtro de envío destacado
            await self._apply_shipping_filter()
            
            # 3. Verificar que los elementos principales estén cargados
            if not await self._verify_page_loaded():
                print("❌ La página no cargó correctamente")
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
        Adaptado de apply_shipping_filter() del script original
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
        Basado en la función scrape_products() del script original
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
    
    def handle_location_popup(page: Page) -> None:
        """Maneja el popup de ubicación si aparece"""
        try:
            page.wait_for_selector('text="Agregar ubicación"', timeout=5000)
            page.click('text="Más tarde"')
        except Exception:
            # Si no aparece el popup, continúa
            pass
        
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
            
            # Crear objeto Product
            product = Product(
                title=title,
                price=price_text,
                url=link,
                marketplace=self.marketplace_name,
                currency="COP" if self.country == "co" else "USD",
                brand=brand_text,
                image_url = image_url
            )
            
            return product
            
        except Exception as e:
            print(f"⚠️ Error extrayendo información de producto: {e}")
            return None
        
    async def extract_product_info2(self, element) -> Optional[Product]:
        """Extrae información de un producto de MercadoLibre"""
        try:
            # Título
            title_selectors = [
                '.ui-search-item__title',
                '.ui-search-result__content-wrapper h2',
                '[data-testid="item-title"]'
            ]
            
            title = ""
            for selector in title_selectors:
                title_elem = await element.query_selector(selector)
                if title_elem:
                    title = await title_elem.inner_text()
                    break
            
            if not title:
                return None
            
            title = clean_text(title)
            
            # Precio
            price = None
            price_selectors = [
                '.ui-search-price__part',
                '.price-tag-fraction',
                '[data-testid="price"]'
            ]
            
            for selector in price_selectors:
                price_elem = await element.query_selector(selector)
                if price_elem:
                    price_text = await price_elem.inner_text()
                    price = clean_price(price_text)
                    if price:
                        break
            
            # URL del producto
            url = ""
            link_selectors = [
                'a.ui-search-link',
                'a[href*="/MLC"]',
                'a[href*="/MLM"]'
            ]
            
            for selector in link_selectors:
                link_elem = await element.query_selector(selector)
                if link_elem:
                    href = await link_elem.get_attribute('href')
                    if href:
                        url = make_absolute_url(f"https://mercadolibre.com.{self.country}", href)
                        break
            
            # Imagen
            image_url = ""
            img_elem = await element.query_selector('img')
            if img_elem:
                src = await img_elem.get_attribute('src')
                if src:
                    image_url = src
            
            # Rating (si existe)
            rating = None
            rating_elem = await element.query_selector('.ui-search-reviews__rating')
            if rating_elem:
                rating_text = await rating_elem.inner_text()
                rating = extract_rating(rating_text)
            
            # Envío gratis
            free_shipping = False
            shipping_elem = await element.query_selector('.ui-search-item__shipping')
            if shipping_elem:
                shipping_text = await shipping_elem.inner_text()
                free_shipping = 'gratis' in shipping_text.lower()
            
            # Ubicación del vendedor
            location = ""
            location_elem = await element.query_selector('.ui-search-item__location')
            if location_elem:
                location = clean_text(await location_elem.inner_text())
            
            return Product(
                title=title,
                price=price,
                currency="COP" if self.country == "co" else "USD",
                url=url,
                image_url=image_url,
                rating=rating,
                seller=location,
                availability="Disponible" if price else "Sin stock",
                marketplace=self.marketplace_name
            )
            
        except Exception as e:
            print(f"Error extrayendo producto de MercadoLibre: {e}")
            return None