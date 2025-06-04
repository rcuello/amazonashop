import os
from typing import Dict,Any

class Settings:
    """Configuraciones del sistema"""
    
    BROWSER_CONFIG = {
        "headless": True,
        "timeout": 30000,
        "user_agent":'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
        "viewport": {"width": 1920, "height": 1080}
    }
    
    USER_AGENTS = [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.6422.112 Safari/537.36 OPR/109.0.5097.38",
        "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:126.0) Gecko/20100101 Firefox/126.0"
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
    
    @classmethod
    def get_output_dir(cls) -> str:
        """Crea y retorna el directorio de salida"""
        output_dir = cls.EXPORT_CONFIG["output_dir"]
        os.makedirs(output_dir, exist_ok=True)
        return output_dir
    
    
        return cls.EXPORT_CONFIG["output_dir"]