import asyncio
import argparse
from typing import List
from scrapers.mercadolibre.scraper import MercadoLibreScraper
from scrapers.falabella.scraper import FalabellaScraper
from utils.exporters import DataExporter
from models.product import Product

class MarketplaceScraper:
    """Orquestador principal del sistema de scraping"""
    
    def __init__(self):
        self.scrapers = {
            'mercadolibre': MercadoLibreScraper,
            'falabella':FalabellaScraper
            #'amazon': AmazonScraper,
            #'ebay': EbayScraper,
            #'aliexpress': AliExpressScraper
        }
        self.exporter = DataExporter()
   
    async def scrape_marketplace(self, marketplace: str, query: str, max_pages: int = 1, **kwargs) -> List[Product]:
        """Scrapea un marketplace específico"""
        if marketplace not in self.scrapers:
            print(f"Marketplace '{marketplace}' no soportado")
            return []
        
        print(f"\n=== Scrapeando {marketplace.upper()} ===")
        print(f"Query: {query}")
        print(f"Páginas: {max_pages}")
        
        # Crear scraper específico
        if marketplace == 'mercadolibre':
            country = kwargs.get('country', 'co')
            scraper = self.scrapers[marketplace](country=country)
        elif marketplace == 'amazon':
            domain = kwargs.get('domain', 'com')
            scraper = self.scrapers[marketplace](domain=domain)
        else:
            scraper = self.scrapers[marketplace]()
        
        # Realizar scraping
        products = await scraper.search_products(query, max_pages)
        
        print(f"Productos encontrados en {marketplace}: {len(products)}")
        return products
    
    
    async def scrape_multiple_marketplaces(self, marketplaces: List[str], query: str, max_pages: int = 1, **kwargs) -> List[Product]:
        """Scrapea múltiples marketplaces"""
        all_products = []
        
        for marketplace in marketplaces:
            try:
                products = await self.scrape_marketplace(marketplace, query, max_pages, **kwargs)
                all_products.extend(products)
            except Exception as e:
                print(f"Error scrapeando {marketplace}: {e}")
        
        return all_products
    
    def export_results(self, products: List[Product], format: str = 'csv', by_marketplace: bool = False):
        """Exporta los resultados"""
        if not products:
            print("No hay productos para exportar")
            return
        
        print(f"\n=== Exportando {len(products)} productos ===")
        
        if by_marketplace:
            self.exporter.export_by_marketplace(products, format)
        else:
            if format == 'csv':
                self.exporter.export_to_csv(products)
            elif format == 'json':
                self.exporter.export_to_json(products)
            else:
                print(f"Formato '{format}' no soportado")
    
    def print_summary(self, products: List[Product]):
        """Imprime resumen de resultados"""
        if not products:
            return
        
        print(f"\n=== RESUMEN ===")
        print(f"Total productos: {len(products)}")
        
        # Resumen por marketplace
        marketplaces = {}
        for product in products:
            mp = product.marketplace
            if mp not in marketplaces:
                marketplaces[mp] = {'count': 0, 'with_price': 0, 'avg_price': 0}
            
            marketplaces[mp]['count'] += 1
            if product.price:
                marketplaces[mp]['with_price'] += 1
        
        for mp, stats in marketplaces.items():
            print(f"- {mp}: {stats['count']} productos ({stats['with_price']} con precio)")
        
        # Productos con mejores precios
        products_with_price = [p for p in products if p.price]
        if products_with_price:
            products_with_price.sort(key=lambda x: x.price)
            print(f"\nMejores precios:")
            for i, product in enumerate(products_with_price[:5]):
                print(f"{i+1}. {product.title[:50]}... - ${product.price} ({product.marketplace})")

async def main():
    """Función principal"""
    parser = argparse.ArgumentParser(description='Marketplace Scraper')
    parser.add_argument('query', help='Término de búsqueda')
    parser.add_argument('-m', '--marketplaces', nargs='+', 
                       choices=['mercadolibre', 'amazon', 'falabella', 'aliexpress'],
                       default=['mercadolibre'], help='Marketplaces a scrapear')
    parser.add_argument('-p', '--pages', type=int, default=1, help='Número de páginas por marketplace')
    parser.add_argument('-f', '--format', choices=['csv', 'json'], default='csv', help='Formato de exportación')
    parser.add_argument('--by-marketplace', action='store_true', help='Exportar separado por marketplace')
    parser.add_argument('--country', default='co', help='País para MercadoLibre (co, mx, ar, etc.)')
    parser.add_argument('--domain', default='com', help='Dominio para Amazon (com, es, mx, etc.)')
    
    args = parser.parse_args()
    
    # Crear scraper principal
    scraper = MarketplaceScraper()
    try:
        print(f"🚀 Iniciando scraping para: '{args.query}'")
        # Scrapear marketplaces
        products = await scraper.scrape_multiple_marketplaces(
            marketplaces=args.marketplaces,
            query=args.query,
            max_pages=args.pages,
            country=args.country,
            domain=args.domain
        )
        
        # Mostrar resumen
        scraper.print_summary(products)
        
        # Exportar resultados
        if products:
            scraper.export_results(products, args.format, args.by_marketplace)
        
        print("\n¡Scraping completado!")
        
    except Exception as e:
        print(f"\n❌ Error inesperado: {e}")
     
if __name__ == "__main__":
    asyncio.run(main())    

# python main.py "televisor" -m falabella --country co
# python main.py "airpods" -m falabella --country co
# python main.py "smarthphone" -m mercadolibre --country co --page 4    
# python main.py "iphone 15" -m mercadolibre --country co
# python main.py "sonos move 2" -m mercadolibre --country co  
# python main.py "televisor samsung" -m mercadolibre --country co
# python main.py "televisor samsung" -m mercadolibre --country co -p 2 
# python main.py "iphone" -m mercadolibre --country co --page 2 
# python main.py "iphone" -m mercadolibre --country co --page 4
# python main.py "iphone" -m mercadolibre --country co
    # https://listado.mercadolibre.com.co/celulares-telefonos/celulares-smartphones/apple/iphone_Desde_51_NoIndex_True
    
# python main.py "airpods" -m mercadolibre --country co --page 2 
# python main.py "airpods" -m mercadolibre --country co 
    # https://listado.mercadolibre.com.co/airpods
    # https://listado.mercadolibre.com.co/celulares-telefonos/accesorios-celulares/airpods_Desde_51_NoIndex_True
   
"""
marketplace_scraper/
├── scrapers/
│   ├── base_scraper.py      # Clase base común
│   ├── mercadolibre.py      # Solo MercadoLibre
│   ├── amazon.py            # Solo Amazon  
│   ├── ebay.py              # Solo eBay
│   └── aliexpress.py        # Solo AliExpress
├── models/
│   └── product.py           # Clase Product simple
├── utils/
│   ├── browser.py           # Wrapper Playwright
│   ├── exporters.py         # CSV, JSON export
│   └── helpers.py           # Funciones comunes
├── config/
│   └── settings.py          # Configuraciones
├── tests/
│   ├── helpers/
│   │   ├── test_is_valid_url.py
│   │   ├── test_clean_price.py
│   │   ├── test_extract_number.py
│   │   ├── test_extract_integer.py
│   │   └── test_clean_text.py
│   ├── html/
│   │   ├── mercadolibre_price_container.html
│   │   ├── mercadolibre_price_con_descuento.html
│   │   ├── mercadolibre_price_sin_descuento.html
│   │
│
└── main.py                  # Orquestador simple
"""
