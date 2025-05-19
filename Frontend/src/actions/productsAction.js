import { createAsyncThunk } from "@reduxjs/toolkit";
import axios from "axios";
import thunk from "redux-thunk";

export const getProducts = createAsyncThunk(
  "products/getProducts",
  async(thunkAPI,{rejectWithValue}) => {
    try{
        return await axios.get("/api/v1/products");
    }catch(error){
      return rejectWithValue(`Errores: ${error.message}`);
    }
  }
);