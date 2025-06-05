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
    brand: str = ""
    availability: str = ""
    description: str = ""
    marketplace: str = ""
    scraped_at: datetime = None
    brand:str=""
    free_shipping:bool=False
    parent_category:str=""
    category:str=""
    category2:str=""
    
    def __post_init__(self):
        self.scraped_at = datetime.now()
        
    def to_dict(self):
        """Convierte el producto a diccionario para exportar""" 
        return {
            'title'         : self.title,
            'price'         : self.price,            
            'original_price': self.original_price,
            'brand'         : self.brand,
            'seller'        : self.seller,
            'currency'      : self.currency,
            'free_shipping' : self.free_shipping,
                        
            'parent_category':self.parent_category,
            'category'      :self.category,
            'category2'     :self.category2,
            
            'rating'        : self.rating,
            'reviews_count' : self.reviews_count,
            
            'availability': self.availability,
            'description': self.description[:200] + '...' if len(self.description) > 200 else self.description,
            'marketplace': self.marketplace,
            'scraped_at': self.scraped_at.isoformat(),
            'url': self.url,
            'image_url': self.image_url,
        }