import { BrowserRouter as Router, Route, Routes } from "react-router-dom";
import "./App.css";
import Home from "./components/Home";
import Footer from "./components/layout/Footer";
import Header from "./components/layout/Header";
import ProductDetail from "./components/products/ProductDetail";
import { useDispatch } from "react-redux";
import { useEffect } from "react";
import { getCategories } from "./actions/categoryAction";
import Login from "./components/security/Login";
import Register from "./components/security/Register";
import Profile from "./components/security/Profile";
import ProtectedRoute from "./components/route/ProtectedRoute";
import { loadUser } from "./actions/userAction";
import UpdateProfile from "./components/security/UpdateProfile";
import ForgotPassword from "./components/security/ForgotPassword";
import NewPassword from "./components/security/NewPassword";
import UpdatePassword from "./components/security/UpdatePassword";

function App() {
  const dispatch = useDispatch();
  const token = localStorage.getItem("token");

  useEffect(() => {
    dispatch(getCategories({}));

    if (token) {
      dispatch(loadUser({}));
    }
  }, [dispatch, token]);

  return (
    <Router>
      <div className="App">
        <Header />

        <div className="container container-fluid">
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/product/:id" element={<ProductDetail />} />
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/password/forgot" element={<ForgotPassword />} />
            <Route path="/password/reset/:token" element={<NewPassword />} />

            {/* Protected Routes */}
            <Route exact path="/me" element={<ProtectedRoute />}>
              <Route path="/me" element={<Profile />} />
            </Route>

            <Route exact path="/password/update" element={<ProtectedRoute />}>
                <Route path="/password/update" element={<UpdatePassword />} />
              </Route>

            <Route exact path="/me/update" element={<ProtectedRoute />}>
              <Route path="/me/update" element={<UpdateProfile />} />
            </Route>
          </Routes>
        </div>

        <Footer />
      </div>
    </Router>
  );
}

export default App;
