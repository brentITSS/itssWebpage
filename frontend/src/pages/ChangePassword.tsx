import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/authService';
import { getPostLoginPath } from '../utils/access';

const ChangePassword: React.FC = () => {
  const navigate = useNavigate();
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (newPassword.length < 8) {
      setError('Password must be at least 8 characters.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    setLoading(true);
    try {
      await authService.changePassword({ newPassword });
      const user = await authService.getCurrentUser();
      navigate(getPostLoginPath(user));
    } catch (err: any) {
      setError(err.message || 'Unable to change password.');
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
            Set a new password
          </h2>
          <p className="mt-2 text-center text-sm text-slate-500">
            For security, you must change your temporary password before continuing.
          </p>
        </div>
        <form className="app-card mt-8 space-y-6 p-6 sm:p-7" onSubmit={handleSubmit}>
          {error && (
            <div className="rounded-xl border border-red-200 bg-red-50 p-4">
              <div className="text-sm font-medium text-red-700">{error}</div>
            </div>
          )}
          <div className="space-y-4">
            <input
              type="password"
              required
              className="field"
              placeholder="New password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
            <input
              type="password"
              required
              className="field"
              placeholder="Confirm new password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
            />
          </div>
          <button type="submit" disabled={loading} className="btn-primary w-full">
            {loading ? 'Saving...' : 'Update password'}
          </button>
        </form>
      </div>
    </div>
  );
};

export default ChangePassword;
