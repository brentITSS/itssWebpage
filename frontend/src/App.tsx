import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { AuthAccessProvider } from './context/AuthAccessContext';
import AppRoutes from './routes';
import './styles/index.css';

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <AuthAccessProvider>
        <AppRoutes />
      </AuthAccessProvider>
    </BrowserRouter>
  );
};

export default App;
