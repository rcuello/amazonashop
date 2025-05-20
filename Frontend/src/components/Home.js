import React, { Fragment, useEffect } from "react";
import MetaData from "./layout/MetaData";
import { useDispatch, useSelector } from "react-redux";
import { getProducts } from "../actions/productsAction";
import Product from "./product/Product";

const Home = () => {
  const dispatch = useDispatch();
  const { products } = useSelector((state) => state.products);

  useEffect(() => {
    dispatch(getProducts());
  }, [dispatch]);

  return (
    <Fragment>
      <MetaData titulo={"Los mejores productos online"} />
      <section id="products" className="container mt-5">
        <div className="row">
          {products ? (
            products.map((productItem) => (
              <Product product={productItem} col={4} />
            ))
          ) : (
            <p>No hay productos</p>
          )}
        </div>
      </section>
    </Fragment>
  );
};

export default Home;
