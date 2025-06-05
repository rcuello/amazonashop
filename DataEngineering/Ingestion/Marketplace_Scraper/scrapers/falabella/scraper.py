from typing import List, Optional
from urllib.parse import quote_plus
from models.product import Product
from scrapers.base_scraper import BaseScraper

from scrapers.falabella.navigation_preparator import NavigationPreparator
from scrapers.falabella.product_extractor import ProductExtractor


class FalabellaScraper(BaseScraper):
    """Scraper para Falabella Colombia"""
    
    def __init__(self, country: str = "co"):
        super().__init__()
        self.marketplace_name = "Falabella"
        self.country = country
        self.base_url = f"https://www.falabella.com.{country}/falabella-{country}"
        self.search_url = f"https://www.falabella.com.{country}/falabella-{country}/search"
        self.products_per_page = 40  # Falabella típicamente muestra 40 productos por página
        
        # Componentes especializados        
        self.page_validator = None
        self.product_extractor = None
        
        # Estado
        self.category_info = {}
    
    def _initialize_components(self):
        """Inicializa los componentes que requieren la página del browser"""
        if not self.browser_manager or not self.browser_manager.page:
            return
            
        page = self.browser_manager.page
        
        self.page_validator     = NavigationPreparator(page)
        self.product_extractor  = ProductExtractor(page, self.marketplace_name, self.country)
    
    async def build_search_url(self, query: str, **kwargs) -> str:
        """Construye URL de búsqueda para MercadoLibre"""
        encoded_query = quote_plus(query)
        page = kwargs.get('page', 1)
        
        # Estructura típica de Falabella: /search?Ntt=query&page=1
        if page == 1:
            return f"{self.search_url}?Ntt={encoded_query}"
        else:
            return f"{self.search_url}?Ntt={encoded_query}&page={page}"
    
    
    async def post_navigate_validation(self) -> bool:
        """Maneja validaciones específicas después de navegar"""
        
        self._initialize_components()
        
        if not self.page_validator:
            return False
        
        # Validaciones post-navegación: remueve popups, espera el contenido dinámico, etc.
        success = await self.page_validator.validate_page_after_navigation()
        
        return success
    
    
    async def get_product_elements(self):
        """Obtiene los elementos de productos"""
        if not self.product_extractor:
            return []
        
        return await self.product_extractor.get_product_elements()
    
    async def extract_product_info(self, element) -> Optional[Product]:
        """Extrae información del producto desde el elemento HTML"""
        if not self.product_extractor:
            return None
        
        return await self.product_extractor.extract_product_info(element, self.category_info)
    