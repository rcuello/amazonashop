from typing import List, Dict, Optional
from utils.helpers import clean_text

class CategoryExtractor:
    """Extrae y maneja información de categorías y breadcrumbs"""
    
    def __init__(self, page):
        self.page = page
        self.category_info = {
            'parent_category': '',
            'parent_category_url': '',
            'category': '',
            'category_url': '',
            'category2': '',
            'category2_url': '',
            'breadcrumb': []
        }
    
    async def extract_breadcrumb(self) -> List[Dict]:
        """Extrae el breadcrumb de MercadoLibre"""
        breadcrumb_items = []
        
        try:
            print("🍞 Extrayendo breadcrumb...")
            
            breadcrumb_container = await self.page.query_selector('ol.andes-breadcrumb')
            if not breadcrumb_container:
                print("⚠️ No se encontró el breadcrumb")
                return breadcrumb_items
                        
            breadcrumb_elements = await breadcrumb_container.query_selector_all('li.andes-breadcrumb__item')
            
            for element in breadcrumb_elements:
                try:
                    link_element = await element.query_selector('a.andes-breadcrumb__link')
                    if not link_element:
                        continue
                    
                    name_element = await link_element.query_selector('span[itemprop="name"]')
                    if not name_element:
                        continue
                    
                    name = clean_text(await name_element.inner_text())
                    url = await link_element.get_attribute('href')
                    
                    position_element = await element.query_selector('meta[itemprop="position"]')
                    position = 0
                    if position_element:
                        position_content = await position_element.get_attribute('content')
                        try:
                            position = int(position_content) if position_content else 0
                        except ValueError:
                            position = 0
                    
                    breadcrumb_item = {
                        'name': name,
                        'url': url,
                        'position': position
                    }
                    
                    breadcrumb_items.append(breadcrumb_item)
                    
                except Exception as e:
                    print(f"⚠️ Error procesando elemento del breadcrumb: {e}")
                    continue
            
            print(f"✅ Breadcrumb extraído: {len(breadcrumb_items)} elementos")
            return breadcrumb_items
            
        except Exception as e:
            print(f"❌ Error extrayendo breadcrumb: {e}")
            return breadcrumb_items
    
    async def extract_category_info(self) -> Dict:
        """Extrae y almacena información completa de categorías"""
        try:
            print("📂 Extrayendo información de categorías...")
            
            breadcrumb = await self.extract_breadcrumb()
            self.category_info['breadcrumb'] = breadcrumb
            
            if not breadcrumb:
                print("⚠️ No se pudo extraer breadcrumb")
                return self.category_info
            
            # Procesar categorías por posición
            self._process_categories_by_position(breadcrumb)
            
            return self.category_info
                         
        except Exception as e:
            print(f"❌ Error extrayendo información de categorías: {e}")
            return self.category_info
    
    def _process_categories_by_position(self, breadcrumb: List[Dict]):
        """Procesa las categorías según su posición en el breadcrumb"""
        # Categoría padre (posición 1)
        parent_category = next((item for item in breadcrumb if item['position'] == 1), None)
        if parent_category:
            self.category_info['parent_category'] = parent_category['name']
            self.category_info['parent_category_url'] = parent_category['url']
            print(f"📁 Categoría padre: {parent_category['name']}")
        
        # Subcategoría (posición 2)
        subcategory = next((item for item in breadcrumb if item['position'] == 2), None)
        if subcategory:
            self.category_info['category'] = subcategory['name']
            self.category_info['category_url'] = subcategory['url']
            print(f"📂 Subcategoría: {subcategory['name']}")
        
        # Subcategoría 2 (posición 3)
        subcategory2 = next((item for item in breadcrumb if item['position'] == 3), None)
        if subcategory2:
            self.category_info['category2'] = subcategory2['name']
            self.category_info['category2_url'] = subcategory2['url']
            print(f"📂 Subcategoría 2: {subcategory2['name']}")
        
        # Si no hay subcategoría, usar la categoría padre como categoría principal
        if not subcategory and parent_category:
            self.category_info['category'] = parent_category['name']
            self.category_info['category_url'] = parent_category['url']
            print("📝 Usando categoría padre como categoría principal")
