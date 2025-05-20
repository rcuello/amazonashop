import { configureStore } from "@reduxjs/toolkit";
import { productReducer } from "./slices/productsSlice";
import { productByIdReducer } from "./slices/productByIdSlice";

export default configureStore({
  reducer: {
    products: productReducer,
    product: productByIdReducer
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false,
    }),
});
