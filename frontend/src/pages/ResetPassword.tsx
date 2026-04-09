import React, { useState, useEffect } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { authService } from '../services/authService';

const ResetPassword: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [token, setToken] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const t = searchParams.get('token');
    if (t) {
      setToken(t);
    }
  }, [searchParams]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!token.trim()) {
      setError('Missing reset token. Open the link from your email or use Forgot password again.');
      return;
    }

    if (newPassword.length < 8) {
      setError('Password must be at least 8 characters.');
      return;
    }

    if (newPassword !== confirm) {
      setError('Passwords do not match.');
      return;
    }

    setLoading(true);
    try {
      await authService.completePasswordReset(token.trim(), newPassword);
      setSuccess(true);
      setTimeout(() => navigate('/Login'), 2000);
    } catch (err: any) {
      setError(err.message || 'Could not reset password.');
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
            Choose a new password
          </h2>
        </div>
        {success ? (
          <div className="rounded-xl border border-green-200 bg-green-50 p-4 text-center text-sm font-medium text-green-800">
            Password updated. Redirecting to sign in…
          </div>
        ) : (
          <form className="app-card mt-8 space-y-6 p-6 sm:p-7" onSubmit={handleSubmit}>
            {error && (
              <div className="rounded-xl border border-red-200 bg-red-50 p-4">
                <div className="text-sm font-medium text-red-700">{error}</div>
              </div>
            )}
            <div className="space-y-4">
              <div>
                <label htmlFor="token" className="mb-1 block text-sm font-medium text-slate-700">
                  Reset token
                </label>
                <input
                  id="token"
                  name="token"
                  type="text"
                  autoComplete="off"
                  className="field"
                  placeholder="Paste token if the link did not fill it"
                  value={token}
                  onChange={(e) => setToken(e.target.value)}
                />
              </div>
              <div>
                <label htmlFor="newPassword" className="sr-only">
                  New password
                </label>
                <input
                  id="newPassword"
                  name="newPassword"
                  type="password"
                  autoComplete="new-password"
                  required
                  className="field"
                  placeholder="New password (min 8 characters)"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                />
              </div>
              <div>
                <label htmlFor="confirm" className="sr-only">
                  Confirm password
                </label>
                <input
                  id="confirm"
                  name="confirm"
                  type="password"
                  autoComplete="new-password"
                  required
                  className="field"
                  placeholder="Confirm new password"
                  value={confirm}
                  onChange={(e) => setConfirm(e.target.value)}
                />
              </div>
            </div>
            <div className="flex flex-col gap-3">
              <button
                type="submit"
                disabled={loading}
                className="btn-primary w-full"
              >
                {loading ? 'Saving…' : 'Update password'}
              </button>
              <Link
                to="/Login"
                className="text-center text-sm font-medium text-indigo-600 hover:text-indigo-500"
              >
                Back to sign in
              </Link>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};

export default ResetPassword;
