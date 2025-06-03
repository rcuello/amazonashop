from dataclasses import dataclass
from typing import List , Optional
from datetime import datetime

@dataclass
class Product:
    """Clase simple para representar un producto"""
    title: str
    price: Optional[float] = None
    original_price: Optional[float] = None
    currency: str = "USD"
    url: str = ""
    image_url: str = ""
    rating: Optional[float] = None
    reviews_count: Optional[int] = None
    seller: str = ""
    availability: str = ""
    description: str = ""
    marketplace: str = ""
    scraped_at: datetime = None
    brand:str=""
    
    def __post_init__(self):
        self.scraped_at = datetime.now()
        
    def to_dict(self):
        """Convierte el producto a diccionario para exportar""" 
        return {
            'title': self.title,
            'price': self.price,
            'brand':self.brand,
            'original_price': self.original_price,
            'currency': self.currency,
            'url': self.url,
            'image_url': self.image_url,
            'rating': self.rating,
            'reviews_count': self.reviews_count,
            'seller': self.seller,
            'availability': self.availability,
            'description': self.description[:200] + '...' if len(self.description) > 200 else self.description,
            'marketplace': self.marketplace,
            'scraped_at': self.scraped_at.isoformat()
        }