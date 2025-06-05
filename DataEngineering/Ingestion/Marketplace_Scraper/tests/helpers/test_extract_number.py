from utils.helpers import extract_number

class TestExtractNumber:
    """Pruebas para la función extract_number"""
    
    def test_extract_number_basic(self):
        """Prueba extracción básica de números"""
        assert extract_number("4.5") == 4.5
        assert extract_number("4,5") == 4.5
        assert extract_number("123") == 123.0
        assert extract_number("0") == 0.0
    
    def test_extract_number_from_text(self):
        """Prueba extracción de números desde texto"""
        assert extract_number("Rating: 4.5 stars") == 4.5
        assert extract_number("Precio: 1234,56 pesos") == 1234.56
        assert extract_number("Total: 999 items") == 999.0
        assert extract_number("Descuento 25,5%") == 25.5
    
    def test_extract_number_first_match(self):
        """Prueba que extrae el primer número encontrado"""
        assert extract_number("1.5 de 5.0 estrellas") == 1.5
        assert extract_number("Página 2 de 10") == 2.0
        assert extract_number("3,5 reseñas de 100") == 3.5
    
    def test_extract_number_invalid_inputs(self):
        """Prueba entradas inválidas"""
        assert extract_number("") is None
        assert extract_number("No hay números") is None
        assert extract_number("abc def") is None
        assert extract_number("$$$") is None
        assert extract_number(None) is None
    
    def test_extract_number_decimal_formats(self):
        """Prueba diferentes formatos decimales"""
        assert extract_number("3.14159") == 3.14159
        assert extract_number("2,718") == 2.718
        assert extract_number("0.5") == 0.5
        assert extract_number("0,1") == 0.1
   
        