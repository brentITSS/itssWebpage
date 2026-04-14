import React from 'react';
import { Link } from 'react-router-dom';

const ForgotPassword: React.FC = () => {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100 px-4 py-12 sm:px-6 lg:px-8">
      <div className="w-full max-w-md space-y-8">
        <div>
          <p className="text-center text-sm font-medium uppercase tracking-[0.16em] text-indigo-600">ITSS Platform</p>
          <h2 className="mt-3 text-center text-3xl font-semibold tracking-tight text-slate-900">
            Reset your password
          </h2>
          <p className="mt-2 text-center text-sm text-slate-500">
            Password resets are handled by an administrator.
          </p>
        </div>
        <div className="app-card mt-8 space-y-6 p-6 sm:p-7">
          <div className="space-y-2 rounded-xl border border-green-200 bg-green-50 p-4">
            <div className="text-sm font-medium text-green-800">
              To reset your password, please contact Brent or your IT Administrator.
            </div>
            <div className="text-sm text-green-700">
              A temporary password will be provided securely, and you will be asked to set a new password after sign in.
            </div>
          </div>
          <div className="flex flex-col gap-3">
            <Link
              to="/Login"
              className="text-center text-sm font-medium text-indigo-600 hover:text-indigo-500"
            >
              Back to sign in
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ForgotPassword;
