from typing import List, Optional
from urllib.parse import quote_plus
from models.product import Product
from scrapers.base_scraper import BaseScraper
from utils.helpers import clean_price, clean_text, extract_rating, extract_number, make_absolute_url

class AmazonScraper(BaseScraper):
    """Scraper para Amazon"""
    
    def __init__(self, domain: str = "com"):
        super().__init__()
        self.marketplace_name = "Amazon"
        self.domain = domain
        self.base_url = f"https://www.amazon.{domain}"
    
    def build_search_url(self, query: str, **kwargs) -> str:
        """Construye URL de búsqueda para Amazon"""
        encoded_query = quote_plus(query)
        page = kwargs.get('page', 1)
        
        url = f"{self.base_url}/s?k={encoded_query}"
        
        if page > 1:
            url += f"&page={page}"
        
        return url
    
    async def get_product_elements(self):
        """Obtiene elementos de productos en Amazon"""
        # Esperar a que carguen los resultados
        await self.browser_manager.wait_for_selector('[data-component-type="s-search-result"]', timeout=15000)
        
        # Selectores para productos
        selectors = [
            '[data-component-type="s-search-result"]',
            '.s-result-item[data-component-type="s-search-result"]'
        ]
        
        for selector in selectors:
            elements = await self.browser_manager.get_elements(selector)
            if elements:
                return elements
        
        return []
    
    async def extract_product_info(self, element) -> Optional[Product]:
        """Extrae información de un producto de Amazon"""
        try:
            # Título
            title = ""
            title_selectors = [
                'h2 a span',
                '[data-cy="title-recipe-title"]',
                '.a-size-mini span'
            ]
            
            for selector in title_selectors:
                title_elem = await element.query_selector(selector)
                if title_elem:
                    title = await title_elem.inner_text()
                    if title.strip():
                        break
            
            if not title:
                return None
            
            title = clean_text(title)
            
            # Precio
            price = None
            original_price = None
            
            # Precio actual
            price_selectors = [
                '.a-price-whole',
                '.a-price .a-offscreen',
                '[data-testid="price-to-pay"]'
            ]
            
            for selector in price_selectors:
                price_elem = await element.query_selector(selector)
                if price_elem:
                    price_text = await price_elem.inner_text()
                    price = clean_price(price_text)
                    if price:
                        break
            
            # Precio original (tachado)
            original_price_elem = await element.query_selector('.a-price.a-text-price .a-offscreen')
            if original_price_elem:
                original_price_text = await original_price_elem.inner_text()
                original_price = clean_price(original_price_text)
            
            # URL del producto
            url = ""
            link_elem = await element.query_selector('h2 a, [data-cy="title-recipe-title"] a')
            if link_elem:
                href = await link_elem.get_attribute('href')
                if href:
                    url = make_absolute_url(self.base_url, href)
            
            # Imagen
            image_url = ""
            img_elem = await element.query_selector('.s-image')
            if img_elem:
                src = await img_elem.get_attribute('src')
                if src:
                    image_url = src
            
            # Rating
            rating = None
            reviews_count = None
            
            rating_elem = await element.query_selector('.a-icon-alt')
            if rating_elem:
                rating_text = await rating_elem.get_attribute('alt')
                if rating_text:
                    rating = extract_rating(rating_text)
            
            # Número de reviews
            reviews_elem = await element.query_selector('.a-size-base')
            if reviews_elem:
                reviews_text = await reviews_elem.inner_text()
                reviews_count = extract_number(reviews_text)
            
            # Disponibilidad
            availability = "Disponible"
            stock_elem = await element.query_selector('.a-color-price')
            if stock_elem:
                stock_text = await stock_elem.inner_text()
                if 'out of stock' in stock_text.lower() or 'no disponible' in stock_text.lower():
                    availability = "Sin stock"
            
            return Product(
                title=title,
                price=price,
                original_price=original_price,
                currency="USD",
                url=url,
                image_url=image_url,
                rating=rating,
                reviews_count=reviews_count,
                availability=availability,
                marketplace=self.marketplace_name
            )
            
        except Exception as e:
            print(f"Error extrayendo producto de Amazon: {e}")
            return None