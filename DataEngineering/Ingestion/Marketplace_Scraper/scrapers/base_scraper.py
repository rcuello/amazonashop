from abc import ABC, abstractmethod
from typing import List, Optional, Dict, Any
import asyncio
from models.product import Product
from utils.browser import BrowserManager
from utils.helpers import random_delay
from config.settings import Settings

class BaseScraper(ABC):
    """Clase base para todos los scrapers"""
    
    def __init__(self):
        self.browser_manager = BrowserManager()
        self.marketplace_name = ""
        self.base_url = ""
        self.products: List[Product] = []
    
    @abstractmethod
    def build_search_url(self, query: str, **kwargs) -> str:
        """Construye la URL de búsqueda"""
        pass
    
    @abstractmethod
    async def extract_product_info(self, element) -> Optional[Product]:
        """Extrae información de un producto desde un elemento HTML"""
        pass
    
    @abstractmethod
    async def get_product_elements(self):
        """Obtiene los elementos de productos de la página"""
        pass
    
    @abstractmethod
    async def post_navigate_validation(self) -> bool:
        """
        Ejecuta validaciones y manejo de elementos después de navegar a una página.
        
        Este método debe ser implementado por cada scraper específico para manejar:
        - Popups de ubicación, cookies, etc.
        - Modales de autenticación
        - Captchas
        - Elementos que bloqueen el contenido
        - Validaciones específicas del marketplace
        
        Returns:
            bool: True si la página está lista para scraping, False si hay errores críticos
        """
        pass
    
    async def search_products(self, query: str, max_pages: int = 1, **kwargs) -> List[Product]:
        """Busca productos por query"""
        self.products = []
        
        try:
            # Iniciar navegador
            await self.browser_manager.start()
            
            for page_num in range(1, max_pages + 1):
                print(f"Scrapeando página {page_num} de {self.marketplace_name}")
                
                # Construir URL
                search_url = self.build_search_url(query, page=page_num, **kwargs)
                print(f"URL: {search_url}")
                
                # Navegar a la página
                success = await self.browser_manager.goto(search_url)
                if not success:
                    print(f"Error cargando página {page_num}")
                    continue
                
                # Ejecutar validaciones post-navegación
                print(f"Ejecutando validaciones post-navegación para {self.marketplace_name}")
                validation_success = await self.post_navigate_validation()
                
                if not validation_success:
                    print(f"Falló la validación post-navegación en página {page_num}")
                    # Puedes decidir si continuar o saltar esta página
                    continue                
                
                
                # Esperar a que cargue el contenido
                await asyncio.sleep(Settings.REQUEST_DELAYS['page_delay'])
                
                # Obtener productos de la página
                page_products = await self.scrape_current_page()
                
                if not page_products:
                    print(f"No se encontraron productos en página {page_num}")
                    break
                
                self.products.extend(page_products)
                print(f"Productos encontrados en página {page_num}: {len(page_products)}")
                
                # Delay entre páginas
                if page_num < max_pages:
                    random_delay(
                        Settings.REQUEST_DELAYS['min_delay'],
                        Settings.REQUEST_DELAYS['max_delay']
                    )
            
        except Exception as e:
            print(f"Error en búsqueda: {e}")
        
        finally:
            await self.browser_manager.close()
        
        print(f"Total productos encontrados: {len(self.products)}")
        return self.products
    
    async def scrape_current_page(self) -> List[Product]:
        """Scrapea la página actual"""
        products = []
        
        try:
            # Obtener elementos de productos
            product_elements = await self.get_product_elements()
            
            if not product_elements:
                return products
            
            # Extraer información de cada producto
            for element in product_elements:
                try:
                    product = await self.extract_product_info(element)
                    if product:
                        product.marketplace = self.marketplace_name
                        products.append(product)
                except Exception as e:
                    print(f"Error extrayendo producto: {e}")
                    continue
        
        except Exception as e:
            print(f"Error scrapeando página: {e}")
        
        return products
    
    async def scroll_and_load(self):
        """Hacer scroll para cargar más productos (útil para sitios con scroll infinito)"""
        await self.browser_manager.scroll_to_bottom()