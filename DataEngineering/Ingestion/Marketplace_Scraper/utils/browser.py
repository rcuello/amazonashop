from playwright.async_api import async_playwright, Browser, Page, BrowserContext
from typing import Optional, Dict, Any
import asyncio
import random
from config.settings import Settings

class BrowserManager:
    """Wrapper para manejar Playwright de forma sencilla"""
    
    def __init__(self, mobile: bool = False):
        self.playwright = None
        self.browser: Optional[Browser] = None
        self.context: Optional[BrowserContext] = None
        self.page: Optional[Page] = None
        self.mobile = mobile
    
    async def start(self, **kwargs):
        """Inicia el navegador"""
        self.playwright = await async_playwright().start()
        
        # Obtener configuración según el modo (desktop o mobile)
        base_config = Settings.get_browser_config(self.mobile)
        browser_config = {**base_config, **kwargs}
        
        self.browser = await self.playwright.chromium.launch(
            headless=browser_config['headless']
        )
        
        # Seleccionar user agent según el modo
        user_agents = Settings.get_user_agents(self.mobile)
        user_agent = random.choice(user_agents)
        
        # Configurar contexto con parámetros móviles si es necesario
        context_config = {
            "user_agent": user_agent,
            "viewport": browser_config['viewport']
        }
        
        # Añadir configuraciones específicas para mobile
        if self.mobile:
            context_config["is_mobile"] = browser_config.get('is_mobile', True)
            context_config["has_touch"] = browser_config.get('has_touch', True)
        
        self.context = await self.browser.new_context(**context_config)
        
        # Crear página
        self.page = await self.context.new_page()
        
        # Configurar timeouts
        self.page.set_default_timeout(browser_config['timeout'])
        
        return self.page
    
    async def goto(self, url: str, **kwargs) -> bool:
        """Navega a una URL"""
        try:
            response = await self.page.goto(url, **kwargs)
            return response.status < 400
        except Exception as e:
            print(f"Error navegando a {url}: {e}")
            return False
    
    async def wait_for_selector(self, selector: str, timeout: int = 10000):
        """Espera por un selector"""
        try:
            return await self.page.wait_for_selector(selector, timeout=timeout)
        except Exception:
            return None
    
    async def get_text(self, selector: str) -> str:
        """Obtiene texto de un elemento"""
        try:
            element = await self.page.query_selector(selector)
            if element:
                return await element.inner_text()
        except Exception:
            pass
        return ""
    
    def is_mobile(self) -> bool:
        """Retorna si está en modo mobile"""
        return self.mobile
    
    async def get_attribute(self, selector: str, attribute: str) -> str:
        """Obtiene atributo de un elemento"""
        try:
            element = await self.page.query_selector(selector)
            if element:
                return await element.get_attribute(attribute) or ""
        except Exception:
            pass
        return ""
    
    async def get_elements(self, selector: str):
        """Obtiene múltiples elementos"""
        try:
            return await self.page.query_selector_all(selector)
        except Exception:
            return []
    
    async def scroll_to_bottom(self, delay: float = 1):
        """Hace scroll hasta abajo de la página"""
        try:
            await self.page.evaluate("""
                () => {
                    return new Promise((resolve) => {
                        let totalHeight = 0;
                        let distance = 100;
                        let timer = setInterval(() => {
                            let scrollHeight = document.body.scrollHeight;
                            window.scrollBy(0, distance);
                            totalHeight += distance;
                            
                            if(totalHeight >= scrollHeight){
                                clearInterval(timer);
                                resolve();
                            }
                        }, 100);
                    });
                }
            """)
            await asyncio.sleep(delay)
        except Exception as e:
            print(f"Error en scroll: {e}")
    
    async def close(self):
        """Cierra el navegador"""
        try:
            if self.page:
                await self.page.close()
            if self.context:
                await self.context.close()
            if self.browser:
                await self.browser.close()
            if self.playwright:
                await self.playwright.stop()
        except Exception as e:
            print(f"Error cerrando navegador: {e}")