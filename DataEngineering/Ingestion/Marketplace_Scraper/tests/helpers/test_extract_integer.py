from utils.helpers import extract_integer

class TestExtractInteger:
    """Pruebas para la función extract_integer"""
    
    def test_extract_integer_basic(self):
        """Prueba extracción básica de enteros"""
        assert extract_integer("123") == 123
        assert extract_integer("0") == 0
        assert extract_integer("999") == 999
    
    def test_extract_integer_with_separators(self):
        """Prueba exteros con separadores"""
        assert extract_integer("1,234") == 1234
        assert extract_integer("1.234.567") == 1234567
        assert extract_integer("999,999") == 999999
    
    def test_extract_integer_from_text(self):
        """Prueba extracción desde texto"""
        assert extract_integer("Total: 1,234 reviews") == 1234
        #assert extract_integer("Página 5 de 100") == 5
        assert extract_integer("Stock: 999 unidades") == 999
    
    def test_extract_integer_mixed_content(self):
        """Prueba contenido mixto"""
        assert extract_integer("abc123def456") == 123456
        assert extract_integer("Tel: +57 1 234-5678") == 5712345678
        assert extract_integer("ID: A1B2C3") == 123
    
    def test_extract_integer_invalid_inputs(self):
        """Prueba entradas inválidas"""
        assert extract_integer("") is None
        assert extract_integer("abc") is None
        assert extract_integer("$$$") is None
        assert extract_integer(None) is None
    
    def test_extract_integer_decimal_numbers(self):
        """Prueba que ignora decimales y toma solo dígitos"""
        assert extract_integer("4.5") == 45
        assert extract_integer("3,14") == 314
        assert extract_integer("1.234,56") == 123456