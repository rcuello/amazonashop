import asyncio

class NavigationPreparator:
    """ Prepara la navegación y Maneja validaciones de página específicas de Falabella"""
    
    def __init__(self, page):
        self.page = page
    
    async def validate_page_after_navigation(self) -> bool:
        """
        Maneja validaciones específicas de Falabella después de navegar
        """
        try:                        
            # 1. Manejar posibles popups o modales
            await self._handle_popups()
            
            # 2. Esperar a que cargue el contenido dinámico
            await self._wait_for_content_load()
            
            # 3. Verificar que se cargaron productos
            #if not await self._verify_products_loaded():
            #    print("❌ No se pudieron cargar los productos")
            #    return False            
            
            print("✅ Validaciones post-navegación completadas")
            return True
            
        except Exception as e:
            print(f"❌ Error en validaciones post-navegación: {e}")
            return False
    
    async def _handle_popups(self):
        """Maneja popups típicos de Falabella"""
        try:
            page = self.page
            print("🧪 Verificando popups o elementos bloqueantes para la navegación...")
            
            # Posibles selectores de popups comunes en Falabella
            popup_selectors = [
                'button[aria-label="Cerrar"]',
                '.modal-close',
                '.popup-close',
                '[data-testid="modal-close"]',
                'button:has-text("Cerrar")',
                'button:has-text("×")'
            ]
            
            for selector in popup_selectors:
                try:
                    await page.wait_for_selector(selector, timeout=3000)
                    await page.click(selector)
                    print(f"✅ Popup cerrado con selector: {selector}")
                    await asyncio.sleep(1)
                    break
                except:
                    continue
                    
        except Exception as e:
            print(f"📝 No se detectaron popups: {e}")
            pass
                
    
    async def _wait_for_content_load(self):
        """Espera a que el contenido dinámico se cargue completamente"""
        try:
            page = self.page
            print("⏳ Esperando carga de contenido...")
            
            # Esperar por elementos típicos de Falabella
            content_selectors = [
                '[data-testid="product-item"]',
                '.product-item',
                '.grid-pod',
                '.product-card',
                '.product-list-item'
            ]
            
            for selector in content_selectors:
                try:
                    await page.wait_for_selector(selector, timeout=10000)
                    print(f"✅ Contenido cargado: {selector}")
                    await asyncio.sleep(2)  # Pausa adicional para JS dinámico
                    return
                except:
                    continue
                    
            print("⚠️ No se detectó contenido específico, continuando...")
            
        except Exception as e:
            print(f"⚠️ Error esperando contenido: {e}")
            pass
    
    async def _verify_products_loaded(self) -> bool:
        """Verifica que los productos se hayan cargado"""
        try:
            page = self.page
            print("🧪 Verificando carga de productos...")
            
            elements = await page.query_selector_all(".grid-pod")
            
            if elements and len(elements) > 0:
                print(f"✅ Productos cargados correctamente: {len(elements)} elementos")
                return True            
            
            print("❌ No se encontraron productos")
            return False
            
        except Exception as e:
            print(f"❌ Error verificando productos: {e}")
            return False
        
    