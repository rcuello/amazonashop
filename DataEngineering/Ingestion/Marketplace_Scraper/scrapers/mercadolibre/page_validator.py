import asyncio

class PageValidator:
    """Maneja validaciones de página específicas de MercadoLibre"""
    
    def __init__(self, page):
        self.page = page
    
    async def validate_page_after_navigation(self) -> bool:
        """Ejecuta todas las validaciones post-navegación"""
        try:
            print("🔍 Ejecutando validaciones post-navegación...")
            
            await self._handle_location_popup()
            await self._apply_shipping_filter()
            
            if not await self._verify_page_loaded():
                return False
            
            print("✅ Validaciones post-navegación completadas")
            return True
            
        except Exception as e:
            print(f"❌ Error en validaciones post-navegación: {e}")
            return False
    
    async def _handle_location_popup(self):
        """Maneja el popup de ubicación"""
        try:
            print("🔍 Verificando popup de ubicación...")
            
            await self.page.wait_for_selector('text="Agregar ubicación"', timeout=5000)
            print("📍 Popup de ubicación detectado, haciendo clic en 'Más tarde'...")
            await self.page.click('text="Más tarde"')
            await asyncio.sleep(1)
                
        except Exception:
            print("📍 No se detectó popup de ubicación")
            pass
    
    async def _apply_shipping_filter(self):
        """Aplica filtro de envío destacado"""
        try:
            print("🚛 Aplicando filtro de envío destacado...")
            
            await self.page.wait_for_selector('#shipping_highlighted_fulfillment', timeout=10000)
            await self.page.click('#shipping_highlighted_fulfillment')
            print("✅ Filtro de envío aplicado")
            await asyncio.sleep(2)
            
        except Exception as e:
            print(f"⚠️ No se pudo aplicar el filtro de envío: {e}")
    
    async def _verify_page_loaded(self) -> bool:
        """Verifica que la página de resultados haya cargado"""
        try:
            print("⏳ Verificando que la página haya cargado...")
            
            await self.page.wait_for_selector('li.ui-search-layout__item', timeout=15000)
            print("✅ Elementos de productos detectados")
            return True
            
        except Exception as e:
            print(f"❌ Error verificando carga de página: {e}")
            return False
