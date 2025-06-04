from typing import List

class FilterExtractor:
    """Extrae y maneja filtros de busqueda aplicados. Ejemplo : Apple"""
    
    def __init__(self, page):
        self.page = page
    
    async def extract_applied_filters(self) -> List[str]:
        """Extrae los filtros actualmente aplicados"""
        filters = []
        
        try:
            print("🔍 Extrayendo filtros aplicados...")
            
            filters_section = await self.page.query_selector('section.ui-search-applied-filters')
            if not filters_section:
                print("📝 No se encontraron filtros aplicados")
                return filters
            
            filter_tags = await filters_section.query_selector_all('.andes-tag__label')
            print(f"🏷️ Se encontraron {len(filter_tags)} etiquetas de filtros")
            
            for tag in filter_tags:
                try:
                    filter_value = await tag.inner_text()
                    filter_value = filter_value.strip()
                    
                    if filter_value:  
                        print(f"   📌 Filtro encontrado: {filter_value}")
                        filters.append(filter_value)
                        
                except Exception as e:
                    print(f"⚠️ Error procesando filtro: {e}")
                    continue
                    
            print(f"✅ Se extrajeron {len(filters)} filtros válidos")
            return filters
            
        except Exception as e:
            print(f"❌ Error extrayendo filtros aplicados: {e}")
            return filters
    
    async def get_active_filters(self) -> List[str]:
        """Obtiene filtros activos de manera más robusta"""
        try:
            filters = []
            
            # Múltiples selectores para filtros aplicados
            filter_selectors = [
                'section.ui-search-applied-filters .andes-tag__label',
                '.ui-search-applied-filters .ui-search-applied-filter-name',
                '[data-testid="applied-filters"] .andes-tag__label'
            ]
            
            for selector in filter_selectors:
                elements = await self.page.query_selector_all(selector)
                if elements:
                    for element in elements:
                        try:
                            filter_text = await element.inner_text()
                            filter_text = filter_text.strip()
                            if filter_text and filter_text not in filters:
                                filters.append(filter_text)
                        except:
                            continue
                    break
            
            return filters
            
        except Exception as e:
            print(f"⚠️ Error obteniendo filtros activos: {e}")
            return []