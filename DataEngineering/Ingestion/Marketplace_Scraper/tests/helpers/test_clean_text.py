from utils.helpers import clean_text

class TestCleanText:
    """Pruebas para la función clean_text"""
    
    def test_clean_text_basic(self):
        """Prueba limpieza básica de texto"""
        assert clean_text("Texto normal") == "Texto normal"
        assert clean_text("Texto  con   espacios") == "Texto con espacios"
        assert clean_text("   Texto con espacios   ") == "Texto con espacios"
        
    def test_clean_text_control_characters(self):
        """Prueba remoción de caracteres de control"""
        assert clean_text("Texto\ncon\rsaltos\tde\nlínea") == "Texto con saltos de línea"
        assert clean_text("Texto\r\ncon\t\tespacios") == "Texto con espacios"
        assert clean_text("Múltiples\n\n\nsaltos") == "Múltiples saltos"    
        
        
    def test_clean_text_empty_and_none(self):
        """Prueba valores vacíos y None"""
        assert clean_text("") == ""
        assert clean_text("   ") == ""
        assert clean_text(None) == ""
    
    def test_clean_text_only_whitespace(self):
        """Prueba texto con solo espacios en blanco"""
        assert clean_text("   \n\t\r   ") == ""
        assert clean_text("\n\n\n") == ""
        assert clean_text("\t\t\t") == ""