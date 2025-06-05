import os
from typing import Dict,Any

class Settings:
    """Configuraciones del sistema"""
    
    # Configuraciones base del navegador
    BROWSER_CONFIG = {
        "headless": False,
        "timeout": 30000,
        "user_agent":'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
        "viewport": {
            "width": 1920, 
            "height": 1080
        }
    }
    
    # Configuraciones específicas para mobile
    MOBILE_CONFIG = {
        "headless": False,
        "timeout": 30000,
        "viewport": {
            "width": 375,
            "height": 812
        },
        "is_mobile": True,
        "has_touch": True
    }
    
    USER_AGENTS = [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.6422.112 Safari/537.36 OPR/109.0.5097.38",
        "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:126.0) Gecko/20100101 Firefox/126.0"
    ]
    
    # User agents específicos para mobile
    MOBILE_USER_AGENTS = [
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 16_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Mobile/15E148 Safari/604.1",
        "Mozilla/5.0 (Linux; Android 13; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 12; Pixel 6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 13; SM-S918B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36"
    ]
    
    REQUEST_DELAYS={
        "min_delay": 1,
        "max_delay":3,
        "page_delay":5
    }
    
    EXPORT_CONFIG={
        #"output_dir": os.path.join(os.getcwd(), "output"),
        "output_dir": "output",
        "csv_delimiter": ",",        
        "json_indent": 2,        
    }
    
    # Dispositivos móviles predefinidos
    DEVICES_NAMES = [
            'iPhone 12',
            'iPhone 12 Pro',
            'iPhone 13',
            'iPhone 13 Pro',
            'iPhone SE',
            'Pixel 5',
            'Galaxy S21',
            'Galaxy S21+',
            'iPad Pro',
            'iPad Mini'
        ]
    
    @classmethod
    def get_output_dir(cls) -> str:
        """Crea y retorna el directorio de salida"""
        output_dir = cls.EXPORT_CONFIG["output_dir"]
        os.makedirs(output_dir, exist_ok=True)
        return output_dir
    
    @classmethod
    def get_browser_config(cls, mobile: bool = False) -> Dict[str, Any]:
        """Retorna la configuración del navegador según el modo"""
        return cls.MOBILE_CONFIG if mobile else cls.BROWSER_CONFIG
    
    @classmethod
    def get_user_agents(cls, mobile: bool = False) -> list:
        """Retorna los user agents según el modo"""
        return cls.MOBILE_USER_AGENTS if mobile else cls.USER_AGENTS