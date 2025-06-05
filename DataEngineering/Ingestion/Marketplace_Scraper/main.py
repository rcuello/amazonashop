import asyncio
import argparse
from typing import List,Optional
from scrapers.mercadolibre.scraper import MercadoLibreScraper
from scrapers.falabella.scraper import FalabellaScraper
from scrapers.megatienda import MegaTiendaScraper
from utils.exporters import DataExporter
from models.product import Product
from config.settings import Settings

class MarketplaceScraper:
    """Orquestador principal del sistema de scraping"""
    
    def __init__(self):
        self.scrapers = {
            'mercadolibre': MercadoLibreScraper,
            'falabella':FalabellaScraper,
            'megatienda':MegaTiendaScraper
            #'amazon': AmazonScraper,
            #'ebay': EbayScraper,
            #'aliexpress': AliExpressScraper
        }
        self.exporter = DataExporter()
   
    async def scrape_marketplace(self, marketplace: str, query: str, max_pages: int = 1, mobile: bool = False, device: Optional[str] = None, **kwargs) -> List[Product]:
        """Scrapea un marketplace específico"""
        if marketplace not in self.scrapers:
            print(f"Marketplace '{marketplace}' no soportado")
            return []
        
        # Mostrar información del modo
        mode_info = self._get_mode_info(mobile, device)
        print(f"\n=== Scrapeando {marketplace.upper()} {mode_info} ===")
        print(f"Query: {query}")
        print(f"Páginas: {max_pages}")
        
        # Crear scraper específico con soporte mobile
        scraper = self._create_scraper(marketplace, mobile, device, **kwargs)
        
        # Realizar scraping
        products = await scraper.search_products(query, max_pages)
        
        print(f"Productos encontrados en {marketplace}: {len(products)}")
        
        return products
    
    def _create_scraper(self, marketplace: str, mobile: bool, device: Optional[str], **kwargs):
        """Crea el scraper específico con configuración mobile"""
        scraper_class = self.scrapers[marketplace]
        
        if marketplace == 'mercadolibre':
            country = kwargs.get('country', 'co')
            return scraper_class(country=country, mobile=mobile, device=device)
        elif marketplace == 'amazon':
            domain = kwargs.get('domain', 'com')
            return scraper_class(domain=domain, mobile=mobile, device=device)
        else:
            return scraper_class(mobile=mobile, device=device)
        
    def _get_mode_info(self, mobile: bool, device: Optional[str]) -> str:
        """Retorna información del modo de scraping"""
        if not mobile:
            return "[🖥️  Desktop]"
        elif device:
            return f"[📱 {device}]"
        else:
            return "[📱 Mobile]"
        
    async def scrape_multiple_marketplaces(self, marketplaces: List[str], query: str, max_pages: int = 1, 
                                         mobile: bool = False, device: Optional[str] = None, **kwargs) -> List[Product]:
        """Scrapea múltiples marketplaces"""
        all_products = []
        
        for marketplace in marketplaces:
            try:
                products = await self.scrape_marketplace(
                    marketplace, query, max_pages, mobile, device, **kwargs
                )
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
def show_available_devices():
    """Muestra dispositivos disponibles"""
    print("\n📱 Dispositivos disponibles para emulación:")
    for i, device in enumerate(Settings.DEVICES_NAMES, 1):
        print(f"  {i:2d}. {device}")
    print()
    
async def main():
    """Función principal"""
    parser = argparse.ArgumentParser(description='Marketplace Scraper')
    parser.add_argument('query', help='Término de búsqueda')
    parser.add_argument('-m', '--marketplaces', nargs='+', 
                       choices=['mercadolibre', 'amazon', 'falabella', 'aliexpress','megatienda'],
                       default=['mercadolibre'], help='Marketplaces a scrapear')
    parser.add_argument('-p', '--pages', type=int, default=1, help='Número de páginas por marketplace')
    parser.add_argument('-f', '--format', choices=['csv', 'json'], default='csv', help='Formato de exportación')
    parser.add_argument('--by-marketplace', action='store_true', help='Exportar separado por marketplace')
    parser.add_argument('--country', default='co', help='País para MercadoLibre (co, mx, ar, etc.)')
    parser.add_argument('--domain', default='com', help='Dominio para Amazon (com, es, mx, etc.)')
    
    # Nuevos argumentos para mobile
    parser.add_argument('--mobile', action='store_true', help='Usar modo mobile')
    parser.add_argument('--device', help='Dispositivo específico a emular (ej: "iPhone 13")')
    #parser.add_argument('--compare', action='store_true', help='Comparar desktop vs mobile')
    parser.add_argument('--show-devices', action='store_true', help='Mostrar dispositivos disponibles')
    
    args = parser.parse_args()
    
    # Mostrar dispositivos si se solicita
    if args.show_devices:
        show_available_devices()
        return
    
    # Validar dispositivo
    if args.device and args.device not in Settings.DEVICES_NAMES:
        print(f"❌ Dispositivo '{args.device}' no válido.")
        show_available_devices()
        return
    
    # Crear scraper principal
    scraper = MarketplaceScraper()
    try:
        print(f"🚀 Iniciando scraping para: '{args.query}'")
        # Scrapear marketplaces
        products = await scraper.scrape_multiple_marketplaces(
            marketplaces=args.marketplaces,
            query=args.query,
            max_pages=args.pages,
            mobile=args.mobile,
            device=args.device,
            country=args.country,
            domain=args.domain
        )
        
        # Mostrar resumen
        scraper.print_summary(products)
        
        # Exportar resultados
        if products:
            scraper.export_results(products, args.format, args.by_marketplace)
        
        print("\n✅ ¡Scraping completado!")
        
    except Exception as e:
        print(f"\n❌ Error inesperado: {e}")
     
if __name__ == "__main__":
    asyncio.run(main())    

# python main.py "galleta" -m megatienda --country co
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
