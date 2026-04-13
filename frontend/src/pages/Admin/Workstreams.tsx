import React, { useState, useEffect, useMemo } from 'react';
import { adminService, WorkstreamResponseDto, CreateWorkstreamRequest, UpdateWorkstreamRequest } from '../../services/adminService';
import { formatDateUk } from '../../dateFormat';

const Workstreams: React.FC = () => {
  const [workstreams, setWorkstreams] = useState<WorkstreamResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [editingWorkstream, setEditingWorkstream] = useState<WorkstreamResponseDto | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');

  useEffect(() => {
    loadWorkstreams();
  }, []);

  const loadWorkstreams = async () => {
    try {
      setLoading(true);
      const data = await adminService.getWorkstreams();
      setWorkstreams(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load workstreams');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateWorkstream = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: CreateWorkstreamRequest = {
      workstreamName: formData.get('workstreamName') as string,
      description: (formData.get('description') as string) || undefined,
    };

    try {
      await adminService.createWorkstream(request);
      setShowModal(false);
      loadWorkstreams();
    } catch (err: any) {
      setError(err.message || 'Failed to create workstream');
    }
  };

  const handleUpdateWorkstream = async (id: number, e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: UpdateWorkstreamRequest = {
      workstreamName: (formData.get('workstreamName') as string) || undefined,
      description: (formData.get('description') as string) || undefined,
      isActive: formData.get('isActive') === 'true',
    };

    try {
      await adminService.updateWorkstream(id, request);
      setEditingWorkstream(null);
      loadWorkstreams();
    } catch (err: any) {
      setError(err.message || 'Failed to update workstream');
    }
  };

  const activeCount = workstreams.filter((w) => w.isActive).length;
  const inactiveCount = workstreams.length - activeCount;

  const filteredWorkstreams = useMemo(() => {
    return workstreams.filter((ws) => {
      if (statusFilter === 'active' && !ws.isActive) return false;
      if (statusFilter === 'inactive' && ws.isActive) return false;
      const q = search.trim().toLowerCase();
      if (!q) return true;
      const desc = (ws.description || '').toLowerCase();
      return ws.workstreamName.toLowerCase().includes(q) || desc.includes(q);
    });
  }, [workstreams, search, statusFilter]);

  if (loading) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm font-medium text-slate-600">
        Loading workstreams...
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <section className="rounded-xl border border-slate-200 bg-white p-5">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-900">Workstream Directory</h2>
            <p className="mt-1 text-sm text-slate-500">
              Organise workstreams in cards with search and status filters.
            </p>
          </div>
          <button
            type="button"
            onClick={() => setShowModal(true)}
            className="inline-flex items-center justify-center rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-black"
          >
            Create Workstream
          </button>
        </div>
      </section>

      <div className="grid gap-6 xl:grid-cols-[280px_minmax(0,1fr)]">
        <aside className="space-y-4">
          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Search</p>
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Name or description"
              className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm text-slate-900 placeholder:text-slate-400 focus:border-slate-500 focus:outline-none"
            />
          </div>

          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Status</p>
            <div className="mt-2 flex flex-wrap gap-2">
              {[
                { id: 'all' as const, label: 'All' },
                { id: 'active' as const, label: 'Active' },
                { id: 'inactive' as const, label: 'Inactive' },
              ].map((filter) => (
                <button
                  key={filter.id}
                  type="button"
                  onClick={() => setStatusFilter(filter.id)}
                  className={`rounded-md px-3 py-2 text-xs font-semibold uppercase tracking-wide transition ${
                    statusFilter === filter.id ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {filter.label}
                </button>
              ))}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="rounded-xl border border-slate-200 bg-white p-3">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Total</p>
              <p className="mt-1 text-xl font-semibold text-slate-900">{workstreams.length}</p>
            </div>
            <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-3">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-emerald-700">Active</p>
              <p className="mt-1 text-xl font-semibold text-emerald-800">{activeCount}</p>
            </div>
            <div className="rounded-xl border border-rose-200 bg-rose-50 p-3">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-rose-700">Inactive</p>
              <p className="mt-1 text-xl font-semibold text-rose-800">{inactiveCount}</p>
            </div>
            <div className="rounded-xl border border-slate-200 bg-white p-3">
              <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Shown</p>
              <p className="mt-1 text-xl font-semibold text-slate-900">{filteredWorkstreams.length}</p>
            </div>
          </div>
        </aside>

        <section className="space-y-3">
          {error && (
            <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700">
              {error}
            </div>
          )}

          {filteredWorkstreams.length === 0 ? (
            <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">
              No workstreams match this filter.
            </div>
          ) : (
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              {filteredWorkstreams.map((workstream) => (
                <article
                  key={workstream.workstreamId}
                  className="rounded-xl border border-slate-200 bg-white p-4 transition hover:border-slate-300 hover:shadow-sm"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <h3 className="text-base font-semibold text-slate-900">{workstream.workstreamName}</h3>
                      <p className="mt-1 text-sm text-slate-500 line-clamp-2">
                        {workstream.description || 'No description'}
                      </p>
                    </div>
                    <span
                      className={`inline-flex shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${
                        workstream.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                      }`}
                    >
                      {workstream.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </div>
                  <div className="mt-3 text-sm text-slate-600">
                    <span className="mr-2 text-xs font-semibold uppercase tracking-wide text-slate-500">Created</span>
                    {formatDateUk(workstream.createdDate)}
                  </div>
                  <div className="mt-4 flex flex-wrap gap-2">
                    <button
                      type="button"
                      onClick={() => setEditingWorkstream(workstream)}
                      className="rounded-md border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-700 transition hover:border-slate-300"
                    >
                      Edit
                    </button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </div>

      {showModal && (
        <div className="fixed inset-0 z-50 h-full w-full overflow-y-auto bg-slate-900/45 backdrop-blur-sm">
          <div className="relative top-16 mx-auto w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
            <h3 className="mb-4 text-lg font-semibold text-slate-900">Create Workstream</h3>
            <form onSubmit={handleCreateWorkstream}>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Workstream Name *</label>
                <input type="text" name="workstreamName" required className="field" />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Description</label>
                <textarea name="description" rows={3} className="field" />
              </div>
              <div className="flex justify-end gap-2">
                <button type="button" onClick={() => setShowModal(false)} className="btn-secondary">
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
                  Create
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {editingWorkstream && (
        <div className="fixed inset-0 z-50 h-full w-full overflow-y-auto bg-slate-900/45 backdrop-blur-sm">
          <div className="relative top-16 mx-auto w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
            <h3 className="mb-4 text-lg font-semibold text-slate-900">Edit Workstream</h3>
            <form onSubmit={(e) => handleUpdateWorkstream(editingWorkstream.workstreamId, e)}>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Workstream Name</label>
                <input type="text" name="workstreamName" defaultValue={editingWorkstream.workstreamName} className="field" />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Description</label>
                <textarea
                  name="description"
                  rows={3}
                  defaultValue={editingWorkstream.description || ''}
                  className="field"
                />
              </div>
              <div className="mb-4">
                <label className="mb-1 block text-sm font-medium text-slate-700">Status</label>
                <select name="isActive" defaultValue={editingWorkstream.isActive ? 'true' : 'false'} className="field">
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
                </select>
              </div>
              <div className="flex justify-end gap-2">
                <button type="button" onClick={() => setEditingWorkstream(null)} className="btn-secondary">
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
                  Update
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Workstreams;
