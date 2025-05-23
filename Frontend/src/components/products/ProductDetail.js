import React, { useEffect } from "react";
import { useDispatch,useSelector } from "react-redux";
import { useParams } from "react-router-dom";
import { getProductById } from "../../actions/productsAction";
import { useAlert } from "react-alert";
import { Carousel } from "react-bootstrap";
import Loader from "../layout/Loader";

const ProductDetail = () => {
  const dispatch = useDispatch();
  const alert = useAlert();
  const { id } = useParams();
  const { product, loading, error } = useSelector((state) => state.product);

  useEffect(() => {
    dispatch(getProductById(id));

    if (error != null) {
      alert.error(error);
    }
  }, [dispatch, alert, error, id]);

  if (!product || loading) {
    return <Loader />;
  }

  return (
    <div className="row f-flex justify-content-around">
      <div className="col-12 col-lg-5 img-fluid" id="product_image">
        <Carousel pause="hover">
          {product.images.map((image) => (
            <Carousel.Item key={image.id}>
              <img src={image.url} alt={product.title} />
            </Carousel.Item>
          ))}
        </Carousel>
      </div>

      <div className="col-12 col-lg-5 mt-5">
        <h3>{product.nombre}</h3>
        <p id="product_id">Product # {product.id}</p>

        <hr />

        <div className="rating-outer">
          <div
            className="rating-inner"
            style={{ width: `${(product.rating / 5) * 100}%` }}
          ></div>
        </div>
        <span id="no_of_reviews">({product.numeroReviews} Reviews)</span>

        <hr />

        <p id="product_price">${product.precio}</p>
        <div className="stockCounter d-inline">
          <span className="btn btn-danger minus">-</span>

          <input
            type="number"
            className="form-control count d-inline"
            value="1"
            readOnly
          />

          <span className="btn btn-primary plus">+</span>
        </div>
        <button
          type="button"
          id="cart_btn"
          className="btn btn-primary d-inline ml-4"
        >
          Add to Cart
        </button>

        <hr />

        <p>
          Status:
          <span
            id="stock_status"
            className={product.stock > 0 ? "greenColor" : "redColor"}
          >
            {product.stock > 0 ? "En Stock" : "Fuera de Stock"}
          </span>
        </p>

        <hr />

        <h4 className="mt-2">Description:</h4>
        <p>{product.descripcion}</p>
        <hr />
        <p id="product_seller mb-3">
          Vendedor: <strong>{product.vendedor}</strong>
        </p>

        <button
          id="review_btn"
          type="button"
          className="btn btn-primary mt-4"
          data-toggle="modal"
          data-target="#ratingModal"
        >
          Submit Your Review
        </button>

        <div className="row mt-2 mb-5">
          <div className="rating w-50">
            <div
              className="modal fade"
              id="ratingModal"
              tabIndex="-1"
              role="dialog"
              aria-labelledby="ratingModalLabel"
              aria-hidden="true"
            >
              <div className="modal-dialog" role="document">
                <div className="modal-content">
                  <div className="modal-header">
                    <h5 className="modal-title" id="ratingModalLabel">
                      Submit Review
                    </h5>
                    <button
                      type="button"
                      className="close"
                      data-dismiss="modal"
                      aria-label="Close"
                    >
                      <span aria-hidden="true">&times;</span>
                    </button>
                  </div>
                  <div className="modal-body">
                    <ul className="stars">
                      <li className="star">
                        <i className="fa fa-star"></i>
                      </li>
                      <li className="star">
                        <i className="fa fa-star"></i>
                      </li>
                      <li className="star">
                        <i className="fa fa-star"></i>
                      </li>
                      <li className="star">
                        <i className="fa fa-star"></i>
                      </li>
                      <li className="star">
                        <i className="fa fa-star"></i>
                      </li>
                    </ul>

                    <textarea
                      name="review"
                      id="review"
                      className="form-control mt-3"
                    ></textarea>

                    <button
                      className="btn my-3 float-right review-btn px-4 text-white"
                      data-dismiss="modal"
                      aria-label="Close"
                    >
                      Submit
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProductDetail;
