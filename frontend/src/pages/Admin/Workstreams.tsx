import React, { useState, useEffect } from 'react';
import { adminService, WorkstreamResponseDto, CreateWorkstreamRequest, UpdateWorkstreamRequest } from '../../services/adminService';

const Workstreams: React.FC = () => {
  const [workstreams, setWorkstreams] = useState<WorkstreamResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [editingWorkstream, setEditingWorkstream] = useState<WorkstreamResponseDto | null>(null);

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
      description: formData.get('description') as string || undefined,
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
      workstreamName: formData.get('workstreamName') as string || undefined,
      description: formData.get('description') as string || undefined,
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

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Workstreams</h2>
        <button
          onClick={() => setShowModal(true)}
          className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700"
        >
          Create Workstream
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Name
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Description
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Created Date
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {workstreams.map((workstream) => (
              <tr key={workstream.workstreamId}>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                  {workstream.workstreamName}
                </td>
                <td className="px-6 py-4 text-sm text-gray-500">
                  {workstream.description || '-'}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span
                    className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      workstream.isActive
                        ? 'bg-green-100 text-green-800'
                        : 'bg-red-100 text-red-800'
                    }`}
                  >
                    {workstream.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {new Date(workstream.createdDate).toLocaleDateString()}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                  <button
                    onClick={() => setEditingWorkstream(workstream)}
                    className="text-blue-600 hover:text-blue-900"
                  >
                    Edit
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Create Workstream Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <h3 className="text-lg font-bold mb-4">Create Workstream</h3>
            <form onSubmit={handleCreateWorkstream}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Workstream Name *
                </label>
                <input
                  type="text"
                  name="workstreamName"
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-md"
                />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Description
                </label>
                <textarea
                  name="description"
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md"
                />
              </div>
              <div className="flex justify-end space-x-2">
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="px-4 py-2 border border-gray-300 rounded-md"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
                >
                  Create
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Workstream Modal */}
      {editingWorkstream && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <h3 className="text-lg font-bold mb-4">Edit Workstream</h3>
            <form onSubmit={(e) => handleUpdateWorkstream(editingWorkstream.workstreamId, e)}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Workstream Name
                </label>
                <input
                  type="text"
                  name="workstreamName"
                  defaultValue={editingWorkstream.workstreamName}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md"
                />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Description
                </label>
                <textarea
                  name="description"
                  rows={3}
                  defaultValue={editingWorkstream.description || ''}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md"
                />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Status
                </label>
                <select
                  name="isActive"
                  defaultValue={editingWorkstream.isActive ? 'true' : 'false'}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md"
                >
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
                </select>
              </div>
              <div className="flex justify-end space-x-2">
                <button
                  type="button"
                  onClick={() => setEditingWorkstream(null)}
                  className="px-4 py-2 border border-gray-300 rounded-md"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
                >
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
