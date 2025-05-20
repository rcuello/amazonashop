import React from "react";
import Loader from "../layout/Loader";

const Products = ({ products, col, loading }) => {
  if(loading){
    return <Loader/>;
  }

  return (
    <React.Fragment>
        {
            products 
            ? products.map((productItem) => (
                <Product key={productItem.id} product={productItem} col={col} />
            ))
            : <Loader/>
        }
    </React.Fragment>
  );
    
};

export default Products;
