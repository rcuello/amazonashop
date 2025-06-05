from utils.helpers import clean_price

class TestCleanPrice:
    """Pruebas para la función clean_price"""
    
    def test_clean_price_colombian_format(self):
        """Prueba formato colombiano típico con puntos como separadores de miles"""
        assert clean_price("2.299.900") == 2299900.0
        assert clean_price("1.234.567") == 1234567.0
        assert clean_price("999.999") == 999999.0
        assert clean_price("123") == 123.0
        

    def test_clean_price_with_currency_symbols(self):
        """Prueba precios con símbolos de moneda"""
        assert clean_price("$2.299.900") == 2299900.0
        assert clean_price("COP 1.234.567") == 1234567.0
        assert clean_price("$$ 999.999") == 999999.0
        assert clean_price("€1.234") == 1234.0
        
    def test_clean_price_with_decimal_comma(self):
        """Prueba formato europeo con coma decimal"""
        assert clean_price("1.234,56") == 1234.56
        assert clean_price("999,99") == 999.99
        assert clean_price("12.345,67") == 12345.67
        assert clean_price("$1.234,50") == 1234.50
    
    def test_clean_price_edge_cases(self):
        """Prueba casos límite"""
        assert clean_price("0") == 0.0
        assert clean_price("1") == 1.0
        assert clean_price("10") == 10.0
        assert clean_price("100") == 100.0
    
    def test_clean_price_invalid_inputs(self):
        """Prueba entradas inválidas"""
        assert clean_price("") is None
        assert clean_price("   ") is None
        assert clean_price("abc") is None
        assert clean_price("$$$") is None
        assert clean_price("COP") is None
        assert clean_price(",,,,") is None
        assert clean_price("....") is None
        
    def test_clean_price_with_spaces(self):
        """Prueba precios con espacios"""
        assert clean_price(" 2.299.900 ") == 2299900.0
        assert clean_price("$ 1.234,56 ") == 1234.56
        assert clean_price("  999.999  ") == 999999.0
        
    def test_clean_price_complex_formats(self):
        """Prueba formatos complejos"""
        assert clean_price("COP $2.299.900 pesos") == 2299900.0
        assert clean_price("Precio: $1.234,56") == 1234.56
        assert clean_price("Total: 999.999 COP") == 999999.0