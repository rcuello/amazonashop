import { createAsyncThunk } from "@reduxjs/toolkit";
import axios from "../utilities/axios";
import { delayedTimeout } from "../utilities/delayedTimeout";

export const getProducts = createAsyncThunk(
  "products/getProducts",
  async (thunkAPI, { rejectWithValue }) => {
    try {
      // Simulate a delay
      await delayedTimeout(1000);
      
      return await axios.get("/api/v1/Product/list");
    } catch (error) {
      return rejectWithValue(`Errores: ${error.message}`);
    }
  }
);
