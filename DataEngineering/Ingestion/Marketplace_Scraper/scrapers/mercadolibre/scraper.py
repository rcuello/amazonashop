from typing import List, Optional
from urllib.parse import quote_plus
from models.product import Product
from scrapers.base_scraper import BaseScraper
from scrapers.mercadolibre.category_extractor import CategoryExtractor
from scrapers.mercadolibre.filter_extractor import FilterExtractor
from scrapers.mercadolibre.url_builder import URLBuilder
from scrapers.mercadolibre.page_validator import PageValidator
from scrapers.mercadolibre.product_extractor import ProductExtractor
# https://mercadolibre.com/robots.txt

class MercadoLibreScraper(BaseScraper):
    """Scraper para MercadoLibre"""
    
    def __init__(self, country: str = "co", mobile: bool = False, device: Optional[str] = None):
        super().__init__(mobile=mobile, device=device)
        self.marketplace_name = "MercadoLibre"
        self.country = country
        self.base_url = f"https://listado.mercadolibre.com.{country}"
        self.products_per_page = 50
        
        # Componentes especializados
        self.url_builder = URLBuilder(self.base_url, self.products_per_page)
        self.category_extractor = None
        self.filter_extractor = None
        self.page_validator = None
        self.product_extractor = None
        
        # Estado
        self.category_info = {}
    
    def _initialize_components(self):
        """Inicializa los componentes que requieren la página del browser"""
        if not self.browser_manager or not self.browser_manager.page:
            return
            
        page = self.browser_manager.page
        self.category_extractor = CategoryExtractor(page)
        self.filter_extractor = FilterExtractor(page)
        self.page_validator = PageValidator(page)
        self.product_extractor = ProductExtractor(page, self.marketplace_name, self.country)
    
    async def build_search_url(self, query: str, **kwargs) -> str:
        """Construye URL de búsqueda para MercadoLibre"""
        page = kwargs.get('page', 1)
        
        if page == 1:
            return self.url_builder.build_search_url(query, page)
        
        return await self._build_advanced_pagination_url(query, page)
    
    async def _build_advanced_pagination_url(self, query: str, page: int) -> str:
        """Construye URL de paginación avanzada"""
        try:
            if not self.category_info:
                await self._extract_category_info()
            
            encoded_query = quote_plus(query)
            filters = await self.filter_extractor.get_active_filters() if self.filter_extractor else []
            category_url = self.category_info.get('category_url')
            
            return self.url_builder.build_advanced_pagination_url(
                encoded_query, page, category_url, filters
            )
            
        except Exception as e:
            print(f"❌ Error construyendo URL de paginación avanzada: {e}")
            return self.url_builder.build_search_url(query, page)
    
    async def post_navigate_validation(self) -> bool:
        """Maneja validaciones específicas después de navegar"""
        self._initialize_components()
        
        if not self.page_validator:
            return False
        
        success = await self.page_validator.validate_page_after_navigation()
        
        if success:
            await self._extract_category_info()
        
        return success
    
    async def _extract_category_info(self):
        """Extrae información de categorías"""
        if not self.category_extractor:
            return
        
        self.category_info = await self.category_extractor.extract_category_info()
    
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
    
    async def extract_breadcrumb(self) -> List[dict]:
        """Extrae el breadcrumb de MercadoLibre"""
        if not self.category_extractor:
            return []
        
        return await self.category_extractor.extract_breadcrumb()
    
    async def extract_applied_filters(self) -> List[str]:
        """Extrae los filtros aplicados"""
        if not self.filter_extractor:
            return []
        
        return await self.filter_extractor.extract_applied_filters()
    
    async def pagination_url(self) -> str:
        """Devuelve la URL de la página de paginación"""
        try:
            breadcrumb = await self.extract_breadcrumb()
            filters = await self.extract_applied_filters()
            
            first_filter = next((x for x in filters), "")
            second_breadcrumb = next((x for x in breadcrumb if x['position'] == 2), {})
            
            return f"{second_breadcrumb.get('url', '')}{first_filter}"
            
        except Exception as e:
            print(f"❌ Error construyendo URL de paginación: {e}")
            return ""