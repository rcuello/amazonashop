from typing import Optional
from dataclasses import dataclass

@dataclass
class PriceInfo:
    """Estructura para información de precios del producto"""
    original_price: Optional[float]
    current_price: Optional[float]
    discount: str
    
    @classmethod
    def empty(cls) -> 'PriceInfo':
        """Factory method para crear PriceInfo vacío"""
        return cls()
    
    def is_on_sale(self) -> bool:
        """Indica si el producto está en oferta"""
        return (self.original_price is not None and 
                self.current_price is not None and 
                self.original_price > self.current_price)
    
    def discount_percentage(self) -> Optional[float]:
        """Calcula el porcentaje de descuento"""
        if not self.is_on_sale():
            return None
        
        return ((self.original_price - self.current_price) / self.original_price) * 100