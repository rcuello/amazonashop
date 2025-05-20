import { configureStore } from "@reduxjs/toolkit";
import { productReducer } from "./slices/productsSlice";

export default configureStore({
  reducer: {
    products: productReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false,
    }),
});
