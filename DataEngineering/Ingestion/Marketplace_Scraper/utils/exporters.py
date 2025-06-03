import csv
import json
import os
from typing import List, Dict, Any
from datetime import datetime
from models.product import Product
from config.settings import Settings

class DataExporter:
    """Maneja la exportación de datos a diferentes formatos"""
    
    def __init__(self):
        self.output_dir = Settings.get_output_dir()
    
    def export_to_csv(self, products: List[Product], filename: str = None) -> str:
        """Exporta productos a CSV"""
        if not filename:
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            filename = f"products_{timestamp}.csv"
        
        filepath = os.path.join(self.output_dir, filename)
        
        if not products:
            print("No hay productos para exportar")
            return filepath
        
        # Obtener todas las claves posibles
        fieldnames = list(products[0].to_dict().keys())
        
        with open(filepath, 'w', newline='', encoding='utf-8') as csvfile:
            writer = csv.DictWriter(
                csvfile, 
                fieldnames=fieldnames,
                delimiter=Settings.EXPORT_CONFIG['csv_delimiter']
            )
            
            writer.writeheader()
            for product in products:
                writer.writerow(product.to_dict())
        
        print(f"Exportado CSV: {filepath} ({len(products)} productos)")
        return filepath
    
    def export_to_json(self, products: List[Product], filename: str = None) -> str:
        """Exporta productos a JSON"""
        if not filename:
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            filename = f"products_{timestamp}.json"
        
        filepath = os.path.join(self.output_dir, filename)
        
        data = {
            'products': [product.to_dict() for product in products],
            'total_count': len(products),
            'exported_at': datetime.now().isoformat(),
            'marketplaces': list(set(p.marketplace for p in products))
        }
        
        with open(filepath, 'w', encoding='utf-8') as jsonfile:
            json.dump(
                data, 
                jsonfile, 
                indent=Settings.EXPORT_CONFIG['json_indent'],
                ensure_ascii=False
            )
        
        print(f"Exportado JSON: {filepath} ({len(products)} productos)")
        return filepath
    
    def export_by_marketplace(self, products: List[Product], format: str = 'csv'):
        """Exporta productos separados por marketplace"""
        marketplaces = {}
        
        # Agrupar por marketplace
        for product in products:
            marketplace = product.marketplace or 'unknown'
            if marketplace not in marketplaces:
                marketplaces[marketplace] = []
            marketplaces[marketplace].append(product)
        
        # Exportar cada marketplace
        exported_files = []
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        
        for marketplace, mp_products in marketplaces.items():
            filename = f"{marketplace}_{timestamp}.{format}"
            
            if format == 'csv':
                filepath = self.export_to_csv(mp_products, filename)
            elif format == 'json':
                filepath = self.export_to_json(mp_products, filename)
            else:
                continue
            
            exported_files.append(filepath)
        
        return exported_files