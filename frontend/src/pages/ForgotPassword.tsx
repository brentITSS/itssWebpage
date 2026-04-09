import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { authService } from '../services/authService';

const ForgotPassword: React.FC = () => {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState<string | null>(null);
  const [resetPath, setResetPath] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setMessage(null);
    setResetPath(null);
    setLoading(true);

    try {
      const res = await authService.forgotPassword(email.trim());
      setMessage(res.message);
      if (res.resetPath) {
        setResetPath(res.resetPath);
      }
    } catch (err: any) {
      setError(err.message || 'Something went wrong.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100 px-4 py-12 sm:px-6 lg:px-8">
      <div className="w-full max-w-md space-y-8">
        <div>
          <p className="text-center text-sm font-medium uppercase tracking-[0.16em] text-indigo-600">ITSS Platform</p>
          <h2 className="mt-3 text-center text-3xl font-semibold tracking-tight text-slate-900">
            Reset your password
          </h2>
          <p className="mt-2 text-center text-sm text-slate-500">
            Enter your email address. If an account exists, you can continue from the reset link.
          </p>
        </div>
        <form className="app-card mt-8 space-y-6 p-6 sm:p-7" onSubmit={handleSubmit}>
          {error && (
            <div className="rounded-xl border border-red-200 bg-red-50 p-4">
              <div className="text-sm font-medium text-red-700">{error}</div>
            </div>
          )}
          {message && (
            <div className="space-y-2 rounded-xl border border-green-200 bg-green-50 p-4">
              <div className="text-sm font-medium text-green-800">{message}</div>
              {resetPath && (
                <div className="text-sm">
                  <Link
                    to={resetPath}
                    className="font-medium text-indigo-600 hover:text-indigo-500"
                  >
                    Open password reset page
                  </Link>
                </div>
              )}
            </div>
          )}
          <div>
            <label htmlFor="email" className="sr-only">
              Email address
            </label>
            <input
              id="email"
              name="email"
              type="email"
              autoComplete="email"
              required
              className="field"
              placeholder="Email address"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-3">
            <button
              type="submit"
              disabled={loading}
              className="btn-primary w-full"
            >
              {loading ? 'Sending…' : 'Send reset link'}
            </button>
            <Link
              to="/Login"
              className="text-center text-sm font-medium text-indigo-600 hover:text-indigo-500"
            >
              Back to sign in
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ForgotPassword;
