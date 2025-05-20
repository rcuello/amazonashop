import { createAsyncThunk } from "@reduxjs/toolkit";
import axios from "../utilities/axios";

export const getProducts = createAsyncThunk(
  "products/getProducts",
  async(thunkAPI,{rejectWithValue}) => {
    try{
        return await axios.get("/api/v1/Product/list");
    }catch(error){
      return rejectWithValue(`Errores: ${error.message}`);
    }
  }
);