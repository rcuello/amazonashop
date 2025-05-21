import { configureStore } from "@reduxjs/toolkit";
import { productReducer } from "./slices/productsSlice";
import { productByIdReducer } from "./slices/productByIdSlice";
import { productPaginationReducer } from "./slices/productPaginationSlice";
import { categoryReducer } from "./slices/categorySlice";

export default configureStore({
  reducer: {
    products: productReducer,
    product: productByIdReducer,
    productPagination: productPaginationReducer,
    category: categoryReducer
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false,
    }),
});
