from utils.helpers import is_valid_url

class TestIsValidUrl:
    """Pruebas para la función is_valid_url"""
    
    def test_valid_urls(self):
        """Prueba URLs válidas"""
        assert is_valid_url("https://www.google.com") is True
        assert is_valid_url("http://example.com") is True
        assert is_valid_url("https://mercadolibre.com.co") is True
        assert is_valid_url("ftp://files.example.com") is True
        assert is_valid_url("https://subdomain.example.com/path") is True
    
    def test_invalid_urls(self):
        """Prueba URLs inválidas"""
        assert is_valid_url("google.com") is False
        assert is_valid_url("www.google.com") is False
        assert is_valid_url("just-text") is False
        assert is_valid_url("") is False
        assert is_valid_url("//missing-scheme.com") is False
    
    def test_edge_cases(self):
        """Prueba casos límite"""
        assert is_valid_url(None) is False
        assert is_valid_url("http://") is False
        assert is_valid_url("https://") is False
        assert is_valid_url("not-a-url") is False
    
    def test_urls_with_params(self):
        """Prueba URLs con parámetros"""
        assert is_valid_url("https://example.com/search?q=test") is True
        assert is_valid_url("https://example.com/path#fragment") is True
        assert is_valid_url("https://example.com:8080/path") is True