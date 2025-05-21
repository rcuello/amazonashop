import { createAsyncThunk } from "@reduxjs/toolkit";
import axios from "../utilities/axios";
import { delayedTimeout } from "../utilities/delayedTimeout";
import { httpParams } from "../utilities/httpParams";

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

export const getProductById = createAsyncThunk(
  "products/getProductById",
  async (id, { rejectWithValue }) => {
    try {
      // Simulate a delay
      await delayedTimeout(1000);
      return await axios.get(`/api/v1/Product/${id}`);
    } catch (error) {
      return rejectWithValue(`Errores: ${error.message}`);
    }
  }
);

export const getProductPagination = createAsyncThunk(
  "products/getProductPagination",
  async (params, { rejectWithValue }) => {
    try {
      // Simulate a delay
      await delayedTimeout(1000);
      params = httpParams(params);
      const paramUrl = new URLSearchParams(params).toString();

      var results = axios.get(`api/v1/product/pagination?${paramUrl}`);

      return (await results).data;
    } catch (err) {
      return rejectWithValue(`Errores: ${err.message}`);
    }
  }
);
