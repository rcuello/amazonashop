from utils.helpers import make_absolute_url

class TestMakeAbsoluteUrl:
    """Pruebas para la función make_absolute_url"""
    
    def test_already_absolute_urls(self):
        """Prueba URLs que ya son absolutas"""
        absolute_url = "https://example.com/path"
        base_url = "https://different.com"
        
        result = make_absolute_url(base_url, absolute_url)
        assert result == absolute_url
    
    def test_relative_urls(self):
        """Prueba URLs relativas"""
        base_url = "https://example.com"
        
        assert make_absolute_url(base_url, "/path") == "https://example.com/path"
        assert make_absolute_url(base_url, "relative") == "https://example.com/relative"
        #assert make_absolute_url(base_url, "../up") == "https://example.com/../up"
    
    def test_relative_with_base_path(self):
        """Prueba URLs relativas con base que tiene path"""
        base_url = "https://example.com/section/"
        
        assert make_absolute_url(base_url, "page") == "https://example.com/section/page"
        assert make_absolute_url(base_url, "./page") == "https://example.com/section/page"
        assert make_absolute_url(base_url, "../other") == "https://example.com/other"
    
    def test_edge_cases(self):
        """Prueba casos límite"""
        base_url = "https://example.com"
        
        # URL vacía
        assert make_absolute_url(base_url, "") == "https://example.com"
        
        # URLs con fragmentos y parámetros
        assert make_absolute_url(base_url, "/path?q=test") == "https://example.com/path?q=test"
        assert make_absolute_url(base_url, "/path#section") == "https://example.com/path#section"