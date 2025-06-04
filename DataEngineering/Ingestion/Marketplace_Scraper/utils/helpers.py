import re
import time
import random
from typing import Optional
from urllib.parse import urljoin, urlparse


def clean_price(price_text: str) -> Optional[float]:
    """
    Extrae el precio numérico de un texto.
    
    Soporta formatos colombianos/europeos: $2.299.900, 1.234,56
    
    Args:
        price_text: Texto conteniendo el precio
        
    Returns:
        float: Precio parseado o None si no es válido
    """
    if not price_text or not isinstance(price_text, str):
        return None
    
    # Limpiar: mantener solo números, puntos y comas
    clean = re.sub(r'[^\d.,]', '', price_text.strip())
    if not clean:
        return None
    
    try:
        # Formato colombiano típico: 2.299.900 (puntos = miles) o 1.234,56 (coma = decimal)
        if ',' in clean:
            # Separador decimal: convertir 1.234,56 -> 1234.56
            parts = clean.split(',')
            integer_part = parts[0].replace('.', '')
            return float(f"{integer_part}.{parts[1]}")
        else:
            # Solo puntos = separadores de miles: 2.299.900 -> 2299900
            return float(clean.replace('.', ''))
            
    except (ValueError, IndexError):
        return None


def clean_text(text: str) -> str:
    """Normaliza texto removiendo espacios extra y caracteres de control."""
    if not text:
        return ""
    
    # Reemplazar caracteres de control y múltiples espacios
    normalized = re.sub(r'[\r\n\t\s]+', ' ', text.strip())
    return normalized


def extract_number(text: str) -> Optional[float]:
    """
    Extrae el primer número encontrado en el texto.
    
    Útil para ratings (4.5), reviews count (1,234), etc.
    """
    if not text:
        return None
    
    # Buscar patrón numérico: 4.5, 4,5, 1234, etc.
    match = re.search(r'(\d+(?:[.,]\d+)?)', text)
    if match:
        try:
            # Normalizar separador decimal
            number_str = match.group(1).replace(',', '.')
            return float(number_str)
        except ValueError:
            pass
    return None


def extract_integer(text: str) -> Optional[int]:
    """Extrae número entero del texto removiendo separadores."""
    if not text:
        return None
    
    # Encontrar todos los dígitos y unirlos
    digits = ''.join(re.findall(r'\d', text))
    if digits:
        try:
            return int(digits)
        except ValueError:
            pass
    return None


def random_delay(min_seconds: float = 1.0, max_seconds: float = 3.0) -> None:
    """Pausa la ejecución por un tiempo aleatorio."""
    delay = random.uniform(min_seconds, max_seconds)
    time.sleep(delay)


def is_valid_url(url: str) -> bool:
    """Verifica si una URL tiene formato válido."""
    try:
        parsed = urlparse(url)
        return bool(parsed.scheme and parsed.netloc)
    except Exception:
        return False


def make_absolute_url(base_url: str, relative_url: str) -> str:
    """Convierte URL relativa a absoluta si es necesario."""
    return relative_url if is_valid_url(relative_url) else urljoin(base_url, relative_url)
