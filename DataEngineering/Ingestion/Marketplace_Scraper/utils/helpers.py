import re
import time
import random
from typing import Optional, Union
from urllib.parse import urljoin, urlparse

def clean_price(price_text: str) -> Optional[float]:
    """
    Extrae el precio numérico de un texto manejando diferentes formatos.
    
    Formatos soportados:
    - Colombiano/Europeo: 2.299.900, 1.234,56
    - Americano: 2,299,900, 1,234.56  
    - Sin separadores: 2299900, 1234.56
    - Con símbolos: $2.299.900, COP 1.234,56
    
    Examples:
        >>> clean_price("2.299.900")
        2299900.0
        >>> clean_price("$1.234,56")
        1234.56
        >>> clean_price("2,299,900")
        2299900.0
    """
    if not price_text or not isinstance(price_text, str):
        return None
    
    # Remover espacios en blanco y caracteres especiales excepto números, puntos y comas
    price_clean = re.sub(r'[^\d.,]', '', price_text.strip())
    
    if not price_clean:
        return None
    
    try:
        return _parse_numeric_string(price_clean)
    except (ValueError, TypeError):
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

def extract_review_count(review_text: str) -> Optional[float]:
    """Extrae review count numérico del texto"""
    if not review_text:
        return None
    
    # Buscar números como 4.5, 4,5, etc.
    match = re.search(r'(\d+[.,]\d+|\d+)', review_text)
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

def _parse_numeric_string(price_clean: str) -> float:
    """
    Parsea una cadena numérica limpia determinando el formato correcto.
    
    Args:
        price_clean: Cadena que solo contiene dígitos, puntos y comas
        
    Returns:
        float: Valor numérico parseado
    """
    # Caso 1: Solo números (sin separadores)
    if '.' not in price_clean and ',' not in price_clean:
        return float(price_clean)
    
    # Caso 2: Solo puntos
    if '.' in price_clean and ',' not in price_clean:
        return _handle_dots_only(price_clean)
    
    # Caso 3: Solo comas
    if ',' in price_clean and '.' not in price_clean:
        return _handle_commas_only(price_clean)
    
    # Caso 4: Ambos puntos y comas
    if '.' in price_clean and ',' in price_clean:
        return _handle_mixed_separators(price_clean)
    
    return float(price_clean)

def _handle_dots_only(price_clean: str) -> float:
    """Maneja cadenas que solo contienen puntos como separadores."""
    dots = price_clean.count('.')
    
    if dots == 1:
        # Podría ser decimal (1234.56) o separador de miles (1234.)
        parts = price_clean.split('.')
        if len(parts[1]) <= 2:
            # Probablemente decimal
            return float(price_clean)
        else:
            # Probablemente separador de miles
            return float(price_clean.replace('.', ''))
    else:
        # Múltiples puntos = separadores de miles (2.299.900)
        return float(price_clean.replace('.', ''))

def _handle_commas_only(price_clean: str) -> float:
    """Maneja cadenas que solo contienen comas como separadores."""
    commas = price_clean.count(',')
    
    if commas == 1:
        # Podría ser decimal europeo (1234,56) o separador de miles americano (1234,)
        parts = price_clean.split(',')
        if len(parts[1]) <= 2:
            # Probablemente decimal europeo
            return float(price_clean.replace(',', '.'))
        else:
            # Probablemente separador de miles
            return float(price_clean.replace(',', ''))
    else:
        # Múltiples comas = separadores de miles americanos (2,299,900)
        return float(price_clean.replace(',', ''))

def _handle_mixed_separators(price_clean: str) -> float:
    """Maneja cadenas con puntos y comas mezclados."""
    last_dot_pos = price_clean.rfind('.')
    last_comma_pos = price_clean.rfind(',')
    
    if last_dot_pos > last_comma_pos:
        # Formato americano: 1,234.56
        # Todo antes del último punto son separadores de miles
        integer_part = price_clean[:last_dot_pos].replace(',', '').replace('.', '')
        decimal_part = price_clean[last_dot_pos + 1:]
        return float(f"{integer_part}.{decimal_part}")
    else:
        # Formato europeo: 1.234,56
        # Todo antes de la última coma son separadores de miles
        integer_part = price_clean[:last_comma_pos].replace('.', '').replace(',', '')
        decimal_part = price_clean[last_comma_pos + 1:]
        return float(f"{integer_part}.{decimal_part}")

# Función alternativa más simple y específica para MercadoLibre Colombia
def clean_price_colombia(price_text: str) -> Optional[float]:
    """
    Versión específica para precios de MercadoLibre Colombia.
    Asume formato colombiano: 2.299.900 (puntos como separadores de miles)
    
    Args:
        price_text: Texto del precio
        
    Returns:
        float: Precio parseado o None
    """
    if not price_text or not isinstance(price_text, str):
        return None
    
    # Remover todo excepto números, puntos y comas
    price_clean = re.sub(r'[^\d.,]', '', price_text.strip())
    
    if not price_clean:
        return None
    
    try:
        # Para Colombia, los puntos son separadores de miles
        # Las comas son separadores decimales (poco común en precios enteros)
        
        if ',' in price_clean:
            # Si hay coma, es separador decimal: 1.234,56
            parts = price_clean.split(',')
            integer_part = parts[0].replace('.', '')  # Remover separadores de miles
            decimal_part = parts[1]
            return float(f"{integer_part}.{decimal_part}")
        else:
            # Solo puntos = separadores de miles: 2.299.900
            return float(price_clean.replace('.', ''))
            
    except (ValueError, TypeError):
        return None