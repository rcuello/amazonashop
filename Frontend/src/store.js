import { configureStore } from "@reduxjs/toolkit";
import { productReducer } from "./slices/productsSlice";
import { productByIdReducer } from "./slices/productByIdSlice";
import { productPaginationReducer } from "./slices/productPaginationSlice";

export default configureStore({
  reducer: {
    products: productReducer,
    product: productByIdReducer,
    productPagination: productPaginationReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false,
    }),
});
