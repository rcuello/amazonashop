from playwright.sync_api import sync_playwright, Page
import polars as pl
from urllib.parse import quote_plus
from typing import List, Tuple, Optional


def create_mercadolibre_url(query: str) -> str:
    """Crea una URL válida para MercadoLibre Colombia"""
    encoded_query = quote_plus(query)
    return f"https://listado.mercadolibre.com.co/{encoded_query}"


def handle_location_popup(page: Page) -> None:
    """Maneja el popup de ubicación si aparece"""
    try:
        page.wait_for_selector('text="Agregar ubicación"', timeout=5000)
        page.click('text="Más tarde"')
    except Exception:
        # Si no aparece el popup, continúa
        pass


def apply_shipping_filter(page: Page) -> None:
    """Aplica filtro de envío destacado"""
    try:
        page.wait_for_selector('#shipping_highlighted_fulfillment', timeout=10000)
        page.click('#shipping_highlighted_fulfillment')
    except Exception:
        print("No se pudo aplicar el filtro de envío")


def extract_product_data(item) -> Optional[Tuple[str, str, str]]:
    """Extrae datos de un producto individual"""
    try:
        title_element = item.query_selector('h3')
        price_element = item.query_selector('span.andes-money-amount.andes-money-amount--cents-superscript')
        link_element = item.query_selector('a.poly-component__title')
        
        if not all([title_element, price_element, link_element]):
            return None
            
        title = title_element.inner_text().strip()
        price = price_element.inner_text().replace("\n", "").strip()
        link = link_element.get_attribute('href')
        
        return (title, price, link)
    except Exception:
        return None


def scrape_products(page: Page) -> List[Tuple[str, str, str]]:
    """Extrae todos los productos de la página"""
    page.wait_for_selector('li.ui-search-layout__item', timeout=15000)
    items = page.query_selector_all('li.ui-search-layout__item')
    
    products = []
    for item in items:
        product_data = extract_product_data(item)
        if product_data:
            products.append(product_data)
    
    return products


def save_products_to_csv(products: List[Tuple[str, str, str]], filename: str = "mercado_libre.csv") -> None:
    """Guarda los productos en un archivo CSV"""
    if not products:
        print("No se encontraron productos para guardar")
        return
        
    df = pl.DataFrame(products, schema=["title", "price", "link"], orient="row")
    df.write_csv(filename)
    print(f"Se guardaron {len(products)} productos en {filename}")


def scrape_mercadolibre(query: str, headless: bool = True) -> List[Tuple[str, str, str]]:
    """
    Realiza scraping en MercadoLibre para una consulta específica
    
    Args:
        query: Término de búsqueda
        headless: Si ejecutar el navegador en modo headless
        
    Returns:
        Lista de tuplas con (título, precio, enlace)
    """
    url = create_mercadolibre_url(query)
    print(f"Navegando a: {url}")
    
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=headless)
        try:
            page = browser.new_page()
            page.goto(url)
            
            # Manejo de popups y filtros
            handle_location_popup(page)
            apply_shipping_filter(page)
            
            # Extracción de productos
            products = scrape_products(page)
            
            # Guardar en CSV
            save_products_to_csv(products)
            
            return products
            
        finally:
            browser.close()


if __name__ == "__main__":
    query = "sonos move 2"
    products = scrape_mercadolibre(query, headless=False)
    print(f"Total de productos extraídos: {len(products)}")
    
    # https://listado.mercadolibre.com.co/sonos-move-2
    # https://listado.mercadolibre.com.co/televisor-samsung