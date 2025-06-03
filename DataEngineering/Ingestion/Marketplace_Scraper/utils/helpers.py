import re
import time
import random
from typing import Optional, Union
from urllib.parse import urljoin, urlparse

def clean_price(price_text: str) -> Optional[float]:
    """Extrae el precio numérico de un texto"""
    if not price_text:
        return None
    
    # Remover caracteres no numéricos excepto punto y coma
    price_clean = re.sub(r'[^\d.,]', '', price_text.replace(',', '.'))
    
    if not price_clean:
        return None
    
    try:
        # Manejar formatos como 1.234,56 (europeo) vs 1,234.56 (americano)
        if '.' in price_clean and ',' in price_clean:
            if price_clean.rindex('.') > price_clean.rindex(','):
                # Formato americano: 1,234.56
                price_clean = price_clean.replace(',', '')
            else:
                # Formato europeo: 1.234,56
                price_clean = price_clean.replace('.', '').replace(',', '.')
        elif ',' in price_clean:
            # Solo comas - podría ser decimal europeo
            if len(price_clean.split(',')[-1]) <= 2:
                price_clean = price_clean.replace(',', '.')
            else:
                price_clean = price_clean.replace(',', '')
        
        return float(price_clean)
    except ValueError:
        return None

def clean_text(text: str) -> str:
    """Limpia y normaliza texto"""
    if not text:
        return ""
    
    # Remover espacios extra y caracteres especiales
    text = re.sub(r'\s+', ' ', text.strip())
    # Remover caracteres de control
    text = re.sub(r'[\r\n\t]', ' ', text)
    return text.strip()

def extract_rating(rating_text: str) -> Optional[float]:
    """Extrae rating numérico del texto"""
    if not rating_text:
        return None
    
    # Buscar números como 4.5, 4,5, etc.
    match = re.search(r'(\d+[.,]\d+|\d+)', rating_text)
    if match:
        try:
            return float(match.group(1).replace(',', '.'))
        except ValueError:
            pass
    return None

def extract_number(text: str) -> Optional[int]:
    """Extrae número entero del texto"""
    if not text:
        return None
    
    # Buscar números, removiendo separadores de miles
    numbers = re.findall(r'\d+', text.replace(',', '').replace('.', ''))
    if numbers:
        try:
            return int(''.join(numbers))
        except ValueError:
            pass
    return None

def random_delay(min_delay: float = 1, max_delay: float = 3):
    """Espera un tiempo aleatorio entre min y max segundos"""
    delay = random.uniform(min_delay, max_delay)
    time.sleep(delay)

def is_valid_url(url: str) -> bool:
    """Valida si una URL es válida"""
    try:
        result = urlparse(url)
        return all([result.scheme, result.netloc])
    except Exception:
        return False

def make_absolute_url(base_url: str, relative_url: str) -> str:
    """Convierte URL relativa a absoluta"""
    if is_valid_url(relative_url):
        return relative_url
    return urljoin(base_url, relative_url)