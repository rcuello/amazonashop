from urllib.parse import quote_plus
from typing import List

class URLBuilder:
    """Construye URLs para navegación y paginación"""
    
    def __init__(self, base_url: str, products_per_page: int = 50):
        self.base_url = base_url
        self.products_per_page = products_per_page
    
    def build_search_url(self, query: str, page: int = 1) -> str:
        """Construye URL de búsqueda básica"""
        encoded_query = quote_plus(query)
        
        if page == 1:
            return f"{self.base_url}/{encoded_query}"
        
        return self._build_pagination_url(encoded_query, page)
    
    def _build_pagination_url(self, encoded_query: str, page: int) -> str:
        """Construye URL de paginación básica"""
        offset = ((page - 1) * self.products_per_page) + 1
        return f"{self.base_url}/{encoded_query}_Desde_{offset}_NoIndex_True"
    
    def build_advanced_pagination_url(self, encoded_query: str, page: int, 
                                    category_url: str = None, filters: List[str] = None) -> str:
        """Construye URL de paginación avanzada con categorías y filtros"""
        try:
            offset = ((page - 1) * self.products_per_page) + 1
            
            # Usar URL de categoría si está disponible
            base_url = category_url.rstrip('/') if category_url else self.base_url
            
            # Agregar filtros si existen
            filter_path = f"/{filters[0]}" if filters else ""
            
            pagination_url = f"{base_url}{filter_path}/{encoded_query}_Desde_{offset}_NoIndex_True"
            
            print(f"📄 Página {page} | Offset: {offset}")
            print(f"🔗 URL: {pagination_url}")
            
            return pagination_url
            
        except Exception as e:
            print(f"❌ Error construyendo URL de paginación: {e}")
            return self._build_pagination_url(encoded_query, page)